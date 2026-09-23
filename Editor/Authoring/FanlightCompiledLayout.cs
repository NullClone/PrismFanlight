using PrismFanlight.Authoring;
using UnityEngine;

namespace PrismFanlight.Editor
{
    internal sealed class FanlightCompiledLayout
    {
        // Fields

        private const int ArcLengthSubdivisions = 64;
        private readonly ulong[] _geometryHashes;
        private readonly float[] _arcLengths = new float[ArcLengthSubdivisions + 1];


        // Properties

        internal FanlightLayoutAsset Source { get; }

        internal FanlightBakedSeatRecord[] Seats { get; }

        internal FanlightBakedBlockRecord[] Blocks { get; }

        internal Bounds LocalBounds { get; private set; }

        internal ulong ContentHash { get; private set; }


        // Methods

        internal FanlightCompiledLayout(FanlightLayoutAsset source)
        {
            Source = source;
            Seats = new FanlightBakedSeatRecord[source.TotalSeatCount];
            Blocks = new FanlightBakedBlockRecord[source.BlockCount];
            _geometryHashes = new ulong[source.BlockCount];

            CompileAll();
        }

        internal void CompileAll()
        {
            var start = 0;
            for (var blockIndex = 0; blockIndex < Blocks.Length; blockIndex++)
            {
                CompileBlock(blockIndex, start);
                start += Blocks[blockIndex].contiguousSeatCount;
            }

            RecalculateSummary();
        }

        internal void SetSummary(Bounds localBounds, ulong contentHash)
        {
            LocalBounds = localBounds;
            ContentHash = contentHash == 0UL ? 1UL : contentHash;
        }


        private void CompileBlock(int blockIndex, int start)
        {
            var block = Source.GetBlock(blockIndex);
            var placement = block.Placement;
            var rotation = placement.Rotation;
            var seatWrite = start;
            var hash = FanlightStableHash.Begin();
            var hasBounds = false;
            var bounds = default(Bounds);

            for (var rowIndex = 0; rowIndex < block.RowCount; rowIndex++)
            {
                var row = block.GetRow(rowIndex);

                BuildArcLengthTable(row);

                for (var seatIndex = 0; seatIndex < row.SeatCount; seatIndex++)
                {
                    var normalizedLength = row.SeatCount == 1 ? 0.5f : (float)seatIndex / (row.SeatCount - 1);
                    var t = FindCurveParameter(normalizedLength);
                    var blockLocal = EvaluateQuadratic(row, t);
                    var local = placement.position + rotation * blockLocal;
                    var stableSeatId = row.GetStableSeatId(seatIndex);

                    Seats[seatWrite] = new FanlightBakedSeatRecord
                    {
                        stableSeatId = stableSeatId,
                        localPosition = local,
                        blockIndex = blockIndex
                    };

                    if (!hasBounds)
                    {
                        bounds = new Bounds(local, Vector3.zero);
                        hasBounds = true;
                    }
                    else
                    {
                        bounds.Encapsulate(local);
                    }

                    hash = FanlightStableHash.Add(hash, stableSeatId);
                    hash = FanlightStableHash.Add(hash, local);
                    seatWrite++;
                }
            }

            bounds.Expand(new Vector3(0.02f, 8f, 0.02f));
            hash = FanlightStableHash.Add(hash, block.BlockId);
            hash = FanlightStableHash.Add(hash, bounds.center);
            hash = FanlightStableHash.Add(hash, bounds.size);

            _geometryHashes[blockIndex] = FanlightStableHash.Finish(hash);
            Blocks[blockIndex] = new FanlightBakedBlockRecord
            {
                blockId = block.BlockId,
                localBounds = bounds,
                contiguousSeatStart = start,
                contiguousSeatCount = seatWrite - start,
                contentHash = _geometryHashes[blockIndex],
                effectCoordinate = Vector2.one * 0.5f
            };
        }

        private void RecalculateSummary()
        {
            var bounds = Blocks[0].localBounds;
            for (var i = 1; i < Blocks.Length; i++)
            {
                bounds.Encapsulate(Blocks[i].localBounds.min);
                bounds.Encapsulate(Blocks[i].localBounds.max);
            }

            var size = bounds.size;
            var hash = FanlightStableHash.Begin();
            hash = FanlightStableHash.Add(hash, Source.LayoutId.Value);
            hash = FanlightStableHash.Add(hash, new Vector3(
                Source.ReferenceSeatSpacing.x,
                0f,
                Source.ReferenceSeatSpacing.y));

            for (var i = 0; i < Blocks.Length; i++)
            {
                var block = Blocks[i];
                var center = block.localBounds.center;
                block.effectCoordinate = new Vector2(
                    size.x > 0.000001f ? Mathf.Clamp01((center.x - bounds.min.x) / size.x) : 0.5f,
                    size.z > 0.000001f ? Mathf.Clamp01((center.z - bounds.min.z) / size.z) : 0.5f);

                var blockHash = FanlightStableHash.Begin();
                blockHash = FanlightStableHash.Add(blockHash, _geometryHashes[i]);
                blockHash = FanlightStableHash.Add(blockHash, new Vector3(block.effectCoordinate.x, 0f, block.effectCoordinate.y));
                block.contentHash = FanlightStableHash.Finish(blockHash);
                Blocks[i] = block;
                hash = FanlightStableHash.Add(hash, block.contentHash);
            }

            LocalBounds = bounds;
            ContentHash = FanlightStableHash.Finish(hash);
        }

        private void BuildArcLengthTable(FanlightLayoutRow row)
        {
            _arcLengths[0] = 0f;
            var previous = row.LeftPoint;

            for (var i = 1; i <= ArcLengthSubdivisions; i++)
            {
                var point = EvaluateQuadratic(row, (float)i / ArcLengthSubdivisions);
                _arcLengths[i] = _arcLengths[i - 1] + Vector3.Distance(previous, point);
                previous = point;
            }
        }

        private float FindCurveParameter(float normalizedLength)
        {
            var totalLength = _arcLengths[ArcLengthSubdivisions];
            if (totalLength <= 0.000001f) return normalizedLength;

            var target = totalLength * Mathf.Clamp01(normalizedLength);
            for (var i = 1; i <= ArcLengthSubdivisions; i++)
            {
                if (_arcLengths[i] < target) continue;

                var segmentLength = _arcLengths[i] - _arcLengths[i - 1];
                var segmentT = segmentLength > 0.000001f ? (target - _arcLengths[i - 1]) / segmentLength : 0f;
                return (i - 1 + segmentT) / ArcLengthSubdivisions;
            }

            return 1f;
        }

        private static Vector3 EvaluateQuadratic(FanlightLayoutRow row, float t)
        {
            var inverse = 1f - t;
            return inverse * inverse * row.LeftPoint
                   + 2f * inverse * t * row.ControlPoint
                   + t * t * row.RightPoint;
        }
    }
}

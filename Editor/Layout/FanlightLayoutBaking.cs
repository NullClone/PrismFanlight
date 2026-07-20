using PrismFanlight.Authoring;
using Unity.Mathematics;
using UnityEngine;

namespace PrismFanlight.Editor
{
    internal sealed class FanlightCompiledLayout
    {
        // Properties

        public FanlightLayoutAsset Source { get; }

        public FanlightBakedSeatRecord[] Seats { get; }

        public FanlightBakedBlockRecord[] Blocks { get; }

        public Bounds LocalBounds { get; private set; }

        public ulong ContentHash { get; private set; }


        // Methods

        public FanlightCompiledLayout(FanlightLayoutAsset source)
        {
            Source = source;
            Seats = new FanlightBakedSeatRecord[source.TotalSeatCount];
            Blocks = new FanlightBakedBlockRecord[source.TotalBlockCount];

            for (var i = 0; i < Blocks.Length; i++)
            {
                CompileBlock(i);
            }

            RecalculateSummary();
        }

        public void SetSummary(Bounds localBounds, ulong contentHash)
        {
            LocalBounds = localBounds;
            ContentHash = contentHash == 0UL ? 1UL : contentHash;
        }

        public void CompileBlock(int blockIndex)
        {
            var block = Source.GetBlockCoordinates(blockIndex);
            var start = blockIndex * Source.BlockSeatCount;
            var hash = FanlightStableHash.Begin();

            for (var y = 0; y < Source.SeatPerBlock.y; y++)
            {
                for (var x = 0; x < Source.SeatPerBlock.x; x++)
                {
                    var localSeat = math.int2(x, y);
                    var plane = Source.GetPositionOnPlane(block, localSeat);
                    var local = Source.TransformBlockPoint(blockIndex, new Vector3(plane.x, 0f, plane.y));
                    var seatIndex = start + y * Source.SeatPerBlock.x + x;
                    var stableSeatId = Source.GetStableSeatId(seatIndex);

                    Seats[seatIndex] = new FanlightBakedSeatRecord
                    {
                        stableSeatId = stableSeatId,
                        localPosition = local,
                        planePosition = new Vector2(plane.x, plane.y),
                        blockCoordinates = new Vector2(block.x, block.y),
                        blockIndex = blockIndex,
                        placementFlags = 1u
                    };

                    hash = FanlightStableHash.Add(hash, stableSeatId);
                    hash = FanlightStableHash.Add(hash, local);
                }
            }

            var bounds = BuildBlockBounds(Source, blockIndex);
            hash = FanlightStableHash.Add(hash, bounds.center);
            hash = FanlightStableHash.Add(hash, bounds.size);
            Blocks[blockIndex] = new FanlightBakedBlockRecord
            {
                blockId = Source.GetBlock(blockIndex).BlockId,
                localBounds = bounds,
                contiguousSeatStart = start,
                contiguousSeatCount = Source.BlockSeatCount,
                contentHash = FanlightStableHash.Finish(hash)
            };
        }

        public void RecalculateSummary()
        {
            var hasBounds = false;
            var bounds = default(Bounds);
            var hashTree = new FanlightHashTree(Blocks.Length);

            for (var i = 0; i < Blocks.Length; i++)
            {
                var block = Blocks[i];
                hashTree.Update(i, block.contentHash);
                if (!hasBounds)
                {
                    bounds = block.localBounds;
                    hasBounds = true;
                }
                else
                {
                    bounds.Encapsulate(block.localBounds.min);
                    bounds.Encapsulate(block.localBounds.max);
                }
            }

            var hash = FanlightStableHash.Begin();
            hash = FanlightStableHash.Add(hash, Source.LayoutId.Value);
            hash = FanlightStableHash.Add(hash, hashTree.Root);
            LocalBounds = hasBounds ? bounds : new Bounds(Vector3.zero, Vector3.one);
            ContentHash = FanlightStableHash.Finish(hash);
        }

        private static Bounds BuildBlockBounds(FanlightLayoutAsset layout, int blockIndex)
        {
            var block = layout.GetBlockCoordinates(blockIndex);

            var min2 = layout.GetPositionOnPlane(block, math.int2(0, 0)) - layout.SeatPitch * 0.5f;
            var max2 = layout.GetPositionOnPlane(block, layout.SeatPerBlock - math.int2(1, 1)) + layout.SeatPitch * 0.5f;

            var min = new Vector3(min2.x, -4f, min2.y);
            var max = new Vector3(max2.x, 4f, max2.y);

            var bounds = new Bounds(layout.TransformBlockPoint(blockIndex, min), Vector3.zero);
            bounds.Encapsulate(layout.TransformBlockPoint(blockIndex, new Vector3(max.x, min.y, min.z)));
            bounds.Encapsulate(layout.TransformBlockPoint(blockIndex, new Vector3(min.x, max.y, min.z)));
            bounds.Encapsulate(layout.TransformBlockPoint(blockIndex, new Vector3(max.x, max.y, min.z)));
            bounds.Encapsulate(layout.TransformBlockPoint(blockIndex, new Vector3(min.x, min.y, max.z)));
            bounds.Encapsulate(layout.TransformBlockPoint(blockIndex, new Vector3(max.x, min.y, max.z)));
            bounds.Encapsulate(layout.TransformBlockPoint(blockIndex, new Vector3(min.x, max.y, max.z)));
            bounds.Encapsulate(layout.TransformBlockPoint(blockIndex, max));
            return bounds;
        }
    }
}

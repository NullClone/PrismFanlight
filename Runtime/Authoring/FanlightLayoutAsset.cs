using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using PrismFanlight.Core;
using Unity.Mathematics;
using UnityEngine;

namespace PrismFanlight.Authoring
{
    [PreferBinarySerialization]
    public sealed class FanlightLayoutAsset : ScriptableObject
    {
        // Fields

        [SerializeField]
        private string _layoutId;

        [SerializeField]
        private ulong _contentHash;

        [SerializeField]
        private float2 _referenceSeatSpacing = new(0.4f, 0.8f);

        [SerializeField]
        private FanlightLayoutBlock[] _blocks = Array.Empty<FanlightLayoutBlock>();

        [SerializeField]
        private FanlightLayoutBakeArtifact _activeBake;


        // Properties

        internal FanlightLayoutId LayoutId => new(_layoutId);

        internal ulong ContentHash => _contentHash;

        internal float2 ReferenceSeatSpacing => _referenceSeatSpacing;

        internal int BlockCount => _blocks?.Length ?? 0;

        internal int TotalSeatCount
        {
            get
            {
                if (_blocks == null) return 0;

                long count = 0;
                for (var i = 0; i < _blocks.Length; i++)
                {
                    if (_blocks[i] != null) count += _blocks[i].SeatCount;
                    if (count > int.MaxValue) return 0;
                }

                return (int)count;
            }
        }

        internal FanlightLayoutBakeArtifact ActiveBake => _activeBake;

        internal bool IsInitialized => HasBasicAuthoringStructure();

        internal bool HasValidBake => LayoutId.IsValid && _activeBake != null && _activeBake.Matches(this);


        // Methods

        internal FanlightLayoutBlock GetBlock(int blockIndex) => _blocks[blockIndex];

        internal void ReplaceAuthoringContents(float2 referenceSeatSpacing, FanlightLayoutBlock[] blocks)
        {
            if (blocks == null || blocks.Length == 0)
            {
                throw new ArgumentException("At least one block is required.", nameof(blocks));
            }

            ValidateReferenceSeatSpacing(referenceSeatSpacing);

            var previousLayoutId = _layoutId;
            var previousContentHash = _contentHash;
            var previousReferenceSeatSpacing = _referenceSeatSpacing;
            var previousBlocks = _blocks;
            var previousActiveBake = _activeBake;

            if (!LayoutId.IsValid) _layoutId = Guid.NewGuid().ToString("N");
            _contentHash = 0UL;
            _referenceSeatSpacing = referenceSeatSpacing;
            _blocks = (FanlightLayoutBlock[])blocks.Clone();
            _activeBake = null;

            if (TryValidateAuthoring()) return;

            _layoutId = previousLayoutId;
            _contentHash = previousContentHash;
            _referenceSeatSpacing = previousReferenceSeatSpacing;
            _blocks = previousBlocks;
            _activeBake = previousActiveBake;
            throw new ArgumentException(
                "The replacement blocks must contain valid rows and unique stable IDs.",
                nameof(blocks));
        }

        internal FanlightLayoutBlock CreateBlock(
            FanlightLayoutRow[] rows,
            FanlightBlockPlacement placement)
        {
            var used = new HashSet<string>(StringComparer.Ordinal);
            for (var i = 0; i < BlockCount; i++)
            {
                var block = _blocks[i];
                if (block != null && !string.IsNullOrEmpty(block.BlockId)) used.Add(block.BlockId);
            }

            string blockId;
            do
            {
                blockId = Guid.NewGuid().ToString("N");
            } while (!used.Add(blockId));

            return new FanlightLayoutBlock(blockId, placement, rows);
        }

        internal void CollectStableSeatIds(HashSet<ulong> results)
        {
            if (results == null) throw new ArgumentNullException(nameof(results));

            for (var blockIndex = 0; blockIndex < BlockCount; blockIndex++)
            {
                var block = _blocks[blockIndex];
                if (block == null) continue;

                for (var rowIndex = 0; rowIndex < block.RowCount; rowIndex++)
                {
                    var row = block.GetRow(rowIndex);
                    if (row == null) continue;

                    for (var seatIndex = 0; seatIndex < row.SeatCount; seatIndex++)
                    {
                        results.Add(row.GetStableSeatId(seatIndex));
                    }
                }
            }
        }

        internal static ulong[] CreateStableSeatIds(int count, HashSet<ulong> reserved)
        {
            if (count < 0) throw new ArgumentOutOfRangeException(nameof(count));

            reserved ??= new HashSet<ulong>();
            var values = new ulong[count];
            var bytes = new byte[8];

            using var random = RandomNumberGenerator.Create();

            for (var i = 0; i < values.Length; i++)
            {
                ulong value;
                do
                {
                    random.GetBytes(bytes);
                    value = BitConverter.ToUInt64(bytes, 0);
                } while (value == 0UL || !reserved.Add(value));

                values[i] = value;
            }

            return values;
        }

        internal bool SetReferenceSeatSpacing(float2 referenceSeatSpacing)
        {
            ValidateReferenceSeatSpacing(referenceSeatSpacing);

            if (_referenceSeatSpacing.Equals(referenceSeatSpacing)) return false;

            _referenceSeatSpacing = referenceSeatSpacing;

            return true;
        }

        internal bool SetBlockPlacement(int blockIndex, FanlightBlockPlacement placement)
        {
            if (!IsInitialized || blockIndex < 0 || blockIndex >= BlockCount) return false;
            if (!IsFinite(placement.position) || !IsFinite(placement.eulerRotation)) return false;
            if (_blocks[blockIndex].Placement.Equals(placement)) return false;

            _blocks[blockIndex].SetPlacement(placement);

            return true;
        }

        internal bool SetRowGeometry(
            int blockIndex,
            int rowIndex,
            Vector3 leftPoint,
            Vector3 controlPoint,
            Vector3 rightPoint)
        {
            if (!IsFinite(leftPoint) || !IsFinite(controlPoint) || !IsFinite(rightPoint)) return false;
            if (blockIndex < 0 || blockIndex >= BlockCount) return false;

            var block = _blocks[blockIndex];
            if (block == null || rowIndex < 0 || rowIndex >= block.RowCount) return false;

            var row = block.GetRow(rowIndex);
            if (row.LeftPoint == leftPoint && row.ControlPoint == controlPoint && row.RightPoint == rightPoint)
            {
                return false;
            }

            row.SetGeometry(leftPoint, controlPoint, rightPoint);
            return true;
        }

        internal bool SetBlockRows(int blockIndex, FanlightLayoutRow[] rows)
        {
            if (blockIndex < 0 || blockIndex >= BlockCount || rows == null || rows.Length == 0) return false;

            var previous = _blocks[blockIndex].CopyRows();
            _blocks[blockIndex].SetRows(rows);
            if (TryValidateAuthoring()) return true;

            _blocks[blockIndex].SetRows(previous);
            return false;
        }

        internal bool AddBlock(FanlightLayoutBlock block)
        {
            if (block == null) return false;

            var next = new FanlightLayoutBlock[BlockCount + 1];
            if (BlockCount > 0) Array.Copy(_blocks, next, BlockCount);
            next[BlockCount] = block;
            var previous = _blocks;
            _blocks = next;
            if (TryValidateAuthoring()) return true;

            _blocks = previous;
            return false;
        }

        internal bool RemoveBlocks(IReadOnlyCollection<int> blockIndices)
        {
            if (blockIndices == null || blockIndices.Count == 0 || BlockCount - blockIndices.Count < 1) return false;

            var removed = new HashSet<int>(blockIndices);
            if (removed.Count != blockIndices.Count) return false;

            foreach (var index in removed)
            {
                if (index < 0 || index >= BlockCount) return false;
            }

            var next = new FanlightLayoutBlock[BlockCount - removed.Count];
            var write = 0;
            for (var i = 0; i < BlockCount; i++)
            {
                if (!removed.Contains(i)) next[write++] = _blocks[i];
            }

            var previous = _blocks;
            _blocks = next;
            if (TryValidateAuthoring()) return true;

            _blocks = previous;
            return false;
        }

        internal bool SetContentHash(ulong contentHash)
        {
            contentHash = contentHash == 0UL ? 1UL : contentHash;
            if (_contentHash == contentHash) return false;

            _contentHash = contentHash;
            return true;
        }

        internal void SetActiveBake(FanlightLayoutBakeArtifact artifact)
        {
            _activeBake = artifact;
        }

        private bool TryValidateAuthoring()
        {
            if (!LayoutId.IsValid
                || !math.all(math.isfinite(_referenceSeatSpacing))
                || !math.all(_referenceSeatSpacing > 0f)
                || _blocks == null
                || _blocks.Length == 0)
            {
                return false;
            }

            var blockIds = new HashSet<string>(StringComparer.Ordinal);
            var seatIds = new HashSet<ulong>();
            long totalSeats = 0;

            for (var blockIndex = 0; blockIndex < _blocks.Length; blockIndex++)
            {
                var block = _blocks[blockIndex];
                if (block == null
                    || string.IsNullOrEmpty(block.BlockId)
                    || !blockIds.Add(block.BlockId)
                    || block.RowCount == 0
                    || !IsFinite(block.Placement.position)
                    || !IsFinite(block.Placement.eulerRotation))
                {
                    return false;
                }

                for (var rowIndex = 0; rowIndex < block.RowCount; rowIndex++)
                {
                    var row = block.GetRow(rowIndex);
                    if (row == null
                        || row.SeatCount == 0
                        || !IsFinite(row.LeftPoint)
                        || !IsFinite(row.ControlPoint)
                        || !IsFinite(row.RightPoint))
                    {
                        return false;
                    }

                    totalSeats += row.SeatCount;
                    if (totalSeats > int.MaxValue) return false;

                    for (var seatIndex = 0; seatIndex < row.SeatCount; seatIndex++)
                    {
                        var stableSeatId = row.GetStableSeatId(seatIndex);
                        if (stableSeatId == 0UL || !seatIds.Add(stableSeatId)) return false;
                    }
                }
            }

            return totalSeats > 0;
        }

        private bool HasBasicAuthoringStructure()
        {
            if (!LayoutId.IsValid
                || !math.all(math.isfinite(_referenceSeatSpacing))
                || !math.all(_referenceSeatSpacing > 0f)
                || _blocks == null
                || _blocks.Length == 0)
            {
                return false;
            }

            long totalSeats = 0;
            for (var blockIndex = 0; blockIndex < _blocks.Length; blockIndex++)
            {
                var block = _blocks[blockIndex];
                if (block == null || string.IsNullOrEmpty(block.BlockId) || block.RowCount == 0) return false;

                for (var rowIndex = 0; rowIndex < block.RowCount; rowIndex++)
                {
                    var row = block.GetRow(rowIndex);
                    if (row == null || row.SeatCount == 0) return false;
                    totalSeats += row.SeatCount;
                    if (totalSeats > int.MaxValue) return false;
                }
            }

            return totalSeats > 0;
        }

        private static void ValidateReferenceSeatSpacing(float2 value)
        {
            if (!math.all(math.isfinite(value)) || !math.all(value > 0f))
            {
                throw new ArgumentOutOfRangeException(nameof(value), "Reference seat spacing must be finite and positive.");
            }
        }

        private static bool IsFinite(Vector3 value)
            => float.IsFinite(value.x) && float.IsFinite(value.y) && float.IsFinite(value.z);
    }
}

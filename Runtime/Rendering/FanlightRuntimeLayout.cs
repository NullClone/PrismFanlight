using System;
using System.Collections.Generic;
using PrismFanlight.Authoring;
using Unity.Mathematics;
using UnityEngine;

namespace PrismFanlight.Rendering
{
    internal sealed class FanlightRuntimeLayout
    {
        // Properties

        internal string LayoutId { get; }

        internal ulong ContentHash { get; }

        internal float2 ReferenceSeatSpacing { get; }

        internal Bounds LocalBounds { get; }

        internal FanlightSeatData[] Seats { get; }

        internal ulong[] StableSeatIds { get; }

        internal ulong StableSeatIdHash { get; }

        internal string[] StableBlockIds { get; }

        internal FanlightBakedBlockData[] Blocks { get; }

        internal int SeatCount => Seats.Length;

        internal int BlockCount => Blocks.Length;

        internal bool HasStableSeatIds => StableSeatIds.Length == SeatCount && StableSeatIdHash != 0UL;

        internal bool HasValidTopology { get; }


        // Methods

        internal FanlightRuntimeLayout(
            string layoutId,
            ulong contentHash,
            float2 referenceSeatSpacing,
            Bounds localBounds,
            FanlightSeatData[] seats,
            ulong[] stableSeatIds,
            string[] stableBlockIds,
            FanlightBakedBlockData[] blocks)
        {
            LayoutId = layoutId ?? string.Empty;
            ContentHash = contentHash;
            ReferenceSeatSpacing = referenceSeatSpacing;
            LocalBounds = localBounds;
            Seats = seats ?? Array.Empty<FanlightSeatData>();
            StableSeatIds = stableSeatIds ?? Array.Empty<ulong>();
            StableBlockIds = stableBlockIds ?? Array.Empty<string>();
            Blocks = blocks ?? Array.Empty<FanlightBakedBlockData>();
            StableSeatIdHash = ComputeStableSeatIdHash(StableSeatIds, out var hasUniqueStableSeatIds);
            HasValidTopology = SeatCount > 0
                               && BlockCount > 0
                               && math.all(math.isfinite(ReferenceSeatSpacing))
                               && math.all(ReferenceSeatSpacing > 0f)
                               && HasStableSeatIds
                               && hasUniqueStableSeatIds
                               && HasValidStableBlockIds(StableBlockIds, BlockCount)
                               && HasValidBlockRanges(Seats, Blocks);
        }

        internal bool HasSameTopology(FanlightRuntimeLayout other)
        {
            if (other == null
                || !string.Equals(LayoutId, other.LayoutId, StringComparison.Ordinal)
                || SeatCount != other.SeatCount
                || BlockCount != other.BlockCount
                || StableSeatIdHash != other.StableSeatIdHash)
            {
                return false;
            }

            for (var i = 0; i < BlockCount; i++)
            {
                if (!string.Equals(StableBlockIds[i], other.StableBlockIds[i], StringComparison.Ordinal)
                    || Blocks[i].startIndex != other.Blocks[i].startIndex
                    || Blocks[i].count != other.Blocks[i].count)
                {
                    return false;
                }
            }

            return true;
        }

        internal static FanlightRuntimeLayout FromArtifact(FanlightLayoutAsset layout)
        {
            if (layout == null || !layout.HasValidBake) return null;

            var artifact = layout.ActiveBake;
            var seats = new FanlightSeatData[artifact.SeatCount];
            var stableSeatIds = new ulong[artifact.SeatCount];
            var stableBlockIds = new string[artifact.BlockCount];
            var blocks = new FanlightBakedBlockData[artifact.BlockCount];

            for (var i = 0; i < seats.Length; i++)
            {
                var source = artifact.GetSeat(i);
                seats[i] = new FanlightSeatData(source.localPosition, source.blockIndex, (uint)i);
                stableSeatIds[i] = source.stableSeatId;
            }

            for (var i = 0; i < blocks.Length; i++)
            {
                var source = artifact.GetBlock(i);
                stableBlockIds[i] = source.blockId;
                blocks[i] = new FanlightBakedBlockData(
                    source.localBounds.center,
                    source.localBounds.extents.magnitude,
                    source.contiguousSeatStart,
                    source.contiguousSeatCount,
                    source.effectCoordinate);
            }

            var spacing = artifact.ReferenceSeatSpacing;
            return new FanlightRuntimeLayout(
                layout.LayoutId.Value,
                artifact.ContentHash,
                new float2(spacing.x, spacing.y),
                artifact.LocalBounds,
                seats,
                stableSeatIds,
                stableBlockIds,
                blocks);
        }

        internal int GetBlockIndex(string stableBlockId)
        {
            if (string.IsNullOrEmpty(stableBlockId)) return -1;

            for (var i = 0; i < StableBlockIds.Length; i++)
            {
                if (string.Equals(StableBlockIds[i], stableBlockId, StringComparison.Ordinal)) return i;
            }

            return -1;
        }

        private static bool HasValidStableBlockIds(string[] stableBlockIds, int blockCount)
        {
            if (stableBlockIds.Length != blockCount) return false;

            var used = new HashSet<string>(StringComparer.Ordinal);
            for (var i = 0; i < stableBlockIds.Length; i++)
            {
                var stableBlockId = stableBlockIds[i];
                if (string.IsNullOrEmpty(stableBlockId) || !used.Add(stableBlockId)) return false;
            }

            return true;
        }

        private static bool HasValidBlockRanges(FanlightSeatData[] seats, FanlightBakedBlockData[] blocks)
        {
            var expectedStart = 0;

            for (var blockIndex = 0; blockIndex < blocks.Length; blockIndex++)
            {
                var block = blocks[blockIndex];
                if (block.startIndex != expectedStart
                    || block.count <= 0
                    || block.startIndex < 0
                    || block.startIndex > seats.Length - block.count)
                {
                    return false;
                }

                var end = block.startIndex + block.count;
                for (var seatIndex = block.startIndex; seatIndex < end; seatIndex++)
                {
                    if (seats[seatIndex].blockIndex != blockIndex) return false;
                }

                expectedStart = end;
            }

            return expectedStart == seats.Length;
        }

        private static ulong ComputeStableSeatIdHash(ulong[] stableSeatIds, out bool hasUniqueStableSeatIds)
        {
            hasUniqueStableSeatIds = stableSeatIds != null && stableSeatIds.Length > 0;
            if (!hasUniqueStableSeatIds) return 0UL;

            var used = new HashSet<ulong>();
            var hash = 14695981039346656037UL;
            for (var i = 0; i < stableSeatIds.Length; i++)
            {
                var value = stableSeatIds[i];
                if (value == 0UL || !used.Add(value))
                {
                    hasUniqueStableSeatIds = false;
                    return 0UL;
                }

                for (var byteIndex = 0; byteIndex < 8; byteIndex++)
                {
                    hash ^= (byte)(value >> (byteIndex * 8));
                    hash *= 1099511628211UL;
                }
            }

            return hash == 0UL ? 1UL : hash;
        }
    }
}

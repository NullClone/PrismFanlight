using System;
using System.Collections.Generic;
using PrismFanlight.Authoring;
using Unity.Mathematics;
using UnityEngine;

namespace PrismFanlight.Rendering
{
    internal sealed class FanlightRuntimeLayout
    {
        internal FanlightRuntimeLayout(
            string layoutId,
            ulong contentHash,
            int2 seatPerBlock,
            float2 seatPitch,
            int2 blockCount,
            Bounds localBounds,
            FanlightSeatData[] seats,
            ulong[] stableSeatIds,
            string[] stableBlockIds,
            FanlightBakedBlockData[] blocks)
        {
            LayoutId = layoutId ?? string.Empty;
            ContentHash = contentHash;
            SeatPerBlock = seatPerBlock;
            SeatPitch = seatPitch;
            BlockCount2D = blockCount;
            LocalBounds = localBounds;
            Seats = seats ?? Array.Empty<FanlightSeatData>();
            StableSeatIds = stableSeatIds ?? Array.Empty<ulong>();
            StableBlockIds = stableBlockIds ?? Array.Empty<string>();
            Blocks = blocks ?? Array.Empty<FanlightBakedBlockData>();
            StableSeatIdHash = ComputeStableSeatIdHash(
                StableSeatIds,
                Seats,
                Blocks.Length,
                out var hasValidSeatBlockIndices);
            HasValidTopology = SeatCount > 0
                               && BlockCount > 0
                               && BlockSeatCount > 0
                               && HasStableSeatIds
                               && hasValidSeatBlockIndices
                               && HasValidStableBlockIds(StableBlockIds, BlockCount);
        }

        internal string LayoutId { get; }

        internal ulong ContentHash { get; }

        internal int2 SeatPerBlock { get; }

        internal float2 SeatPitch { get; }

        internal int2 BlockCount2D { get; }

        internal Bounds LocalBounds { get; }

        internal FanlightSeatData[] Seats { get; }

        internal ulong[] StableSeatIds { get; }

        internal ulong StableSeatIdHash { get; }

        internal string[] StableBlockIds { get; }

        internal FanlightBakedBlockData[] Blocks { get; }

        internal int SeatCount => Seats.Length;

        internal int BlockCount => Blocks.Length;

        internal int BlockSeatCount => SeatPerBlock.x * SeatPerBlock.y;

        internal bool HasStableSeatIds => StableSeatIds.Length == SeatCount && StableSeatIdHash != 0UL;

        internal bool HasValidTopology { get; }

        internal bool HasSameTopology(FanlightRuntimeLayout other)
        {
            return other != null
                   && string.Equals(LayoutId, other.LayoutId, StringComparison.Ordinal)
                   && SeatCount == other.SeatCount
                   && BlockCount == other.BlockCount
                   && SeatPerBlock.Equals(other.SeatPerBlock)
                   && BlockCount2D.Equals(other.BlockCount2D);
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
                seats[i] = new FanlightSeatData(
                    source.localPosition,
                    source.planePosition,
                    source.blockCoordinates,
                    source.blockIndex,
                    source.placementFlags,
                    (uint)i);
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
                    source.contiguousSeatCount);
            }

            return new FanlightRuntimeLayout(
                layout.LayoutId.Value,
                artifact.ContentHash,
                layout.SeatPerBlock,
                layout.SeatPitch,
                layout.BlockCount,
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

        private static ulong ComputeStableSeatIdHash(
            ulong[] stableSeatIds,
            FanlightSeatData[] seats,
            int blockCount,
            out bool hasValidSeatBlockIndices)
        {
            hasValidSeatBlockIndices = stableSeatIds != null
                                       && seats != null
                                       && stableSeatIds.Length == seats.Length
                                       && blockCount > 0;
            if (stableSeatIds == null || stableSeatIds.Length == 0) return 0UL;

            var hash = 14695981039346656037UL;
            for (var i = 0; i < stableSeatIds.Length; i++)
            {
                var value = stableSeatIds[i];
                if (value == 0UL) return 0UL;
                if (hasValidSeatBlockIndices
                    && (seats[i].blockIndex < 0 || seats[i].blockIndex >= blockCount))
                {
                    hasValidSeatBlockIndices = false;
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

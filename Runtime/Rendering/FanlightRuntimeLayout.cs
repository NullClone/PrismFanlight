using System;
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
            Blocks = blocks ?? Array.Empty<FanlightBakedBlockData>();
            StableSeatIdHash = ComputeStableSeatIdHash(StableSeatIds);
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

        internal FanlightBakedBlockData[] Blocks { get; }

        internal int SeatCount => Seats.Length;

        internal int BlockCount => Blocks.Length;

        internal int BlockSeatCount => SeatPerBlock.x * SeatPerBlock.y;

        internal bool HasStableSeatIds => StableSeatIds.Length == SeatCount && StableSeatIdHash != 0UL;

        internal bool HasValidTopology => SeatCount > 0 && BlockCount > 0 && BlockSeatCount > 0 && HasStableSeatIds;

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
            var blocks = new FanlightBakedBlockData[artifact.BlockCount];

            for (var i = 0; i < seats.Length; i++)
            {
                var source = artifact.GetSeat(i);
                seats[i] = new FanlightSeatData(source.localPosition, source.planePosition, source.blockCoordinates, (uint)i);
                stableSeatIds[i] = source.stableSeatId;
            }

            for (var i = 0; i < blocks.Length; i++)
            {
                var source = artifact.GetBlock(i);
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
                blocks);
        }

        private static ulong ComputeStableSeatIdHash(ulong[] stableSeatIds)
        {
            if (stableSeatIds == null || stableSeatIds.Length == 0) return 0UL;
            var hash = 14695981039346656037UL;
            for (var i = 0; i < stableSeatIds.Length; i++)
            {
                var value = stableSeatIds[i];
                if (value == 0UL) return 0UL;
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

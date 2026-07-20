using System;
using PrismFanlight.Authoring;
using Unity.Mathematics;
using UnityEngine;

namespace PrismFanlight.Rendering
{
    internal sealed class FanlightRuntimeLayout
    {
        public FanlightRuntimeLayout(
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

        public string LayoutId { get; }

        public ulong ContentHash { get; }

        public int2 SeatPerBlock { get; }

        public float2 SeatPitch { get; }

        public int2 BlockCount2D { get; }

        public Bounds LocalBounds { get; }

        public FanlightSeatData[] Seats { get; }

        public ulong[] StableSeatIds { get; }

        public ulong StableSeatIdHash { get; }

        public FanlightBakedBlockData[] Blocks { get; }

        public int SeatCount => Seats.Length;

        public int BlockCount => Blocks.Length;

        public int BlockSeatCount => SeatPerBlock.x * SeatPerBlock.y;

        public bool HasStableSeatIds => StableSeatIds.Length == SeatCount && StableSeatIdHash != 0UL;

        public bool HasValidTopology => SeatCount > 0 && BlockCount > 0 && BlockSeatCount > 0 && HasStableSeatIds;

        public bool HasSameTopology(FanlightRuntimeLayout other)
        {
            return other != null
                   && string.Equals(LayoutId, other.LayoutId, StringComparison.Ordinal)
                   && SeatCount == other.SeatCount
                   && BlockCount == other.BlockCount
                   && SeatPerBlock.Equals(other.SeatPerBlock)
                   && BlockCount2D.Equals(other.BlockCount2D);
        }

        public static FanlightRuntimeLayout FromArtifact(FanlightLayoutAsset layout)
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

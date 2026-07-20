using System;
using UnityEngine;

namespace PrismFanlight.Authoring
{
    [Serializable]
    public struct FanlightBakedSeatRecord
    {
        public ulong stableSeatId;
        public Vector3 localPosition;
        public Vector2 planePosition;
        public Vector2 blockCoordinates;
        public int blockIndex;
        public uint placementFlags;
    }

    [Serializable]
    public struct FanlightBakedBlockRecord
    {
        public string blockId;
        public Bounds localBounds;
        public int contiguousSeatStart;
        public int contiguousSeatCount;
        public ulong contentHash;
    }

    [PreferBinarySerialization]
    public sealed class FanlightLayoutBakeArtifact : ScriptableObject
    {
        // Fields

        public const int CurrentFormatVersion = 2;


        [SerializeField]
        private int _formatVersion;

        [SerializeField]
        private string _layoutId;

        [SerializeField]
        private ulong _contentHash;

        [SerializeField]
        private Bounds _localBounds;

        [SerializeField]
        private FanlightBakedSeatRecord[] _seats = Array.Empty<FanlightBakedSeatRecord>();

        [SerializeField]
        private FanlightBakedBlockRecord[] _blocks = Array.Empty<FanlightBakedBlockRecord>();


        // Properties

        public int FormatVersion => _formatVersion;

        public string LayoutId => _layoutId ?? string.Empty;

        public ulong ContentHash => _contentHash;

        public Bounds LocalBounds => _localBounds;

        public int SeatCount => _seats?.Length ?? 0;

        public int BlockCount => _blocks?.Length ?? 0;

        public ReadOnlySpan<FanlightBakedSeatRecord> Seats => _seats;

        public ReadOnlySpan<FanlightBakedBlockRecord> Blocks => _blocks;


        // Methods

        public FanlightBakedSeatRecord GetSeat(int index) => _seats[index];

        public FanlightBakedBlockRecord GetBlock(int index) => _blocks[index];

        public bool Matches(FanlightLayoutAsset layout)
        {
            if (layout == null || !layout.IsInitialized) return false;

            if (_formatVersion != CurrentFormatVersion
                || !string.Equals(LayoutId, layout.LayoutId.Value, StringComparison.Ordinal)
                || _contentHash == 0UL
                || _contentHash != layout.ContentHash
                || SeatCount != layout.TotalSeatCount
                || BlockCount != layout.TotalBlockCount)
            {
                return false;
            }

            return true;
        }

        internal void Initialize(
            string layoutId,
            ulong contentHash,
            Bounds localBounds,
            FanlightBakedSeatRecord[] seats,
            FanlightBakedBlockRecord[] blocks)
        {
            _formatVersion = CurrentFormatVersion;
            _layoutId = layoutId;
            _contentHash = contentHash;
            _localBounds = localBounds;
            _seats = seats == null
                ? Array.Empty<FanlightBakedSeatRecord>()
                : (FanlightBakedSeatRecord[])seats.Clone();
            _blocks = blocks == null
                ? Array.Empty<FanlightBakedBlockRecord>()
                : (FanlightBakedBlockRecord[])blocks.Clone();
        }
    }
}

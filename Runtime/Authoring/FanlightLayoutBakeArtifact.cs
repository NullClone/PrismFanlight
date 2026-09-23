using System;
using UnityEngine;

namespace PrismFanlight.Authoring
{
    [Serializable]
    internal struct FanlightBakedSeatRecord
    {
        [SerializeField]
        internal ulong stableSeatId;

        [SerializeField]
        internal Vector3 localPosition;

        [SerializeField]
        internal int blockIndex;
    }

    [Serializable]
    internal struct FanlightBakedBlockRecord
    {
        [SerializeField]
        internal string blockId;

        [SerializeField]
        internal Bounds localBounds;

        [SerializeField]
        internal int contiguousSeatStart;

        [SerializeField]
        internal int contiguousSeatCount;

        [SerializeField]
        internal ulong contentHash;

        [SerializeField]
        internal Vector2 effectCoordinate;
    }

    [PreferBinarySerialization]
    public sealed class FanlightLayoutBakeArtifact : ScriptableObject
    {
        // Fields

        internal const int CurrentFormatVersion = 3;

        [SerializeField]
        private int _formatVersion;

        [SerializeField]
        private string _layoutId;

        [SerializeField]
        private ulong _contentHash;

        [SerializeField]
        private Vector2 _referenceSeatSpacing;

        [SerializeField]
        private Bounds _localBounds;

        [SerializeField]
        private FanlightBakedSeatRecord[] _seats = Array.Empty<FanlightBakedSeatRecord>();

        [SerializeField]
        private FanlightBakedBlockRecord[] _blocks = Array.Empty<FanlightBakedBlockRecord>();


        // Properties

        internal int FormatVersion => _formatVersion;

        internal string LayoutId => _layoutId ?? string.Empty;

        internal ulong ContentHash => _contentHash;

        internal Vector2 ReferenceSeatSpacing => _referenceSeatSpacing;

        internal Bounds LocalBounds => _localBounds;

        internal int SeatCount => _seats?.Length ?? 0;

        internal int BlockCount => _blocks?.Length ?? 0;

        internal ReadOnlySpan<FanlightBakedSeatRecord> Seats => _seats;

        internal ReadOnlySpan<FanlightBakedBlockRecord> Blocks => _blocks;


        // Methods

        internal FanlightBakedSeatRecord GetSeat(int index) => _seats[index];

        internal FanlightBakedBlockRecord GetBlock(int index) => _blocks[index];

        internal bool Matches(FanlightLayoutAsset layout)
        {
            if (layout == null) return false;

            return _formatVersion == CurrentFormatVersion
                   && string.Equals(LayoutId, layout.LayoutId.Value, StringComparison.Ordinal)
                   && _contentHash != 0UL
                   && _contentHash == layout.ContentHash
                   && BlockCount == layout.BlockCount;
        }

        internal void Initialize(
            string layoutId,
            ulong contentHash,
            Vector2 referenceSeatSpacing,
            Bounds localBounds,
            FanlightBakedSeatRecord[] seats,
            FanlightBakedBlockRecord[] blocks)
        {
            _formatVersion = CurrentFormatVersion;
            _layoutId = layoutId;
            _contentHash = contentHash;
            _referenceSeatSpacing = referenceSeatSpacing;
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

using System;
using UnityEngine;

namespace PrismFanlight.Core
{
    [Serializable]
    internal struct FanlightBlockPaletteEntry : IEquatable<FanlightBlockPaletteEntry>
    {
        // Fields

        [SerializeField]
        private string _stableBlockId;

        [SerializeField]
        private int _paletteSlot;


        // Properties

        internal string StableBlockId => _stableBlockId ?? string.Empty;

        internal int PaletteSlot => _paletteSlot;


        // Methods

        internal FanlightBlockPaletteEntry(string stableBlockId, int paletteSlot)
        {
            if (string.IsNullOrEmpty(stableBlockId))
            {
                throw new ArgumentException("A Stable Block ID is required.", nameof(stableBlockId));
            }

            if (paletteSlot < 0 || paletteSlot > 5)
            {
                throw new ArgumentOutOfRangeException(nameof(paletteSlot));
            }

            _stableBlockId = stableBlockId;
            _paletteSlot = paletteSlot;
        }

        public bool Equals(FanlightBlockPaletteEntry other)
        {
            return string.Equals(StableBlockId, other.StableBlockId, StringComparison.Ordinal)
                   && PaletteSlot == other.PaletteSlot;
        }
    }
}

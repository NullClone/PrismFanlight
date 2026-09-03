using System;
using UnityEngine;

namespace PrismFanlight.Core
{
    [Serializable]
    internal struct FanlightBlockPulseEntry : IEquatable<FanlightBlockPulseEntry>
    {
        // Fields

        [SerializeField]
        private string _stableBlockId;

        [SerializeField]
        private FanlightBlockPulseGroup _group;


        // Properties

        internal string StableBlockId => _stableBlockId ?? string.Empty;

        internal FanlightBlockPulseGroup Group => _group;


        // Methods

        internal FanlightBlockPulseEntry(string stableBlockId, FanlightBlockPulseGroup group)
        {
            if (string.IsNullOrEmpty(stableBlockId))
            {
                throw new ArgumentException("A Stable Block ID is required.", nameof(stableBlockId));
            }

            if (group != FanlightBlockPulseGroup.A && group != FanlightBlockPulseGroup.B)
            {
                throw new ArgumentOutOfRangeException(nameof(group));
            }

            _stableBlockId = stableBlockId;
            _group = group;
        }

        public bool Equals(FanlightBlockPulseEntry other)
        {
            return string.Equals(StableBlockId, other.StableBlockId, StringComparison.Ordinal)
                   && Group == other.Group;
        }
    }
}

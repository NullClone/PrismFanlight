using System;

namespace PrismFanlight.Core
{
    internal sealed class FanlightSequenceContext
    {
        // Properties

        internal FanlightSequenceContext Parent { get; }

        internal bool IsReleased { get; private set; }


        // Methods

        internal FanlightSequenceContext(FanlightSequenceContext parent)
        {
            if (parent is { IsReleased: true })
            {
                throw new ArgumentException("A released Sequence Context cannot be a parent.", nameof(parent));
            }

            Parent = parent;
        }

        internal bool IsAncestorOf(FanlightSequenceContext other)
        {
            if (IsReleased || other == null || other.IsReleased) return false;

            for (var current = other.Parent; current != null; current = current.Parent)
            {
                if (ReferenceEquals(this, current)) return true;
            }

            return false;
        }

        internal void Release()
        {
            IsReleased = true;
        }
    }
}

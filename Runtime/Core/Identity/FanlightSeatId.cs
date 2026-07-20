using System;

namespace PrismFanlight.Core
{
    [Serializable]
    public readonly struct FanlightSeatId : IEquatable<FanlightSeatId>
    {
        // Properties

        public ulong Value { get; }

        public bool IsValid => Value != 0UL;


        // Methods

        public FanlightSeatId(ulong value)
        {
            Value = value;
        }

        public bool Equals(FanlightSeatId other) => Value == other.Value;

        public override bool Equals(object obj) => obj is FanlightSeatId other && Equals(other);

        public override int GetHashCode() => Value.GetHashCode();

        public override string ToString() => Value.ToString("X16");
    }
}

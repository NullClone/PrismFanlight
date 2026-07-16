using System;

namespace PrismFanlight.Core
{
    [Serializable]
    public readonly struct FanlightLayoutId : IEquatable<FanlightLayoutId>
    {
        private readonly string _value;

        public FanlightLayoutId(string value)
        {
            _value = Normalize(value);
        }

        public string Value => _value ?? string.Empty;

        public bool IsValid
        {
            get
            {
                var value = Value;
                if (value.Length != 32) return false;
                for (var i = 0; i < value.Length; i++)
                {
                    var c = value[i];
                    if (!((c >= '0' && c <= '9') || (c >= 'a' && c <= 'f'))) return false;
                }
                return true;
            }
        }

        public bool Equals(FanlightLayoutId other) => string.Equals(Value, other.Value, StringComparison.Ordinal);

        public override bool Equals(object obj) => obj is FanlightLayoutId other && Equals(other);

        public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(Value);

        public override string ToString() => Value;

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? string.Empty
                : value.Replace("-", string.Empty).Trim().ToLowerInvariant();
        }
    }

    [Serializable]
    public readonly struct FanlightSeatId : IEquatable<FanlightSeatId>
    {
        public FanlightSeatId(ulong value)
        {
            Value = value;
        }

        public ulong Value { get; }

        public bool IsValid => Value != 0UL;

        public bool Equals(FanlightSeatId other) => Value == other.Value;

        public override bool Equals(object obj) => obj is FanlightSeatId other && Equals(other);

        public override int GetHashCode() => Value.GetHashCode();

        public override string ToString() => Value.ToString("X16");
    }
}

using System;
using UnityEngine;

namespace PrismFanlight.Editor
{
    internal static class FanlightStableHash
    {
        private const ulong Offset = 14695981039346656037UL;
        private const ulong Prime = 1099511628211UL;


        public static ulong Begin() => Offset;

        public static ulong Finish(ulong hash) => hash == 0UL ? 1UL : hash;

        public static ulong Add(ulong hash, int value) => Add(hash, unchecked((uint)value));

        public static ulong Add(ulong hash, uint value)
        {
            for (var i = 0; i < 4; i++)
            {
                hash = AddByte(hash, (byte)(value >> (i * 8)));
            }

            return hash;
        }

        public static ulong Add(ulong hash, ulong value)
        {
            for (var i = 0; i < 8; i++)
            {
                hash = AddByte(hash, (byte)(value >> (i * 8)));
            }

            return hash;
        }

        public static ulong Add(ulong hash, float value) => Add(hash, BitConverter.SingleToInt32Bits(value));

        public static ulong Add(ulong hash, Vector3 value)
        {
            hash = Add(hash, value.x);
            hash = Add(hash, value.y);
            return Add(hash, value.z);
        }

        public static ulong Add(ulong hash, string value)
        {
            value ??= string.Empty;

            for (var i = 0; i < value.Length; i++)
            {
                var c = value[i];
                hash = AddByte(hash, (byte)c);
                hash = AddByte(hash, (byte)(c >> 8));
            }

            return hash;
        }

        private static ulong AddByte(ulong hash, byte value) => (hash ^ value) * Prime;
    }
}

using System;

namespace PrismFanlight.Rendering
{
    internal static class FanlightPenlightAssignment
    {
        internal const int PersonaAlgorithmVersion = 1;


        internal static int SelectVariantIndex(ulong stableSeatId, uint assignmentSeed, int personaAlgorithmVersion, ReadOnlySpan<uint> stableVariantIds)
        {
            if (stableSeatId == 0UL)
            {
                throw new ArgumentOutOfRangeException(nameof(stableSeatId));
            }

            if (personaAlgorithmVersion != PersonaAlgorithmVersion)
            {
                throw new ArgumentOutOfRangeException(nameof(personaAlgorithmVersion));
            }

            if (stableVariantIds.Length == 0)
            {
                throw new ArgumentException("At least one variant ID is required.", nameof(stableVariantIds));
            }

            var selectedIndex = -1;
            var selectedId = uint.MaxValue;
            var selectedScore = 0UL;

            for (var i = 0; i < stableVariantIds.Length; i++)
            {
                var variantId = stableVariantIds[i];
                if (variantId == 0u)
                {
                    throw new ArgumentException("Variant IDs must be non-zero.", nameof(stableVariantIds));
                }

                for (var previous = 0; previous < i; previous++)
                {
                    if (stableVariantIds[previous] == variantId)
                    {
                        throw new ArgumentException("Variant IDs must be unique.", nameof(stableVariantIds));
                    }
                }

                var score = Score(stableSeatId, assignmentSeed, personaAlgorithmVersion, variantId);
                if (selectedIndex < 0 || score > selectedScore || score == selectedScore && variantId < selectedId)
                {
                    selectedIndex = i;
                    selectedId = variantId;
                    selectedScore = score;
                }
            }

            return selectedIndex;
        }

        private static ulong Score(
            ulong stableSeatId,
            uint assignmentSeed,
            int personaAlgorithmVersion,
            uint stableVariantId)
        {
            var hash = 14695981039346656037UL;

            AddUInt(assignmentSeed);
            AddULong(stableSeatId);
            AddUInt(unchecked((uint)personaAlgorithmVersion));
            AddUInt(stableVariantId);

            return Mix(hash);

            void AddUInt(uint value)
            {
                for (var i = 0; i < 4; i++) AddByte((byte)(value >> (i * 8)));
            }

            void AddULong(ulong value)
            {
                for (var i = 0; i < 8; i++) AddByte((byte)(value >> (i * 8)));
            }

            void AddByte(byte value)
            {
                hash ^= value;
                hash *= 1099511628211UL;
            }
        }

        private static ulong Mix(ulong value)
        {
            value ^= value >> 30;
            value *= 0xBF58476D1CE4E5B9UL;
            value ^= value >> 27;
            value *= 0x94D049BB133111EBUL;
            value ^= value >> 31;
            return value;
        }
    }
}

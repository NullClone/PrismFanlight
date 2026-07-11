using System;
using Unity.Mathematics;
using UnityEngine;

namespace PrismFanlight
{
    [Serializable]
    public struct FanlightColorSettings
    {
        // Fields

        public const int MaxPaletteColors = 16;

        public FanlightColorMode mode;

        [ColorUsage(false, true)]
        public Color primaryColor;

        [ColorUsage(false, true)]
        public Color secondaryColor;

        [ColorUsage(false, true)]
        public Color[] paletteColors;

        [Min(0.0f)]
        public float intensity;

        [Range(0.0f, 1.0f)]
        public float randomIntensity;


        // Methods

        public static FanlightColorSettings Default() => new()
        {
            mode = FanlightColorMode.Single,
            primaryColor = Color.white,
            secondaryColor = Color.cyan,
            paletteColors = new[] { Color.white, Color.cyan },
            intensity = 20.0f,
            randomIntensity = 0.0f
        };

        public FanlightColorSettings Validated()
        {
            return new FanlightColorSettings
            {
                mode = IsSupportedMode(mode) ? mode : FanlightColorMode.Single,
                primaryColor = primaryColor,
                secondaryColor = secondaryColor,
                // The GPU dispatcher already clamps the palette to MaxPaletteColors
                // and falls back to primaryColor for an empty palette. Keeping the
                // original reference avoids a Color[] allocation during evaluation.
                paletteColors = paletteColors,
                intensity = math.max(intensity, 0.0f),
                randomIntensity = math.saturate(randomIntensity)
            };
        }

        public Color GetGlobalColor()
        {
            return primaryColor;
        }

        public float GetGlobalIntensity()
        {
            return math.max(intensity, 0.0f);
        }

        public int GetStableHash()
        {
            unchecked
            {
                var hash = 17;
                hash = hash * 31 + mode.GetHashCode();
                hash = hash * 31 + primaryColor.GetHashCode();
                hash = hash * 31 + secondaryColor.GetHashCode();
                hash = hash * 31 + randomIntensity.GetHashCode();

                var palette = paletteColors;
                if (palette != null)
                {
                    var count = math.min(palette.Length, MaxPaletteColors);
                    hash = hash * 31 + count;
                    for (var i = 0; i < count; i++)
                    {
                        hash = hash * 31 + palette[i].GetHashCode();
                    }
                }
                else
                {
                    hash = hash * 31;
                }

                return hash;
            }
        }

        private static bool IsSupportedMode(FanlightColorMode value)
        {
            return value is FanlightColorMode.Single or FanlightColorMode.Random or FanlightColorMode.Gradient;
        }
    }
}

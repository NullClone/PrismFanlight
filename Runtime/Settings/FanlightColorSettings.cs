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
            var palette = paletteColors == null || paletteColors.Length == 0
                ? new[] { primaryColor }
                : CopyPalette(paletteColors);

            return new FanlightColorSettings
            {
                mode = IsSupportedMode(mode) ? mode : FanlightColorMode.Single,
                primaryColor = primaryColor,
                secondaryColor = secondaryColor,
                paletteColors = palette,
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
                hash = hash * 31 + (palette?.Length ?? 0);

                if (palette != null)
                {
                    var count = math.min(palette.Length, MaxPaletteColors);
                    for (var i = 0; i < count; i++)
                    {
                        hash = hash * 31 + palette[i].GetHashCode();
                    }
                }

                return hash;
            }
        }

        private static Color[] CopyPalette(Color[] source)
        {
            var count = math.clamp(source.Length, 1, MaxPaletteColors);
            var destination = new Color[count];
            Array.Copy(source, destination, count);
            return destination;
        }

        private static float3 ToFloat3(Color color) => math.float3(color.r, color.g, color.b);

        private static bool IsSupportedMode(FanlightColorMode value)
        {
            return value is FanlightColorMode.Single or FanlightColorMode.Random or FanlightColorMode.Gradient;
        }
    }
}

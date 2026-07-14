using System;
using Unity.Mathematics;
using UnityEngine;

namespace PrismFanlight
{
    [Serializable]
    public struct FanlightColorSettings
    {
        // Fields

        public const int PaletteSlotCount = 6;

        private const int CurrentSerializationVersion = 1;


        [ColorUsage(false, true)]
        public Color slot1;

        [ColorUsage(false, true)]
        public Color slot2;

        [ColorUsage(false, true)]
        public Color slot3;

        [ColorUsage(false, true)]
        public Color slot4;

        [ColorUsage(false, true)]
        public Color slot5;

        [ColorUsage(false, true)]
        public Color slot6;

        [Min(0.0f)]
        public float intensity;

        [Range(0.0f, 1.0f)]
        public float randomIntensity;

        [SerializeField, HideInInspector]
        private int _serializationVersion;

        // Retained so existing components and preset assets can be upgraded without
        // losing their serialized color data. New code uses the six fixed slots.
        [Obsolete("Use the fixed palette slots instead."), FanlightTimelineIgnore, HideInInspector]
        public FanlightColorMode mode;

        [Obsolete("Use slot1 instead."), FanlightTimelineIgnore, HideInInspector]
        public Color primaryColor;

        [Obsolete("Use the fixed palette slots instead."), FanlightTimelineIgnore, HideInInspector]
        public Color secondaryColor;

        [Obsolete("Use the fixed palette slots instead."), FanlightTimelineIgnore, HideInInspector]
        public Color[] paletteColors;


        // Methods

        public static FanlightColorSettings Default()
        {
#pragma warning disable CS0618
            return new FanlightColorSettings
            {
                slot1 = Color.white,
                slot2 = Color.white,
                slot3 = Color.white,
                slot4 = Color.white,
                slot5 = Color.white,
                slot6 = Color.white,
                intensity = 20.0f,
                randomIntensity = 0.0f,
                _serializationVersion = CurrentSerializationVersion,
                mode = FanlightColorMode.Single,
                primaryColor = Color.white,
                secondaryColor = Color.cyan
            };
#pragma warning restore CS0618
        }

        public FanlightColorSettings Validated()
        {
            var result = _serializationVersion < CurrentSerializationVersion
                ? UpgradeLegacy()
                : this;

            result.intensity = math.max(result.intensity, 0.0f);
            result.randomIntensity = math.saturate(result.randomIntensity);
            result._serializationVersion = CurrentSerializationVersion;
            return result;
        }

        public Color GetSlot(int index)
        {
            var settings = Validated();
            return index switch
            {
                0 => settings.slot1,
                1 => settings.slot2,
                2 => settings.slot3,
                3 => settings.slot4,
                4 => settings.slot5,
                5 => settings.slot6,
                _ => throw new ArgumentOutOfRangeException(nameof(index), index, "Palette slot index must be between 0 and 5.")
            };
        }

        public FanlightColorSettings WithSlot(int index, Color color)
        {
            var result = Validated();
            switch (index)
            {
                case 0: result.slot1 = color; break;
                case 1: result.slot2 = color; break;
                case 2: result.slot3 = color; break;
                case 3: result.slot4 = color; break;
                case 4: result.slot5 = color; break;
                case 5: result.slot6 = color; break;
                default: throw new ArgumentOutOfRangeException(nameof(index), index, "Palette slot index must be between 0 and 5.");
            }

            return result;
        }

        public float GetGlobalIntensity() => math.max(intensity, 0.0f);

        public int GetStableHash()
        {
            var settings = Validated();

            unchecked
            {
                var hash = 17;
                hash = hash * 31 + settings.slot1.GetHashCode();
                hash = hash * 31 + settings.slot2.GetHashCode();
                hash = hash * 31 + settings.slot3.GetHashCode();
                hash = hash * 31 + settings.slot4.GetHashCode();
                hash = hash * 31 + settings.slot5.GetHashCode();
                hash = hash * 31 + settings.slot6.GetHashCode();
                hash = hash * 31 + settings.intensity.GetHashCode();
                hash = hash * 31 + settings.randomIntensity.GetHashCode();
                return hash;
            }
        }

        [Obsolete("Use GetSlot(0) instead.")]
        public Color GetGlobalColor() => Validated().slot1;

        private FanlightColorSettings UpgradeLegacy()
        {
            var result = this;

#pragma warning disable CS0618
            switch (mode)
            {
                case FanlightColorMode.Random when paletteColors is { Length: > 0 }:
                    for (var i = 0; i < PaletteSlotCount; i++)
                    {
                        var paletteIndex = PaletteSlotCount <= 1
                            ? 0
                            : Mathf.RoundToInt((float)i * (paletteColors.Length - 1) / (PaletteSlotCount - 1));
                        result = result.SetSlotUnchecked(i, paletteColors[Mathf.Clamp(paletteIndex, 0, paletteColors.Length - 1)]);
                    }
                    break;

                case FanlightColorMode.Gradient:
                    for (var i = 0; i < PaletteSlotCount; i++)
                    {
                        result = result.SetSlotUnchecked(i, Color.Lerp(primaryColor, secondaryColor, (float)i / (PaletteSlotCount - 1)));
                    }
                    break;

                default:
                    for (var i = 0; i < PaletteSlotCount; i++)
                    {
                        result = result.SetSlotUnchecked(i, primaryColor);
                    }
                    break;
            }
#pragma warning restore CS0618

            result._serializationVersion = CurrentSerializationVersion;
            return result;
        }

        private FanlightColorSettings SetSlotUnchecked(int index, Color color)
        {
            var result = this;
            switch (index)
            {
                case 0: result.slot1 = color; break;
                case 1: result.slot2 = color; break;
                case 2: result.slot3 = color; break;
                case 3: result.slot4 = color; break;
                case 4: result.slot5 = color; break;
                case 5: result.slot6 = color; break;
            }

            return result;
        }
    }
}

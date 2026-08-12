using System;
using PrismFanlight.Authoring;
using UnityEngine;

namespace PrismFanlight.Core
{
    internal static class FanlightShowStateDefaults
    {
        // Methods

        internal static FanlightIntentState Intent() => new(
            0.5f,
            0.5f,
            0.8f,
            0.5f,
            0.3f);

        internal static FanlightMotionState Motion() => Motion(null);

        internal static FanlightMotionState Motion(FanlightMotionAsset motionAsset) => new(
            motionAsset,
            1f,
            0f,
            0f,
            0f,
            1f,
            0f,
            1f,
            1f,
            0.06f,
            1f);

        internal static FanlightVariationState Variation() => new(
            0f,
            0f,
            0f,
            0f,
            0f,
            0f,
            0f,
            0f);

        internal static FanlightNoiseState Noise() => new(0f, 0f, 0f, 0f, 0f, 1, 0f);

        internal static FanlightRestState Rest() => new(0f, 0f, 1f, 0f, 0f, 0f);

        internal static FanlightAudienceBodyState AudienceBody() => new(
            1.7f,
            0.5f,
            0.2f,
            0.8f,
            0.16f,
            0.1f,
            0.6f,
            0f,
            0f);

        internal static FanlightDirectionState Direction() => new(FanlightDirectionMode.WorldDirection, 0f);

        internal static FanlightColorState Color() => new(
            new FanlightColorSource(
                FanlightColorMode.StablePalette,
                UnityEngine.Color.red,
                UnityEngine.Color.yellow,
                UnityEngine.Color.green,
                UnityEngine.Color.cyan,
                UnityEngine.Color.blue,
                UnityEngine.Color.magenta,
                UnityEngine.Color.white,
                UnityEngine.Color.white,
                Vector2.zero,
                Vector2.right,
                1f,
                0f,
                Array.Empty<FanlightBlockPaletteEntry>()));

        internal static FanlightIntensityState Intensity() => new(
            20f,
            0f,
            new FanlightIntensityMask(
                FanlightIntensityMaskMode.None,
                1f,
                0f,
                0f,
                0f,
                0f,
                1f,
                Vector2.zero,
                Vector2.right,
                1f));

        internal static FanlightVisibilityState Visibility() => new(true, true);
    }
}

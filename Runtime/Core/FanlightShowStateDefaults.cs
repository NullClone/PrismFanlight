using UnityEngine;

namespace PrismFanlight.Core
{
    internal static class FanlightShowStateDefaults
    {
        // Methods

        internal static FanlightIntentState Intent() => new(
            0.5f,
            0.5f,
            1f,
            1f,
            0.5f);

        internal static FanlightGestureState Gesture() => new(
            1f,
            0f,
            0.5f,
            0.5f,
            0f,
            0f);

        internal static FanlightPoseState Pose() => new(
            FanlightHandZone.Shoulder,
            0f,
            0f,
            1f,
            0f,
            1f,
            0.5f,
            1,
            0f,
            1f,
            0f,
            0f);

        internal static FanlightVariationState Variation() => new(
            0f,
            0f,
            0f,
            0f,
            0f,
            0f,
            0f,
            0f,
            0f,
            0f,
            1f,
            0f,
            0f);

        internal static FanlightNoiseState Noise() => new(0f, 0f, 0f, 0f, 1, 0f);

        internal static FanlightRestState Rest() => new(0f, 0f, 1f, 0f, 0f, 0f);

        internal static FanlightAudienceBodyState AudienceBody() => new(
            1.7f,
            0f,
            0.5f,
            0.2f,
            0.75f,
            0f,
            0.1f,
            1f,
            0f,
            0f,
            0f,
            0f,
            1f,
            0f);

        internal static FanlightDirectionState Direction() => new(
            FanlightDirectionMode.WorldDirection,
            0f,
            0f);

        internal static FanlightPaletteState Palette() => new(
            new Color(8f, 8f, 8f),
            new Color(8f, 8f, 8f),
            new Color(8f, 8f, 8f),
            new Color(8f, 8f, 8f),
            new Color(8f, 8f, 8f),
            new Color(8f, 8f, 8f),
            2f,
            0f);

        internal static FanlightVisibilityState Visibility() => new(true, true);
    }
}

using PrismFanlight.Core;
using UnityEngine;

namespace PrismFanlight.Timeline
{
    internal static class FanlightTimelineDefaults
    {
        internal static FanlightIntentState IntentState() => new(0.5f, 1f, 0.5f, 0.5f, 0.5f);

        internal static FanlightGestureState GestureState() => new(
            "gesture.default",
            1f,
            0f,
            0f,
            0.5f,
            1f,
            0.5f,
            0f,
            0f);

        internal static FanlightPoseState PoseState() => new(
            FanlightHandZone.Shoulder,
            0f,
            0f,
            1f,
            0f,
            1f,
            0f,
            Mathf.PI,
            0.5f,
            1f,
            0f,
            0f);

        internal static FanlightVariationState VariationState() => new(
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

        internal static FanlightNoiseState NoiseState() => new(0f, 0f, 0f, 0f, 1, 0f);

        internal static FanlightRestState RestState() => new(0f, 0f, 1f, 0f, 0f, 0f);

        internal static FanlightAudienceBodyState AudienceBodyState() => new(
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

        internal static FanlightDirectionState DirectionState() => new(
            FanlightDirectionMode.WorldDirection,
            0f,
            0f);

        internal static FanlightPaletteState PaletteState() => new(
            Color.white,
            Color.white,
            Color.white,
            Color.white,
            Color.white,
            Color.white,
            1f,
            0f);

        internal static FanlightVisibilityState VisibilityState() => new(true, true);
    }
}

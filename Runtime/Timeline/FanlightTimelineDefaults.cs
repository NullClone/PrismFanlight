using PrismFanlight.Core;

namespace PrismFanlight.Timeline
{
    internal static class FanlightTimelineDefaults
    {
        // Methods

        internal static FanlightIntentState IntentState() => FanlightShowStateDefaults.Intent();

        internal static FanlightMotionState MotionState() => FanlightShowStateDefaults.Motion();

        internal static FanlightVariationState VariationState() => FanlightShowStateDefaults.Variation();

        internal static FanlightNoiseState NoiseState() => FanlightShowStateDefaults.Noise();

        internal static FanlightRestState RestState() => FanlightShowStateDefaults.Rest();

        internal static FanlightAudienceBodyState AudienceBodyState() => FanlightShowStateDefaults.AudienceBody();

        internal static FanlightDirectionState DirectionState() => FanlightShowStateDefaults.Direction();

        internal static FanlightPaletteState PaletteState() => FanlightShowStateDefaults.Palette();

        internal static FanlightVisibilityState VisibilityState() => FanlightShowStateDefaults.Visibility();
    }
}

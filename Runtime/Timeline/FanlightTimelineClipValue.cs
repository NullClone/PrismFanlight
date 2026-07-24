using PrismFanlight.Core;

namespace PrismFanlight.Timeline
{
    internal readonly struct FanlightTimelineClipValue
    {
        // Properties

        internal FanlightIntentState Intent { get; }

        internal FanlightMotionState Motion { get; }

        internal FanlightVariationState Variation { get; }

        internal FanlightNoiseState Noise { get; }

        internal FanlightRestState Rest { get; }

        internal FanlightAudienceBodyState AudienceBody { get; }

        internal FanlightDirectionState Direction { get; }

        internal FanlightColorState Color { get; }

        internal FanlightIntensityState Intensity { get; }

        internal FanlightVisibilityState Visibility { get; }


        // Methods

        private FanlightTimelineClipValue(
            FanlightIntentState intent,
            FanlightMotionState motion,
            FanlightVariationState variation,
            FanlightNoiseState noise,
            FanlightRestState rest,
            FanlightAudienceBodyState audienceBody,
            FanlightDirectionState direction,
            FanlightColorState color,
            FanlightIntensityState intensity,
            FanlightVisibilityState visibility)
        {
            Intent = intent;
            Motion = motion;
            Variation = variation;
            Noise = noise;
            Rest = rest;
            AudienceBody = audienceBody;
            Direction = direction;
            Color = color;
            Intensity = intensity;
            Visibility = visibility;
        }

        internal static FanlightTimelineClipValue From(FanlightIntentState value) =>
            new(value, default, default, default, default, default, default, default, default, default);

        internal static FanlightTimelineClipValue From(FanlightMotionState value) =>
            new(default, value, default, default, default, default, default, default, default, default);

        internal static FanlightTimelineClipValue From(FanlightVariationState value) =>
            new(default, default, value, default, default, default, default, default, default, default);

        internal static FanlightTimelineClipValue From(FanlightNoiseState value) =>
            new(default, default, default, value, default, default, default, default, default, default);

        internal static FanlightTimelineClipValue From(FanlightRestState value) =>
            new(default, default, default, default, value, default, default, default, default, default);

        internal static FanlightTimelineClipValue From(FanlightAudienceBodyState value) =>
            new(default, default, default, default, default, value, default, default, default, default);

        internal static FanlightTimelineClipValue From(FanlightDirectionState value) =>
            new(default, default, default, default, default, default, value, default, default, default);

        internal static FanlightTimelineClipValue From(FanlightColorState value) =>
            new(default, default, default, default, default, default, default, value, default, default);

        internal static FanlightTimelineClipValue From(FanlightIntensityState value) =>
            new(default, default, default, default, default, default, default, default, value, default);

        internal static FanlightTimelineClipValue From(FanlightVisibilityState value) =>
            new(default, default, default, default, default, default, default, default, default, value);
    }
}

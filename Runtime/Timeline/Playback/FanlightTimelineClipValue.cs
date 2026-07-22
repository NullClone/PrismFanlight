using PrismFanlight.Core;

namespace PrismFanlight.Timeline
{
    internal readonly struct FanlightTimelineClipValue
    {
        internal FanlightIntentState Intent { get; }

        internal FanlightMotionState Motion { get; }

        internal FanlightVariationState Variation { get; }

        internal FanlightNoiseState Noise { get; }

        internal FanlightRestState Rest { get; }

        internal FanlightAudienceBodyState AudienceBody { get; }

        internal FanlightDirectionState Direction { get; }

        internal FanlightPaletteState Palette { get; }

        internal FanlightVisibilityState Visibility { get; }


        private FanlightTimelineClipValue(
            FanlightIntentState intent,
            FanlightMotionState motion,
            FanlightVariationState variation,
            FanlightNoiseState noise,
            FanlightRestState rest,
            FanlightAudienceBodyState audienceBody,
            FanlightDirectionState direction,
            FanlightPaletteState palette,
            FanlightVisibilityState visibility)
        {
            Intent = intent;
            Motion = motion;
            Variation = variation;
            Noise = noise;
            Rest = rest;
            AudienceBody = audienceBody;
            Direction = direction;
            Palette = palette;
            Visibility = visibility;
        }

        internal static FanlightTimelineClipValue From(FanlightIntentState value) =>
            new(value, default, default, default, default, default, default, default, default);

        internal static FanlightTimelineClipValue From(FanlightMotionState value) =>
            new(default, value, default, default, default, default, default, default, default);

        internal static FanlightTimelineClipValue From(FanlightVariationState value) =>
            new(default, default, value, default, default, default, default, default, default);

        internal static FanlightTimelineClipValue From(FanlightNoiseState value) =>
            new(default, default, default, value, default, default, default, default, default);

        internal static FanlightTimelineClipValue From(FanlightRestState value) =>
            new(default, default, default, default, value, default, default, default, default);

        internal static FanlightTimelineClipValue From(FanlightAudienceBodyState value) =>
            new(default, default, default, default, default, value, default, default, default);

        internal static FanlightTimelineClipValue From(FanlightDirectionState value) =>
            new(default, default, default, default, default, default, value, default, default);

        internal static FanlightTimelineClipValue From(FanlightPaletteState value) =>
            new(default, default, default, default, default, default, default, value, default);

        internal static FanlightTimelineClipValue From(FanlightVisibilityState value) =>
            new(default, default, default, default, default, default, default, default, value);
    }
}

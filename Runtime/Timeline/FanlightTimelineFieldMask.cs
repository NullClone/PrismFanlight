using PrismFanlight.Core;

namespace PrismFanlight.Timeline
{
    internal readonly struct FanlightTimelineFieldMask
    {
        // Properties

        internal FanlightIntentFields Intent { get; }

        internal FanlightMotionFields Motion { get; }

        internal FanlightVariationFields Variation { get; }

        internal FanlightNoiseFields Noise { get; }

        internal FanlightRestFields Rest { get; }

        internal FanlightAudienceBodyFields AudienceBody { get; }

        internal FanlightDirectionFields Direction { get; }

        internal FanlightColorFields Color { get; }

        internal FanlightIntensityFields Intensity { get; }


        // Methods

        private FanlightTimelineFieldMask(
            FanlightIntentFields intent,
            FanlightMotionFields motion,
            FanlightVariationFields variation,
            FanlightNoiseFields noise,
            FanlightRestFields rest,
            FanlightAudienceBodyFields audienceBody,
            FanlightDirectionFields direction,
            FanlightColorFields color,
            FanlightIntensityFields intensity)
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
        }

        internal static FanlightTimelineFieldMask From(FanlightIntentFields fields) =>
            new(fields, default, default, default, default, default, default, default, default);

        internal static FanlightTimelineFieldMask From(FanlightMotionFields fields) =>
            new(default, fields, default, default, default, default, default, default, default);

        internal static FanlightTimelineFieldMask From(FanlightVariationFields fields) =>
            new(default, default, fields, default, default, default, default, default, default);

        internal static FanlightTimelineFieldMask From(FanlightNoiseFields fields) =>
            new(default, default, default, fields, default, default, default, default, default);

        internal static FanlightTimelineFieldMask From(FanlightRestFields fields) =>
            new(default, default, default, default, fields, default, default, default, default);

        internal static FanlightTimelineFieldMask From(FanlightAudienceBodyFields fields) =>
            new(default, default, default, default, default, fields, default, default, default);

        internal static FanlightTimelineFieldMask From(FanlightDirectionFields fields) =>
            new(default, default, default, default, default, default, fields, default, default);

        internal static FanlightTimelineFieldMask From(FanlightColorFields fields) =>
            new(default, default, default, default, default, default, default, fields, default);

        internal static FanlightTimelineFieldMask From(FanlightIntensityFields fields) =>
            new(default, default, default, default, default, default, default, default, fields);
    }
}

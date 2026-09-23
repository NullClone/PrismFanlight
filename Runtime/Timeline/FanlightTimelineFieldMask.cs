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
            new(fields == (FanlightIntentFields)(-1) ? FanlightIntentFields.All : fields, default, default, default, default, default, default, default, default);

        internal static FanlightTimelineFieldMask From(FanlightMotionFields fields) =>
            new(default, fields == (FanlightMotionFields)(-1) ? FanlightMotionFields.All : fields, default, default, default, default, default, default, default);

        internal static FanlightTimelineFieldMask From(FanlightVariationFields fields) =>
            new(default, default, fields == (FanlightVariationFields)(-1) ? FanlightVariationFields.All : fields, default, default, default, default, default, default);

        internal static FanlightTimelineFieldMask From(FanlightNoiseFields fields) =>
            new(default, default, default, fields == (FanlightNoiseFields)(-1) ? FanlightNoiseFields.All : fields, default, default, default, default, default);

        internal static FanlightTimelineFieldMask From(FanlightRestFields fields) =>
            new(default, default, default, default, fields == (FanlightRestFields)(-1) ? FanlightRestFields.All : fields, default, default, default, default);

        internal static FanlightTimelineFieldMask From(FanlightAudienceBodyFields fields) =>
            new(default, default, default, default, default, fields == (FanlightAudienceBodyFields)(-1) ? FanlightAudienceBodyFields.All : fields, default, default, default);

        internal static FanlightTimelineFieldMask From(FanlightDirectionFields fields) =>
            new(default, default, default, default, default, default, fields == (FanlightDirectionFields)(-1) ? FanlightDirectionFields.All : fields, default, default);

        internal static FanlightTimelineFieldMask From(FanlightColorFields fields) =>
            new(default, default, default, default, default, default, default, fields == (FanlightColorFields)(-1) ? FanlightColorFields.All : fields, default);

        internal static FanlightTimelineFieldMask From(FanlightIntensityFields fields) =>
            new(default, default, default, default, default, default, default, default, fields == (FanlightIntensityFields)(-1) ? FanlightIntensityFields.All : fields);
    }
}

namespace PrismFanlight.Core
{
    internal readonly struct FanlightShowState
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

        internal uint GlobalSeed { get; }


        // Methods

        internal FanlightShowState(
            FanlightIntentState intent,
            FanlightMotionState motion,
            FanlightVariationState variation,
            FanlightNoiseState noise,
            FanlightRestState rest,
            FanlightAudienceBodyState audienceBody,
            FanlightDirectionState direction,
            FanlightColorState color,
            FanlightIntensityState intensity,
            FanlightVisibilityState visibility,
            uint globalSeed)
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
            GlobalSeed = globalSeed;
        }
    }
}

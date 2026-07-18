namespace PrismFanlight.Core
{
    internal readonly struct FanlightShowState
    {
        internal FanlightIntentState Intent { get; }
        internal FanlightGestureState Gesture { get; }
        internal FanlightPoseState Pose { get; }
        internal FanlightVariationState Variation { get; }
        internal FanlightNoiseState Noise { get; }
        internal FanlightRestState Rest { get; }
        internal FanlightAudienceBodyState AudienceBody { get; }
        internal FanlightDirectionState Direction { get; }
        internal FanlightPaletteState Palette { get; }
        internal FanlightVisibilityState Visibility { get; }
        internal uint GlobalSeed { get; }

        internal FanlightShowState(
            FanlightIntentState intent,
            FanlightGestureState gesture,
            FanlightPoseState pose,
            FanlightVariationState variation,
            FanlightNoiseState noise,
            FanlightRestState rest,
            FanlightAudienceBodyState audienceBody,
            FanlightDirectionState direction,
            FanlightPaletteState palette,
            FanlightVisibilityState visibility,
            uint globalSeed)
        {
            Intent = intent;
            Gesture = gesture;
            Pose = pose;
            Variation = variation;
            Noise = noise;
            Rest = rest;
            AudienceBody = audienceBody;
            Direction = direction;
            Palette = palette;
            Visibility = visibility;
            GlobalSeed = globalSeed;
        }
    }
}

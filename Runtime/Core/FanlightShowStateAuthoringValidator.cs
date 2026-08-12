#if UNITY_EDITOR
using System;

namespace PrismFanlight.Core
{
    internal static class FanlightShowStateAuthoringValidator
    {
        // Methods

        internal static FanlightIntentState Validate(FanlightIntentState value)
        {
            try
            {
                return new FanlightIntentState(
                    value.Energy,
                    value.Participation,
                    value.Synchronization,
                    value.Realism,
                    value.Reach);
            }
            catch (ArgumentException)
            {
                return FanlightShowStateDefaults.Intent();
            }
        }

        internal static FanlightMotionState Validate(FanlightMotionState value)
        {
            try
            {
                return new FanlightMotionState(
                    value.MotionAsset,
                    value.BeatsPerCycle,
                    value.PhaseOffsetBeats,
                    value.BlockDelayXBeats,
                    value.BlockDelayYBeats,
                    value.MotionAmount,
                    value.HeightBias,
                    value.SideScale,
                    value.ForwardScale,
                    value.WristDelayRatio,
                    value.Variation);
            }
            catch (ArgumentException)
            {
                return FanlightShowStateDefaults.Motion();
            }
        }

        internal static FanlightVariationState Validate(FanlightVariationState value)
        {
            try
            {
                return new FanlightVariationState(
                    value.StandingPositionSpread,
                    value.HeightVariation,
                    value.ArmExtensionVariation,
                    value.PenlightDirectionSpread,
                    value.ReactionDelaySeconds,
                    value.BeatJitterBeats,
                    value.EnergyResponse,
                    value.HandPositionSpread);
            }
            catch (ArgumentException)
            {
                return FanlightShowStateDefaults.Variation();
            }
        }

        internal static FanlightNoiseState Validate(FanlightNoiseState value)
        {
            try
            {
                return new FanlightNoiseState(
                    value.PhaseAmount,
                    value.PhaseRate,
                    value.PositionAmount,
                    value.DirectionAmount,
                    value.SpatialRate,
                    value.Octaves,
                    value.Persistence);
            }
            catch (ArgumentException)
            {
                return FanlightShowStateDefaults.Noise();
            }
        }

        internal static FanlightRestState Validate(FanlightRestState value)
        {
            try
            {
                if (value.DurationSeconds > value.CycleSeconds) return FanlightShowStateDefaults.Rest();

                return new FanlightRestState(
                    value.Probability,
                    value.MotionLevel,
                    value.CycleSeconds,
                    value.DurationSeconds,
                    value.FadeSeconds,
                    value.PhaseRandomness);
            }
            catch (ArgumentException)
            {
                return FanlightShowStateDefaults.Rest();
            }
        }

        internal static FanlightAudienceBodyState Validate(FanlightAudienceBodyState value)
        {
            try
            {
                return new FanlightAudienceBodyState(
                    value.Height,
                    value.Width,
                    value.HeadSize,
                    value.ShoulderHeightRatio,
                    value.ShoulderSideOffset,
                    value.ArmWidth,
                    value.ArmLengthLimit,
                    value.Bounce,
                    value.Sway);
            }
            catch (ArgumentException)
            {
                return FanlightShowStateDefaults.AudienceBody();
            }
        }

        internal static FanlightDirectionState Validate(FanlightDirectionState value)
        {
            try
            {
                return new FanlightDirectionState(value.Mode, value.Direction);
            }
            catch (ArgumentException)
            {
                return FanlightShowStateDefaults.Direction();
            }
        }
    }
}
#endif

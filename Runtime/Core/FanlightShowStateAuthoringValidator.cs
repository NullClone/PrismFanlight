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

        internal static FanlightGestureState Validate(FanlightGestureState value)
        {
            try
            {
                return new FanlightGestureState(
                    value.BeatsPerCycle,
                    value.PhaseOffsetBeats,
                    value.HoldRatio,
                    value.Crispness,
                    value.FollowThrough,
                    value.DownbeatAccent);
            }
            catch (ArgumentException)
            {
                return FanlightShowStateDefaults.Gesture();
            }
        }

        internal static FanlightPoseState Validate(FanlightPoseState value)
        {
            var fallback = FanlightShowStateDefaults.Pose();

            try
            {
                if (value.ArmLengthMinimum > value.ArmLengthMaximum
                    || value.AngleMinimumRadians > value.AngleMaximumRadians)
                {
                    return fallback;
                }

                var handReachScale = FanlightStateValidation.IsFinite(value.HandReachScale)
                                     && value.HandReachScale >= 0.01f
                    ? value.HandReachScale
                    : fallback.HandReachScale;

                return new FanlightPoseState(
                    value.HandZone,
                    value.HandHeightOffset,
                    value.HandForwardOffset,
                    handReachScale,
                    value.ArmLengthMinimum,
                    value.ArmLengthMaximum,
                    value.AngleMinimumRadians,
                    value.AngleMaximumRadians,
                    value.HorizontalRatio,
                    value.WristFrequencyMultiplier,
                    value.WristAngleRadians,
                    value.BodyLean);
            }
            catch (ArgumentException)
            {
                return fallback;
            }
        }

        internal static FanlightVariationState Validate(FanlightVariationState value)
        {
            try
            {
                return new FanlightVariationState(
                    value.SeatPosition,
                    value.BodyHeight,
                    value.ArmLength,
                    value.Angle,
                    value.DirectionSpread,
                    value.ReactionDelaySeconds,
                    value.BeatJitter,
                    value.BlockDelayXBeats,
                    value.BlockDelayYBeats,
                    value.EnergyResponse,
                    value.Speed,
                    value.BeatReactionDelaySeconds,
                    value.HandZone);
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
                    value.PhaseSpeed,
                    value.AxisAmount,
                    value.AxisSpeed,
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
                    value.HeightVariation,
                    value.Width,
                    value.HeadSize,
                    value.ShoulderHeightRatio,
                    value.ShoulderSideOffset,
                    value.ArmWidth,
                    value.ArmLengthLimit,
                    value.UpperBodyLeanMaximumRadians,
                    value.UpperBodyLean,
                    value.Bounce,
                    value.Sway,
                    value.MotionSpeed,
                    value.LeanMotion);
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
                return new FanlightDirectionState(value.Mode, value.WorldYawDegrees, value.AimStrength);
            }
            catch (ArgumentException)
            {
                return FanlightShowStateDefaults.Direction();
            }
        }

        internal static FanlightPaletteState Validate(FanlightPaletteState value)
        {
            try
            {
                return new FanlightPaletteState(
                    value.Slot1,
                    value.Slot2,
                    value.Slot3,
                    value.Slot4,
                    value.Slot5,
                    value.Slot6,
                    value.GlobalIntensity,
                    value.RandomIntensity);
            }
            catch (ArgumentException)
            {
                return FanlightShowStateDefaults.Palette();
            }
        }
    }
}
#endif

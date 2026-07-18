using System;
using UnityEngine;

namespace PrismFanlight.Core
{
    internal static class FanlightShowStatePatcher
    {
        internal static FanlightShowState Apply(FanlightShowState state, FanlightShowPatch patch, float weight)
        {
            weight = Mathf.Clamp01(weight);

            return new FanlightShowState(
                Apply(state.Intent, patch.Intent, weight),
                Apply(state.Gesture, patch.Gesture, weight),
                Apply(state.Pose, patch.Pose, weight),
                Apply(state.Variation, patch.Variation, weight),
                Apply(state.Noise, patch.Noise, weight),
                Apply(state.Rest, patch.Rest, weight),
                Apply(state.AudienceBody, patch.AudienceBody, weight),
                Apply(state.Direction, patch.Direction, weight),
                Apply(state.Palette, patch.Palette, weight),
                Apply(state.Visibility, patch.Visibility, weight),
                state.GlobalSeed);
        }

        internal static FanlightShowState Validate(FanlightShowState state)
        {
            if (state.Gesture.AttackRatio > state.Gesture.ReturnRatio)
                throw new InvalidOperationException("Gesture attack ratio must not exceed return ratio.");
            if (state.Pose.ArmLengthMinimum > state.Pose.ArmLengthMaximum)
                throw new InvalidOperationException("Minimum arm length must not exceed maximum arm length.");
            if (state.Pose.AngleMinimumRadians > state.Pose.AngleMaximumRadians)
                throw new InvalidOperationException("Minimum pose angle must not exceed maximum pose angle.");
            if (state.Rest.DurationSeconds > state.Rest.CycleSeconds)
                throw new InvalidOperationException("Rest duration must not exceed its cycle.");

            return new FanlightShowState(
                Apply(state.Intent, new FanlightIntentPatch(FanlightIntentFields.All, state.Intent), 1f),
                Apply(state.Gesture, new FanlightGesturePatch(FanlightGestureFields.All, state.Gesture), 1f),
                Apply(state.Pose, new FanlightPosePatch(FanlightPoseFields.All, state.Pose), 1f),
                Apply(state.Variation, new FanlightVariationPatch(FanlightVariationFields.All, state.Variation), 1f),
                Apply(state.Noise, new FanlightNoisePatch(FanlightNoiseFields.All, state.Noise), 1f),
                Apply(state.Rest, new FanlightRestPatch(FanlightRestFields.All, state.Rest), 1f),
                Apply(state.AudienceBody, new FanlightAudienceBodyPatch(FanlightAudienceBodyFields.All, state.AudienceBody), 1f),
                Apply(state.Direction, new FanlightDirectionPatch(FanlightDirectionFields.All, state.Direction), 1f),
                Apply(state.Palette, new FanlightPalettePatch(FanlightPaletteFields.All, state.Palette), 1f),
                Apply(state.Visibility, new FanlightVisibilityPatch(FanlightVisibilityFields.All, state.Visibility), 1f),
                state.GlobalSeed);
        }

        internal static FanlightIntentState Apply(FanlightIntentState current, FanlightIntentPatch patch, float weight)
        {
            ValidateMask((int)patch.Fields, (int)FanlightIntentFields.All, nameof(patch));

            var value = patch.Value;
            return new FanlightIntentState(
                Has(patch.Fields, FanlightIntentFields.Energy) ? Lerp(current.Energy, value.Energy, weight) : current.Energy,
                Has(patch.Fields, FanlightIntentFields.Participation) ? Lerp(current.Participation, value.Participation, weight) : current.Participation,
                Has(patch.Fields, FanlightIntentFields.Synchronization) ? Lerp(current.Synchronization, value.Synchronization, weight) : current.Synchronization,
                Has(patch.Fields, FanlightIntentFields.Realism) ? Lerp(current.Realism, value.Realism, weight) : current.Realism,
                Has(patch.Fields, FanlightIntentFields.Reach) ? Lerp(current.Reach, value.Reach, weight) : current.Reach);
        }

        internal static FanlightGestureState Apply(FanlightGestureState current, FanlightGesturePatch patch, float weight)
        {
            ValidateMask((int)patch.Fields, (int)FanlightGestureFields.All, nameof(patch));

            var value = patch.Value;
            var discrete = weight >= 0.5f;
            return new FanlightGestureState(
                Has(patch.Fields, FanlightGestureFields.GestureId) && discrete ? value.GestureId : current.GestureId,
                Has(patch.Fields, FanlightGestureFields.BeatsPerCycle) ? Lerp(current.BeatsPerCycle, value.BeatsPerCycle, weight) : current.BeatsPerCycle,
                Has(patch.Fields, FanlightGestureFields.PhaseOffsetBeats) ? Lerp(current.PhaseOffsetBeats, value.PhaseOffsetBeats, weight) : current.PhaseOffsetBeats,
                Has(patch.Fields, FanlightGestureFields.AttackRatio) ? Lerp(current.AttackRatio, value.AttackRatio, weight) : current.AttackRatio,
                Has(patch.Fields, FanlightGestureFields.HoldRatio) ? Lerp(current.HoldRatio, value.HoldRatio, weight) : current.HoldRatio,
                Has(patch.Fields, FanlightGestureFields.ReturnRatio) ? Lerp(current.ReturnRatio, value.ReturnRatio, weight) : current.ReturnRatio,
                Has(patch.Fields, FanlightGestureFields.Crispness) ? Lerp(current.Crispness, value.Crispness, weight) : current.Crispness,
                Has(patch.Fields, FanlightGestureFields.FollowThrough) ? Lerp(current.FollowThrough, value.FollowThrough, weight) : current.FollowThrough,
                Has(patch.Fields, FanlightGestureFields.DownbeatAccent) ? Lerp(current.DownbeatAccent, value.DownbeatAccent, weight) : current.DownbeatAccent);
        }

        internal static FanlightPoseState Apply(FanlightPoseState current, FanlightPosePatch patch, float weight)
        {
            ValidateMask((int)patch.Fields, (int)FanlightPoseFields.All, nameof(patch));

            var value = patch.Value;
            var discrete = weight >= 0.5f;
            return new FanlightPoseState(
                Has(patch.Fields, FanlightPoseFields.HandZone) && discrete ? value.HandZone : current.HandZone,
                Has(patch.Fields, FanlightPoseFields.HandHeightOffset) ? Lerp(current.HandHeightOffset, value.HandHeightOffset, weight) : current.HandHeightOffset,
                Has(patch.Fields, FanlightPoseFields.HandForwardOffset) ? Lerp(current.HandForwardOffset, value.HandForwardOffset, weight) : current.HandForwardOffset,
                Has(patch.Fields, FanlightPoseFields.HandReachScale) ? Lerp(current.HandReachScale, value.HandReachScale, weight) : current.HandReachScale,
                Has(patch.Fields, FanlightPoseFields.ArmLengthMinimum) ? Lerp(current.ArmLengthMinimum, value.ArmLengthMinimum, weight) : current.ArmLengthMinimum,
                Has(patch.Fields, FanlightPoseFields.ArmLengthMaximum) ? Lerp(current.ArmLengthMaximum, value.ArmLengthMaximum, weight) : current.ArmLengthMaximum,
                Has(patch.Fields, FanlightPoseFields.AngleMinimumRadians) ? Lerp(current.AngleMinimumRadians, value.AngleMinimumRadians, weight) : current.AngleMinimumRadians,
                Has(patch.Fields, FanlightPoseFields.AngleMaximumRadians) ? Lerp(current.AngleMaximumRadians, value.AngleMaximumRadians, weight) : current.AngleMaximumRadians,
                Has(patch.Fields, FanlightPoseFields.HorizontalRatio) ? Lerp(current.HorizontalRatio, value.HorizontalRatio, weight) : current.HorizontalRatio,
                Has(patch.Fields, FanlightPoseFields.WristFrequencyMultiplier) ? Lerp(current.WristFrequencyMultiplier, value.WristFrequencyMultiplier, weight) : current.WristFrequencyMultiplier,
                Has(patch.Fields, FanlightPoseFields.WristAngleRadians) ? Lerp(current.WristAngleRadians, value.WristAngleRadians, weight) : current.WristAngleRadians,
                Has(patch.Fields, FanlightPoseFields.BodyLean) ? Lerp(current.BodyLean, value.BodyLean, weight) : current.BodyLean);
        }

        internal static FanlightVariationState Apply(FanlightVariationState current, FanlightVariationPatch patch, float weight)
        {
            ValidateMask((int)patch.Fields, (int)FanlightVariationFields.All, nameof(patch));

            var value = patch.Value;
            return new FanlightVariationState(
                Has(patch.Fields, FanlightVariationFields.SeatPosition) ? Lerp(current.SeatPosition, value.SeatPosition, weight) : current.SeatPosition,
                Has(patch.Fields, FanlightVariationFields.BodyHeight) ? Lerp(current.BodyHeight, value.BodyHeight, weight) : current.BodyHeight,
                Has(patch.Fields, FanlightVariationFields.ArmLength) ? Lerp(current.ArmLength, value.ArmLength, weight) : current.ArmLength,
                Has(patch.Fields, FanlightVariationFields.Angle) ? Lerp(current.Angle, value.Angle, weight) : current.Angle,
                Has(patch.Fields, FanlightVariationFields.DirectionSpread) ? Lerp(current.DirectionSpread, value.DirectionSpread, weight) : current.DirectionSpread,
                Has(patch.Fields, FanlightVariationFields.ReactionDelaySeconds) ? Lerp(current.ReactionDelaySeconds, value.ReactionDelaySeconds, weight) : current.ReactionDelaySeconds,
                Has(patch.Fields, FanlightVariationFields.BeatJitter) ? Lerp(current.BeatJitter, value.BeatJitter, weight) : current.BeatJitter,
                Has(patch.Fields, FanlightVariationFields.BlockDelayXBeats) ? Lerp(current.BlockDelayXBeats, value.BlockDelayXBeats, weight) : current.BlockDelayXBeats,
                Has(patch.Fields, FanlightVariationFields.BlockDelayYBeats) ? Lerp(current.BlockDelayYBeats, value.BlockDelayYBeats, weight) : current.BlockDelayYBeats,
                Has(patch.Fields, FanlightVariationFields.EnergyResponse) ? Lerp(current.EnergyResponse, value.EnergyResponse, weight) : current.EnergyResponse,
                Has(patch.Fields, FanlightVariationFields.Speed) ? Lerp(current.Speed, value.Speed, weight) : current.Speed,
                Has(patch.Fields, FanlightVariationFields.BeatReactionDelaySeconds) ? Lerp(current.BeatReactionDelaySeconds, value.BeatReactionDelaySeconds, weight) : current.BeatReactionDelaySeconds,
                Has(patch.Fields, FanlightVariationFields.HandZone) ? Lerp(current.HandZone, value.HandZone, weight) : current.HandZone);
        }

        internal static FanlightNoiseState Apply(FanlightNoiseState current, FanlightNoisePatch patch, float weight)
        {
            ValidateMask((int)patch.Fields, (int)FanlightNoiseFields.All, nameof(patch));

            var value = patch.Value;
            return new FanlightNoiseState(
                Has(patch.Fields, FanlightNoiseFields.PhaseAmount) ? Lerp(current.PhaseAmount, value.PhaseAmount, weight) : current.PhaseAmount,
                Has(patch.Fields, FanlightNoiseFields.PhaseSpeed) ? Lerp(current.PhaseSpeed, value.PhaseSpeed, weight) : current.PhaseSpeed,
                Has(patch.Fields, FanlightNoiseFields.AxisAmount) ? Lerp(current.AxisAmount, value.AxisAmount, weight) : current.AxisAmount,
                Has(patch.Fields, FanlightNoiseFields.AxisSpeed) ? Lerp(current.AxisSpeed, value.AxisSpeed, weight) : current.AxisSpeed,
                Has(patch.Fields, FanlightNoiseFields.Octaves) && weight >= 0.5f ? value.Octaves : current.Octaves,
                Has(patch.Fields, FanlightNoiseFields.Persistence) ? Lerp(current.Persistence, value.Persistence, weight) : current.Persistence);
        }

        internal static FanlightRestState Apply(FanlightRestState current, FanlightRestPatch patch, float weight)
        {
            ValidateMask((int)patch.Fields, (int)FanlightRestFields.All, nameof(patch));

            var value = patch.Value;
            return new FanlightRestState(
                Has(patch.Fields, FanlightRestFields.Probability) ? Lerp(current.Probability, value.Probability, weight) : current.Probability,
                Has(patch.Fields, FanlightRestFields.MotionLevel) ? Lerp(current.MotionLevel, value.MotionLevel, weight) : current.MotionLevel,
                Has(patch.Fields, FanlightRestFields.CycleSeconds) ? Lerp(current.CycleSeconds, value.CycleSeconds, weight) : current.CycleSeconds,
                Has(patch.Fields, FanlightRestFields.DurationSeconds) ? Lerp(current.DurationSeconds, value.DurationSeconds, weight) : current.DurationSeconds,
                Has(patch.Fields, FanlightRestFields.FadeSeconds) ? Lerp(current.FadeSeconds, value.FadeSeconds, weight) : current.FadeSeconds,
                Has(patch.Fields, FanlightRestFields.PhaseRandomness) ? Lerp(current.PhaseRandomness, value.PhaseRandomness, weight) : current.PhaseRandomness);
        }

        internal static FanlightAudienceBodyState Apply(FanlightAudienceBodyState current, FanlightAudienceBodyPatch patch, float weight)
        {
            ValidateMask((int)patch.Fields, (int)FanlightAudienceBodyFields.All, nameof(patch));

            var value = patch.Value;
            return new FanlightAudienceBodyState(
                Has(patch.Fields, FanlightAudienceBodyFields.Height) ? Lerp(current.Height, value.Height, weight) : current.Height,
                Has(patch.Fields, FanlightAudienceBodyFields.HeightVariation) ? Lerp(current.HeightVariation, value.HeightVariation, weight) : current.HeightVariation,
                Has(patch.Fields, FanlightAudienceBodyFields.Width) ? Lerp(current.Width, value.Width, weight) : current.Width,
                Has(patch.Fields, FanlightAudienceBodyFields.HeadSize) ? Lerp(current.HeadSize, value.HeadSize, weight) : current.HeadSize,
                Has(patch.Fields, FanlightAudienceBodyFields.ShoulderHeightRatio) ? Lerp(current.ShoulderHeightRatio, value.ShoulderHeightRatio, weight) : current.ShoulderHeightRatio,
                Has(patch.Fields, FanlightAudienceBodyFields.ShoulderSideOffset) ? Lerp(current.ShoulderSideOffset, value.ShoulderSideOffset, weight) : current.ShoulderSideOffset,
                Has(patch.Fields, FanlightAudienceBodyFields.ArmWidth) ? Lerp(current.ArmWidth, value.ArmWidth, weight) : current.ArmWidth,
                Has(patch.Fields, FanlightAudienceBodyFields.ArmLengthLimit) ? Lerp(current.ArmLengthLimit, value.ArmLengthLimit, weight) : current.ArmLengthLimit,
                Has(patch.Fields, FanlightAudienceBodyFields.UpperBodyLeanMaximumRadians) ? Lerp(current.UpperBodyLeanMaximumRadians, value.UpperBodyLeanMaximumRadians, weight) : current.UpperBodyLeanMaximumRadians,
                Has(patch.Fields, FanlightAudienceBodyFields.UpperBodyLean) ? Lerp(current.UpperBodyLean, value.UpperBodyLean, weight) : current.UpperBodyLean,
                Has(patch.Fields, FanlightAudienceBodyFields.Bounce) ? Lerp(current.Bounce, value.Bounce, weight) : current.Bounce,
                Has(patch.Fields, FanlightAudienceBodyFields.Sway) ? Lerp(current.Sway, value.Sway, weight) : current.Sway,
                Has(patch.Fields, FanlightAudienceBodyFields.MotionSpeed) ? Lerp(current.MotionSpeed, value.MotionSpeed, weight) : current.MotionSpeed,
                Has(patch.Fields, FanlightAudienceBodyFields.LeanMotion) ? Lerp(current.LeanMotion, value.LeanMotion, weight) : current.LeanMotion);
        }

        internal static FanlightDirectionState Apply(FanlightDirectionState current, FanlightDirectionPatch patch, float weight)
        {
            ValidateMask((int)patch.Fields, (int)FanlightDirectionFields.All, nameof(patch));

            var value = patch.Value;
            return new FanlightDirectionState(
                Has(patch.Fields, FanlightDirectionFields.Mode) && weight >= 0.5f ? value.Mode : current.Mode,
                Has(patch.Fields, FanlightDirectionFields.WorldYawDegrees) ? Mathf.LerpAngle(current.WorldYawDegrees, value.WorldYawDegrees, weight) : current.WorldYawDegrees,
                Has(patch.Fields, FanlightDirectionFields.AimStrength) ? Lerp(current.AimStrength, value.AimStrength, weight) : current.AimStrength);
        }

        internal static FanlightPaletteState Apply(FanlightPaletteState current, FanlightPalettePatch patch, float weight)
        {
            ValidateMask((int)patch.Fields, (int)FanlightPaletteFields.All, nameof(patch));

            var value = patch.Value;
            return new FanlightPaletteState(
                Has(patch.Fields, FanlightPaletteFields.Slot1) ? Color.LerpUnclamped(current.Slot1, value.Slot1, weight) : current.Slot1,
                Has(patch.Fields, FanlightPaletteFields.Slot2) ? Color.LerpUnclamped(current.Slot2, value.Slot2, weight) : current.Slot2,
                Has(patch.Fields, FanlightPaletteFields.Slot3) ? Color.LerpUnclamped(current.Slot3, value.Slot3, weight) : current.Slot3,
                Has(patch.Fields, FanlightPaletteFields.Slot4) ? Color.LerpUnclamped(current.Slot4, value.Slot4, weight) : current.Slot4,
                Has(patch.Fields, FanlightPaletteFields.Slot5) ? Color.LerpUnclamped(current.Slot5, value.Slot5, weight) : current.Slot5,
                Has(patch.Fields, FanlightPaletteFields.Slot6) ? Color.LerpUnclamped(current.Slot6, value.Slot6, weight) : current.Slot6,
                Has(patch.Fields, FanlightPaletteFields.GlobalIntensity) ? Lerp(current.GlobalIntensity, value.GlobalIntensity, weight) : current.GlobalIntensity,
                Has(patch.Fields, FanlightPaletteFields.RandomIntensity) ? Lerp(current.RandomIntensity, value.RandomIntensity, weight) : current.RandomIntensity);
        }

        internal static FanlightVisibilityState Apply(FanlightVisibilityState current, FanlightVisibilityPatch patch, float weight)
        {
            ValidateMask((int)patch.Fields, (int)FanlightVisibilityFields.All, nameof(patch));

            var value = patch.Value;
            var discrete = weight >= 0.5f;
            return new FanlightVisibilityState(
                Has(patch.Fields, FanlightVisibilityFields.PenlightsEnabled) && discrete ? value.PenlightsEnabled : current.PenlightsEnabled,
                Has(patch.Fields, FanlightVisibilityFields.AudienceBodiesEnabled) && discrete ? value.AudienceBodiesEnabled : current.AudienceBodiesEnabled);
        }


        private static bool Has(FanlightIntentFields fields, FanlightIntentFields field) => (fields & field) != 0;

        private static bool Has(FanlightGestureFields fields, FanlightGestureFields field) => (fields & field) != 0;

        private static bool Has(FanlightPoseFields fields, FanlightPoseFields field) => (fields & field) != 0;

        private static bool Has(FanlightVariationFields fields, FanlightVariationFields field) => (fields & field) != 0;

        private static bool Has(FanlightNoiseFields fields, FanlightNoiseFields field) => (fields & field) != 0;

        private static bool Has(FanlightRestFields fields, FanlightRestFields field) => (fields & field) != 0;

        private static bool Has(FanlightAudienceBodyFields fields, FanlightAudienceBodyFields field) => (fields & field) != 0;

        private static bool Has(FanlightDirectionFields fields, FanlightDirectionFields field) => (fields & field) != 0;

        private static bool Has(FanlightPaletteFields fields, FanlightPaletteFields field) => (fields & field) != 0;

        private static bool Has(FanlightVisibilityFields fields, FanlightVisibilityFields field) => (fields & field) != 0;


        private static void ValidateMask(int fields, int all, string name)
        {
            if ((fields & ~all) != 0) throw new ArgumentOutOfRangeException(name);
        }

        private static float Lerp(float current, float incoming, float weight) => current + (incoming - current) * weight;
    }
}

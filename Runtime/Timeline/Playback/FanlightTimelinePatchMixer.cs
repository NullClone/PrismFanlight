using System;
using PrismFanlight.Core;

namespace PrismFanlight.Timeline
{
    internal static class FanlightTimelinePatchMixer
    {
        internal static bool HasFields(FanlightTimelinePatchKind kind, FanlightTimelineFieldMask fields)
        {
            return kind switch
            {
                FanlightTimelinePatchKind.Intent => fields.Intent != FanlightIntentFields.None,
                FanlightTimelinePatchKind.Gesture => fields.Gesture != FanlightGestureFields.None,
                FanlightTimelinePatchKind.Pose => fields.Pose != FanlightPoseFields.None,
                FanlightTimelinePatchKind.Variation => fields.Variation != FanlightVariationFields.None,
                FanlightTimelinePatchKind.Noise => fields.Noise != FanlightNoiseFields.None,
                FanlightTimelinePatchKind.Rest => fields.Rest != FanlightRestFields.None,
                FanlightTimelinePatchKind.AudienceBody => fields.AudienceBody != FanlightAudienceBodyFields.None,
                FanlightTimelinePatchKind.Direction => fields.Direction != FanlightDirectionFields.None,
                FanlightTimelinePatchKind.Palette => fields.Palette != FanlightPaletteFields.None,
                FanlightTimelinePatchKind.Visibility => fields.Visibility != FanlightVisibilityFields.None,
                _ => throw new ArgumentOutOfRangeException(nameof(kind))
            };
        }

        internal static bool TryBlend(
            FanlightTimelinePatchKind kind,
            FanlightTimelineFieldMask fieldMask,
            ReadOnlySpan<FanlightTimelineClipSample> samples,
            out FanlightShowPatch patch)
        {
            return kind switch
            {
                FanlightTimelinePatchKind.Intent => TryBlendIntent(fieldMask.Intent, samples, out patch),
                FanlightTimelinePatchKind.Gesture => TryBlendGesture(fieldMask.Gesture, samples, out patch),
                FanlightTimelinePatchKind.Pose => TryBlendPose(fieldMask.Pose, samples, out patch),
                FanlightTimelinePatchKind.Variation => TryBlendVariation(fieldMask.Variation, samples, out patch),
                FanlightTimelinePatchKind.Noise => TryBlendNoise(fieldMask.Noise, samples, out patch),
                FanlightTimelinePatchKind.Rest => TryBlendRest(fieldMask.Rest, samples, out patch),
                FanlightTimelinePatchKind.AudienceBody => TryBlendAudienceBody(fieldMask.AudienceBody, samples, out patch),
                FanlightTimelinePatchKind.Direction => TryBlendDirection(fieldMask.Direction, samples, out patch),
                FanlightTimelinePatchKind.Palette => TryBlendPalette(fieldMask.Palette, samples, out patch),
                FanlightTimelinePatchKind.Visibility => TryBlendVisibility(fieldMask.Visibility, samples, out patch),
                _ => throw new ArgumentOutOfRangeException(nameof(kind))
            };
        }

        private static bool TryBlendIntent(
            FanlightIntentFields fields,
            ReadOnlySpan<FanlightTimelineClipSample> samples,
            out FanlightShowPatch patch)
        {
            ValidateMask((int)fields, (int)FanlightIntentFields.All);

            var energy = new FanlightWeightedFloat();
            var participation = new FanlightWeightedFloat();
            var synchronization = new FanlightWeightedFloat();
            var realism = new FanlightWeightedFloat();
            var reach = new FanlightWeightedFloat();

            for (var i = 0; i < samples.Length; i++)
            {
                var sample = samples[i];
                var sourceValue = sample.Value.Intent;

                if (Has(fields, FanlightIntentFields.Energy)) energy.Add(sourceValue.Energy, sample.Weight);
                if (Has(fields, FanlightIntentFields.Participation)) participation.Add(sourceValue.Participation, sample.Weight);
                if (Has(fields, FanlightIntentFields.Synchronization)) synchronization.Add(sourceValue.Synchronization, sample.Weight);
                if (Has(fields, FanlightIntentFields.Realism)) realism.Add(sourceValue.Realism, sample.Weight);
                if (Has(fields, FanlightIntentFields.Reach)) reach.Add(sourceValue.Reach, sample.Weight);
            }

            if (fields == FanlightIntentFields.None)
            {
                patch = default;
                return false;
            }

            var fallback = FanlightTimelineDefaults.IntentState();

            var value = new FanlightIntentState(
                energy.Value(fallback.Energy),
                participation.Value(fallback.Participation),
                synchronization.Value(fallback.Synchronization),
                realism.Value(fallback.Realism),
                reach.Value(fallback.Reach)
            );

            patch = new FanlightShowPatch(
                new FanlightIntentPatch(fields, value),
                default,
                default,
                default,
                default,
                default,
                default,
                default,
                default,
                default
            );
            return true;
        }

        private static bool TryBlendGesture(
            FanlightGestureFields fields,
            ReadOnlySpan<FanlightTimelineClipSample> samples,
            out FanlightShowPatch patch)
        {
            ValidateMask((int)fields, (int)FanlightGestureFields.All);

            var beatsPerCycle = new FanlightWeightedFloat();
            var phaseOffsetBeats = new FanlightWeightedFloat();
            var holdRatio = new FanlightWeightedFloat();
            var crispness = new FanlightWeightedFloat();
            var followThrough = new FanlightWeightedFloat();
            var downbeatAccent = new FanlightWeightedFloat();

            for (var i = 0; i < samples.Length; i++)
            {
                var sample = samples[i];
                var sourceValue = sample.Value.Gesture;
                if (Has(fields, FanlightGestureFields.BeatsPerCycle)) beatsPerCycle.Add(sourceValue.BeatsPerCycle, sample.Weight);
                if (Has(fields, FanlightGestureFields.PhaseOffsetBeats)) phaseOffsetBeats.Add(sourceValue.PhaseOffsetBeats, sample.Weight);
                if (Has(fields, FanlightGestureFields.HoldRatio)) holdRatio.Add(sourceValue.HoldRatio, sample.Weight);
                if (Has(fields, FanlightGestureFields.Crispness)) crispness.Add(sourceValue.Crispness, sample.Weight);
                if (Has(fields, FanlightGestureFields.FollowThrough)) followThrough.Add(sourceValue.FollowThrough, sample.Weight);
                if (Has(fields, FanlightGestureFields.DownbeatAccent)) downbeatAccent.Add(sourceValue.DownbeatAccent, sample.Weight);
            }

            if (fields == FanlightGestureFields.None)
            {
                patch = default;
                return false;
            }

            var fallback = FanlightTimelineDefaults.GestureState();
            var value = new FanlightGestureState(
                beatsPerCycle.Value(fallback.BeatsPerCycle),
                phaseOffsetBeats.Value(fallback.PhaseOffsetBeats),
                holdRatio.Value(fallback.HoldRatio),
                crispness.Value(fallback.Crispness),
                followThrough.Value(fallback.FollowThrough),
                downbeatAccent.Value(fallback.DownbeatAccent)
            );

            patch = new FanlightShowPatch(
                default,
                new FanlightGesturePatch(fields, value),
                default,
                default,
                default,
                default,
                default,
                default,
                default,
                default
            );

            return true;
        }

        private static bool TryBlendPose(
            FanlightPoseFields fields,
            ReadOnlySpan<FanlightTimelineClipSample> samples,
            out FanlightShowPatch patch)
        {
            ValidateMask((int)fields, (int)FanlightPoseFields.All);

            var handZone = new FanlightDiscreteValue<FanlightHandZone>();
            var handHeightOffset = new FanlightWeightedFloat();
            var handForwardOffset = new FanlightWeightedFloat();
            var handReachScale = new FanlightWeightedFloat();
            var armLengthMinimum = new FanlightWeightedFloat();
            var armLengthMaximum = new FanlightWeightedFloat();
            var angleMinimumRadians = new FanlightWeightedAngle();
            var angleMaximumRadians = new FanlightWeightedAngle();
            var horizontalRatio = new FanlightWeightedFloat();
            var wristFrequencyMultiplier = new FanlightWeightedFloat();
            var wristAngleRadians = new FanlightWeightedFloat();
            var bodyLean = new FanlightWeightedFloat();

            for (var i = 0; i < samples.Length; i++)
            {
                var sample = samples[i];
                var sourceValue = sample.Value.Pose;
                if (Has(fields, FanlightPoseFields.HandZone)) handZone.Consider(sourceValue.HandZone, sample.Weight, sample.StableClipId);
                if (Has(fields, FanlightPoseFields.HandHeightOffset)) handHeightOffset.Add(sourceValue.HandHeightOffset, sample.Weight);
                if (Has(fields, FanlightPoseFields.HandForwardOffset)) handForwardOffset.Add(sourceValue.HandForwardOffset, sample.Weight);
                if (Has(fields, FanlightPoseFields.HandReachScale)) handReachScale.Add(sourceValue.HandReachScale, sample.Weight);
                if (Has(fields, FanlightPoseFields.ArmLengthMinimum)) armLengthMinimum.Add(sourceValue.ArmLengthMinimum, sample.Weight);
                if (Has(fields, FanlightPoseFields.ArmLengthMaximum)) armLengthMaximum.Add(sourceValue.ArmLengthMaximum, sample.Weight);
                if (Has(fields, FanlightPoseFields.AngleMinimumRadians)) angleMinimumRadians.AddRadians(sourceValue.AngleMinimumRadians, sample.Weight);
                if (Has(fields, FanlightPoseFields.AngleMaximumRadians)) angleMaximumRadians.AddRadians(sourceValue.AngleMaximumRadians, sample.Weight);
                if (Has(fields, FanlightPoseFields.HorizontalRatio)) horizontalRatio.Add(sourceValue.HorizontalRatio, sample.Weight);
                if (Has(fields, FanlightPoseFields.WristFrequencyMultiplier)) wristFrequencyMultiplier.Add(sourceValue.WristFrequencyMultiplier, sample.Weight);
                if (Has(fields, FanlightPoseFields.WristAngleRadians)) wristAngleRadians.Add(sourceValue.WristAngleRadians, sample.Weight);
                if (Has(fields, FanlightPoseFields.BodyLean)) bodyLean.Add(sourceValue.BodyLean, sample.Weight);
            }

            if (fields == FanlightPoseFields.None)
            {
                patch = default;
                return false;
            }

            var fallback = FanlightTimelineDefaults.PoseState();
            var value = new FanlightPoseState(
                handZone.Value(fallback.HandZone),
                handHeightOffset.Value(fallback.HandHeightOffset),
                handForwardOffset.Value(fallback.HandForwardOffset),
                handReachScale.Value(fallback.HandReachScale),
                armLengthMinimum.Value(fallback.ArmLengthMinimum),
                armLengthMaximum.Value(fallback.ArmLengthMaximum),
                angleMinimumRadians.ValueRadians(fallback.AngleMinimumRadians),
                angleMaximumRadians.ValueRadians(fallback.AngleMaximumRadians),
                horizontalRatio.Value(fallback.HorizontalRatio),
                wristFrequencyMultiplier.Value(fallback.WristFrequencyMultiplier),
                wristAngleRadians.Value(fallback.WristAngleRadians),
                bodyLean.Value(fallback.BodyLean)
            );

            patch = new FanlightShowPatch(
                default,
                default,
                new FanlightPosePatch(fields, value),
                default,
                default,
                default,
                default,
                default,
                default,
                default
            );

            return true;
        }

        private static bool TryBlendVariation(
            FanlightVariationFields fields,
            ReadOnlySpan<FanlightTimelineClipSample> samples,
            out FanlightShowPatch patch)
        {
            ValidateMask((int)fields, (int)FanlightVariationFields.All);

            var seatPosition = new FanlightWeightedFloat();
            var bodyHeight = new FanlightWeightedFloat();
            var armLength = new FanlightWeightedFloat();
            var angle = new FanlightWeightedFloat();
            var directionSpread = new FanlightWeightedFloat();
            var reactionDelaySeconds = new FanlightWeightedFloat();
            var beatJitter = new FanlightWeightedFloat();
            var blockDelayXBeats = new FanlightWeightedFloat();
            var blockDelayYBeats = new FanlightWeightedFloat();
            var energyResponse = new FanlightWeightedFloat();
            var speed = new FanlightWeightedFloat();
            var beatReactionDelaySeconds = new FanlightWeightedFloat();
            var handZone = new FanlightWeightedFloat();

            for (var i = 0; i < samples.Length; i++)
            {
                var sample = samples[i];
                var sourceValue = sample.Value.Variation;
                if (Has(fields, FanlightVariationFields.SeatPosition)) seatPosition.Add(sourceValue.SeatPosition, sample.Weight);
                if (Has(fields, FanlightVariationFields.BodyHeight)) bodyHeight.Add(sourceValue.BodyHeight, sample.Weight);
                if (Has(fields, FanlightVariationFields.ArmLength)) armLength.Add(sourceValue.ArmLength, sample.Weight);
                if (Has(fields, FanlightVariationFields.Angle)) angle.Add(sourceValue.Angle, sample.Weight);
                if (Has(fields, FanlightVariationFields.DirectionSpread)) directionSpread.Add(sourceValue.DirectionSpread, sample.Weight);
                if (Has(fields, FanlightVariationFields.ReactionDelaySeconds)) reactionDelaySeconds.Add(sourceValue.ReactionDelaySeconds, sample.Weight);
                if (Has(fields, FanlightVariationFields.BeatJitter)) beatJitter.Add(sourceValue.BeatJitter, sample.Weight);
                if (Has(fields, FanlightVariationFields.BlockDelayXBeats)) blockDelayXBeats.Add(sourceValue.BlockDelayXBeats, sample.Weight);
                if (Has(fields, FanlightVariationFields.BlockDelayYBeats)) blockDelayYBeats.Add(sourceValue.BlockDelayYBeats, sample.Weight);
                if (Has(fields, FanlightVariationFields.EnergyResponse)) energyResponse.Add(sourceValue.EnergyResponse, sample.Weight);
                if (Has(fields, FanlightVariationFields.Speed)) speed.Add(sourceValue.Speed, sample.Weight);
                if (Has(fields, FanlightVariationFields.BeatReactionDelaySeconds)) beatReactionDelaySeconds.Add(sourceValue.BeatReactionDelaySeconds, sample.Weight);
                if (Has(fields, FanlightVariationFields.HandZone)) handZone.Add(sourceValue.HandZone, sample.Weight);
            }

            if (fields == FanlightVariationFields.None)
            {
                patch = default;
                return false;
            }

            var fallback = FanlightTimelineDefaults.VariationState();
            var value = new FanlightVariationState(
                seatPosition.Value(fallback.SeatPosition),
                bodyHeight.Value(fallback.BodyHeight),
                armLength.Value(fallback.ArmLength),
                angle.Value(fallback.Angle),
                directionSpread.Value(fallback.DirectionSpread),
                reactionDelaySeconds.Value(fallback.ReactionDelaySeconds),
                beatJitter.Value(fallback.BeatJitter),
                blockDelayXBeats.Value(fallback.BlockDelayXBeats),
                blockDelayYBeats.Value(fallback.BlockDelayYBeats),
                energyResponse.Value(fallback.EnergyResponse),
                speed.Value(fallback.Speed),
                beatReactionDelaySeconds.Value(fallback.BeatReactionDelaySeconds),
                handZone.Value(fallback.HandZone)
            );

            patch = new FanlightShowPatch(
                default,
                default,
                default,
                new FanlightVariationPatch(fields, value),
                default,
                default,
                default,
                default,
                default,
                default
            );

            return true;
        }

        private static bool TryBlendNoise(
            FanlightNoiseFields fields,
            ReadOnlySpan<FanlightTimelineClipSample> samples,
            out FanlightShowPatch patch)
        {
            ValidateMask((int)fields, (int)FanlightNoiseFields.All);

            var phaseAmount = new FanlightWeightedFloat();
            var phaseSpeed = new FanlightWeightedFloat();
            var axisAmount = new FanlightWeightedFloat();
            var axisSpeed = new FanlightWeightedFloat();
            var octaves = new FanlightDiscreteValue<int>();
            var persistence = new FanlightWeightedFloat();

            for (var i = 0; i < samples.Length; i++)
            {
                var sample = samples[i];
                var sourceValue = sample.Value.Noise;
                if (Has(fields, FanlightNoiseFields.PhaseAmount)) phaseAmount.Add(sourceValue.PhaseAmount, sample.Weight);
                if (Has(fields, FanlightNoiseFields.PhaseSpeed)) phaseSpeed.Add(sourceValue.PhaseSpeed, sample.Weight);
                if (Has(fields, FanlightNoiseFields.AxisAmount)) axisAmount.Add(sourceValue.AxisAmount, sample.Weight);
                if (Has(fields, FanlightNoiseFields.AxisSpeed)) axisSpeed.Add(sourceValue.AxisSpeed, sample.Weight);
                if (Has(fields, FanlightNoiseFields.Octaves)) octaves.Consider(sourceValue.Octaves, sample.Weight, sample.StableClipId);
                if (Has(fields, FanlightNoiseFields.Persistence)) persistence.Add(sourceValue.Persistence, sample.Weight);
            }

            if (fields == FanlightNoiseFields.None)
            {
                patch = default;
                return false;
            }

            var fallback = FanlightTimelineDefaults.NoiseState();
            var value = new FanlightNoiseState(
                phaseAmount.Value(fallback.PhaseAmount),
                phaseSpeed.Value(fallback.PhaseSpeed),
                axisAmount.Value(fallback.AxisAmount),
                axisSpeed.Value(fallback.AxisSpeed),
                octaves.Value(fallback.Octaves),
                persistence.Value(fallback.Persistence)
            );

            patch = new FanlightShowPatch(
                default,
                default,
                default,
                default,
                new FanlightNoisePatch(fields, value),
                default,
                default,
                default,
                default,
                default
            );

            return true;
        }

        private static bool TryBlendRest(
            FanlightRestFields fields,
            ReadOnlySpan<FanlightTimelineClipSample> samples,
            out FanlightShowPatch patch)
        {
            ValidateMask((int)fields, (int)FanlightRestFields.All);

            var probability = new FanlightWeightedFloat();
            var motionLevel = new FanlightWeightedFloat();
            var cycleSeconds = new FanlightWeightedFloat();
            var durationSeconds = new FanlightWeightedFloat();
            var fadeSeconds = new FanlightWeightedFloat();
            var phaseRandomness = new FanlightWeightedFloat();

            for (var i = 0; i < samples.Length; i++)
            {
                var sample = samples[i];
                var sourceValue = sample.Value.Rest;
                if (Has(fields, FanlightRestFields.Probability)) probability.Add(sourceValue.Probability, sample.Weight);
                if (Has(fields, FanlightRestFields.MotionLevel)) motionLevel.Add(sourceValue.MotionLevel, sample.Weight);
                if (Has(fields, FanlightRestFields.CycleSeconds)) cycleSeconds.Add(sourceValue.CycleSeconds, sample.Weight);
                if (Has(fields, FanlightRestFields.DurationSeconds)) durationSeconds.Add(sourceValue.DurationSeconds, sample.Weight);
                if (Has(fields, FanlightRestFields.FadeSeconds)) fadeSeconds.Add(sourceValue.FadeSeconds, sample.Weight);
                if (Has(fields, FanlightRestFields.PhaseRandomness)) phaseRandomness.Add(sourceValue.PhaseRandomness, sample.Weight);
            }

            if (fields == FanlightRestFields.None)
            {
                patch = default;
                return false;
            }

            var fallback = FanlightTimelineDefaults.RestState();
            var value = new FanlightRestState(
                probability.Value(fallback.Probability),
                motionLevel.Value(fallback.MotionLevel),
                cycleSeconds.Value(fallback.CycleSeconds),
                durationSeconds.Value(fallback.DurationSeconds),
                fadeSeconds.Value(fallback.FadeSeconds),
                phaseRandomness.Value(fallback.PhaseRandomness)
            );

            patch = new FanlightShowPatch(
                default,
                default,
                default,
                default,
                default,
                new FanlightRestPatch(fields, value),
                default,
                default,
                default,
                default
            );

            return true;
        }

        private static bool TryBlendAudienceBody(
            FanlightAudienceBodyFields fields,
            ReadOnlySpan<FanlightTimelineClipSample> samples,
            out FanlightShowPatch patch)
        {
            ValidateMask((int)fields, (int)FanlightAudienceBodyFields.All);

            var height = new FanlightWeightedFloat();
            var heightVariation = new FanlightWeightedFloat();
            var width = new FanlightWeightedFloat();
            var headSize = new FanlightWeightedFloat();
            var shoulderHeightRatio = new FanlightWeightedFloat();
            var shoulderSideOffset = new FanlightWeightedFloat();
            var armWidth = new FanlightWeightedFloat();
            var armLengthLimit = new FanlightWeightedFloat();
            var upperBodyLeanMaximumRadians = new FanlightWeightedFloat();
            var upperBodyLean = new FanlightWeightedFloat();
            var bounce = new FanlightWeightedFloat();
            var sway = new FanlightWeightedFloat();
            var motionSpeed = new FanlightWeightedFloat();
            var leanMotion = new FanlightWeightedFloat();

            for (var i = 0; i < samples.Length; i++)
            {
                var sample = samples[i];
                var sourceValue = sample.Value.AudienceBody;
                if (Has(fields, FanlightAudienceBodyFields.Height)) height.Add(sourceValue.Height, sample.Weight);
                if (Has(fields, FanlightAudienceBodyFields.HeightVariation)) heightVariation.Add(sourceValue.HeightVariation, sample.Weight);
                if (Has(fields, FanlightAudienceBodyFields.Width)) width.Add(sourceValue.Width, sample.Weight);
                if (Has(fields, FanlightAudienceBodyFields.HeadSize)) headSize.Add(sourceValue.HeadSize, sample.Weight);
                if (Has(fields, FanlightAudienceBodyFields.ShoulderHeightRatio)) shoulderHeightRatio.Add(sourceValue.ShoulderHeightRatio, sample.Weight);
                if (Has(fields, FanlightAudienceBodyFields.ShoulderSideOffset)) shoulderSideOffset.Add(sourceValue.ShoulderSideOffset, sample.Weight);
                if (Has(fields, FanlightAudienceBodyFields.ArmWidth)) armWidth.Add(sourceValue.ArmWidth, sample.Weight);
                if (Has(fields, FanlightAudienceBodyFields.ArmLengthLimit)) armLengthLimit.Add(sourceValue.ArmLengthLimit, sample.Weight);
                if (Has(fields, FanlightAudienceBodyFields.UpperBodyLeanMaximumRadians)) upperBodyLeanMaximumRadians.Add(sourceValue.UpperBodyLeanMaximumRadians, sample.Weight);
                if (Has(fields, FanlightAudienceBodyFields.UpperBodyLean)) upperBodyLean.Add(sourceValue.UpperBodyLean, sample.Weight);
                if (Has(fields, FanlightAudienceBodyFields.Bounce)) bounce.Add(sourceValue.Bounce, sample.Weight);
                if (Has(fields, FanlightAudienceBodyFields.Sway)) sway.Add(sourceValue.Sway, sample.Weight);
                if (Has(fields, FanlightAudienceBodyFields.MotionSpeed)) motionSpeed.Add(sourceValue.MotionSpeed, sample.Weight);
                if (Has(fields, FanlightAudienceBodyFields.LeanMotion)) leanMotion.Add(sourceValue.LeanMotion, sample.Weight);
            }

            if (fields == FanlightAudienceBodyFields.None)
            {
                patch = default;
                return false;
            }

            var fallback = FanlightTimelineDefaults.AudienceBodyState();
            var value = new FanlightAudienceBodyState(
                height.Value(fallback.Height),
                heightVariation.Value(fallback.HeightVariation),
                width.Value(fallback.Width),
                headSize.Value(fallback.HeadSize),
                shoulderHeightRatio.Value(fallback.ShoulderHeightRatio),
                shoulderSideOffset.Value(fallback.ShoulderSideOffset),
                armWidth.Value(fallback.ArmWidth),
                armLengthLimit.Value(fallback.ArmLengthLimit),
                upperBodyLeanMaximumRadians.Value(fallback.UpperBodyLeanMaximumRadians),
                upperBodyLean.Value(fallback.UpperBodyLean),
                bounce.Value(fallback.Bounce),
                sway.Value(fallback.Sway),
                motionSpeed.Value(fallback.MotionSpeed),
                leanMotion.Value(fallback.LeanMotion)
            );

            patch = new FanlightShowPatch(
                default,
                default,
                default,
                default,
                default,
                default,
                new FanlightAudienceBodyPatch(fields, value),
                default,
                default,
                default
            );

            return true;
        }

        private static bool TryBlendDirection(
            FanlightDirectionFields fields,
            ReadOnlySpan<FanlightTimelineClipSample> samples,
            out FanlightShowPatch patch)
        {
            ValidateMask((int)fields, (int)FanlightDirectionFields.All);

            var mode = new FanlightDiscreteValue<FanlightDirectionMode>();
            var worldYawDegrees = new FanlightWeightedAngle();
            var aimStrength = new FanlightWeightedFloat();

            for (var i = 0; i < samples.Length; i++)
            {
                var sample = samples[i];
                var sourceValue = sample.Value.Direction;
                if (Has(fields, FanlightDirectionFields.Mode)) mode.Consider(sourceValue.Mode, sample.Weight, sample.StableClipId);
                if (Has(fields, FanlightDirectionFields.WorldYawDegrees)) worldYawDegrees.AddDegrees(sourceValue.WorldYawDegrees, sample.Weight);
                if (Has(fields, FanlightDirectionFields.AimStrength)) aimStrength.Add(sourceValue.AimStrength, sample.Weight);
            }

            if (fields == FanlightDirectionFields.None)
            {
                patch = default;
                return false;
            }

            var fallback = FanlightTimelineDefaults.DirectionState();
            var value = new FanlightDirectionState(
                mode.Value(fallback.Mode),
                worldYawDegrees.ValueDegrees(fallback.WorldYawDegrees),
                aimStrength.Value(fallback.AimStrength)
            );

            patch = new FanlightShowPatch(
                default,
                default,
                default,
                default,
                default,
                default,
                default,
                new FanlightDirectionPatch(fields, value),
                default,
                default
            );

            return true;
        }

        private static bool TryBlendPalette(
            FanlightPaletteFields fields,
            ReadOnlySpan<FanlightTimelineClipSample> samples,
            out FanlightShowPatch patch)
        {
            ValidateMask((int)fields, (int)FanlightPaletteFields.All);

            var slot1 = new FanlightWeightedColor();
            var slot2 = new FanlightWeightedColor();
            var slot3 = new FanlightWeightedColor();
            var slot4 = new FanlightWeightedColor();
            var slot5 = new FanlightWeightedColor();
            var slot6 = new FanlightWeightedColor();
            var globalIntensity = new FanlightWeightedFloat();
            var randomIntensity = new FanlightWeightedFloat();

            for (var i = 0; i < samples.Length; i++)
            {
                var sample = samples[i];
                var sourceValue = sample.Value.Palette;
                if (Has(fields, FanlightPaletteFields.Slot1)) slot1.Add(sourceValue.Slot1, sample.Weight);
                if (Has(fields, FanlightPaletteFields.Slot2)) slot2.Add(sourceValue.Slot2, sample.Weight);
                if (Has(fields, FanlightPaletteFields.Slot3)) slot3.Add(sourceValue.Slot3, sample.Weight);
                if (Has(fields, FanlightPaletteFields.Slot4)) slot4.Add(sourceValue.Slot4, sample.Weight);
                if (Has(fields, FanlightPaletteFields.Slot5)) slot5.Add(sourceValue.Slot5, sample.Weight);
                if (Has(fields, FanlightPaletteFields.Slot6)) slot6.Add(sourceValue.Slot6, sample.Weight);
                if (Has(fields, FanlightPaletteFields.GlobalIntensity)) globalIntensity.Add(sourceValue.GlobalIntensity, sample.Weight);
                if (Has(fields, FanlightPaletteFields.RandomIntensity)) randomIntensity.Add(sourceValue.RandomIntensity, sample.Weight);
            }

            if (fields == FanlightPaletteFields.None)
            {
                patch = default;
                return false;
            }

            var fallback = FanlightTimelineDefaults.PaletteState();
            var value = new FanlightPaletteState(
                slot1.Value(fallback.Slot1),
                slot2.Value(fallback.Slot2),
                slot3.Value(fallback.Slot3),
                slot4.Value(fallback.Slot4),
                slot5.Value(fallback.Slot5),
                slot6.Value(fallback.Slot6),
                globalIntensity.Value(fallback.GlobalIntensity),
                randomIntensity.Value(fallback.RandomIntensity)
            );

            patch = new FanlightShowPatch(
                default,
                default,
                default,
                default,
                default,
                default,
                default,
                default,
                new FanlightPalettePatch(fields, value),
                default
            );

            return true;
        }

        private static bool TryBlendVisibility(
            FanlightVisibilityFields fields,
            ReadOnlySpan<FanlightTimelineClipSample> samples,
            out FanlightShowPatch patch)
        {
            ValidateMask((int)fields, (int)FanlightVisibilityFields.All);

            var penlightsEnabled = new FanlightDiscreteValue<bool>();
            var audienceBodiesEnabled = new FanlightDiscreteValue<bool>();

            for (var i = 0; i < samples.Length; i++)
            {
                var sample = samples[i];
                var sourceValue = sample.Value.Visibility;
                if (Has(fields, FanlightVisibilityFields.PenlightsEnabled)) penlightsEnabled.Consider(sourceValue.PenlightsEnabled, sample.Weight, sample.StableClipId);
                if (Has(fields, FanlightVisibilityFields.AudienceBodiesEnabled)) audienceBodiesEnabled.Consider(sourceValue.AudienceBodiesEnabled, sample.Weight, sample.StableClipId);
            }

            if (fields == FanlightVisibilityFields.None)
            {
                patch = default;
                return false;
            }

            var fallback = FanlightTimelineDefaults.VisibilityState();
            var value = new FanlightVisibilityState(
                penlightsEnabled.Value(fallback.PenlightsEnabled),
                audienceBodiesEnabled.Value(fallback.AudienceBodiesEnabled)
            );

            patch = new FanlightShowPatch(
                default,
                default,
                default,
                default,
                default,
                default,
                default,
                default,
                default,
                new FanlightVisibilityPatch(fields, value)
            );

            return true;
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

        private static void ValidateMask(int fields, int all)
        {
            if ((fields & ~all) != 0) throw new ArgumentOutOfRangeException(nameof(fields));
        }
    }
}

using System;
using PrismFanlight.Core;
using UnityEngine;

namespace PrismFanlight.Timeline
{
    internal static class FanlightTimelinePatchMixer
    {
        internal static bool HasFields(FanlightTimelinePatchKind kind, FanlightShowPatch patch)
        {
            return kind switch
            {
                FanlightTimelinePatchKind.Intent => patch.Intent.Fields != FanlightIntentFields.None,
                FanlightTimelinePatchKind.Gesture => patch.Gesture.Fields != FanlightGestureFields.None,
                FanlightTimelinePatchKind.Pose => patch.Pose.Fields != FanlightPoseFields.None,
                FanlightTimelinePatchKind.Variation => patch.Variation.Fields != FanlightVariationFields.None,
                FanlightTimelinePatchKind.Noise => patch.Noise.Fields != FanlightNoiseFields.None,
                FanlightTimelinePatchKind.Rest => patch.Rest.Fields != FanlightRestFields.None,
                FanlightTimelinePatchKind.AudienceBody => patch.AudienceBody.Fields != FanlightAudienceBodyFields.None,
                FanlightTimelinePatchKind.Direction => patch.Direction.Fields != FanlightDirectionFields.None,
                FanlightTimelinePatchKind.Palette => patch.Palette.Fields != FanlightPaletteFields.None,
                FanlightTimelinePatchKind.Visibility => patch.Visibility.Fields != FanlightVisibilityFields.None,
                _ => throw new ArgumentOutOfRangeException(nameof(kind))
            };
        }

        internal static bool TryBlend(
            FanlightTimelinePatchKind kind,
            ReadOnlySpan<FanlightTimelineClipSample> samples,
            out FanlightShowPatch patch)
        {
            return kind switch
            {
                FanlightTimelinePatchKind.Intent => TryBlendIntent(samples, out patch),
                FanlightTimelinePatchKind.Gesture => TryBlendGesture(samples, out patch),
                FanlightTimelinePatchKind.Pose => TryBlendPose(samples, out patch),
                FanlightTimelinePatchKind.Variation => TryBlendVariation(samples, out patch),
                FanlightTimelinePatchKind.Noise => TryBlendNoise(samples, out patch),
                FanlightTimelinePatchKind.Rest => TryBlendRest(samples, out patch),
                FanlightTimelinePatchKind.AudienceBody => TryBlendAudienceBody(samples, out patch),
                FanlightTimelinePatchKind.Direction => TryBlendDirection(samples, out patch),
                FanlightTimelinePatchKind.Palette => TryBlendPalette(samples, out patch),
                FanlightTimelinePatchKind.Visibility => TryBlendVisibility(samples, out patch),
                _ => throw new ArgumentOutOfRangeException(nameof(kind))
            };
        }

        private static bool TryBlendIntent(
            ReadOnlySpan<FanlightTimelineClipSample> samples,
            out FanlightShowPatch patch)
        {
            var fields = FanlightIntentFields.None;
            var energy = new FanlightWeightedFloat();
            var participation = new FanlightWeightedFloat();
            var synchronization = new FanlightWeightedFloat();
            var realism = new FanlightWeightedFloat();
            var reach = new FanlightWeightedFloat();
            for (var i = 0; i < samples.Length; i++)
            {
                var sample = samples[i];
                var source = sample.Patch.Intent;
                ValidateMask((int)source.Fields, (int)FanlightIntentFields.All);
                fields |= source.Fields;
                var sourceValue = source.Value;
                if (Has(source.Fields, FanlightIntentFields.Energy)) energy.Add(sourceValue.Energy, sample.Weight);
                if (Has(source.Fields, FanlightIntentFields.Participation)) participation.Add(sourceValue.Participation, sample.Weight);
                if (Has(source.Fields, FanlightIntentFields.Synchronization)) synchronization.Add(sourceValue.Synchronization, sample.Weight);
                if (Has(source.Fields, FanlightIntentFields.Realism)) realism.Add(sourceValue.Realism, sample.Weight);
                if (Has(source.Fields, FanlightIntentFields.Reach)) reach.Add(sourceValue.Reach, sample.Weight);
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
            ReadOnlySpan<FanlightTimelineClipSample> samples,
            out FanlightShowPatch patch)
        {
            var fields = FanlightGestureFields.None;
            var gestureId = new FanlightDiscreteValue<string>();
            var beatsPerCycle = new FanlightWeightedFloat();
            var phaseOffsetBeats = new FanlightWeightedFloat();
            var attackRatio = new FanlightWeightedFloat();
            var holdRatio = new FanlightWeightedFloat();
            var returnRatio = new FanlightWeightedFloat();
            var crispness = new FanlightWeightedFloat();
            var followThrough = new FanlightWeightedFloat();
            var downbeatAccent = new FanlightWeightedFloat();
            for (var i = 0; i < samples.Length; i++)
            {
                var sample = samples[i];
                var source = sample.Patch.Gesture;
                ValidateMask((int)source.Fields, (int)FanlightGestureFields.All);
                fields |= source.Fields;
                var sourceValue = source.Value;
                if (Has(source.Fields, FanlightGestureFields.GestureId)) gestureId.Consider(sourceValue.GestureId, sample.Weight, sample.StableClipId);
                if (Has(source.Fields, FanlightGestureFields.BeatsPerCycle)) beatsPerCycle.Add(sourceValue.BeatsPerCycle, sample.Weight);
                if (Has(source.Fields, FanlightGestureFields.PhaseOffsetBeats)) phaseOffsetBeats.Add(sourceValue.PhaseOffsetBeats, sample.Weight);
                if (Has(source.Fields, FanlightGestureFields.AttackRatio)) attackRatio.Add(sourceValue.AttackRatio, sample.Weight);
                if (Has(source.Fields, FanlightGestureFields.HoldRatio)) holdRatio.Add(sourceValue.HoldRatio, sample.Weight);
                if (Has(source.Fields, FanlightGestureFields.ReturnRatio)) returnRatio.Add(sourceValue.ReturnRatio, sample.Weight);
                if (Has(source.Fields, FanlightGestureFields.Crispness)) crispness.Add(sourceValue.Crispness, sample.Weight);
                if (Has(source.Fields, FanlightGestureFields.FollowThrough)) followThrough.Add(sourceValue.FollowThrough, sample.Weight);
                if (Has(source.Fields, FanlightGestureFields.DownbeatAccent)) downbeatAccent.Add(sourceValue.DownbeatAccent, sample.Weight);
            }

            if (fields == FanlightGestureFields.None)
            {
                patch = default;
                return false;
            }

            var fallback = FanlightTimelineDefaults.GestureState();
            var value = new FanlightGestureState(
                gestureId.Value(fallback.GestureId),
                beatsPerCycle.Value(fallback.BeatsPerCycle),
                phaseOffsetBeats.Value(fallback.PhaseOffsetBeats),
                attackRatio.Value(fallback.AttackRatio),
                holdRatio.Value(fallback.HoldRatio),
                returnRatio.Value(fallback.ReturnRatio),
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
            ReadOnlySpan<FanlightTimelineClipSample> samples,
            out FanlightShowPatch patch)
        {
            var fields = FanlightPoseFields.None;
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
                var source = sample.Patch.Pose;
                ValidateMask((int)source.Fields, (int)FanlightPoseFields.All);
                fields |= source.Fields;
                var sourceValue = source.Value;
                if (Has(source.Fields, FanlightPoseFields.HandZone)) handZone.Consider(sourceValue.HandZone, sample.Weight, sample.StableClipId);
                if (Has(source.Fields, FanlightPoseFields.HandHeightOffset)) handHeightOffset.Add(sourceValue.HandHeightOffset, sample.Weight);
                if (Has(source.Fields, FanlightPoseFields.HandForwardOffset)) handForwardOffset.Add(sourceValue.HandForwardOffset, sample.Weight);
                if (Has(source.Fields, FanlightPoseFields.HandReachScale)) handReachScale.Add(sourceValue.HandReachScale, sample.Weight);
                if (Has(source.Fields, FanlightPoseFields.ArmLengthMinimum)) armLengthMinimum.Add(sourceValue.ArmLengthMinimum, sample.Weight);
                if (Has(source.Fields, FanlightPoseFields.ArmLengthMaximum)) armLengthMaximum.Add(sourceValue.ArmLengthMaximum, sample.Weight);
                if (Has(source.Fields, FanlightPoseFields.AngleMinimumRadians)) angleMinimumRadians.AddRadians(sourceValue.AngleMinimumRadians, sample.Weight);
                if (Has(source.Fields, FanlightPoseFields.AngleMaximumRadians)) angleMaximumRadians.AddRadians(sourceValue.AngleMaximumRadians, sample.Weight);
                if (Has(source.Fields, FanlightPoseFields.HorizontalRatio)) horizontalRatio.Add(sourceValue.HorizontalRatio, sample.Weight);
                if (Has(source.Fields, FanlightPoseFields.WristFrequencyMultiplier)) wristFrequencyMultiplier.Add(sourceValue.WristFrequencyMultiplier, sample.Weight);
                if (Has(source.Fields, FanlightPoseFields.WristAngleRadians)) wristAngleRadians.Add(sourceValue.WristAngleRadians, sample.Weight);
                if (Has(source.Fields, FanlightPoseFields.BodyLean)) bodyLean.Add(sourceValue.BodyLean, sample.Weight);
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
            ReadOnlySpan<FanlightTimelineClipSample> samples,
            out FanlightShowPatch patch)
        {
            var fields = FanlightVariationFields.None;
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
                var source = sample.Patch.Variation;
                ValidateMask((int)source.Fields, (int)FanlightVariationFields.All);
                fields |= source.Fields;
                var sourceValue = source.Value;
                if (Has(source.Fields, FanlightVariationFields.SeatPosition)) seatPosition.Add(sourceValue.SeatPosition, sample.Weight);
                if (Has(source.Fields, FanlightVariationFields.BodyHeight)) bodyHeight.Add(sourceValue.BodyHeight, sample.Weight);
                if (Has(source.Fields, FanlightVariationFields.ArmLength)) armLength.Add(sourceValue.ArmLength, sample.Weight);
                if (Has(source.Fields, FanlightVariationFields.Angle)) angle.Add(sourceValue.Angle, sample.Weight);
                if (Has(source.Fields, FanlightVariationFields.DirectionSpread)) directionSpread.Add(sourceValue.DirectionSpread, sample.Weight);
                if (Has(source.Fields, FanlightVariationFields.ReactionDelaySeconds)) reactionDelaySeconds.Add(sourceValue.ReactionDelaySeconds, sample.Weight);
                if (Has(source.Fields, FanlightVariationFields.BeatJitter)) beatJitter.Add(sourceValue.BeatJitter, sample.Weight);
                if (Has(source.Fields, FanlightVariationFields.BlockDelayXBeats)) blockDelayXBeats.Add(sourceValue.BlockDelayXBeats, sample.Weight);
                if (Has(source.Fields, FanlightVariationFields.BlockDelayYBeats)) blockDelayYBeats.Add(sourceValue.BlockDelayYBeats, sample.Weight);
                if (Has(source.Fields, FanlightVariationFields.EnergyResponse)) energyResponse.Add(sourceValue.EnergyResponse, sample.Weight);
                if (Has(source.Fields, FanlightVariationFields.Speed)) speed.Add(sourceValue.Speed, sample.Weight);
                if (Has(source.Fields, FanlightVariationFields.BeatReactionDelaySeconds)) beatReactionDelaySeconds.Add(sourceValue.BeatReactionDelaySeconds, sample.Weight);
                if (Has(source.Fields, FanlightVariationFields.HandZone)) handZone.Add(sourceValue.HandZone, sample.Weight);
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
            ReadOnlySpan<FanlightTimelineClipSample> samples,
            out FanlightShowPatch patch)
        {
            var fields = FanlightNoiseFields.None;
            var phaseAmount = new FanlightWeightedFloat();
            var phaseSpeed = new FanlightWeightedFloat();
            var axisAmount = new FanlightWeightedFloat();
            var axisSpeed = new FanlightWeightedFloat();
            var octaves = new FanlightDiscreteValue<int>();
            var persistence = new FanlightWeightedFloat();
            for (var i = 0; i < samples.Length; i++)
            {
                var sample = samples[i];
                var source = sample.Patch.Noise;
                ValidateMask((int)source.Fields, (int)FanlightNoiseFields.All);
                fields |= source.Fields;
                var sourceValue = source.Value;
                if (Has(source.Fields, FanlightNoiseFields.PhaseAmount)) phaseAmount.Add(sourceValue.PhaseAmount, sample.Weight);
                if (Has(source.Fields, FanlightNoiseFields.PhaseSpeed)) phaseSpeed.Add(sourceValue.PhaseSpeed, sample.Weight);
                if (Has(source.Fields, FanlightNoiseFields.AxisAmount)) axisAmount.Add(sourceValue.AxisAmount, sample.Weight);
                if (Has(source.Fields, FanlightNoiseFields.AxisSpeed)) axisSpeed.Add(sourceValue.AxisSpeed, sample.Weight);
                if (Has(source.Fields, FanlightNoiseFields.Octaves)) octaves.Consider(sourceValue.Octaves, sample.Weight, sample.StableClipId);
                if (Has(source.Fields, FanlightNoiseFields.Persistence)) persistence.Add(sourceValue.Persistence, sample.Weight);
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
            ReadOnlySpan<FanlightTimelineClipSample> samples,
            out FanlightShowPatch patch)
        {
            var fields = FanlightRestFields.None;
            var probability = new FanlightWeightedFloat();
            var motionLevel = new FanlightWeightedFloat();
            var cycleSeconds = new FanlightWeightedFloat();
            var durationSeconds = new FanlightWeightedFloat();
            var fadeSeconds = new FanlightWeightedFloat();
            var phaseRandomness = new FanlightWeightedFloat();
            for (var i = 0; i < samples.Length; i++)
            {
                var sample = samples[i];
                var source = sample.Patch.Rest;
                ValidateMask((int)source.Fields, (int)FanlightRestFields.All);
                fields |= source.Fields;
                var sourceValue = source.Value;
                if (Has(source.Fields, FanlightRestFields.Probability)) probability.Add(sourceValue.Probability, sample.Weight);
                if (Has(source.Fields, FanlightRestFields.MotionLevel)) motionLevel.Add(sourceValue.MotionLevel, sample.Weight);
                if (Has(source.Fields, FanlightRestFields.CycleSeconds)) cycleSeconds.Add(sourceValue.CycleSeconds, sample.Weight);
                if (Has(source.Fields, FanlightRestFields.DurationSeconds)) durationSeconds.Add(sourceValue.DurationSeconds, sample.Weight);
                if (Has(source.Fields, FanlightRestFields.FadeSeconds)) fadeSeconds.Add(sourceValue.FadeSeconds, sample.Weight);
                if (Has(source.Fields, FanlightRestFields.PhaseRandomness)) phaseRandomness.Add(sourceValue.PhaseRandomness, sample.Weight);
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
            ReadOnlySpan<FanlightTimelineClipSample> samples,
            out FanlightShowPatch patch)
        {
            var fields = FanlightAudienceBodyFields.None;
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
                var source = sample.Patch.AudienceBody;
                ValidateMask((int)source.Fields, (int)FanlightAudienceBodyFields.All);
                fields |= source.Fields;
                var sourceValue = source.Value;
                if (Has(source.Fields, FanlightAudienceBodyFields.Height)) height.Add(sourceValue.Height, sample.Weight);
                if (Has(source.Fields, FanlightAudienceBodyFields.HeightVariation)) heightVariation.Add(sourceValue.HeightVariation, sample.Weight);
                if (Has(source.Fields, FanlightAudienceBodyFields.Width)) width.Add(sourceValue.Width, sample.Weight);
                if (Has(source.Fields, FanlightAudienceBodyFields.HeadSize)) headSize.Add(sourceValue.HeadSize, sample.Weight);
                if (Has(source.Fields, FanlightAudienceBodyFields.ShoulderHeightRatio)) shoulderHeightRatio.Add(sourceValue.ShoulderHeightRatio, sample.Weight);
                if (Has(source.Fields, FanlightAudienceBodyFields.ShoulderSideOffset)) shoulderSideOffset.Add(sourceValue.ShoulderSideOffset, sample.Weight);
                if (Has(source.Fields, FanlightAudienceBodyFields.ArmWidth)) armWidth.Add(sourceValue.ArmWidth, sample.Weight);
                if (Has(source.Fields, FanlightAudienceBodyFields.ArmLengthLimit)) armLengthLimit.Add(sourceValue.ArmLengthLimit, sample.Weight);
                if (Has(source.Fields, FanlightAudienceBodyFields.UpperBodyLeanMaximumRadians)) upperBodyLeanMaximumRadians.Add(sourceValue.UpperBodyLeanMaximumRadians, sample.Weight);
                if (Has(source.Fields, FanlightAudienceBodyFields.UpperBodyLean)) upperBodyLean.Add(sourceValue.UpperBodyLean, sample.Weight);
                if (Has(source.Fields, FanlightAudienceBodyFields.Bounce)) bounce.Add(sourceValue.Bounce, sample.Weight);
                if (Has(source.Fields, FanlightAudienceBodyFields.Sway)) sway.Add(sourceValue.Sway, sample.Weight);
                if (Has(source.Fields, FanlightAudienceBodyFields.MotionSpeed)) motionSpeed.Add(sourceValue.MotionSpeed, sample.Weight);
                if (Has(source.Fields, FanlightAudienceBodyFields.LeanMotion)) leanMotion.Add(sourceValue.LeanMotion, sample.Weight);
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
            ReadOnlySpan<FanlightTimelineClipSample> samples,
            out FanlightShowPatch patch)
        {
            var fields = FanlightDirectionFields.None;
            var mode = new FanlightDiscreteValue<FanlightDirectionMode>();
            var worldYawDegrees = new FanlightWeightedAngle();
            var aimStrength = new FanlightWeightedFloat();
            for (var i = 0; i < samples.Length; i++)
            {
                var sample = samples[i];
                var source = sample.Patch.Direction;
                ValidateMask((int)source.Fields, (int)FanlightDirectionFields.All);
                fields |= source.Fields;
                var sourceValue = source.Value;
                if (Has(source.Fields, FanlightDirectionFields.Mode)) mode.Consider(sourceValue.Mode, sample.Weight, sample.StableClipId);
                if (Has(source.Fields, FanlightDirectionFields.WorldYawDegrees)) worldYawDegrees.AddDegrees(sourceValue.WorldYawDegrees, sample.Weight);
                if (Has(source.Fields, FanlightDirectionFields.AimStrength)) aimStrength.Add(sourceValue.AimStrength, sample.Weight);
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
            ReadOnlySpan<FanlightTimelineClipSample> samples,
            out FanlightShowPatch patch)
        {
            var fields = FanlightPaletteFields.None;
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
                var source = sample.Patch.Palette;
                ValidateMask((int)source.Fields, (int)FanlightPaletteFields.All);
                fields |= source.Fields;
                var sourceValue = source.Value;
                if (Has(source.Fields, FanlightPaletteFields.Slot1)) slot1.Add(sourceValue.Slot1, sample.Weight);
                if (Has(source.Fields, FanlightPaletteFields.Slot2)) slot2.Add(sourceValue.Slot2, sample.Weight);
                if (Has(source.Fields, FanlightPaletteFields.Slot3)) slot3.Add(sourceValue.Slot3, sample.Weight);
                if (Has(source.Fields, FanlightPaletteFields.Slot4)) slot4.Add(sourceValue.Slot4, sample.Weight);
                if (Has(source.Fields, FanlightPaletteFields.Slot5)) slot5.Add(sourceValue.Slot5, sample.Weight);
                if (Has(source.Fields, FanlightPaletteFields.Slot6)) slot6.Add(sourceValue.Slot6, sample.Weight);
                if (Has(source.Fields, FanlightPaletteFields.GlobalIntensity)) globalIntensity.Add(sourceValue.GlobalIntensity, sample.Weight);
                if (Has(source.Fields, FanlightPaletteFields.RandomIntensity)) randomIntensity.Add(sourceValue.RandomIntensity, sample.Weight);
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
            ReadOnlySpan<FanlightTimelineClipSample> samples,
            out FanlightShowPatch patch)
        {
            var fields = FanlightVisibilityFields.None;
            var penlightsEnabled = new FanlightDiscreteValue<bool>();
            var audienceBodiesEnabled = new FanlightDiscreteValue<bool>();
            for (var i = 0; i < samples.Length; i++)
            {
                var sample = samples[i];
                var source = sample.Patch.Visibility;
                ValidateMask((int)source.Fields, (int)FanlightVisibilityFields.All);
                fields |= source.Fields;
                var sourceValue = source.Value;
                if (Has(source.Fields, FanlightVisibilityFields.PenlightsEnabled)) penlightsEnabled.Consider(sourceValue.PenlightsEnabled, sample.Weight, sample.StableClipId);
                if (Has(source.Fields, FanlightVisibilityFields.AudienceBodiesEnabled)) audienceBodiesEnabled.Consider(sourceValue.AudienceBodiesEnabled, sample.Weight, sample.StableClipId);
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

        private struct FanlightWeightedFloat
        {
            private double _sum;
            private double _weight;

            internal void Add(float value, float weight)
            {
                if (!FanlightStateValidation.IsFinite(value)) throw new ArgumentOutOfRangeException(nameof(value));
                if (!FanlightStateValidation.IsFinite(weight) || weight <= 0f) return;
                _sum += value * weight;
                _weight += weight;
            }

            internal float Value(float fallback) => _weight > 0d ? (float)(_sum / _weight) : fallback;
        }

        private struct FanlightWeightedAngle
        {
            private float _degrees;
            private double _weight;

            internal void AddDegrees(float value, float weight)
            {
                if (!FanlightStateValidation.IsFinite(value)) throw new ArgumentOutOfRangeException(nameof(value));
                if (!FanlightStateValidation.IsFinite(weight) || weight <= 0f) return;
                if (_weight <= 0d) _degrees = value;
                else _degrees = Mathf.LerpAngle(_degrees, value, (float)(weight / (_weight + weight)));
                _weight += weight;
            }

            internal void AddRadians(float value, float weight) => AddDegrees(value * Mathf.Rad2Deg, weight);

            internal float ValueDegrees(float fallback) => _weight > 0d ? Mathf.Repeat(_degrees, 360f) : fallback;

            internal float ValueRadians(float fallback) => _weight > 0d ? Mathf.Repeat(_degrees, 360f) * Mathf.Deg2Rad : fallback;
        }

        private struct FanlightWeightedColor
        {
            private Color _sum;
            private double _weight;

            internal void Add(Color value, float weight)
            {
                if (!FanlightStateValidation.IsFinite(value.r)
                    || !FanlightStateValidation.IsFinite(value.g)
                    || !FanlightStateValidation.IsFinite(value.b)
                    || !FanlightStateValidation.IsFinite(value.a))
                    throw new ArgumentOutOfRangeException(nameof(value));
                if (!FanlightStateValidation.IsFinite(weight) || weight <= 0f) return;
                var linear = QualitySettings.activeColorSpace == ColorSpace.Linear ? value : value.linear;
                _sum += linear * weight;
                _weight += weight;
            }

            internal Color Value(Color fallback)
            {
                if (_weight <= 0d) return fallback;
                var linear = _sum * (float)(1d / _weight);
                return QualitySettings.activeColorSpace == ColorSpace.Linear ? linear : linear.gamma;
            }
        }

        private struct FanlightDiscreteValue<T>
        {
            private bool _hasValue;
            private T _value;
            private float _weight;
            private string _stableClipId;

            internal void Consider(T value, float weight, string stableClipId)
            {
                if (!FanlightStateValidation.IsFinite(weight) || weight <= 0f) return;
                if ((_hasValue && weight < _weight)
                    || (_hasValue
                        && weight == _weight
                        && string.Compare(stableClipId, _stableClipId, StringComparison.Ordinal) <= 0))
                    return;
                _hasValue = true;
                _value = value;
                _weight = weight;
                _stableClipId = stableClipId;
            }

            internal T Value(T fallback) => _hasValue ? _value : fallback;
        }
    }
}

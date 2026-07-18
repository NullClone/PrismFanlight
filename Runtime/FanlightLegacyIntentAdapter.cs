using System;
using PrismFanlight.Core;
using UnityEngine;

namespace PrismFanlight
{
    internal static class FanlightLegacyIntentAdapter
    {
        internal static FanlightShowState ToShowState(
            FanlightMotionSettings motion,
            FanlightColorSettings color,
            FanlightAudienceSettings audience,
            uint globalSeed)
        {
            motion = motion.Validated();
            color = color.Validated();
            audience = audience.Validated();
            var cycleSeconds = Mathf.Clamp(motion.human.restCycleDuration, 0f, 3600f);
            return new FanlightShowState(
                new FanlightIntentState(
                    Mathf.Clamp01(motion.human.enthusiasm * 0.5f),
                    1f - motion.human.lazyFanRatio,
                    1f - motion.swing.randomPhase,
                    1f,
                    Mathf.Clamp01(audience.handZone.reachScale / 1.28f)),
                new FanlightGestureState(
                    "Wave",
                    Mathf.Clamp(motion.beatSync.beatsPerSwing, 0.001f, 64f),
                    Mathf.Clamp(motion.beatSync.beatPhaseOffset, -64f, 64f),
                    0.25f,
                    motion.swing.peakHold,
                    0.75f,
                    motion.swing.crispness,
                    motion.swing.followThrough,
                    Mathf.Clamp(motion.beatSync.downbeatAccent, 0f, 4f)),
                new FanlightPoseState(
                    audience.handZone.zone,
                    Mathf.Clamp(audience.handZone.heightOffset, -1f, 1.5f),
                    Mathf.Clamp(audience.handZone.forwardOffset, -1f, 1f),
                    Mathf.Max(0.01f, audience.handZone.reachScale),
                    Mathf.Clamp(motion.swing.armLengthMin, 0f, 5f),
                    Mathf.Clamp(motion.swing.armLengthMax, 0f, 5f),
                    Mathf.Clamp(motion.swing.minAngle, 0f, Mathf.PI * 2f),
                    Mathf.Clamp(motion.swing.maxAngle, 0f, Mathf.PI * 2f),
                    motion.swing.horizontalRatio,
                    Mathf.Clamp(motion.swing.wristSwingSpeed, 1f, 64f),
                    Mathf.Clamp(motion.swing.wristSwingAngle, 0f, Mathf.PI),
                    motion.swing.lean),
                new FanlightVariationState(
                    motion.human.seatJitter,
                    Mathf.Clamp01(motion.human.heightJitter),
                    motion.human.armLengthJitter,
                    motion.swing.angleNoise,
                    motion.direction.directionSpread,
                    Mathf.Clamp(motion.human.reactionDelay, 0f, 10f),
                    Mathf.Clamp(motion.beatSync.beatSeatJitter, 0f, 8f),
                    Mathf.Clamp(motion.beatSync.beatBlockDelay.x, -64f, 64f),
                    Mathf.Clamp(motion.beatSync.beatBlockDelay.y, -64f, 64f),
                    motion.human.enthusiasmVariation,
                    Mathf.Clamp(motion.human.speedVariation, 0f, 4f),
                    Mathf.Clamp(motion.beatSync.beatReactionDelay, 0f, 10f),
                    Mathf.Clamp(audience.handZone.variation, 0f, 0.5f)),
                new FanlightNoiseState(
                    Mathf.Clamp(motion.noise.phaseIrregularity, 0f, 4f),
                    Mathf.Clamp(motion.noise.phaseIrregularitySpeed, 0f, 16f),
                    Mathf.Clamp(motion.noise.axisNoiseAmount, 0f, 4f),
                    Mathf.Clamp(motion.noise.axisNoiseSpeed, 0f, 16f),
                    motion.noise.noiseOctaves,
                    motion.noise.noiseDetail),
                new FanlightRestState(
                    motion.human.restProbability,
                    motion.human.restMotionLevel,
                    cycleSeconds,
                    Mathf.Clamp(motion.human.restDuration, 0f, cycleSeconds),
                    Mathf.Clamp(motion.human.restFadeDuration, 0f, 60f),
                    motion.human.restPhaseRandomness),
                new FanlightAudienceBodyState(
                    Mathf.Clamp(audience.bodyHeight, 0.1f, 3f),
                    audience.bodyHeightJitter,
                    Mathf.Clamp(audience.bodyWidth, 0.01f, 3f),
                    Mathf.Clamp(audience.headSize, 0.01f, 1f),
                    audience.shoulderHeight,
                    audience.shoulderOffset,
                    Mathf.Clamp(audience.armWidth, 0.01f, 1f),
                    Mathf.Clamp(audience.armLengthLimit, 0.01f, 3f),
                    Mathf.Clamp(audience.upperBodyLeanMax, 0f, Mathf.PI * 0.5f),
                    audience.upperBodyLean,
                    Mathf.Clamp01(audience.motion.bodyBounce),
                    Mathf.Clamp01(audience.motion.bodySway),
                    Mathf.Clamp(audience.motion.bodyMotionSpeed, 0.01f, 16f),
                    audience.motion.upperBodyLeanMotion),
                new FanlightDirectionState(
                    motion.direction.swingMode == FanlightSwingMode.WorldDirection
                        ? FanlightDirectionMode.WorldDirection
                        : FanlightDirectionMode.Target,
                    motion.direction.swingYaw,
                    motion.direction.aimStrength),
                ToPalette(color),
                new FanlightVisibilityState(true, audience.enabled),
                globalSeed);
        }

        internal static FanlightResolvedState ToLegacyState(in FanlightShowSample sample, in FanlightResolvedState template)
        {
            var state = sample.State;
            var motion = template.Motion.Validated();
            var audience = template.Audience.Validated();
            var color = ToColor(state.Palette);

            motion.human.enthusiasm = state.Intent.Energy * 2f;
            motion.human.lazyFanRatio = 1f - state.Intent.Participation;
            motion.swing.randomPhase = 1f - state.Intent.Synchronization;
            motion.beatSync.beatsPerSwing = state.Gesture.BeatsPerCycle;
            motion.beatSync.beatPhaseOffset = state.Gesture.PhaseOffsetBeats;
            motion.swing.peakHold = state.Gesture.HoldRatio;
            motion.swing.crispness = state.Gesture.Crispness;
            motion.swing.followThrough = state.Gesture.FollowThrough;
            motion.beatSync.downbeatAccent = state.Gesture.DownbeatAccent;
            audience.handZone.zone = state.Pose.HandZone;
            audience.handZone.heightOffset = state.Pose.HandHeightOffset;
            audience.handZone.forwardOffset = state.Pose.HandForwardOffset;
            audience.handZone.reachScale = state.Pose.HandReachScale;
            motion.swing.armLengthMin = state.Pose.ArmLengthMinimum;
            motion.swing.armLengthMax = state.Pose.ArmLengthMaximum;
            motion.swing.minAngle = state.Pose.AngleMinimumRadians;
            motion.swing.maxAngle = state.Pose.AngleMaximumRadians;
            motion.swing.horizontalRatio = state.Pose.HorizontalRatio;
            motion.swing.wristSwingSpeed = state.Pose.WristFrequencyMultiplier;
            motion.swing.wristSwingAngle = state.Pose.WristAngleRadians;
            motion.swing.lean = state.Pose.BodyLean;
            motion.human.seatJitter = state.Variation.SeatPosition;
            motion.human.heightJitter = state.Variation.BodyHeight;
            motion.human.armLengthJitter = state.Variation.ArmLength;
            motion.swing.angleNoise = state.Variation.Angle;
            motion.direction.directionSpread = state.Variation.DirectionSpread;
            motion.human.reactionDelay = state.Variation.ReactionDelaySeconds;
            motion.beatSync.beatSeatJitter = state.Variation.BeatJitter;
            motion.beatSync.beatBlockDelay = new Vector2(state.Variation.BlockDelayXBeats, state.Variation.BlockDelayYBeats);
            motion.human.enthusiasmVariation = state.Variation.EnergyResponse;
            motion.human.speedVariation = state.Variation.Speed;
            motion.beatSync.beatReactionDelay = state.Variation.BeatReactionDelaySeconds;
            audience.handZone.variation = state.Variation.HandZone;
            motion.noise.phaseIrregularity = state.Noise.PhaseAmount;
            motion.noise.phaseIrregularitySpeed = state.Noise.PhaseSpeed;
            motion.noise.axisNoiseAmount = state.Noise.AxisAmount;
            motion.noise.axisNoiseSpeed = state.Noise.AxisSpeed;
            motion.noise.noiseOctaves = state.Noise.Octaves;
            motion.noise.noiseDetail = state.Noise.Persistence;
            motion.human.restProbability = state.Rest.Probability;
            motion.human.restMotionLevel = state.Rest.MotionLevel;
            motion.human.restCycleDuration = state.Rest.CycleSeconds;
            motion.human.restDuration = state.Rest.DurationSeconds;
            motion.human.restFadeDuration = state.Rest.FadeSeconds;
            motion.human.restPhaseRandomness = state.Rest.PhaseRandomness;
            audience.bodyHeight = state.AudienceBody.Height;
            audience.bodyHeightJitter = state.AudienceBody.HeightVariation;
            audience.bodyWidth = state.AudienceBody.Width;
            audience.headSize = state.AudienceBody.HeadSize;
            audience.shoulderHeight = state.AudienceBody.ShoulderHeightRatio;
            audience.shoulderOffset = state.AudienceBody.ShoulderSideOffset;
            audience.armWidth = state.AudienceBody.ArmWidth;
            audience.armLengthLimit = state.AudienceBody.ArmLengthLimit;
            audience.upperBodyLeanMax = state.AudienceBody.UpperBodyLeanMaximumRadians;
            audience.upperBodyLean = state.AudienceBody.UpperBodyLean;
            audience.motion.bodyBounce = state.AudienceBody.Bounce;
            audience.motion.bodySway = state.AudienceBody.Sway;
            audience.motion.bodyMotionSpeed = state.AudienceBody.MotionSpeed;
            audience.motion.upperBodyLeanMotion = state.AudienceBody.LeanMotion;
            motion.direction.swingMode = state.Direction.Mode == FanlightDirectionMode.WorldDirection
                ? FanlightSwingMode.WorldDirection
                : FanlightSwingMode.Target;
            motion.direction.swingYaw = state.Direction.WorldYawDegrees;
            motion.direction.aimStrength = state.Direction.AimStrength;
            audience.enabled = state.Visibility.AudienceBodiesEnabled;
            if (!state.Visibility.PenlightsEnabled) color.intensity = 0f;

            var random = template.Random;
            random.globalSeed = state.GlobalSeed;
            return new FanlightResolvedState(
                FanlightTempoState.FromMusicalPosition(true, sample.MusicalPosition),
                motion,
                color,
                audience,
                template.Lod,
                random,
                template.SwingTargetWorldPosition,
                template.LocalToWorld,
                (float)sample.ShowSeconds,
                (float)sample.AnimationSampleSeconds,
                sample.Discontinuity != FanlightTimeDiscontinuity.None);
        }

        internal static bool TryAddLegacyParameter(
            FanlightShowPatchBuilder builder,
            string path,
            object value,
            FanlightShowState baseState)
        {
            if (builder == null || string.IsNullOrEmpty(path) || value == null) return false;
            return TryAddIntent(builder, path, value, baseState)
                   || TryAddGesture(builder, path, value, baseState)
                   || TryAddPose(builder, path, value, baseState)
                   || TryAddVariation(builder, path, value, baseState)
                   || TryAddNoise(builder, path, value, baseState)
                   || TryAddRest(builder, path, value, baseState)
                   || TryAddAudienceBody(builder, path, value, baseState)
                   || TryAddDirection(builder, path, value, baseState)
                   || TryAddPalette(builder, path, value, baseState)
                   || TryAddVisibility(builder, path, value, baseState);
        }

        internal static FanlightPaletteState ToPalette(FanlightColorSettings value) => new(
            value.slot1,
            value.slot2,
            value.slot3,
            value.slot4,
            value.slot5,
            value.slot6,
            Mathf.Max(0f, value.intensity),
            Mathf.Clamp01(value.randomIntensity));

        private static bool TryAddIntent(FanlightShowPatchBuilder builder, string path, object value, FanlightShowState state)
        {
            var current = state.Intent;
            switch (path)
            {
                case "motion.swing.randomPhase":
                    builder.SetIntent(FanlightIntentFields.Synchronization, new FanlightIntentState(current.Energy, current.Participation, 1f - Convert.ToSingle(value), current.Realism, current.Reach));
                    return true;
                case "motion.human.enthusiasm":
                    builder.SetIntent(FanlightIntentFields.Energy, new FanlightIntentState(Convert.ToSingle(value) * 0.5f, current.Participation, current.Synchronization, current.Realism, current.Reach));
                    return true;
                case "motion.human.lazyFanRatio":
                    builder.SetIntent(FanlightIntentFields.Participation, new FanlightIntentState(current.Energy, 1f - Convert.ToSingle(value), current.Synchronization, current.Realism, current.Reach));
                    return true;
                case "audience.handZone.reachScale":
                    builder.SetIntent(FanlightIntentFields.Reach, new FanlightIntentState(current.Energy, current.Participation, current.Synchronization, current.Realism, Mathf.Clamp01(Convert.ToSingle(value) / 1.28f)));
                    return true;
                default:
                    return false;
            }
        }

        private static bool TryAddGesture(FanlightShowPatchBuilder builder, string path, object value, FanlightShowState state)
        {
            var current = state.Gesture;
            var number = value is string ? 0f : Convert.ToSingle(value);
            var fields = path switch
            {
                "motion.beatSync.beatsPerSwing" => FanlightGestureFields.BeatsPerCycle,
                "motion.beatSync.beatPhaseOffset" => FanlightGestureFields.PhaseOffsetBeats,
                "motion.swing.peakHold" => FanlightGestureFields.HoldRatio,
                "motion.swing.crispness" => FanlightGestureFields.Crispness,
                "motion.swing.followThrough" => FanlightGestureFields.FollowThrough,
                "motion.beatSync.downbeatAccent" => FanlightGestureFields.DownbeatAccent,
                _ => FanlightGestureFields.None
            };
            if (fields == FanlightGestureFields.None) return false;
            builder.SetGesture(fields, new FanlightGestureState(
                current.GestureId,
                fields == FanlightGestureFields.BeatsPerCycle ? number : current.BeatsPerCycle,
                fields == FanlightGestureFields.PhaseOffsetBeats ? number : current.PhaseOffsetBeats,
                current.AttackRatio,
                fields == FanlightGestureFields.HoldRatio ? number : current.HoldRatio,
                current.ReturnRatio,
                fields == FanlightGestureFields.Crispness ? number : current.Crispness,
                fields == FanlightGestureFields.FollowThrough ? number : current.FollowThrough,
                fields == FanlightGestureFields.DownbeatAccent ? number : current.DownbeatAccent));
            return true;
        }

        private static bool TryAddPose(FanlightShowPatchBuilder builder, string path, object value, FanlightShowState state)
        {
            var current = state.Pose;
            var fields = path switch
            {
                "audience.handZone.zone" => FanlightPoseFields.HandZone,
                "audience.handZone.heightOffset" => FanlightPoseFields.HandHeightOffset,
                "audience.handZone.forwardOffset" => FanlightPoseFields.HandForwardOffset,
                "motion.swing.armLengthMin" => FanlightPoseFields.ArmLengthMinimum,
                "motion.swing.armLengthMax" => FanlightPoseFields.ArmLengthMaximum,
                "motion.swing.minAngle" => FanlightPoseFields.AngleMinimumRadians,
                "motion.swing.maxAngle" => FanlightPoseFields.AngleMaximumRadians,
                "motion.swing.horizontalRatio" => FanlightPoseFields.HorizontalRatio,
                "motion.swing.wristSwingSpeed" => FanlightPoseFields.WristFrequencyMultiplier,
                "motion.swing.wristSwingAngle" => FanlightPoseFields.WristAngleRadians,
                "motion.swing.lean" => FanlightPoseFields.BodyLean,
                _ => FanlightPoseFields.None
            };
            if (fields == FanlightPoseFields.None) return false;
            var number = fields == FanlightPoseFields.HandZone ? 0f : Convert.ToSingle(value);
            builder.SetPose(fields, new FanlightPoseState(
                fields == FanlightPoseFields.HandZone ? (FanlightHandZone)value : current.HandZone,
                fields == FanlightPoseFields.HandHeightOffset ? number : current.HandHeightOffset,
                fields == FanlightPoseFields.HandForwardOffset ? number : current.HandForwardOffset,
                current.HandReachScale,
                fields == FanlightPoseFields.ArmLengthMinimum ? number : current.ArmLengthMinimum,
                fields == FanlightPoseFields.ArmLengthMaximum ? number : current.ArmLengthMaximum,
                fields == FanlightPoseFields.AngleMinimumRadians ? number : current.AngleMinimumRadians,
                fields == FanlightPoseFields.AngleMaximumRadians ? number : current.AngleMaximumRadians,
                fields == FanlightPoseFields.HorizontalRatio ? number : current.HorizontalRatio,
                fields == FanlightPoseFields.WristFrequencyMultiplier ? number : current.WristFrequencyMultiplier,
                fields == FanlightPoseFields.WristAngleRadians ? number : current.WristAngleRadians,
                fields == FanlightPoseFields.BodyLean ? number : current.BodyLean));
            return true;
        }

        private static bool TryAddVariation(FanlightShowPatchBuilder builder, string path, object value, FanlightShowState state)
        {
            var current = state.Variation;
            if (path == "motion.beatSync.beatBlockDelay")
            {
                var vector = (Vector2)value;
                builder.SetVariation(FanlightVariationFields.BlockDelayXBeats | FanlightVariationFields.BlockDelayYBeats,
                    new FanlightVariationState(current.SeatPosition, current.BodyHeight, current.ArmLength, current.Angle, current.DirectionSpread, current.ReactionDelaySeconds, current.BeatJitter, vector.x, vector.y, current.EnergyResponse, current.Speed, current.BeatReactionDelaySeconds, current.HandZone));
                return true;
            }

            var fields = path switch
            {
                "motion.human.seatJitter" => FanlightVariationFields.SeatPosition,
                "motion.human.heightJitter" => FanlightVariationFields.BodyHeight,
                "motion.human.armLengthJitter" => FanlightVariationFields.ArmLength,
                "motion.swing.angleNoise" => FanlightVariationFields.Angle,
                "motion.direction.directionSpread" => FanlightVariationFields.DirectionSpread,
                "motion.human.reactionDelay" => FanlightVariationFields.ReactionDelaySeconds,
                "motion.beatSync.beatSeatJitter" => FanlightVariationFields.BeatJitter,
                "motion.human.enthusiasmVariation" => FanlightVariationFields.EnergyResponse,
                "motion.human.speedVariation" => FanlightVariationFields.Speed,
                "motion.beatSync.beatReactionDelay" => FanlightVariationFields.BeatReactionDelaySeconds,
                "audience.handZone.variation" => FanlightVariationFields.HandZone,
                _ => FanlightVariationFields.None
            };
            if (fields == FanlightVariationFields.None) return false;
            var number = Convert.ToSingle(value);
            builder.SetVariation(fields, new FanlightVariationState(
                fields == FanlightVariationFields.SeatPosition ? number : current.SeatPosition,
                fields == FanlightVariationFields.BodyHeight ? number : current.BodyHeight,
                fields == FanlightVariationFields.ArmLength ? number : current.ArmLength,
                fields == FanlightVariationFields.Angle ? number : current.Angle,
                fields == FanlightVariationFields.DirectionSpread ? number : current.DirectionSpread,
                fields == FanlightVariationFields.ReactionDelaySeconds ? number : current.ReactionDelaySeconds,
                fields == FanlightVariationFields.BeatJitter ? number : current.BeatJitter,
                current.BlockDelayXBeats,
                current.BlockDelayYBeats,
                fields == FanlightVariationFields.EnergyResponse ? number : current.EnergyResponse,
                fields == FanlightVariationFields.Speed ? number : current.Speed,
                fields == FanlightVariationFields.BeatReactionDelaySeconds ? number : current.BeatReactionDelaySeconds,
                fields == FanlightVariationFields.HandZone ? number : current.HandZone));
            return true;
        }

        private static bool TryAddNoise(FanlightShowPatchBuilder builder, string path, object value, FanlightShowState state)
        {
            var current = state.Noise;
            var fields = path switch
            {
                "motion.noise.phaseIrregularity" => FanlightNoiseFields.PhaseAmount,
                "motion.noise.phaseIrregularitySpeed" => FanlightNoiseFields.PhaseSpeed,
                "motion.noise.axisNoiseAmount" => FanlightNoiseFields.AxisAmount,
                "motion.noise.axisNoiseSpeed" => FanlightNoiseFields.AxisSpeed,
                "motion.noise.noiseOctaves" => FanlightNoiseFields.Octaves,
                "motion.noise.noiseDetail" => FanlightNoiseFields.Persistence,
                _ => FanlightNoiseFields.None
            };
            if (fields == FanlightNoiseFields.None) return false;
            var number = Convert.ToSingle(value);
            builder.SetNoise(fields, new FanlightNoiseState(
                fields == FanlightNoiseFields.PhaseAmount ? number : current.PhaseAmount,
                fields == FanlightNoiseFields.PhaseSpeed ? number : current.PhaseSpeed,
                fields == FanlightNoiseFields.AxisAmount ? number : current.AxisAmount,
                fields == FanlightNoiseFields.AxisSpeed ? number : current.AxisSpeed,
                fields == FanlightNoiseFields.Octaves ? Convert.ToInt32(value) : current.Octaves,
                fields == FanlightNoiseFields.Persistence ? number : current.Persistence));
            return true;
        }

        private static bool TryAddRest(FanlightShowPatchBuilder builder, string path, object value, FanlightShowState state)
        {
            var current = state.Rest;
            var fields = path switch
            {
                "motion.human.restProbability" => FanlightRestFields.Probability,
                "motion.human.restMotionLevel" => FanlightRestFields.MotionLevel,
                "motion.human.restCycleDuration" => FanlightRestFields.CycleSeconds,
                "motion.human.restDuration" => FanlightRestFields.DurationSeconds,
                "motion.human.restFadeDuration" => FanlightRestFields.FadeSeconds,
                "motion.human.restPhaseRandomness" => FanlightRestFields.PhaseRandomness,
                _ => FanlightRestFields.None
            };
            if (fields == FanlightRestFields.None) return false;
            var number = Convert.ToSingle(value);
            builder.SetRest(fields, new FanlightRestState(
                fields == FanlightRestFields.Probability ? number : current.Probability,
                fields == FanlightRestFields.MotionLevel ? number : current.MotionLevel,
                fields == FanlightRestFields.CycleSeconds ? number : current.CycleSeconds,
                fields == FanlightRestFields.DurationSeconds ? number : current.DurationSeconds,
                fields == FanlightRestFields.FadeSeconds ? number : current.FadeSeconds,
                fields == FanlightRestFields.PhaseRandomness ? number : current.PhaseRandomness));
            return true;
        }

        private static bool TryAddAudienceBody(FanlightShowPatchBuilder builder, string path, object value, FanlightShowState state)
        {
            var current = state.AudienceBody;
            var fields = path switch
            {
                "audience.bodyHeight" => FanlightAudienceBodyFields.Height,
                "audience.bodyHeightJitter" => FanlightAudienceBodyFields.HeightVariation,
                "audience.bodyWidth" => FanlightAudienceBodyFields.Width,
                "audience.headSize" => FanlightAudienceBodyFields.HeadSize,
                "audience.shoulderHeight" => FanlightAudienceBodyFields.ShoulderHeightRatio,
                "audience.shoulderOffset" => FanlightAudienceBodyFields.ShoulderSideOffset,
                "audience.armWidth" => FanlightAudienceBodyFields.ArmWidth,
                "audience.armLengthLimit" => FanlightAudienceBodyFields.ArmLengthLimit,
                "audience.upperBodyLeanMax" => FanlightAudienceBodyFields.UpperBodyLeanMaximumRadians,
                "audience.upperBodyLean" => FanlightAudienceBodyFields.UpperBodyLean,
                "audience.motion.bodyBounce" => FanlightAudienceBodyFields.Bounce,
                "audience.motion.bodySway" => FanlightAudienceBodyFields.Sway,
                "audience.motion.bodyMotionSpeed" => FanlightAudienceBodyFields.MotionSpeed,
                "audience.motion.upperBodyLeanMotion" => FanlightAudienceBodyFields.LeanMotion,
                _ => FanlightAudienceBodyFields.None
            };
            if (fields == FanlightAudienceBodyFields.None) return false;
            var number = Convert.ToSingle(value);
            builder.SetAudienceBody(fields, new FanlightAudienceBodyState(
                fields == FanlightAudienceBodyFields.Height ? number : current.Height,
                fields == FanlightAudienceBodyFields.HeightVariation ? number : current.HeightVariation,
                fields == FanlightAudienceBodyFields.Width ? number : current.Width,
                fields == FanlightAudienceBodyFields.HeadSize ? number : current.HeadSize,
                fields == FanlightAudienceBodyFields.ShoulderHeightRatio ? number : current.ShoulderHeightRatio,
                fields == FanlightAudienceBodyFields.ShoulderSideOffset ? number : current.ShoulderSideOffset,
                fields == FanlightAudienceBodyFields.ArmWidth ? number : current.ArmWidth,
                fields == FanlightAudienceBodyFields.ArmLengthLimit ? number : current.ArmLengthLimit,
                fields == FanlightAudienceBodyFields.UpperBodyLeanMaximumRadians ? number : current.UpperBodyLeanMaximumRadians,
                fields == FanlightAudienceBodyFields.UpperBodyLean ? number : current.UpperBodyLean,
                fields == FanlightAudienceBodyFields.Bounce ? number : current.Bounce,
                fields == FanlightAudienceBodyFields.Sway ? number : current.Sway,
                fields == FanlightAudienceBodyFields.MotionSpeed ? number : current.MotionSpeed,
                fields == FanlightAudienceBodyFields.LeanMotion ? number : current.LeanMotion));
            return true;
        }

        private static bool TryAddDirection(FanlightShowPatchBuilder builder, string path, object value, FanlightShowState state)
        {
            var current = state.Direction;
            switch (path)
            {
                case "motion.direction.swingYaw":
                    builder.SetDirection(FanlightDirectionFields.WorldYawDegrees, new FanlightDirectionState(current.Mode, Convert.ToSingle(value), current.AimStrength));
                    return true;
                case "motion.direction.aimStrength":
                    builder.SetDirection(FanlightDirectionFields.AimStrength, new FanlightDirectionState(current.Mode, current.WorldYawDegrees, Convert.ToSingle(value)));
                    return true;
                case "motion.direction.swingMode":
                    builder.SetDirection(FanlightDirectionFields.Mode, new FanlightDirectionState((FanlightSwingMode)value == FanlightSwingMode.WorldDirection ? FanlightDirectionMode.WorldDirection : FanlightDirectionMode.Target, current.WorldYawDegrees, current.AimStrength));
                    return true;
                default:
                    return false;
            }
        }

        private static bool TryAddPalette(FanlightShowPatchBuilder builder, string path, object value, FanlightShowState state)
        {
            var current = state.Palette;
            var fields = path switch
            {
                "color.slot1" => FanlightPaletteFields.Slot1,
                "color.slot2" => FanlightPaletteFields.Slot2,
                "color.slot3" => FanlightPaletteFields.Slot3,
                "color.slot4" => FanlightPaletteFields.Slot4,
                "color.slot5" => FanlightPaletteFields.Slot5,
                "color.slot6" => FanlightPaletteFields.Slot6,
                "color.intensity" => FanlightPaletteFields.GlobalIntensity,
                "color.randomIntensity" => FanlightPaletteFields.RandomIntensity,
                _ => FanlightPaletteFields.None
            };
            if (fields == FanlightPaletteFields.None) return false;
            var color = value is Color incoming ? incoming : default;
            var number = value is Color ? 0f : Convert.ToSingle(value);
            builder.SetPalette(fields, new FanlightPaletteState(
                fields == FanlightPaletteFields.Slot1 ? color : current.Slot1,
                fields == FanlightPaletteFields.Slot2 ? color : current.Slot2,
                fields == FanlightPaletteFields.Slot3 ? color : current.Slot3,
                fields == FanlightPaletteFields.Slot4 ? color : current.Slot4,
                fields == FanlightPaletteFields.Slot5 ? color : current.Slot5,
                fields == FanlightPaletteFields.Slot6 ? color : current.Slot6,
                fields == FanlightPaletteFields.GlobalIntensity ? number : current.GlobalIntensity,
                fields == FanlightPaletteFields.RandomIntensity ? number : current.RandomIntensity));
            return true;
        }

        private static bool TryAddVisibility(FanlightShowPatchBuilder builder, string path, object value, FanlightShowState state)
        {
            if (path != "audience.enabled") return false;
            builder.SetVisibility(FanlightVisibilityFields.AudienceBodiesEnabled,
                new FanlightVisibilityState(state.Visibility.PenlightsEnabled, Convert.ToBoolean(value)));
            return true;
        }

        private static FanlightColorSettings ToColor(FanlightPaletteState value) => new()
        {
            slot1 = value.Slot1,
            slot2 = value.Slot2,
            slot3 = value.Slot3,
            slot4 = value.Slot4,
            slot5 = value.Slot5,
            slot6 = value.Slot6,
            intensity = value.GlobalIntensity,
            randomIntensity = value.RandomIntensity
        };
    }
}

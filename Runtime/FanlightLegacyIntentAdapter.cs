using System;
using System.Collections.Generic;
using PrismFanlight.Core;
using UnityEngine;

namespace PrismFanlight
{
    public static class FanlightLegacyIntentAdapter
    {
        public static FanlightResolvedIntent ToIntent(
            FanlightMotionSettings motion,
            FanlightColorSettings color,
            FanlightAudienceSettings audience)
        {
            motion = motion.Validated();
            color = color.Validated();
            audience = audience.Validated();
            var palette = ToPalette(color);
            var direction = ToDirection(motion.direction);
            var handZone = ToHandZone(audience.handZone);
            return new FanlightResolvedIntent(
                "Wave",
                handZone,
                Mathf.Clamp01(motion.human.enthusiasm * 0.5f),
                1f - motion.human.lazyFanRatio,
                1f - motion.swing.randomPhase,
                1f,
                Mathf.Clamp01(audience.handZone.reachScale / 1.28f),
                direction,
                palette,
                true,
                audience.enabled,
                new FanlightExpertPatch(CreateExpertValues(motion, audience)));
        }

        public static FanlightIntentPatch ToFullPatch(FanlightResolvedState state)
        {
            var intent = ToIntent(state.Motion, state.Color, state.Audience);
            var builder = new FanlightIntentPatchBuilder()
                .SetGesture(intent.GestureId)
                .SetHandZone(intent.HandZone)
                .SetEnergy(intent.Energy)
                .SetParticipation(intent.Participation)
                .SetSynchronization(intent.Synchronization)
                .SetRealism(intent.Realism)
                .SetReach(intent.Reach)
                .SetDirection(intent.Direction)
                .SetPalette(new FanlightPalettePatch(intent.Palette, FanlightPaletteFieldMask.All))
                .SetPenlightsEnabled(intent.PenlightsEnabled)
                .SetAudienceBodiesEnabled(intent.AudienceBodiesEnabled);
            var expert = intent.Expert.Values.Span;
            for (var i = 0; i < expert.Length; i++) builder.SetExpert(expert[i]);
            return builder.Build();
        }

        public static FanlightResolvedState ToLegacyState(in FanlightShowSample sample, in FanlightResolvedState template)
        {
            var motion = template.Motion.Validated();
            var audience = template.Audience.Validated();
            var color = ToColor(sample.Intent.Palette);

            motion.human.enthusiasm = sample.Intent.Energy * 2f;
            motion.human.lazyFanRatio = 1f - sample.Intent.Participation;
            motion.swing.randomPhase = 1f - sample.Intent.Synchronization;
            motion.direction.swingMode = sample.Intent.Direction.Mode == FanlightDirectionMode.WorldDirection
                ? FanlightSwingMode.WorldDirection
                : FanlightSwingMode.Target;
            motion.direction.swingYaw = sample.Intent.Direction.WorldYawDegrees;
            motion.direction.aimStrength = sample.Intent.Direction.AimStrength;
            audience.handZone = FromHandZone(sample.Intent.HandZone, audience.handZone);
            audience.enabled = sample.Intent.AudienceBodiesEnabled;
            if (!sample.Intent.PenlightsEnabled) color.intensity = 0f;

            var expert = sample.Intent.Expert.Values.Span;
            for (var i = 0; i < expert.Length; i++) ApplyExpert(ref motion, ref audience, expert[i]);
            var random = template.Random;
            random.globalSeed = sample.GlobalSeed;
            return new FanlightResolvedState(
                FanlightTempoState.FromMusicalPosition(sample.ClockStatus is FanlightClockStatus.Ready or FanlightClockStatus.Holding, sample.MusicalPosition),
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

        public static void AddLegacyParameter(
            FanlightIntentPatchBuilder builder,
            string path,
            object value,
            FanlightResolvedIntent baseIntent) =>
            TryAddLegacyParameter(builder, path, value, baseIntent);

        public static bool TryAddLegacyParameter(
            FanlightIntentPatchBuilder builder,
            string path,
            object value,
            FanlightResolvedIntent baseIntent)
        {
            if (builder == null || string.IsNullOrEmpty(path) || value == null) return false;
            switch (path)
            {
                case "color.slot1": MergeColor(builder, baseIntent.Palette, (Color)value, FanlightPaletteFieldMask.Slot1); return true;
                case "color.slot2": MergeColor(builder, baseIntent.Palette, (Color)value, FanlightPaletteFieldMask.Slot2); return true;
                case "color.slot3": MergeColor(builder, baseIntent.Palette, (Color)value, FanlightPaletteFieldMask.Slot3); return true;
                case "color.slot4": MergeColor(builder, baseIntent.Palette, (Color)value, FanlightPaletteFieldMask.Slot4); return true;
                case "color.slot5": MergeColor(builder, baseIntent.Palette, (Color)value, FanlightPaletteFieldMask.Slot5); return true;
                case "color.slot6": MergeColor(builder, baseIntent.Palette, (Color)value, FanlightPaletteFieldMask.Slot6); return true;
                case "color.intensity": MergeFloat(builder, baseIntent.Palette, (float)value, FanlightPaletteFieldMask.GlobalIntensity); return true;
                case "color.randomIntensity": MergeFloat(builder, baseIntent.Palette, (float)value, FanlightPaletteFieldMask.RandomIntensity); return true;
                case "motion.swing.randomPhase": builder.SetSynchronization(1f - (float)value); return true;
                case "motion.human.enthusiasm": builder.SetEnergy((float)value * 0.5f); return true;
                case "motion.human.lazyFanRatio": builder.SetParticipation(1f - (float)value); return true;
                case "motion.beatSync.beatBlockDelay":
                    var blockDelay = (Vector2)value;
                    builder.SetExpert(F(FanlightExpertParameterId.VariationBlockDelayXBeats, blockDelay.x));
                    builder.SetExpert(F(FanlightExpertParameterId.VariationBlockDelayYBeats, blockDelay.y));
                    return true;
                case "motion.direction.swingYaw": builder.SetDirection(WithYaw(baseIntent.Direction, (float)value)); return true;
                case "motion.direction.aimStrength": builder.SetDirection(WithAim(baseIntent.Direction, (float)value)); return true;
                case "motion.direction.swingMode": builder.SetDirection(WithMode(baseIntent.Direction, (FanlightSwingMode)value)); return true;
                case "audience.enabled": builder.SetAudienceBodiesEnabled((bool)value); return true;
                case "audience.handZone.zone": builder.SetHandZone(WithZone(baseIntent.HandZone, (FanlightHandZone)value)); return true;
                case "audience.handZone.heightOffset": builder.SetHandZone(WithHeight(baseIntent.HandZone, (float)value)); return true;
                case "audience.handZone.forwardOffset": builder.SetHandZone(WithForward(baseIntent.HandZone, (float)value)); return true;
                case "audience.handZone.reachScale": builder.SetReach(Mathf.Clamp01((float)value / 1.28f)); return true;
            }

            if (!TryMapExpert(path, value, out var expert)) return false;
            builder.SetExpert(expert);
            return true;
        }

        public static FanlightPaletteIntent ToPalette(FanlightColorSettings value) => new(
            value.slot1, value.slot2, value.slot3, value.slot4, value.slot5, value.slot6,
            value.intensity, value.randomIntensity);

        private static FanlightColorSettings ToColor(FanlightPaletteIntent value) => new()
        {
            slot1 = value.Slot1, slot2 = value.Slot2, slot3 = value.Slot3,
            slot4 = value.Slot4, slot5 = value.Slot5, slot6 = value.Slot6,
            intensity = Mathf.Max(0f, value.GlobalIntensity),
            randomIntensity = Mathf.Clamp01(value.RandomIntensity)
        };

        private static FanlightDirectionIntent ToDirection(FanlightDirectionSettings value) => new(
            value.swingMode == FanlightSwingMode.WorldDirection ? FanlightDirectionMode.WorldDirection : FanlightDirectionMode.TargetPoint,
            value.swingYaw,
            0f,
            Vector3.zero,
            string.Empty,
            value.aimStrength,
            value.swingYaw,
            0f);

        private static FanlightHandZoneIntent ToHandZone(FanlightHandZoneSettings value) => new(
            value.zone switch
            {
                FanlightHandZone.Chest => FanlightHandZoneId.Chest,
                FanlightHandZone.Face => FanlightHandZoneId.Face,
                FanlightHandZone.Overhead => FanlightHandZoneId.Overhead,
                FanlightHandZone.High => FanlightHandZoneId.High,
                _ => FanlightHandZoneId.Shoulder
            },
            value.heightOffset,
            value.forwardOffset,
            0f);

        private static FanlightHandZoneSettings FromHandZone(FanlightHandZoneIntent value, FanlightHandZoneSettings template)
        {
            template.zone = value.Zone switch
            {
                FanlightHandZoneId.Chest => FanlightHandZone.Chest,
                FanlightHandZoneId.Face => FanlightHandZone.Face,
                FanlightHandZoneId.Overhead => FanlightHandZone.Overhead,
                FanlightHandZoneId.High => FanlightHandZone.High,
                _ => FanlightHandZone.Shoulder
            };
            template.heightOffset = value.HeightOffset;
            template.forwardOffset = value.ForwardOffset;
            return template.Validated();
        }

        private static FanlightExpertParameterValue[] CreateExpertValues(FanlightMotionSettings motion, FanlightAudienceSettings audience)
        {
            var values = new List<FanlightExpertParameterValue>(56)
            {
                F(FanlightExpertParameterId.GestureBeatsPerCycle, motion.beatSync.beatsPerSwing),
                F(FanlightExpertParameterId.GesturePhaseOffset, motion.beatSync.beatPhaseOffset),
                F(FanlightExpertParameterId.GestureHoldRatio, motion.swing.peakHold),
                F(FanlightExpertParameterId.GestureCrispness, motion.swing.crispness),
                F(FanlightExpertParameterId.GestureFollowThrough, motion.swing.followThrough),
                F(FanlightExpertParameterId.GestureDownbeatAccent, motion.beatSync.downbeatAccent),
                F(FanlightExpertParameterId.PoseArmLengthMinimum, motion.swing.armLengthMin),
                F(FanlightExpertParameterId.PoseArmLengthMaximum, motion.swing.armLengthMax),
                F(FanlightExpertParameterId.PoseAngleMinimumRadians, motion.swing.minAngle),
                F(FanlightExpertParameterId.PoseAngleMaximumRadians, motion.swing.maxAngle),
                F(FanlightExpertParameterId.PoseHorizontalRatio, motion.swing.horizontalRatio),
                F(FanlightExpertParameterId.PoseWristFrequencyMultiplier, motion.swing.wristSwingSpeed),
                F(FanlightExpertParameterId.PoseWristAngleRadians, motion.swing.wristSwingAngle),
                F(FanlightExpertParameterId.PoseBodyLean, motion.swing.lean),
                F(FanlightExpertParameterId.PoseBodyBounce, audience.motion.bodyBounce),
                F(FanlightExpertParameterId.PoseBodySway, audience.motion.bodySway),
                F(FanlightExpertParameterId.PoseBodyMotionSpeed, audience.motion.bodyMotionSpeed),
                F(FanlightExpertParameterId.PoseUpperBodyLeanMotion, audience.motion.upperBodyLeanMotion),
                F(FanlightExpertParameterId.VariationSeatPosition, motion.human.seatJitter),
                F(FanlightExpertParameterId.VariationBodyHeight, motion.human.heightJitter),
                F(FanlightExpertParameterId.VariationArmLength, motion.human.armLengthJitter),
                F(FanlightExpertParameterId.VariationAngle, motion.swing.angleNoise),
                F(FanlightExpertParameterId.VariationDirectionSpread, motion.direction.directionSpread),
                F(FanlightExpertParameterId.VariationReactionDelaySeconds, motion.human.reactionDelay),
                F(FanlightExpertParameterId.VariationBeatJitter, motion.beatSync.beatSeatJitter),
                F(FanlightExpertParameterId.VariationBlockDelayXBeats, motion.beatSync.beatBlockDelay.x),
                F(FanlightExpertParameterId.VariationBlockDelayYBeats, motion.beatSync.beatBlockDelay.y),
                F(FanlightExpertParameterId.VariationEnergyResponse, motion.human.enthusiasmVariation),
                F(FanlightExpertParameterId.VariationSpeed, motion.human.speedVariation),
                F(FanlightExpertParameterId.VariationBeatReactionDelaySeconds, motion.beatSync.beatReactionDelay),
                F(FanlightExpertParameterId.VariationHandZone, audience.handZone.variation),
                F(FanlightExpertParameterId.NoisePhaseAmount, motion.noise.phaseIrregularity),
                F(FanlightExpertParameterId.NoisePhaseSpeed, motion.noise.phaseIrregularitySpeed),
                F(FanlightExpertParameterId.NoiseAxisAmount, motion.noise.axisNoiseAmount),
                F(FanlightExpertParameterId.NoiseAxisSpeed, motion.noise.axisNoiseSpeed),
                I(FanlightExpertParameterId.NoiseOctaves, motion.noise.noiseOctaves),
                F(FanlightExpertParameterId.NoisePersistence, motion.noise.noiseDetail),
                F(FanlightExpertParameterId.RestProbability, motion.human.restProbability),
                F(FanlightExpertParameterId.RestMotionLevel, motion.human.restMotionLevel),
                F(FanlightExpertParameterId.RestCycleSeconds, motion.human.restCycleDuration),
                F(FanlightExpertParameterId.RestDurationSeconds, motion.human.restDuration),
                F(FanlightExpertParameterId.RestFadeSeconds, motion.human.restFadeDuration),
                F(FanlightExpertParameterId.RestPhaseRandomness, motion.human.restPhaseRandomness),
                F(FanlightExpertParameterId.BodyHeight, audience.bodyHeight),
                F(FanlightExpertParameterId.BodyHeightVariation, audience.bodyHeightJitter),
                F(FanlightExpertParameterId.BodyWidth, audience.bodyWidth),
                F(FanlightExpertParameterId.BodyHeadSize, audience.headSize),
                F(FanlightExpertParameterId.BodyShoulderHeightRatio, audience.shoulderHeight),
                F(FanlightExpertParameterId.BodyShoulderSideOffset, audience.shoulderOffset),
                F(FanlightExpertParameterId.BodyArmWidth, audience.armWidth),
                F(FanlightExpertParameterId.BodyArmLengthLimit, audience.armLengthLimit),
                F(FanlightExpertParameterId.BodyUpperBodyLeanMaximum, audience.upperBodyLeanMax),
                F(FanlightExpertParameterId.BodyUpperBodyLean, audience.upperBodyLean)
            };
            values.Sort((left, right) => ((int)left.ParameterId).CompareTo((int)right.ParameterId));
            return values.ToArray();
        }

        private static void ApplyExpert(ref FanlightMotionSettings motion, ref FanlightAudienceSettings audience, FanlightExpertParameterValue value)
        {
            var f = value.ValueKind == FanlightExpertValueKind.Integer ? value.IntegerValue : value.FloatValue;
            switch (value.ParameterId)
            {
                case FanlightExpertParameterId.GestureBeatsPerCycle: motion.beatSync.beatsPerSwing = f; break;
                case FanlightExpertParameterId.GesturePhaseOffset: motion.beatSync.beatPhaseOffset = f; break;
                case FanlightExpertParameterId.GestureHoldRatio: motion.swing.peakHold = f; break;
                case FanlightExpertParameterId.GestureCrispness: motion.swing.crispness = f; break;
                case FanlightExpertParameterId.GestureFollowThrough: motion.swing.followThrough = f; break;
                case FanlightExpertParameterId.GestureDownbeatAccent: motion.beatSync.downbeatAccent = f; break;
                case FanlightExpertParameterId.PoseArmLengthMinimum: motion.swing.armLengthMin = f; break;
                case FanlightExpertParameterId.PoseArmLengthMaximum: motion.swing.armLengthMax = f; break;
                case FanlightExpertParameterId.PoseAngleMinimumRadians: motion.swing.minAngle = f; break;
                case FanlightExpertParameterId.PoseAngleMaximumRadians: motion.swing.maxAngle = f; break;
                case FanlightExpertParameterId.PoseHorizontalRatio: motion.swing.horizontalRatio = f; break;
                case FanlightExpertParameterId.PoseWristFrequencyMultiplier: motion.swing.wristSwingSpeed = f; break;
                case FanlightExpertParameterId.PoseWristAngleRadians: motion.swing.wristSwingAngle = f; break;
                case FanlightExpertParameterId.PoseBodyLean: motion.swing.lean = f; break;
                case FanlightExpertParameterId.PoseBodyBounce: audience.motion.bodyBounce = f; break;
                case FanlightExpertParameterId.PoseBodySway: audience.motion.bodySway = f; break;
                case FanlightExpertParameterId.PoseBodyMotionSpeed: audience.motion.bodyMotionSpeed = f; break;
                case FanlightExpertParameterId.PoseUpperBodyLeanMotion: audience.motion.upperBodyLeanMotion = f; break;
                case FanlightExpertParameterId.VariationSeatPosition: motion.human.seatJitter = f; break;
                case FanlightExpertParameterId.VariationBodyHeight: motion.human.heightJitter = f; break;
                case FanlightExpertParameterId.VariationArmLength: motion.human.armLengthJitter = f; break;
                case FanlightExpertParameterId.VariationAngle: motion.swing.angleNoise = f; break;
                case FanlightExpertParameterId.VariationDirectionSpread: motion.direction.directionSpread = f; break;
                case FanlightExpertParameterId.VariationReactionDelaySeconds: motion.human.reactionDelay = f; break;
                case FanlightExpertParameterId.VariationBeatJitter: motion.beatSync.beatSeatJitter = f; break;
                case FanlightExpertParameterId.VariationBlockDelayXBeats: motion.beatSync.beatBlockDelay.x = f; break;
                case FanlightExpertParameterId.VariationBlockDelayYBeats: motion.beatSync.beatBlockDelay.y = f; break;
                case FanlightExpertParameterId.VariationEnergyResponse: motion.human.enthusiasmVariation = f; break;
                case FanlightExpertParameterId.VariationSpeed: motion.human.speedVariation = f; break;
                case FanlightExpertParameterId.VariationBeatReactionDelaySeconds: motion.beatSync.beatReactionDelay = f; break;
                case FanlightExpertParameterId.VariationHandZone: audience.handZone.variation = f; break;
                case FanlightExpertParameterId.NoisePhaseAmount: motion.noise.phaseIrregularity = f; break;
                case FanlightExpertParameterId.NoisePhaseSpeed: motion.noise.phaseIrregularitySpeed = f; break;
                case FanlightExpertParameterId.NoiseAxisAmount: motion.noise.axisNoiseAmount = f; break;
                case FanlightExpertParameterId.NoiseAxisSpeed: motion.noise.axisNoiseSpeed = f; break;
                case FanlightExpertParameterId.NoiseOctaves: motion.noise.noiseOctaves = value.IntegerValue; break;
                case FanlightExpertParameterId.NoisePersistence: motion.noise.noiseDetail = f; break;
                case FanlightExpertParameterId.RestProbability: motion.human.restProbability = f; break;
                case FanlightExpertParameterId.RestMotionLevel: motion.human.restMotionLevel = f; break;
                case FanlightExpertParameterId.RestCycleSeconds: motion.human.restCycleDuration = f; break;
                case FanlightExpertParameterId.RestDurationSeconds: motion.human.restDuration = f; break;
                case FanlightExpertParameterId.RestFadeSeconds: motion.human.restFadeDuration = f; break;
                case FanlightExpertParameterId.RestPhaseRandomness: motion.human.restPhaseRandomness = f; break;
                case FanlightExpertParameterId.BodyHeight: audience.bodyHeight = f; break;
                case FanlightExpertParameterId.BodyHeightVariation: audience.bodyHeightJitter = f; break;
                case FanlightExpertParameterId.BodyWidth: audience.bodyWidth = f; break;
                case FanlightExpertParameterId.BodyHeadSize: audience.headSize = f; break;
                case FanlightExpertParameterId.BodyShoulderHeightRatio: audience.shoulderHeight = f; break;
                case FanlightExpertParameterId.BodyShoulderSideOffset: audience.shoulderOffset = f; break;
                case FanlightExpertParameterId.BodyArmWidth: audience.armWidth = f; break;
                case FanlightExpertParameterId.BodyArmLengthLimit: audience.armLengthLimit = f; break;
                case FanlightExpertParameterId.BodyUpperBodyLeanMaximum: audience.upperBodyLeanMax = f; break;
                case FanlightExpertParameterId.BodyUpperBodyLean: audience.upperBodyLean = f; break;
            }
        }

        private static bool TryMapExpert(string path, object value, out FanlightExpertParameterValue expert)
        {
            var mapped = path switch
            {
                "motion.swing.armLengthMin" => FanlightExpertParameterId.PoseArmLengthMinimum,
                "motion.swing.armLengthMax" => FanlightExpertParameterId.PoseArmLengthMaximum,
                "motion.swing.minAngle" => FanlightExpertParameterId.PoseAngleMinimumRadians,
                "motion.swing.maxAngle" => FanlightExpertParameterId.PoseAngleMaximumRadians,
                "motion.swing.angleNoise" => FanlightExpertParameterId.VariationAngle,
                "motion.swing.crispness" => FanlightExpertParameterId.GestureCrispness,
                "motion.swing.peakHold" => FanlightExpertParameterId.GestureHoldRatio,
                "motion.swing.followThrough" => FanlightExpertParameterId.GestureFollowThrough,
                "motion.swing.lean" => FanlightExpertParameterId.PoseBodyLean,
                "motion.swing.horizontalRatio" => FanlightExpertParameterId.PoseHorizontalRatio,
                "motion.swing.wristSwingSpeed" => FanlightExpertParameterId.PoseWristFrequencyMultiplier,
                "motion.swing.wristSwingAngle" => FanlightExpertParameterId.PoseWristAngleRadians,
                "motion.direction.directionSpread" => FanlightExpertParameterId.VariationDirectionSpread,
                "motion.noise.phaseIrregularity" => FanlightExpertParameterId.NoisePhaseAmount,
                "motion.noise.phaseIrregularitySpeed" => FanlightExpertParameterId.NoisePhaseSpeed,
                "motion.noise.axisNoiseAmount" => FanlightExpertParameterId.NoiseAxisAmount,
                "motion.noise.axisNoiseSpeed" => FanlightExpertParameterId.NoiseAxisSpeed,
                "motion.noise.noiseOctaves" => FanlightExpertParameterId.NoiseOctaves,
                "motion.noise.noiseDetail" => FanlightExpertParameterId.NoisePersistence,
                "motion.human.reactionDelay" => FanlightExpertParameterId.VariationReactionDelaySeconds,
                "motion.human.seatJitter" => FanlightExpertParameterId.VariationSeatPosition,
                "motion.human.heightJitter" => FanlightExpertParameterId.VariationBodyHeight,
                "motion.human.armLengthJitter" => FanlightExpertParameterId.VariationArmLength,
                "motion.human.enthusiasmVariation" => FanlightExpertParameterId.VariationEnergyResponse,
                "motion.human.speedVariation" => FanlightExpertParameterId.VariationSpeed,
                "motion.human.restProbability" => FanlightExpertParameterId.RestProbability,
                "motion.human.restMotionLevel" => FanlightExpertParameterId.RestMotionLevel,
                "motion.human.restCycleDuration" => FanlightExpertParameterId.RestCycleSeconds,
                "motion.human.restDuration" => FanlightExpertParameterId.RestDurationSeconds,
                "motion.human.restFadeDuration" => FanlightExpertParameterId.RestFadeSeconds,
                "motion.human.restPhaseRandomness" => FanlightExpertParameterId.RestPhaseRandomness,
                "motion.beatSync.beatsPerSwing" => FanlightExpertParameterId.GestureBeatsPerCycle,
                "motion.beatSync.beatPhaseOffset" => FanlightExpertParameterId.GesturePhaseOffset,
                "motion.beatSync.downbeatAccent" => FanlightExpertParameterId.GestureDownbeatAccent,
                "motion.beatSync.beatSeatJitter" => FanlightExpertParameterId.VariationBeatJitter,
                "motion.beatSync.beatReactionDelay" => FanlightExpertParameterId.VariationBeatReactionDelaySeconds,
                "audience.bodyHeight" => FanlightExpertParameterId.BodyHeight,
                "audience.bodyHeightJitter" => FanlightExpertParameterId.BodyHeightVariation,
                "audience.bodyWidth" => FanlightExpertParameterId.BodyWidth,
                "audience.headSize" => FanlightExpertParameterId.BodyHeadSize,
                "audience.shoulderHeight" => FanlightExpertParameterId.BodyShoulderHeightRatio,
                "audience.shoulderOffset" => FanlightExpertParameterId.BodyShoulderSideOffset,
                "audience.armWidth" => FanlightExpertParameterId.BodyArmWidth,
                "audience.armLengthLimit" => FanlightExpertParameterId.BodyArmLengthLimit,
                "audience.upperBodyLeanMax" => FanlightExpertParameterId.BodyUpperBodyLeanMaximum,
                "audience.upperBodyLean" => FanlightExpertParameterId.BodyUpperBodyLean,
                "audience.handZone.variation" => FanlightExpertParameterId.VariationHandZone,
                "audience.motion.bodyBounce" => FanlightExpertParameterId.PoseBodyBounce,
                "audience.motion.bodySway" => FanlightExpertParameterId.PoseBodySway,
                "audience.motion.bodyMotionSpeed" => FanlightExpertParameterId.PoseBodyMotionSpeed,
                "audience.motion.upperBodyLeanMotion" => FanlightExpertParameterId.PoseUpperBodyLeanMotion,
                _ => (FanlightExpertParameterId)(-1)
            };
            if ((int)mapped < 0)
            {
                expert = default;
                return false;
            }
            expert = value is int integer ? I(mapped, integer) : F(mapped, Convert.ToSingle(value));
            return true;
        }

        private static FanlightExpertParameterValue F(FanlightExpertParameterId id, float value) => FanlightExpertParameterValue.Float(id, value);
        private static FanlightExpertParameterValue I(FanlightExpertParameterId id, int value) => FanlightExpertParameterValue.Integer(id, value);

        private static void MergeColor(FanlightIntentPatchBuilder builder, FanlightPaletteIntent palette, Color value, FanlightPaletteFieldMask field)
        {
            var incoming = field switch
            {
                FanlightPaletteFieldMask.Slot1 => new FanlightPaletteIntent(value, palette.Slot2, palette.Slot3, palette.Slot4, palette.Slot5, palette.Slot6, palette.GlobalIntensity, palette.RandomIntensity),
                FanlightPaletteFieldMask.Slot2 => new FanlightPaletteIntent(palette.Slot1, value, palette.Slot3, palette.Slot4, palette.Slot5, palette.Slot6, palette.GlobalIntensity, palette.RandomIntensity),
                FanlightPaletteFieldMask.Slot3 => new FanlightPaletteIntent(palette.Slot1, palette.Slot2, value, palette.Slot4, palette.Slot5, palette.Slot6, palette.GlobalIntensity, palette.RandomIntensity),
                FanlightPaletteFieldMask.Slot4 => new FanlightPaletteIntent(palette.Slot1, palette.Slot2, palette.Slot3, value, palette.Slot5, palette.Slot6, palette.GlobalIntensity, palette.RandomIntensity),
                FanlightPaletteFieldMask.Slot5 => new FanlightPaletteIntent(palette.Slot1, palette.Slot2, palette.Slot3, palette.Slot4, value, palette.Slot6, palette.GlobalIntensity, palette.RandomIntensity),
                _ => new FanlightPaletteIntent(palette.Slot1, palette.Slot2, palette.Slot3, palette.Slot4, palette.Slot5, value, palette.GlobalIntensity, palette.RandomIntensity)
            };
            builder.MergePalette(new FanlightPalettePatch(incoming, field));
        }

        private static void MergeFloat(FanlightIntentPatchBuilder builder, FanlightPaletteIntent palette, float value, FanlightPaletteFieldMask field)
        {
            var incoming = new FanlightPaletteIntent(palette.Slot1, palette.Slot2, palette.Slot3, palette.Slot4, palette.Slot5, palette.Slot6,
                field == FanlightPaletteFieldMask.GlobalIntensity ? value : palette.GlobalIntensity,
                field == FanlightPaletteFieldMask.RandomIntensity ? value : palette.RandomIntensity);
            builder.MergePalette(new FanlightPalettePatch(incoming, field));
        }

        private static FanlightDirectionIntent WithYaw(FanlightDirectionIntent value, float yaw) => new(value.Mode, yaw, value.WorldPitchDegrees, value.TargetWorldPosition, value.TargetBindingId, value.AimStrength, value.FallbackWorldYawDegrees, value.FallbackWorldPitchDegrees);
        private static FanlightDirectionIntent WithAim(FanlightDirectionIntent value, float aim) => new(value.Mode, value.WorldYawDegrees, value.WorldPitchDegrees, value.TargetWorldPosition, value.TargetBindingId, aim, value.FallbackWorldYawDegrees, value.FallbackWorldPitchDegrees);
        private static FanlightDirectionIntent WithMode(FanlightDirectionIntent value, FanlightSwingMode mode) => new(mode == FanlightSwingMode.WorldDirection ? FanlightDirectionMode.WorldDirection : FanlightDirectionMode.TargetPoint, value.WorldYawDegrees, value.WorldPitchDegrees, value.TargetWorldPosition, value.TargetBindingId, value.AimStrength, value.FallbackWorldYawDegrees, value.FallbackWorldPitchDegrees);
        private static FanlightHandZoneIntent WithZone(FanlightHandZoneIntent value, FanlightHandZone zone) => new(ToHandZone(new FanlightHandZoneSettings { zone = zone, reachScale = 1f }).Zone, value.HeightOffset, value.ForwardOffset, value.SideOffset);
        private static FanlightHandZoneIntent WithHeight(FanlightHandZoneIntent value, float height) => new(value.Zone, height, value.ForwardOffset, value.SideOffset);
        private static FanlightHandZoneIntent WithForward(FanlightHandZoneIntent value, float forward) => new(value.Zone, value.HeightOffset, forward, value.SideOffset);
    }
}

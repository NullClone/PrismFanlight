using UnityEngine;

namespace PrismFanlight
{
    internal static class FanlightStateComposer
    {
        private const float WeightEpsilon = 0.0001f;
        private const float DiscreteSwitchWeight = 0.5f;

        public static FanlightResolvedState ApplyColor(
            FanlightResolvedState baseState,
            FanlightColorSettings weightedColor,
            float totalWeight)
        {
            if (totalWeight <= WeightEpsilon) return baseState;

            return With(baseState, color: BlendColorSettings(baseState.Color, weightedColor, Mathf.Clamp01(totalWeight)));
        }

        public static FanlightResolvedState ApplyMotion(
            FanlightResolvedState baseState,
            FanlightMotionSettings weightedMotion,
            float totalWeight)
        {
            if (totalWeight <= WeightEpsilon) return baseState;

            return With(baseState, motion: BlendMotion(baseState.Motion, weightedMotion, Mathf.Clamp01(totalWeight)));
        }

        public static FanlightResolvedState ApplyTempo(
            FanlightResolvedState baseState,
            FanlightTempoSettings weightedTempo,
            float totalWeight,
            float timelineTime)
        {
            if (totalWeight <= WeightEpsilon) return baseState;

            var weight = Mathf.Clamp01(totalWeight);
            var tempo = weightedTempo.Validated();
            var bpm = Mathf.Lerp(baseState.Tempo.Bpm, tempo.bpm, weight);
            var beatsPerBar = weight >= DiscreteSwitchWeight
                ? tempo.beatsPerBar
                : baseState.Tempo.BeatsPerBar;
            var cueSongTime = Mathf.Max(0.0f, timelineTime - tempo.offsetSeconds + tempo.latencyCompensationSeconds);
            var songTime = Mathf.Lerp(baseState.Tempo.SongTime, cueSongTime, weight);

            // The cue can override tempo settings, but Timeline owns absolute song time.
            return With(baseState, tempo: FanlightTempoState.FromSongTime(true, songTime, bpm, beatsPerBar));
        }

        public static FanlightResolvedState ApplyAudience(
            FanlightResolvedState baseState,
            FanlightAudienceSettings weightedAudience,
            float totalWeight)
        {
            if (totalWeight <= WeightEpsilon) return baseState;

            return With(baseState, audience: BlendAudience(baseState.Audience, weightedAudience, Mathf.Clamp01(totalWeight)));
        }

        internal static FanlightMotionSettings BlendMotion(FanlightMotionSettings from, FanlightMotionSettings to, float weight)
        {
            var t = Mathf.Clamp01(weight);
            from = from.Validated();
            to = to.Validated();

            return new FanlightMotionSettings
            {
                swing = new FanlightSwingSettings
                {
                    randomPhase = Mathf.Lerp(from.swing.randomPhase, to.swing.randomPhase, t), armLengthMin = Mathf.Lerp(from.swing.armLengthMin, to.swing.armLengthMin, t), armLengthMax = Mathf.Lerp(from.swing.armLengthMax, to.swing.armLengthMax, t), minAngle = Mathf.Lerp(from.swing.minAngle, to.swing.minAngle, t), maxAngle = Mathf.Lerp(from.swing.maxAngle, to.swing.maxAngle, t), angleNoise = Mathf.Lerp(from.swing.angleNoise, to.swing.angleNoise, t), crispness = Mathf.Lerp(from.swing.crispness, to.swing.crispness, t), peakHold = Mathf.Lerp(from.swing.peakHold, to.swing.peakHold, t), followThrough = Mathf.Lerp(from.swing.followThrough, to.swing.followThrough, t), lean = Mathf.Lerp(from.swing.lean, to.swing.lean, t), horizontalRatio = Mathf.Lerp(from.swing.horizontalRatio, to.swing.horizontalRatio, t), wristSwingSpeed = Mathf.Lerp(from.swing.wristSwingSpeed, to.swing.wristSwingSpeed, t), wristSwingAngle = Mathf.Lerp(from.swing.wristSwingAngle, to.swing.wristSwingAngle, t)
                },
                direction = new FanlightDirectionSettings
                {
                    swingMode = t >= DiscreteSwitchWeight ? to.direction.swingMode : from.direction.swingMode, swingYaw = Mathf.LerpAngle(from.direction.swingYaw, to.direction.swingYaw, t), directionSpread = Mathf.Lerp(from.direction.directionSpread, to.direction.directionSpread, t), aimStrength = Mathf.Lerp(from.direction.aimStrength, to.direction.aimStrength, t)
                },
                noise = new FanlightNoiseSettings
                {
                    phaseIrregularity = Mathf.Lerp(from.noise.phaseIrregularity, to.noise.phaseIrregularity, t), phaseIrregularitySpeed = Mathf.Lerp(from.noise.phaseIrregularitySpeed, to.noise.phaseIrregularitySpeed, t), axisNoiseAmount = Mathf.Lerp(from.noise.axisNoiseAmount, to.noise.axisNoiseAmount, t), axisNoiseSpeed = Mathf.Lerp(from.noise.axisNoiseSpeed, to.noise.axisNoiseSpeed, t), noiseOctaves = Mathf.RoundToInt(Mathf.Lerp(from.noise.noiseOctaves, to.noise.noiseOctaves, t)), noiseDetail = Mathf.Lerp(from.noise.noiseDetail, to.noise.noiseDetail, t)
                },
                human = new FanlightHumanSettings
                {
                    enthusiasm = Mathf.Lerp(from.human.enthusiasm, to.human.enthusiasm, t), enthusiasmVariation = Mathf.Lerp(from.human.enthusiasmVariation, to.human.enthusiasmVariation, t), lazyFanRatio = Mathf.Lerp(from.human.lazyFanRatio, to.human.lazyFanRatio, t), reactionDelay = Mathf.Lerp(from.human.reactionDelay, to.human.reactionDelay, t), speedVariation = Mathf.Lerp(from.human.speedVariation, to.human.speedVariation, t), seatJitter = Mathf.Lerp(from.human.seatJitter, to.human.seatJitter, t), heightJitter = Mathf.Lerp(from.human.heightJitter, to.human.heightJitter, t), armLengthJitter = Mathf.Lerp(from.human.armLengthJitter, to.human.armLengthJitter, t), restProbability = Mathf.Lerp(from.human.restProbability, to.human.restProbability, t), restMotionLevel = Mathf.Lerp(from.human.restMotionLevel, to.human.restMotionLevel, t), restCycleDuration = Mathf.Lerp(from.human.restCycleDuration, to.human.restCycleDuration, t), restDuration = Mathf.Lerp(from.human.restDuration, to.human.restDuration, t), restFadeDuration = Mathf.Lerp(from.human.restFadeDuration, to.human.restFadeDuration, t), restPhaseRandomness = Mathf.Lerp(from.human.restPhaseRandomness, to.human.restPhaseRandomness, t)
                },
                beatSync = new FanlightBeatSyncSettings
                {
                    beatsPerSwing = Mathf.Lerp(from.beatSync.beatsPerSwing, to.beatSync.beatsPerSwing, t), beatPhaseOffset = Mathf.Lerp(from.beatSync.beatPhaseOffset, to.beatSync.beatPhaseOffset, t), downbeatAccent = Mathf.Lerp(from.beatSync.downbeatAccent, to.beatSync.downbeatAccent, t), beatReactionDelay = Mathf.Lerp(from.beatSync.beatReactionDelay, to.beatSync.beatReactionDelay, t), beatSeatJitter = Mathf.Lerp(from.beatSync.beatSeatJitter, to.beatSync.beatSeatJitter, t), beatBlockDelay = Vector2.Lerp(from.beatSync.beatBlockDelay, to.beatSync.beatBlockDelay, t)
                }
            }.Validated();
        }

        internal static FanlightColorSettings BlendColorSettings(FanlightColorSettings from, FanlightColorSettings to, float weight)
        {
            var t = Mathf.Clamp01(weight);
            from = from.Validated();
            to = to.Validated();

            return new FanlightColorSettings
            {
                // Distribution mode and palette are discrete. They switch only
                // after the incoming cue meaningfully contributes to the blend.
                mode = t >= DiscreteSwitchWeight ? to.mode : from.mode,
                primaryColor = Color.Lerp(from.primaryColor, to.primaryColor, t),
                secondaryColor = Color.Lerp(from.secondaryColor, to.secondaryColor, t),
                paletteColors = t >= DiscreteSwitchWeight ? to.paletteColors : from.paletteColors,
                intensity = Mathf.Lerp(from.intensity, to.intensity, t),
                randomIntensity = Mathf.Lerp(from.randomIntensity, to.randomIntensity, t)
            }.Validated();
        }

        internal static FanlightTempoSettings BlendTempoSettings(FanlightTempoSettings from, FanlightTempoSettings to, float weight)
        {
            var t = Mathf.Clamp01(weight);
            from = from.Validated();
            to = to.Validated();
            return new FanlightTempoSettings
            {
                bpm = Mathf.Lerp(from.bpm, to.bpm, t), beatsPerBar = Mathf.RoundToInt(Mathf.Lerp(from.beatsPerBar, to.beatsPerBar, t)), offsetSeconds = Mathf.Lerp(from.offsetSeconds, to.offsetSeconds, t), latencyCompensationSeconds = Mathf.Lerp(from.latencyCompensationSeconds, to.latencyCompensationSeconds, t), clockSource = FanlightTempoClockSource.ManualTime, manualTime = 0.0f
            }.Validated();
        }

        internal static FanlightAudienceSettings BlendAudience(FanlightAudienceSettings from, FanlightAudienceSettings to, float weight)
        {
            var t = Mathf.Clamp01(weight);
            from = from.Validated();
            to = to.Validated();
            return new FanlightAudienceSettings
            {
                enabled = t >= DiscreteSwitchWeight ? to.enabled : from.enabled, bodyHeight = Mathf.Lerp(from.bodyHeight, to.bodyHeight, t), bodyHeightJitter = Mathf.Lerp(from.bodyHeightJitter, to.bodyHeightJitter, t), bodyWidth = Mathf.Lerp(from.bodyWidth, to.bodyWidth, t), headSize = Mathf.Lerp(from.headSize, to.headSize, t), shoulderHeight = Mathf.Lerp(from.shoulderHeight, to.shoulderHeight, t), shoulderOffset = Mathf.Lerp(from.shoulderOffset, to.shoulderOffset, t), armWidth = Mathf.Lerp(from.armWidth, to.armWidth, t), armLengthLimit = Mathf.Lerp(from.armLengthLimit, to.armLengthLimit, t), handZone = new FanlightHandZoneSettings
                {
                    zone = t >= DiscreteSwitchWeight ? to.handZone.zone : from.handZone.zone, heightOffset = Mathf.Lerp(from.handZone.heightOffset, to.handZone.heightOffset, t), forwardOffset = Mathf.Lerp(from.handZone.forwardOffset, to.handZone.forwardOffset, t), reachScale = Mathf.Lerp(from.handZone.reachScale, to.handZone.reachScale, t), variation = Mathf.Lerp(from.handZone.variation, to.handZone.variation, t)
                },
                upperBodyLean = Mathf.Lerp(from.upperBodyLean, to.upperBodyLean, t), upperBodyLeanMax = Mathf.Lerp(from.upperBodyLeanMax, to.upperBodyLeanMax, t), motion = new FanlightAudienceMotionSettings
                {
                    bodyBounce = Mathf.Lerp(from.motion.bodyBounce, to.motion.bodyBounce, t), bodySway = Mathf.Lerp(from.motion.bodySway, to.motion.bodySway, t), bodyMotionSpeed = Mathf.Lerp(from.motion.bodyMotionSpeed, to.motion.bodyMotionSpeed, t), upperBodyLeanMotion = Mathf.Lerp(from.motion.upperBodyLeanMotion, to.motion.upperBodyLeanMotion, t)
                }
            }.Validated();
        }

        private static FanlightResolvedState With(FanlightResolvedState state, FanlightTempoState? tempo = null, FanlightMotionSettings? motion = null, FanlightColorSettings? color = null, FanlightAudienceSettings? audience = null)
        {
            return new FanlightResolvedState(
                tempo ?? state.Tempo,
                motion ?? state.Motion,
                color ?? state.Color,
                audience ?? state.Audience,
                state.Lod,
                state.Random,
                state.SwingTargetWorldPosition,
                state.LocalToWorld,
                state.Time,
                state.UpdateClock,
                state.IsTimeJump);
        }
    }
}

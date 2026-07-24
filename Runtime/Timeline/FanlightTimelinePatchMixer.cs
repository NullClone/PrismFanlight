using System;
using PrismFanlight.Authoring;
using PrismFanlight.Core;
using UnityEngine;

namespace PrismFanlight.Timeline
{
    internal static class FanlightTimelinePatchMixer
    {
        internal static bool HasFields(FanlightTimelinePatchKind kind, FanlightTimelineFieldMask fields)
        {
            return kind switch
            {
                FanlightTimelinePatchKind.Intent => fields.Intent != FanlightIntentFields.None,
                FanlightTimelinePatchKind.Motion => fields.Motion != FanlightMotionFields.None,
                FanlightTimelinePatchKind.Variation => fields.Variation != FanlightVariationFields.None,
                FanlightTimelinePatchKind.Noise => fields.Noise != FanlightNoiseFields.None,
                FanlightTimelinePatchKind.Rest => fields.Rest != FanlightRestFields.None,
                FanlightTimelinePatchKind.AudienceBody => fields.AudienceBody != FanlightAudienceBodyFields.None,
                FanlightTimelinePatchKind.Direction => fields.Direction != FanlightDirectionFields.None,
                FanlightTimelinePatchKind.Color => fields.Color != FanlightColorFields.None,
                FanlightTimelinePatchKind.Intensity => fields.Intensity != FanlightIntensityFields.None,
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
                FanlightTimelinePatchKind.Motion => TryBlendMotion(fieldMask.Motion, samples, out patch),
                FanlightTimelinePatchKind.Variation => TryBlendVariation(fieldMask.Variation, samples, out patch),
                FanlightTimelinePatchKind.Noise => TryBlendNoise(fieldMask.Noise, samples, out patch),
                FanlightTimelinePatchKind.Rest => TryBlendRest(fieldMask.Rest, samples, out patch),
                FanlightTimelinePatchKind.AudienceBody => TryBlendAudienceBody(fieldMask.AudienceBody, samples, out patch),
                FanlightTimelinePatchKind.Direction => TryBlendDirection(fieldMask.Direction, samples, out patch),
                FanlightTimelinePatchKind.Color => TryBlendColor(fieldMask.Color, samples, out patch),
                FanlightTimelinePatchKind.Intensity => TryBlendIntensity(fieldMask.Intensity, samples, out patch),
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

        private static bool TryBlendMotion(
            FanlightMotionFields fields,
            ReadOnlySpan<FanlightTimelineClipSample> samples,
            out FanlightShowPatch patch)
        {
            ValidateMask((int)fields, (int)FanlightMotionFields.All);

            var beatsPerCycle = new FanlightWeightedFloat();
            var phaseOffsetBeats = new FanlightWeightedFloat();
            var motionAmount = new FanlightWeightedFloat();
            var heightBias = new FanlightWeightedFloat();
            var sideScale = new FanlightWeightedFloat();
            var forwardScale = new FanlightWeightedFloat();
            var wristDelayRatio = new FanlightWeightedFloat();
            var variation = new FanlightWeightedFloat();
            var assetA = default(FanlightMotionAsset);
            var assetB = default(FanlightMotionAsset);
            var assetWeights = Vector2.zero;

            for (var i = 0; i < samples.Length; i++)
            {
                var sample = samples[i];
                var sourceValue = sample.Value.Motion;
                if (Has(fields, FanlightMotionFields.MotionAsset)) AddAsset(sourceValue.MotionAsset, sample.Weight, ref assetA, ref assetB, ref assetWeights);
                if (Has(fields, FanlightMotionFields.BeatsPerCycle)) beatsPerCycle.Add(sourceValue.BeatsPerCycle, sample.Weight);
                if (Has(fields, FanlightMotionFields.PhaseOffsetBeats)) phaseOffsetBeats.Add(sourceValue.PhaseOffsetBeats, sample.Weight);
                if (Has(fields, FanlightMotionFields.MotionAmount)) motionAmount.Add(sourceValue.MotionAmount, sample.Weight);
                if (Has(fields, FanlightMotionFields.HeightBias)) heightBias.Add(sourceValue.HeightBias, sample.Weight);
                if (Has(fields, FanlightMotionFields.SideScale)) sideScale.Add(sourceValue.SideScale, sample.Weight);
                if (Has(fields, FanlightMotionFields.ForwardScale)) forwardScale.Add(sourceValue.ForwardScale, sample.Weight);
                if (Has(fields, FanlightMotionFields.WristDelayRatio)) wristDelayRatio.Add(sourceValue.WristDelayRatio, sample.Weight);
                if (Has(fields, FanlightMotionFields.Variation)) variation.Add(sourceValue.Variation, sample.Weight);
            }

            if (fields == FanlightMotionFields.None)
            {
                patch = default;
                return false;
            }

            var fallback = FanlightTimelineDefaults.MotionState();
            var value = FanlightMotionState.BlendAssets(
                Has(fields, FanlightMotionFields.MotionAsset) ? assetA : fallback.MotionAsset,
                Has(fields, FanlightMotionFields.MotionAsset) ? assetB : null,
                null,
                Has(fields, FanlightMotionFields.MotionAsset) ? new Vector3(assetWeights.x, assetWeights.y, 0f) : new Vector3(1f, 0f, 0f),
                beatsPerCycle.Value(fallback.BeatsPerCycle),
                phaseOffsetBeats.Value(fallback.PhaseOffsetBeats),
                motionAmount.Value(fallback.MotionAmount),
                heightBias.Value(fallback.HeightBias),
                sideScale.Value(fallback.SideScale),
                forwardScale.Value(fallback.ForwardScale),
                wristDelayRatio.Value(fallback.WristDelayRatio),
                variation.Value(fallback.Variation)
            );

            patch = new FanlightShowPatch(
                default,
                new FanlightMotionPatch(fields, value),
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
                new FanlightVariationPatch(fields, value),
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
                if (Has(fields, FanlightNoiseFields.Octaves)) octaves.Consider(sourceValue.Octaves, sample.Weight, sample.StartSeconds);
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
                new FanlightNoisePatch(fields, value),
                default,
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
                new FanlightRestPatch(fields, value),
                default,
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
                new FanlightAudienceBodyPatch(fields, value),
                default,
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
                if (Has(fields, FanlightDirectionFields.Mode)) mode.Consider(sourceValue.Mode, sample.Weight, sample.StartSeconds);
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
                new FanlightDirectionPatch(fields, value),
                default,
                default,
                default
            );

            return true;
        }

        private static bool TryBlendColor(
            FanlightColorFields fields,
            ReadOnlySpan<FanlightTimelineClipSample> samples,
            out FanlightShowPatch patch)
        {
            ValidateMask((int)fields, (int)FanlightColorFields.All);
            if (fields == FanlightColorFields.None)
            {
                patch = default;
                return false;
            }

            var sourceA = samples[0].Value.Color.Source;
            var sourceB = samples.Length > 1 ? samples[1].Value.Color.Source : default;
            var weights = new Vector3(
                samples[0].Weight,
                samples.Length > 1 ? samples[1].Weight : 0f,
                0f);
            var value = FanlightColorState.BlendSources(sourceA, sourceB, default, weights);

            patch = new FanlightShowPatch(
                default,
                default,
                default,
                default,
                default,
                default,
                default,
                new FanlightColorPatch(fields, value),
                default,
                default
            );

            return true;
        }

        private static bool TryBlendIntensity(
            FanlightIntensityFields fields,
            ReadOnlySpan<FanlightTimelineClipSample> samples,
            out FanlightShowPatch patch)
        {
            ValidateMask((int)fields, (int)FanlightIntensityFields.All);

            if (fields == FanlightIntensityFields.None)
            {
                patch = default;
                return false;
            }

            var baseIntensity = new FanlightWeightedFloat();
            var randomIntensity = new FanlightWeightedFloat();

            for (var i = 0; i < samples.Length; i++)
            {
                var sourceValue = samples[i].Value.Intensity;
                if (Has(fields, FanlightIntensityFields.BaseIntensity))
                {
                    baseIntensity.Add(sourceValue.BaseIntensity, samples[i].Weight);
                }

                if (Has(fields, FanlightIntensityFields.RandomIntensity))
                {
                    randomIntensity.Add(sourceValue.RandomIntensity, samples[i].Weight);
                }
            }

            var fallback = FanlightTimelineDefaults.IntensityState();
            var maskA = Has(fields, FanlightIntensityFields.SpatialMask)
                ? samples[0].Value.Intensity.SpatialMask
                : fallback.SpatialMask;
            var maskB = Has(fields, FanlightIntensityFields.SpatialMask) && samples.Length > 1
                ? samples[1].Value.Intensity.SpatialMask
                : default;
            var maskWeights = Has(fields, FanlightIntensityFields.SpatialMask)
                ? new Vector3(samples[0].Weight, samples.Length > 1 ? samples[1].Weight : 0f, 0f)
                : new Vector3(1f, 0f, 0f);
            var value = FanlightIntensityState.BlendMasks(
                baseIntensity.Value(fallback.BaseIntensity),
                randomIntensity.Value(fallback.RandomIntensity),
                maskA,
                maskB,
                default,
                maskWeights);

            patch = new FanlightShowPatch(
                default,
                default,
                default,
                default,
                default,
                default,
                default,
                default,
                new FanlightIntensityPatch(fields, value),
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
                if (Has(fields, FanlightVisibilityFields.PenlightsEnabled)) penlightsEnabled.Consider(sourceValue.PenlightsEnabled, sample.Weight, sample.StartSeconds);
                if (Has(fields, FanlightVisibilityFields.AudienceBodiesEnabled)) audienceBodiesEnabled.Consider(sourceValue.AudienceBodiesEnabled, sample.Weight, sample.StartSeconds);
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

        private static bool Has(FanlightMotionFields fields, FanlightMotionFields field) => (fields & field) != 0;

        private static bool Has(FanlightVariationFields fields, FanlightVariationFields field) => (fields & field) != 0;

        private static bool Has(FanlightNoiseFields fields, FanlightNoiseFields field) => (fields & field) != 0;

        private static bool Has(FanlightRestFields fields, FanlightRestFields field) => (fields & field) != 0;

        private static bool Has(FanlightAudienceBodyFields fields, FanlightAudienceBodyFields field) => (fields & field) != 0;

        private static bool Has(FanlightDirectionFields fields, FanlightDirectionFields field) => (fields & field) != 0;

        private static bool Has(FanlightColorFields fields, FanlightColorFields field) => (fields & field) != 0;

        private static bool Has(FanlightIntensityFields fields, FanlightIntensityFields field) => (fields & field) != 0;

        private static bool Has(FanlightVisibilityFields fields, FanlightVisibilityFields field) => (fields & field) != 0;

        private static void AddAsset(
            FanlightMotionAsset asset,
            float weight,
            ref FanlightMotionAsset assetA,
            ref FanlightMotionAsset assetB,
            ref Vector2 weights)
        {
            if (assetA == asset)
            {
                weights.x += weight;
                return;
            }

            if (weights.x <= 0.000001f)
            {
                assetA = asset;
                weights.x = weight;
                return;
            }

            if (assetB == asset)
            {
                weights.y += weight;
                return;
            }

            assetB = asset;
            weights.y += weight;
        }

        private static void ValidateMask(int fields, int all)
        {
            if ((fields & ~all) != 0) throw new ArgumentOutOfRangeException(nameof(fields));
        }
    }
}

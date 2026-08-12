using System;
using PrismFanlight.Authoring;
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
                Apply(state.Motion, patch.Motion, weight),
                Apply(state.Variation, patch.Variation, weight),
                Apply(state.Noise, patch.Noise, weight),
                Apply(state.Rest, patch.Rest, weight),
                Apply(state.AudienceBody, patch.AudienceBody, weight),
                Apply(state.Direction, patch.Direction, weight),
                Apply(state.Color, patch.Color, weight),
                Apply(state.Intensity, patch.Intensity, weight),
                state.Visibility,
                state.GlobalSeed);
        }

        internal static FanlightShowState Validate(FanlightShowState state)
        {
            if (state.Rest.DurationSeconds > state.Rest.CycleSeconds)
                throw new InvalidOperationException("Rest duration must not exceed its cycle.");

            return new FanlightShowState(
                Apply(state.Intent, new FanlightIntentPatch(FanlightIntentFields.All, state.Intent), 1f),
                Apply(state.Motion, new FanlightMotionPatch(FanlightMotionFields.All, state.Motion), 1f),
                Apply(state.Variation, new FanlightVariationPatch(FanlightVariationFields.All, state.Variation), 1f),
                new FanlightNoiseState(
                    state.Noise.PhaseAmount,
                    state.Noise.PhaseRate,
                    state.Noise.PositionAmount,
                    state.Noise.DirectionAmount,
                    state.Noise.SpatialRate,
                    state.Noise.Octaves,
                    state.Noise.Persistence),
                Apply(state.Rest, new FanlightRestPatch(FanlightRestFields.All, state.Rest), 1f),
                Apply(state.AudienceBody, new FanlightAudienceBodyPatch(FanlightAudienceBodyFields.All, state.AudienceBody), 1f),
                Apply(state.Direction, new FanlightDirectionPatch(FanlightDirectionFields.All, state.Direction), 1f),
                Apply(state.Color, new FanlightColorPatch(FanlightColorFields.All, state.Color), 1f),
                Apply(state.Intensity, new FanlightIntensityPatch(FanlightIntensityFields.All, state.Intensity), 1f),
                state.Visibility,
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

        internal static FanlightMotionState Apply(FanlightMotionState current, FanlightMotionPatch patch, float weight)
        {
            ValidateMask((int)patch.Fields, (int)FanlightMotionFields.All, nameof(patch));

            var value = patch.Value;
            var assetA = current.GetAsset(0);
            var assetB = current.GetAsset(1);
            var assetC = current.GetAsset(2);
            var assetWeights = new Vector3(
                current.GetAssetWeight(0),
                current.GetAssetWeight(1),
                current.GetAssetWeight(2));

            if (Has(patch.Fields, FanlightMotionFields.MotionAsset))
            {
                assetA = null;
                assetB = null;
                assetC = null;
                assetWeights = Vector3.zero;
                var currentWeight = 1f - weight;
                var incomingWeight = weight;

                for (var i = 0; i < 3; i++)
                {
                    AddAsset(
                        current.GetAsset(i),
                        current.GetAssetWeight(i) * currentWeight,
                        ref assetA,
                        ref assetB,
                        ref assetC,
                        ref assetWeights);
                    AddAsset(
                        value.GetAsset(i),
                        value.GetAssetWeight(i) * incomingWeight,
                        ref assetA,
                        ref assetB,
                        ref assetC,
                        ref assetWeights);
                }
            }

            return FanlightMotionState.BlendAssets(
                assetA,
                assetB,
                assetC,
                assetWeights,
                Has(patch.Fields, FanlightMotionFields.BeatsPerCycle) ? Lerp(current.BeatsPerCycle, value.BeatsPerCycle, weight) : current.BeatsPerCycle,
                Has(patch.Fields, FanlightMotionFields.PhaseOffsetBeats) ? Lerp(current.PhaseOffsetBeats, value.PhaseOffsetBeats, weight) : current.PhaseOffsetBeats,
                Has(patch.Fields, FanlightMotionFields.BlockDelayXBeats) ? Lerp(current.BlockDelayXBeats, value.BlockDelayXBeats, weight) : current.BlockDelayXBeats,
                Has(patch.Fields, FanlightMotionFields.BlockDelayYBeats) ? Lerp(current.BlockDelayYBeats, value.BlockDelayYBeats, weight) : current.BlockDelayYBeats,
                Has(patch.Fields, FanlightMotionFields.MotionAmount) ? Lerp(current.MotionAmount, value.MotionAmount, weight) : current.MotionAmount,
                Has(patch.Fields, FanlightMotionFields.HeightBias) ? Lerp(current.HeightBias, value.HeightBias, weight) : current.HeightBias,
                Has(patch.Fields, FanlightMotionFields.SideScale) ? Lerp(current.SideScale, value.SideScale, weight) : current.SideScale,
                Has(patch.Fields, FanlightMotionFields.ForwardScale) ? Lerp(current.ForwardScale, value.ForwardScale, weight) : current.ForwardScale,
                Has(patch.Fields, FanlightMotionFields.WristDelayRatio) ? Lerp(current.WristDelayRatio, value.WristDelayRatio, weight) : current.WristDelayRatio,
                Has(patch.Fields, FanlightMotionFields.Variation) ? Lerp(current.Variation, value.Variation, weight) : current.Variation);
        }

        internal static FanlightVariationState Apply(FanlightVariationState current, FanlightVariationPatch patch, float weight)
        {
            ValidateMask((int)patch.Fields, (int)FanlightVariationFields.All, nameof(patch));

            var value = patch.Value;
            return new FanlightVariationState(
                Has(patch.Fields, FanlightVariationFields.StandingPositionSpread) ? Lerp(current.StandingPositionSpread, value.StandingPositionSpread, weight) : current.StandingPositionSpread,
                Has(patch.Fields, FanlightVariationFields.HeightVariation) ? Lerp(current.HeightVariation, value.HeightVariation, weight) : current.HeightVariation,
                Has(patch.Fields, FanlightVariationFields.ArmExtensionVariation) ? Lerp(current.ArmExtensionVariation, value.ArmExtensionVariation, weight) : current.ArmExtensionVariation,
                Has(patch.Fields, FanlightVariationFields.PenlightDirectionSpread) ? Lerp(current.PenlightDirectionSpread, value.PenlightDirectionSpread, weight) : current.PenlightDirectionSpread,
                Has(patch.Fields, FanlightVariationFields.ReactionDelaySeconds) ? Lerp(current.ReactionDelaySeconds, value.ReactionDelaySeconds, weight) : current.ReactionDelaySeconds,
                Has(patch.Fields, FanlightVariationFields.BeatJitterBeats) ? Lerp(current.BeatJitterBeats, value.BeatJitterBeats, weight) : current.BeatJitterBeats,
                Has(patch.Fields, FanlightVariationFields.EnergyResponse) ? Lerp(current.EnergyResponse, value.EnergyResponse, weight) : current.EnergyResponse,
                Has(patch.Fields, FanlightVariationFields.HandPositionSpread) ? Lerp(current.HandPositionSpread, value.HandPositionSpread, weight) : current.HandPositionSpread);
        }

        internal static FanlightNoiseState Apply(FanlightNoiseState current, FanlightNoisePatch patch, float weight)
        {
            ValidateMask((int)patch.Fields, (int)FanlightNoiseFields.All, nameof(patch));

            var value = patch.Value;
            return new FanlightNoiseState(
                Has(patch.Fields, FanlightNoiseFields.PhaseAmount) ? Lerp(current.PhaseAmount, value.PhaseAmount, weight) : current.PhaseAmount,
                current.PhaseRate,
                Has(patch.Fields, FanlightNoiseFields.PositionAmount) ? Lerp(current.PositionAmount, value.PositionAmount, weight) : current.PositionAmount,
                Has(patch.Fields, FanlightNoiseFields.DirectionAmount) ? Lerp(current.DirectionAmount, value.DirectionAmount, weight) : current.DirectionAmount,
                current.SpatialRate,
                current.Octaves,
                current.Persistence);
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
                Has(patch.Fields, FanlightAudienceBodyFields.Width) ? Lerp(current.Width, value.Width, weight) : current.Width,
                Has(patch.Fields, FanlightAudienceBodyFields.HeadSize) ? Lerp(current.HeadSize, value.HeadSize, weight) : current.HeadSize,
                Has(patch.Fields, FanlightAudienceBodyFields.ShoulderHeightRatio) ? Lerp(current.ShoulderHeightRatio, value.ShoulderHeightRatio, weight) : current.ShoulderHeightRatio,
                Has(patch.Fields, FanlightAudienceBodyFields.ShoulderSideOffset) ? Lerp(current.ShoulderSideOffset, value.ShoulderSideOffset, weight) : current.ShoulderSideOffset,
                Has(patch.Fields, FanlightAudienceBodyFields.ArmWidth) ? Lerp(current.ArmWidth, value.ArmWidth, weight) : current.ArmWidth,
                Has(patch.Fields, FanlightAudienceBodyFields.ArmLengthLimit) ? Lerp(current.ArmLengthLimit, value.ArmLengthLimit, weight) : current.ArmLengthLimit,
                Has(patch.Fields, FanlightAudienceBodyFields.Bounce) ? Lerp(current.Bounce, value.Bounce, weight) : current.Bounce,
                Has(patch.Fields, FanlightAudienceBodyFields.Sway) ? Lerp(current.Sway, value.Sway, weight) : current.Sway);
        }

        internal static FanlightDirectionState Apply(FanlightDirectionState current, FanlightDirectionPatch patch, float weight)
        {
            ValidateMask((int)patch.Fields, (int)FanlightDirectionFields.All, nameof(patch));

            var value = patch.Value;
            return new FanlightDirectionState(
                Has(patch.Fields, FanlightDirectionFields.Mode) && weight >= 0.5f ? value.Mode : current.Mode,
                Has(patch.Fields, FanlightDirectionFields.Direction) ? Mathf.LerpAngle(current.Direction, value.Direction, weight) : current.Direction);
        }

        internal static FanlightColorState Apply(FanlightColorState current, FanlightColorPatch patch, float weight)
        {
            ValidateMask((int)patch.Fields, (int)FanlightColorFields.All, nameof(patch));
            if (!Has(patch.Fields, FanlightColorFields.Source)) return current;

            var sourceA = default(FanlightColorSource);
            var sourceB = default(FanlightColorSource);
            var sourceC = default(FanlightColorSource);
            var sourceWeights = Vector3.zero;
            var value = patch.Value;

            for (var i = 0; i < 3; i++)
            {
                AddColorSource(
                    current.GetSource(i),
                    current.GetSourceWeight(i) * (1f - weight),
                    ref sourceA,
                    ref sourceB,
                    ref sourceC,
                    ref sourceWeights);
                AddColorSource(
                    value.GetSource(i),
                    value.GetSourceWeight(i) * weight,
                    ref sourceA,
                    ref sourceB,
                    ref sourceC,
                    ref sourceWeights);
            }

            return FanlightColorState.BlendSources(sourceA, sourceB, sourceC, sourceWeights);
        }

        internal static FanlightIntensityState Apply(
            FanlightIntensityState current,
            FanlightIntensityPatch patch,
            float weight)
        {
            ValidateMask((int)patch.Fields, (int)FanlightIntensityFields.All, nameof(patch));
            if (patch.Fields == FanlightIntensityFields.None) return current;

            var value = patch.Value;
            var maskA = current.GetMask(0);
            var maskB = current.GetMask(1);
            var maskC = current.GetMask(2);
            var maskWeights = new Vector3(
                current.GetMaskWeight(0),
                current.GetMaskWeight(1),
                current.GetMaskWeight(2));

            if (Has(patch.Fields, FanlightIntensityFields.Mask))
            {
                maskA = default;
                maskB = default;
                maskC = default;
                maskWeights = Vector3.zero;

                for (var i = 0; i < 3; i++)
                {
                    AddIntensityMask(
                        current.GetMask(i),
                        current.GetMaskWeight(i) * (1f - weight),
                        ref maskA,
                        ref maskB,
                        ref maskC,
                        ref maskWeights);
                    AddIntensityMask(
                        value.GetMask(i),
                        value.GetMaskWeight(i) * weight,
                        ref maskA,
                        ref maskB,
                        ref maskC,
                        ref maskWeights);
                }
            }

            return FanlightIntensityState.BlendMasks(
                Has(patch.Fields, FanlightIntensityFields.BaseIntensity)
                    ? Lerp(current.BaseIntensity, value.BaseIntensity, weight)
                    : current.BaseIntensity,
                Has(patch.Fields, FanlightIntensityFields.RandomIntensity)
                    ? Lerp(current.RandomIntensity, value.RandomIntensity, weight)
                    : current.RandomIntensity,
                maskA,
                maskB,
                maskC,
                maskWeights);
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

        private static void ValidateMask(int fields, int all, string name)
        {
            if ((fields & ~all) != 0) throw new ArgumentOutOfRangeException(name);
        }

        private static float Lerp(float current, float incoming, float weight) => current + (incoming - current) * weight;

        private static void AddAsset(
            FanlightMotionAsset asset,
            float weight,
            ref FanlightMotionAsset assetA,
            ref FanlightMotionAsset assetB,
            ref FanlightMotionAsset assetC,
            ref Vector3 weights)
        {
            if (weight <= 0.000001f) return;

            if (assetA == asset)
            {
                weights.x += weight;
                return;
            }

            if (assetB == asset)
            {
                weights.y += weight;
                return;
            }

            if (assetC == asset)
            {
                weights.z += weight;
                return;
            }

            if (weights.x <= 0.000001f)
            {
                assetA = asset;
                weights.x = weight;
                return;
            }

            if (weights.y <= 0.000001f)
            {
                assetB = asset;
                weights.y = weight;
                return;
            }

            if (weights.z <= 0.000001f)
            {
                assetC = asset;
                weights.z = weight;
                return;
            }

            throw new InvalidOperationException("Motion evaluation cannot contain more than three assets.");
        }

        private static void AddColorSource(
            FanlightColorSource source,
            float weight,
            ref FanlightColorSource sourceA,
            ref FanlightColorSource sourceB,
            ref FanlightColorSource sourceC,
            ref Vector3 weights)
        {
            if (weight <= 0.000001f) return;

            if (weights.x > 0f && sourceA.ContentEquals(source))
            {
                weights.x += weight;
                return;
            }

            if (weights.y > 0f && sourceB.ContentEquals(source))
            {
                weights.y += weight;
                return;
            }

            if (weights.z > 0f && sourceC.ContentEquals(source))
            {
                weights.z += weight;
                return;
            }

            if (weights.x <= 0.000001f)
            {
                sourceA = source;
                weights.x = weight;
                return;
            }

            if (weights.y <= 0.000001f)
            {
                sourceB = source;
                weights.y = weight;
                return;
            }

            if (weights.z <= 0.000001f)
            {
                sourceC = source;
                weights.z = weight;
                return;
            }

            throw new InvalidOperationException("Color evaluation cannot contain more than three sources.");
        }

        private static void AddIntensityMask(
            FanlightIntensityMask mask,
            float weight,
            ref FanlightIntensityMask maskA,
            ref FanlightIntensityMask maskB,
            ref FanlightIntensityMask maskC,
            ref Vector3 weights)
        {
            if (weight <= 0.000001f) return;

            if (weights.x > 0f && maskA.ContentEquals(mask))
            {
                weights.x += weight;
                return;
            }

            if (weights.y > 0f && maskB.ContentEquals(mask))
            {
                weights.y += weight;
                return;
            }

            if (weights.z > 0f && maskC.ContentEquals(mask))
            {
                weights.z += weight;
                return;
            }

            if (weights.x <= 0.000001f)
            {
                maskA = mask;
                weights.x = weight;
                return;
            }

            if (weights.y <= 0.000001f)
            {
                maskB = mask;
                weights.y = weight;
                return;
            }

            if (weights.z <= 0.000001f)
            {
                maskC = mask;
                weights.z = weight;
                return;
            }

            throw new InvalidOperationException("Intensity evaluation cannot contain more than three spatial masks.");
        }
    }
}

using System;
using Unity.Mathematics;
using UnityEngine;

namespace PrismFanlight
{
    // Appearance and motion settings for one audience member.
    // The hand position is driven by the penlight motion, while the body and
    // head stay anchored to the seat with low-frequency crowd motion.
    [Serializable]
    public struct FanlightAudienceSettings
    {
        public bool enabled;

        [Min(0.1f)]
        public float bodyHeight;

        [Range(0f, 1f)]
        public float bodyHeightJitter;

        [Min(0.01f)]
        public float bodyWidth;

        [Min(0.01f)]
        public float headSize;

        [Range(0f, 1f)]
        public float shoulderHeight;

        [Range(-1f, 1f)]
        public float shoulderOffset;

        [Min(0.01f)]
        public float armWidth;

        [Min(0.01f)]
        public float maxReach;

        [Range(0f, 1f)]
        public float leanFactor;

        [Min(0f)]
        public float leanMax;

        public FanlightAudienceMotionSettings motion;

        public FanlightAudienceVariationSettings variation;


        public static FanlightAudienceSettings Default() => new()
        {
            enabled = true,
            bodyHeight = 1.5f,
            bodyHeightJitter = 0.08f,
            bodyWidth = 0.55f,
            headSize = 0.28f,
            shoulderHeight = 0.82f,
            shoulderOffset = 0.16f,
            armWidth = 0.14f,
            maxReach = 0.55f,
            leanFactor = 0.5f,
            leanMax = 0.4f,
            motion = FanlightAudienceMotionSettings.Default(),
            variation = FanlightAudienceVariationSettings.Default()
        };

        public FanlightAudienceSettings Validated() => new()
        {
            enabled = enabled,
            bodyHeight = math.max(0.1f, bodyHeight),
            bodyHeightJitter = math.saturate(bodyHeightJitter),
            bodyWidth = math.max(0.01f, bodyWidth),
            // Migration guard: older serialized body settings can read as zero.
            headSize = headSize > 0f ? math.max(0.01f, headSize) : 0.28f,
            shoulderHeight = math.saturate(shoulderHeight),
            shoulderOffset = math.clamp(shoulderOffset, -1f, 1f),
            armWidth = math.max(0.01f, armWidth),
            maxReach = maxReach > 0f ? math.max(0.01f, maxReach) : 0.55f,
            leanFactor = math.saturate(leanFactor),
            leanMax = math.max(0f, leanMax),
            motion = motion.Validated(),
            variation = variation.Validated()
        };
    }

    [Serializable]
    public struct FanlightAudienceMotionSettings
    {
        [Min(0f)]
        public float bodyBounce;

        [Min(0.01f)]
        public float bodyMotionSpeed;

        [Min(0f)]
        public float bodySway;

        [Min(0.01f)]
        public float headMotionSpeed;

        [Range(0f, 1f)]
        public float shoulderFollow;

        [Min(0f)]
        public float shoulderFollowMax;

        [Min(0f)]
        public float shoulderBounce;

        [Min(0f)]
        public float headBob;

        [Min(0f)]
        public float headSway;

        [Range(0f, 1f)]
        public float headCounterMotion;


        public static FanlightAudienceMotionSettings Default() => new()
        {
            bodyBounce = 0.018f,
            bodyMotionSpeed = 0.65f,
            bodySway = 0.025f,
            headMotionSpeed = 0.45f,
            shoulderFollow = 0.2f,
            shoulderFollowMax = 0.045f,
            shoulderBounce = 0.012f,
            headBob = 0.012f,
            headSway = 0.015f,
            headCounterMotion = 0.15f
        };

        public FanlightAudienceMotionSettings Validated()
        {
            var uninitialized = bodyBounce <= 0f
                                && bodyMotionSpeed <= 0f
                                && bodySway <= 0f
                                && headMotionSpeed <= 0f
                                && shoulderFollow <= 0f
                                && shoulderFollowMax <= 0f
                                && shoulderBounce <= 0f
                                && headBob <= 0f
                                && headSway <= 0f
                                && headCounterMotion <= 0f;
            var source = uninitialized ? Default() : this;

            return new FanlightAudienceMotionSettings
            {
                bodyBounce = math.max(0f, source.bodyBounce),
                bodyMotionSpeed = math.max(0.01f, source.bodyMotionSpeed),
                bodySway = math.max(0f, source.bodySway),
                headMotionSpeed = math.max(0.01f, source.headMotionSpeed),
                shoulderFollow = math.saturate(source.shoulderFollow),
                shoulderFollowMax = math.max(0f, source.shoulderFollowMax),
                shoulderBounce = math.max(0f, source.shoulderBounce),
                headBob = math.max(0f, source.headBob),
                headSway = math.max(0f, source.headSway),
                headCounterMotion = math.saturate(source.headCounterMotion)
            };
        }
    }

    [Serializable]
    public struct FanlightAudienceVariationSettings
    {
        [Range(0f, 1f)]
        public float enthusiasmVariation;

        [Range(0f, 1f)]
        public float bodyMotionVariation;

        [Range(0f, 1f)]
        public float headMotionVariation;

        [Min(0f)]
        public float reactionDelay;

        [Range(0f, 1f)]
        public float quietProbability;

        [Range(0f, 1f)]
        public float quietMotionLevel;


        public static FanlightAudienceVariationSettings Default() => new()
        {
            enthusiasmVariation = 0.25f,
            bodyMotionVariation = 0.2f,
            headMotionVariation = 0.25f,
            reactionDelay = 0.08f,
            quietProbability = 0.05f,
            quietMotionLevel = 0.35f
        };

        public FanlightAudienceVariationSettings Validated() => new()
        {
            enthusiasmVariation = math.saturate(enthusiasmVariation),
            bodyMotionVariation = math.saturate(bodyMotionVariation),
            headMotionVariation = math.saturate(headMotionVariation),
            reactionDelay = math.max(0f, reactionDelay),
            quietProbability = math.saturate(quietProbability),
            quietMotionLevel = math.saturate(quietMotionLevel)
        };
    }
}

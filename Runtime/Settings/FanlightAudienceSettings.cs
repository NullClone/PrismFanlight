using System;
using Unity.Mathematics;
using UnityEngine;

namespace PrismFanlight
{
    // Appearance and minimal crowd motion for one audience member.
    // The body/head stay anchored to the seat; only the arm and a tiny shoulder
    // offset connect the silhouette to the penlight hand.
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
            motion = FanlightAudienceMotionSettings.Default()
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
            motion = motion.Validated()
        };
    }

    [Serializable]
    public struct FanlightAudienceMotionSettings
    {
        [Min(0f)]
        public float bodyBounce;

        [Min(0f)]
        public float bodySway;

        [Min(0.01f)]
        public float bodyMotionSpeed;

        [Range(0f, 1f)]
        public float shoulderFollow;

        [Min(0f)]
        public float shoulderFollowMax;


        public static FanlightAudienceMotionSettings Default() => new()
        {
            bodyBounce = 0.018f,
            bodySway = 0.025f,
            bodyMotionSpeed = 0.65f,
            shoulderFollow = 0.2f,
            shoulderFollowMax = 0.045f
        };

        public FanlightAudienceMotionSettings Validated()
        {
            var uninitialized = bodyBounce <= 0f
                                && bodySway <= 0f
                                && bodyMotionSpeed <= 0f
                                && shoulderFollow <= 0f
                                && shoulderFollowMax <= 0f;
            var source = uninitialized ? Default() : this;

            return new FanlightAudienceMotionSettings
            {
                bodyBounce = math.max(0f, source.bodyBounce),
                bodySway = math.max(0f, source.bodySway),
                bodyMotionSpeed = math.max(0.01f, source.bodyMotionSpeed),
                shoulderFollow = math.saturate(source.shoulderFollow),
                shoulderFollowMax = math.max(0f, source.shoulderFollowMax)
            };
        }
    }
}

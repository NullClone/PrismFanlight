using System;
using Unity.Mathematics;
using UnityEngine;

namespace PrismFanlight
{
    [Serializable]
    public struct FanlightAudienceSettings
    {
        // Fields

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
        public float armLengthLimit;

        public FanlightHandZoneSettings handZone;

        [Range(0f, 1f)]
        public float upperBodyLean;

        [Min(0f)]
        public float upperBodyLeanMax;

        public FanlightAudienceMotionSettings motion;


        // Methods

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
            armLengthLimit = 0.55f,
            handZone = FanlightHandZoneSettings.Default(),
            upperBodyLean = 0.5f,
            upperBodyLeanMax = 0.4f,
            motion = FanlightAudienceMotionSettings.Default()
        };

        public FanlightAudienceSettings Validated() => new()
        {
            enabled = enabled,
            bodyHeight = math.max(0.1f, bodyHeight),
            bodyHeightJitter = math.saturate(bodyHeightJitter),
            bodyWidth = math.max(0.01f, bodyWidth),
            headSize = headSize > 0f ? math.max(0.01f, headSize) : 0.28f,
            shoulderHeight = math.saturate(shoulderHeight),
            shoulderOffset = math.clamp(shoulderOffset, -1f, 1f),
            armWidth = math.max(0.01f, armWidth),
            armLengthLimit = armLengthLimit > 0f ? math.max(0.01f, armLengthLimit) : 0.55f,
            handZone = handZone.Validated(),
            upperBodyLean = math.saturate(upperBodyLean),
            upperBodyLeanMax = math.max(0f, upperBodyLeanMax),
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
        public float upperBodyLeanMotion;


        public static FanlightAudienceMotionSettings Default() => new()
        {
            bodyBounce = 0.018f,
            bodySway = 0.025f,
            bodyMotionSpeed = 0.65f,
            upperBodyLeanMotion = 0.2f
        };

        public FanlightAudienceMotionSettings Validated()
        {
            var uninitialized = bodyBounce <= 0f
                                && bodySway <= 0f
                                && bodyMotionSpeed <= 0f
                                && upperBodyLeanMotion <= 0f;
            var source = uninitialized ? Default() : this;

            return new FanlightAudienceMotionSettings
            {
                bodyBounce = math.max(0f, source.bodyBounce),
                bodySway = math.max(0f, source.bodySway),
                bodyMotionSpeed = math.max(0.01f, source.bodyMotionSpeed),
                upperBodyLeanMotion = math.saturate(source.upperBodyLeanMotion)
            };
        }
    }
}

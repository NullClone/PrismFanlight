using System;
using Unity.Mathematics;
using UnityEngine;

namespace PrismFanlight
{
    [Serializable]
    public struct FanlightAudienceMotionSettings
    {
        // Fields

        [Min(0f)]
        public float bodyBounce;

        [Min(0f)]
        public float bodySway;

        [Min(0.01f)]
        public float bodyMotionSpeed;

        [Range(0f, 1f)]
        public float upperBodyLeanMotion;


        // Methods

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

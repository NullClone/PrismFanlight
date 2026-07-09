using System;
using Unity.Mathematics;

namespace PrismFanlight
{
    [Serializable]
    public struct FanlightLodSettings
    {
        public bool enableAudienceDistanceLod;

        public float audienceVisibleDistance;

        public float audienceFadeRange;

        public static FanlightLodSettings Default() => new()
        {
            enableAudienceDistanceLod = false,
            audienceVisibleDistance = 60.0f,
            audienceFadeRange = 0.0f
        };

        public FanlightLodSettings Validated() => new()
        {
            enableAudienceDistanceLod = enableAudienceDistanceLod,
            audienceVisibleDistance = math.max(0.01f, audienceVisibleDistance),
            audienceFadeRange = math.max(0.0f, audienceFadeRange)
        };
    }
}

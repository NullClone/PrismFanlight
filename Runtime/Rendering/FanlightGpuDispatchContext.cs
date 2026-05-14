using UnityEngine;

namespace PrismFanlight.Rendering
{
    internal readonly struct FanlightGpuDispatchContext
    {
        public readonly Camera CullingCamera;
        public readonly bool EnableCulling;
        public readonly Audience Audience;
        public readonly FanlightMotionSettings Motion;
        public readonly FanlightColorSettings Color;
        public readonly Matrix4x4 LocalToWorld;
        public readonly float Time;
        public readonly Bounds WorldBounds;

        public FanlightGpuDispatchContext(
            Camera cullingCamera,
            bool enableCulling,
            Audience audience,
            FanlightMotionSettings motion,
            FanlightColorSettings color,
            Matrix4x4 localToWorld,
            float time,
            Bounds worldBounds)
        {
            CullingCamera = cullingCamera;
            EnableCulling = enableCulling;
            Audience = audience;
            Motion = motion;
            Color = color;
            LocalToWorld = localToWorld;
            Time = time;
            WorldBounds = worldBounds;
        }
    }
}

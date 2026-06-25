using UnityEngine;

namespace PrismFanlight.Rendering
{
    internal readonly struct FanlightGpuDispatchContext
    {
        public readonly Camera CullingCamera;
        public readonly bool EnableCulling;
        public readonly SeatLayout Layout;
        public readonly FanlightTempoState Tempo;
        public readonly FanlightMotionSettings Motion;
        public readonly FanlightColorSettings Color;
        public readonly FanlightAudienceSettings Audience;
        public readonly float HandBaseHeight;
        public readonly Vector3 SwingTargetWorldPos;
        public readonly Matrix4x4 LocalToWorld;
        public readonly Matrix4x4 WorldToLocal;
        public readonly float Time;
        public readonly Bounds WorldBounds;

        public FanlightGpuDispatchContext(
            Camera cullingCamera,
            bool enableCulling,
            SeatLayout layout,
            FanlightTempoState tempo,
            FanlightMotionSettings motion,
            FanlightColorSettings color,
            FanlightAudienceSettings audience,
            float handBaseHeight,
            Vector3 swingTargetWorldPos,
            Matrix4x4 localToWorld,
            float time,
            Bounds worldBounds)
        {
            CullingCamera = cullingCamera;
            EnableCulling = enableCulling;
            Layout = layout;
            Tempo = tempo;
            Motion = motion;
            Color = color;
            Audience = audience;
            HandBaseHeight = handBaseHeight;
            SwingTargetWorldPos = swingTargetWorldPos;
            LocalToWorld = localToWorld;
            WorldToLocal = localToWorld.inverse;
            Time = time;
            WorldBounds = worldBounds;
        }
    }
}

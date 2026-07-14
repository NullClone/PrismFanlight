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
        public readonly FanlightAudienceSettings Audience;
        public readonly FanlightLodSettings Lod;
        public readonly Vector3 SwingTargetWorldPos;
        public readonly Vector3 LodCameraWorldPos;
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
            FanlightAudienceSettings audience,
            FanlightLodSettings lod,
            Vector3 swingTargetWorldPos,
            Vector3 lodCameraWorldPos,
            Matrix4x4 localToWorld,
            float time,
            Bounds worldBounds)
        {
            CullingCamera = cullingCamera;
            EnableCulling = enableCulling;
            Layout = layout;
            Tempo = tempo;
            Motion = motion;
            Audience = audience;
            Lod = lod;
            SwingTargetWorldPos = swingTargetWorldPos;
            LodCameraWorldPos = lodCameraWorldPos;
            LocalToWorld = localToWorld;
            WorldToLocal = localToWorld.inverse;
            Time = time;
            WorldBounds = worldBounds;
        }
    }
}

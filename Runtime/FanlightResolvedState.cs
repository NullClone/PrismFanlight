using UnityEngine;

namespace PrismFanlight
{
    public readonly struct FanlightResolvedState
    {
        public readonly FanlightTempoState Tempo;
        public readonly FanlightMotionSettings Motion;
        public readonly FanlightColorSettings Color;
        public readonly FanlightAudienceSettings Audience;
        public readonly FanlightLodSettings Lod;
        public readonly FanlightRandomSettings Random;
        public readonly Vector3 SwingTargetWorldPosition;
        public readonly Matrix4x4 LocalToWorld;
        public readonly float Time;
        public readonly float UpdateClock;
        public readonly bool IsTimeJump;

        public FanlightResolvedState(
            FanlightTempoState tempo,
            FanlightMotionSettings motion,
            FanlightColorSettings color,
            FanlightAudienceSettings audience,
            FanlightLodSettings lod,
            FanlightRandomSettings random,
            Vector3 swingTargetWorldPosition,
            Matrix4x4 localToWorld,
            float time,
            float updateClock,
            bool isTimeJump = false)
        {
            Tempo = tempo;
            Motion = motion.Validated();
            Color = color.Validated();
            Audience = audience.Validated();
            Lod = lod.Validated();
            Random = random.Validated();
            SwingTargetWorldPosition = swingTargetWorldPosition;
            LocalToWorld = localToWorld;
            Time = time;
            UpdateClock = updateClock;
            IsTimeJump = isTimeJump;
        }
    }
}

using System;
using Unity.Mathematics;
using UnityEngine;

namespace PrismFanlight
{
    [Serializable]
    public struct FanlightBodySettings
    {
        public bool enabled;

        [Min(0.1f)]
        public float bodyHeight;

        [Range(0f, 1f)]
        public float bodyHeightJitter;

        [Min(0.01f)]
        public float bodyWidth;

        [Range(0f, 1f)]
        public float shoulderHeight;

        [Range(-1f, 1f)]
        public float shoulderOffset;

        [Min(0.01f)]
        public float upperArmLength;

        [Min(0.01f)]
        public float forearmLength;

        [Min(0.01f)]
        public float armWidth;

        [Range(0f, 1f)]
        public float leanFactor;

        [Min(0f)]
        public float leanMax;

        [Range(0f, 1f)]
        public float elbowBias;


        public static FanlightBodySettings Default() => new()
        {
            enabled = true,
            bodyHeight = 1.5f,
            bodyHeightJitter = 0.08f,
            bodyWidth = 0.55f,
            shoulderHeight = 0.82f,
            shoulderOffset = 0.16f,
            upperArmLength = 0.3f,
            forearmLength = 0.28f,
            armWidth = 0.14f,
            leanFactor = 0.5f,
            leanMax = 0.4f,
            elbowBias = 0.35f
        };

        public FanlightBodySettings Validated() => new()
        {
            enabled = enabled,
            bodyHeight = math.max(0.1f, bodyHeight),
            bodyHeightJitter = math.saturate(bodyHeightJitter),
            bodyWidth = math.max(0.01f, bodyWidth),
            shoulderHeight = math.saturate(shoulderHeight),
            shoulderOffset = math.clamp(shoulderOffset, -1f, 1f),
            upperArmLength = math.max(0.01f, upperArmLength),
            forearmLength = math.max(0.01f, forearmLength),
            armWidth = math.max(0.01f, armWidth),
            leanFactor = math.saturate(leanFactor),
            leanMax = math.max(0f, leanMax),
            elbowBias = math.saturate(elbowBias)
        };
    }
}

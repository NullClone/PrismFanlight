using System;
using Unity.Mathematics;
using UnityEngine;

namespace PrismFanlight
{
    [Serializable]
    public struct FanlightSwingSettings
    {
        [Min(0f)]
        public float swingSpeed;

        [Range(0f, 1f)]
        public float randomPhase;

        [Min(0f)]
        public float armLength;

        [Min(0f)]
        public float minAngle;

        [Min(0f)]
        public float maxAngle;

        [Range(0f, 1f)]
        public float snapAmount;

        [Range(0f, 1f)]
        public float holdAmount;

        [Range(0f, 1f)]
        public float flickAmount;

        [Range(-1f, 1f)]
        public float returnBias;


        public FanlightSwingSettings Validated() => new()
        {
            swingSpeed = math.max(swingSpeed, 0f),
            randomPhase = math.saturate(randomPhase),
            armLength = math.max(armLength, 0f),
            minAngle = math.max(math.min(minAngle, maxAngle), 0f),
            maxAngle = math.max(math.max(minAngle, maxAngle), 0f),
            snapAmount = math.saturate(snapAmount),
            holdAmount = math.saturate(holdAmount),
            flickAmount = math.saturate(flickAmount),
            returnBias = math.clamp(returnBias, -1f, 1f)
        };
    }
}

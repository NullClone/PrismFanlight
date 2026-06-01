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
        public float armLengthMin;

        [Min(0f)]
        public float armLengthMax;

        [Min(0f)]
        public float minAngle;

        [Min(0f)]
        public float maxAngle;

        [Range(0f, 1f)]
        public float angleNoise;

        [Range(0f, 1f)]
        public float crispness;

        [Range(0f, 1f)]
        public float peakHold;

        [Range(0f, 1f)]
        public float followThrough;

        [Range(-1f, 1f)]
        public float lean;

        [Range(0f, 1f)]
        public float horizontalRatio;

        [Min(1f)]
        public float wristSwingSpeed;

        [Range(0f, 1.5f)]
        public float wristSwingAngle;


        public FanlightSwingSettings Validated() => new()
        {
            swingSpeed = math.max(swingSpeed, 0f),
            randomPhase = math.saturate(randomPhase),
            armLengthMin = math.max(math.min(armLengthMin, armLengthMax), 0f),
            armLengthMax = math.max(math.max(armLengthMin, armLengthMax), 0f),
            minAngle = math.max(math.min(minAngle, maxAngle), 0f),
            maxAngle = math.max(math.max(minAngle, maxAngle), 0f),
            angleNoise = math.saturate(angleNoise),
            crispness = math.saturate(crispness),
            peakHold = math.saturate(peakHold),
            followThrough = math.saturate(followThrough),
            lean = math.clamp(lean, -1f, 1f),
            horizontalRatio = math.saturate(horizontalRatio),
            wristSwingSpeed = math.max(1f, wristSwingSpeed),
            wristSwingAngle = math.clamp(wristSwingAngle, 0f, 1.5f)
        };
    }
}

using System;
using Unity.Mathematics;
using UnityEngine;

namespace PrismFanlight
{
    [Serializable]
    public struct FanlightDirectionSettings
    {
        public FanlightSwingMode swingMode;

        [Range(0f, 360f)]
        public float swingYaw;

        [Range(0f, 1f)]
        public float directionSpread;

        [Range(0f, 1f)]
        public float aimStrength;


        public FanlightDirectionSettings Validated() => new()
        {
            swingMode = swingMode is FanlightSwingMode.WorldDirection or FanlightSwingMode.Target ? swingMode : FanlightSwingMode.WorldDirection,
            swingYaw = ((swingYaw % 360f) + 360f) % 360f,
            directionSpread = math.saturate(directionSpread),
            aimStrength = math.saturate(aimStrength)
        };
    }
}

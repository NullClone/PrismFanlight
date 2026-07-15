using System;
using Unity.Mathematics;
using UnityEngine;

namespace PrismFanlight
{
    [Serializable]
    public struct FanlightNoiseSettings
    {
        // Fields

        [Min(0f)]
        public float phaseIrregularity;

        [Min(0f)]
        public float phaseIrregularitySpeed;

        [Min(0f)]
        public float axisNoiseAmount;

        [Min(0f)]
        public float axisNoiseSpeed;

        [Range(1, 4)]
        public int noiseOctaves;

        [Range(0f, 1f)]
        public float noiseDetail;


        // Methods

        public FanlightNoiseSettings Validated()
        {
            return new FanlightNoiseSettings
            {
                phaseIrregularity = math.max(phaseIrregularity, 0f),
                phaseIrregularitySpeed = math.max(phaseIrregularitySpeed, 0f),
                axisNoiseAmount = math.max(axisNoiseAmount, 0f),
                axisNoiseSpeed = math.max(axisNoiseSpeed, 0f),
                noiseOctaves = math.clamp(noiseOctaves, 1, 4),
                noiseDetail = math.saturate(noiseDetail)
            };
        }
    }
}

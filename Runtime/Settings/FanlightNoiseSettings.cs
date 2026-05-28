using System;
using Unity.Mathematics;
using UnityEngine;

namespace PrismFanlight
{
    [Serializable]
    public struct FanlightNoiseSettings
    {
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


        public FanlightNoiseSettings Validated()
        {
            var uninitialized = noiseOctaves <= 0;

            return new FanlightNoiseSettings
            {
                phaseIrregularity = uninitialized ? 1f : math.max(phaseIrregularity, 0f),
                phaseIrregularitySpeed = uninitialized ? 0.27f : math.max(phaseIrregularitySpeed, 0f),
                axisNoiseAmount = uninitialized ? 1f : math.max(axisNoiseAmount, 0f),
                axisNoiseSpeed = uninitialized ? 0.23f : math.max(axisNoiseSpeed, 0f),
                noiseOctaves = uninitialized ? 2 : math.clamp(noiseOctaves, 1, 4),
                noiseDetail = uninitialized ? 0.5f : math.saturate(noiseDetail)
            };
        }
    }
}

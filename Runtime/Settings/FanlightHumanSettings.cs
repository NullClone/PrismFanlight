using System;
using Unity.Mathematics;
using UnityEngine;

namespace PrismFanlight
{
    [Serializable]
    public struct FanlightHumanSettings
    {
        // Fields

        [Range(0f, 2f)]
        public float enthusiasm;

        [Range(0f, 1f)]
        public float enthusiasmVariation;

        [Range(0f, 1f)]
        public float lazyFanRatio;

        [Min(0f)]
        public float reactionDelay;

        [Min(0f)]
        public float speedVariation;

        [Range(0f, 1f)]
        public float seatJitter;

        [Min(0f)]
        public float heightJitter;

        [Range(0f, 1f)]
        public float armLengthJitter;

        [Range(0f, 1f)]
        public float restProbability;

        [Range(0f, 1f)]
        public float restMotionLevel;

        [Min(0f)]
        public float restCycleDuration;

        [Min(0f)]
        public float restDuration;

        [Min(0f)]
        public float restFadeDuration;

        [Range(0f, 1f)]
        public float restPhaseRandomness;


        // Methods

        public FanlightHumanSettings Validated()
        {
            var uninitialized = enthusiasm <= 0f && enthusiasmVariation <= 0f
                                                 && lazyFanRatio <= 0f && restProbability <= 0f;
            return new FanlightHumanSettings
            {
                enthusiasm = uninitialized ? 1f : math.max(enthusiasm, 0f),
                enthusiasmVariation = uninitialized ? 0.15f : math.saturate(enthusiasmVariation),
                lazyFanRatio = math.saturate(lazyFanRatio),
                reactionDelay = math.max(reactionDelay, 0f),
                speedVariation = math.max(speedVariation, 0f),
                seatJitter = math.saturate(seatJitter),
                heightJitter = math.max(heightJitter, 0f),
                armLengthJitter = math.saturate(armLengthJitter),
                restProbability = math.saturate(restProbability),
                restMotionLevel = uninitialized ? 0.1f : math.saturate(restMotionLevel),
                restCycleDuration = math.max(restCycleDuration, 0f),
                restDuration = math.max(restDuration, 0f),
                restFadeDuration = math.max(restFadeDuration, 0f),
                restPhaseRandomness = math.saturate(restPhaseRandomness)
            };
        }
    }
}

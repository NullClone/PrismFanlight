using System;
using UnityEngine;

namespace PrismFanlight
{
    [Serializable]
    public struct FanlightMotionSettings
    {
        public FanlightSwingSettings swing;
        public FanlightDirectionSettings direction;
        public FanlightNoiseSettings noise;
        public FanlightHumanSettings human;
        public FanlightBeatSyncSettings beatSync;


        public static FanlightMotionSettings Default() => new()
        {
            swing = new FanlightSwingSettings
            {
                swingType = FanlightSwingType.Arm,
                swingSpeed = 0.5f,
                randomPhase = 0f,
                armLengthMin = 0.2f,
                armLengthMax = 0.4f,
                minAngle = 0.3f,
                maxAngle = 1f,
                angleNoise = 0f,
                crispness = 1f,
                peakHold = 0f,
                followThrough = 0f,
                lean = 0f
            },
            direction = new FanlightDirectionSettings
            {
                swingMode = FanlightSwingMode.WorldDirection,
                swingYaw = 180f,
                directionSpread = 0.3f,
                aimStrength = 1f
            },
            noise = new FanlightNoiseSettings
            {
                phaseIrregularity = 1f,
                phaseIrregularitySpeed = 0.27f,
                axisNoiseAmount = 1f,
                axisNoiseSpeed = 0.23f,
                noiseOctaves = 2,
                noiseDetail = 0.5f
            },
            human = new FanlightHumanSettings
            {
                enthusiasm = 1f,
                enthusiasmVariation = 0.15f,
                lazyFanRatio = 0f,
                reactionDelay = 0f,
                speedVariation = 0f,
                seatJitter = 0.3f,
                heightJitter = 0.2f,
                armLengthJitter = 0.25f,
                restProbability = 0f,
                restMotionLevel = 0.1f,
                restCycleDuration = 0f,
                restDuration = 0f,
                restFadeDuration = 0.5f,
                restPhaseRandomness = 1f
            },
            beatSync = new FanlightBeatSyncSettings
            {
                beatSyncBlend = 1f,
                beatsPerSwing = 1f,
                beatPhaseOffset = 0f,
                downbeatAccent = 0f,
                beatReactionDelay = 0f,
                beatSeatJitter = 0f,
                beatBlockDelay = Vector2.zero
            }
        };

        public FanlightMotionSettings Validated() => new()
        {
            swing = swing.Validated(),
            direction = direction.Validated(),
            noise = noise.Validated(),
            human = human.Validated(),
            beatSync = beatSync.Validated()
        };
    }
}

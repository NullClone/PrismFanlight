using System;
using Unity.Mathematics;
using UnityEngine;

namespace PrismFanlight
{
    [Serializable]
    public struct FanlightBeatSyncSettings
    {
        [Range(0f, 1f)]
        public float beatSyncBlend;

        [Min(0.001f)]
        public float beatsPerSwing;

        public float beatPhaseOffset;

        [Min(0f)]
        public float downbeatAccent;

        [Min(0f)]
        public float beatReactionDelay;

        [Min(0f)]
        public float beatSeatJitter;

        public Vector2 beatBlockDelay;


        public FanlightBeatSyncSettings Validated() => new()
        {
            beatSyncBlend = math.saturate(beatSyncBlend),
            beatsPerSwing = math.max(beatsPerSwing, 0.001f),
            beatPhaseOffset = beatPhaseOffset,
            downbeatAccent = math.max(downbeatAccent, 0f),
            beatReactionDelay = math.max(beatReactionDelay, 0f),
            beatSeatJitter = math.max(beatSeatJitter, 0f),
            beatBlockDelay = beatBlockDelay
        };
    }
}

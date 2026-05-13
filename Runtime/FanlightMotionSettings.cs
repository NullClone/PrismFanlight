using System;
using Unity.Mathematics;
using UnityEngine;
using Random = Unity.Mathematics.Random;

namespace PrismFanlight
{
    [Serializable]
    public struct FanlightMotionSettings
    {
        [Min(0.0f)]
        public float frequency;

        [Range(0.0f, 1.0f)]
        public float randomPhase;

        [Min(0.0f)]
        public float phaseNoiseAmount;

        [Min(0.0f)]
        public float phaseNoiseSpeed;

        [Min(0.0f)]
        public float armLength;

        [Min(0.0f)]
        public float minAngle;

        [Min(0.0f)]
        public float maxAngle;

        [Range(0.0f, 1.0f)]
        public float snapAmount;

        [Range(0.0f, 1.0f)]
        public float seatJitter;

        [Min(0.0f)]
        public float heightJitter;

        [Range(0.0f, 1.0f)]
        public float armLengthJitter;

        [Min(0.0f)]
        public float axisNoiseAmount;

        [Min(0.0f)]
        public float axisNoiseSpeed;


        public static FanlightMotionSettings Default() => new()
        {
            frequency = 0.5f,
            randomPhase = 0.0f,
            phaseNoiseAmount = 1.0f,
            phaseNoiseSpeed = 0.27f,
            armLength = 0.3f,
            minAngle = 0.3f,
            maxAngle = 1.0f,
            snapAmount = 1.0f,
            seatJitter = 0.3f,
            heightJitter = 0.2f,
            armLengthJitter = 0.25f,
            axisNoiseAmount = 1.0f,
            axisNoiseSpeed = 0.23f
        };

        public FanlightMotionSettings Validated() => new()
        {
            frequency = math.max(frequency, 0.0f),
            randomPhase = math.saturate(randomPhase),
            phaseNoiseAmount = math.max(phaseNoiseAmount, 0.0f),
            phaseNoiseSpeed = math.max(phaseNoiseSpeed, 0.0f),
            armLength = math.max(armLength, 0.0f),
            minAngle = math.max(math.min(minAngle, maxAngle), 0.0f),
            maxAngle = math.max(math.max(minAngle, maxAngle), 0.0f),
            snapAmount = math.saturate(snapAmount),
            seatJitter = math.saturate(seatJitter),
            heightJitter = math.max(heightJitter, 0.0f),
            armLengthJitter = math.saturate(armLengthJitter),
            axisNoiseAmount = math.max(axisNoiseAmount, 0.0f),
            axisNoiseSpeed = math.max(axisNoiseSpeed, 0.0f)
        };

        public float4x4 GetMatrix(Audience audience, float2 pos, float4x4 xform, float time, uint seed)
        {
            var rand = new Random(seed);
            rand.NextUInt4();

            var phase = 2 * math.PI * frequency * time;
            phase += rand.NextFloat(0.0f, 2 * math.PI) * randomPhase;
            phase += noise.snoise(math.float2(rand.NextFloat(-1000, 1000), time * phaseNoiseSpeed)) * phaseNoiseAmount;

            var origin = float3.zero;
            origin.xz = pos + rand.NextFloat2(-seatJitter, seatJitter) * audience.seatPitch;
            origin.y = rand.NextFloat(-heightJitter, heightJitter);

            var angle = math.cos(phase);
            var snappedAngle = math.smoothstep(-1, 1, angle) * 2 - 1;
            angle = math.lerp(angle, snappedAngle, snapAmount * rand.NextFloat());
            angle *= rand.NextFloat(minAngle, maxAngle);

            var axisNoise = noise.snoise(math.float2(rand.NextFloat(-1000, 1000), time * axisNoiseSpeed + 100));
            var axis = math.normalize(math.float3(axisNoise * axisNoiseAmount, 0, 1));

            var armJitter = 1.0f + rand.NextFloat(-armLengthJitter, armLengthJitter);
            var offset = armLength * armJitter;

            var m1 = float4x4.Translate(origin);
            var m2 = float4x4.AxisAngle(axis, angle);
            var m3 = float4x4.Translate(math.float3(0, offset, 0));
            return math.mul(math.mul(math.mul(xform, m1), m2), m3);
        }
    }
}

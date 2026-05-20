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
        public float holdAmount;

        [Range(0.0f, 1.0f)]
        public float flickAmount;

        [Range(-1.0f, 1.0f)]
        public float returnBias;

        public Vector3 baseAxis;

        [Range(0.0f, 1.0f)]
        public float forwardBackAmount;

        [Range(0.0f, 1.0f)]
        public float verticalAmount;

        [Range(0.0f, 1.0f)]
        public float axisRandomness;

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

        [Range(0.0f, 2.0f)]
        public float enthusiasm;

        [Range(0.0f, 1.0f)]
        public float enthusiasmVariation;

        [Min(0.0f)]
        public float reactionDelay;

        [Min(0.0f)]
        public float tempoDrift;

        [Range(0.0f, 1.0f)]
        public float beatSyncAmount;

        [Min(0.001f)]
        public float beatsPerSwing;

        public float beatPhaseOffset;

        [Min(0.0f)]
        public float downbeatAccent;

        [Min(0.0f)]
        public float beatReactionDelay;

        [Min(0.0f)]
        public float beatSeatJitter;

        public Vector2 beatBlockDelay;

        [Range(0.0f, 1.0f)]
        public float restAmount;

        [Range(0.0f, 1.0f)]
        public float restIntensity;

        [Min(0.0f)]
        public float restCycleDuration;

        [Min(0.0f)]
        public float restDuration;

        [Min(0.0f)]
        public float restFadeDuration;

        [Range(0.0f, 1.0f)]
        public float restPhaseRandomness;

        [Range(0.0f, 1.0f)]
        public float smallMotionRatio;


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
            holdAmount = 0.0f,
            flickAmount = 0.0f,
            returnBias = 0.0f,
            baseAxis = Vector3.forward,
            forwardBackAmount = 1.0f,
            verticalAmount = 0.0f,
            axisRandomness = 1.0f,
            seatJitter = 0.3f,
            heightJitter = 0.2f,
            armLengthJitter = 0.25f,
            axisNoiseAmount = 1.0f,
            axisNoiseSpeed = 0.23f,
            enthusiasm = 1.0f,
            enthusiasmVariation = 0.15f,
            reactionDelay = 0.0f,
            tempoDrift = 0.0f,
            beatSyncAmount = 0.0f,
            beatsPerSwing = 1.0f,
            beatPhaseOffset = 0.0f,
            downbeatAccent = 0.0f,
            beatReactionDelay = 0.0f,
            beatSeatJitter = 0.0f,
            beatBlockDelay = Vector2.zero,
            restAmount = 0.0f,
            restIntensity = 0.1f,
            restCycleDuration = 0.0f,
            restDuration = 0.0f,
            restFadeDuration = 0.5f,
            restPhaseRandomness = 1.0f,
            smallMotionRatio = 0.0f
        };

        public FanlightMotionSettings Validated()
        {
            var legacyDirectionDefaults = baseAxis.sqrMagnitude <= 0.0001f
                                          && forwardBackAmount <= 0.0f
                                          && verticalAmount <= 0.0f
                                          && axisRandomness <= 0.0f;

            var legacyHumanDefaults = enthusiasm <= 0.0f
                                      && enthusiasmVariation <= 0.0f
                                      && restAmount <= 0.0f
                                      && smallMotionRatio <= 0.0f;

            return new FanlightMotionSettings
            {
                frequency = math.max(frequency, 0.0f),
                randomPhase = math.saturate(randomPhase),
                phaseNoiseAmount = math.max(phaseNoiseAmount, 0.0f),
                phaseNoiseSpeed = math.max(phaseNoiseSpeed, 0.0f),
                armLength = math.max(armLength, 0.0f),
                minAngle = math.max(math.min(minAngle, maxAngle), 0.0f),
                maxAngle = math.max(math.max(minAngle, maxAngle), 0.0f),
                snapAmount = math.saturate(snapAmount),
                holdAmount = math.saturate(holdAmount),
                flickAmount = math.saturate(flickAmount),
                returnBias = math.clamp(returnBias, -1.0f, 1.0f),
                baseAxis = ValidateAxis(baseAxis),
                forwardBackAmount = legacyDirectionDefaults ? 1.0f : math.saturate(forwardBackAmount),
                verticalAmount = math.saturate(verticalAmount),
                axisRandomness = legacyDirectionDefaults ? 1.0f : math.saturate(axisRandomness),
                seatJitter = math.saturate(seatJitter),
                heightJitter = math.max(heightJitter, 0.0f),
                armLengthJitter = math.saturate(armLengthJitter),
                axisNoiseAmount = math.max(axisNoiseAmount, 0.0f),
                axisNoiseSpeed = math.max(axisNoiseSpeed, 0.0f),
                enthusiasm = legacyHumanDefaults ? 1.0f : math.max(enthusiasm, 0.0f),
                enthusiasmVariation = legacyHumanDefaults ? 0.15f : math.saturate(enthusiasmVariation),
                reactionDelay = math.max(reactionDelay, 0.0f),
                tempoDrift = math.max(tempoDrift, 0.0f),
                beatSyncAmount = math.saturate(beatSyncAmount),
                beatsPerSwing = math.max(beatsPerSwing, 0.001f),
                beatPhaseOffset = beatPhaseOffset,
                downbeatAccent = math.max(downbeatAccent, 0.0f),
                beatReactionDelay = math.max(beatReactionDelay, 0.0f),
                beatSeatJitter = math.max(beatSeatJitter, 0.0f),
                beatBlockDelay = beatBlockDelay,
                restAmount = math.saturate(restAmount),
                restIntensity = legacyHumanDefaults ? 0.1f : math.saturate(restIntensity),
                restCycleDuration = math.max(restCycleDuration, 0.0f),
                restDuration = math.max(restDuration, 0.0f),
                restFadeDuration = math.max(restFadeDuration, 0.0f),
                restPhaseRandomness = math.saturate(restPhaseRandomness),
                smallMotionRatio = math.saturate(smallMotionRatio)
            };
        }

        public float4x4 GetMatrix(Audience audience, float2 pos, float4x4 xform, float time, uint seed)
        {
            return GetMatrix(audience, pos, xform, time, FanlightTempoState.Disabled(time), seed);
        }

        public float4x4 GetMatrix(Audience audience, float2 pos, float4x4 xform, float time, FanlightTempoState tempo, uint seed)
        {
            return GetMatrix(audience, pos, float2.zero, xform, time, tempo, seed);
        }

        public float4x4 GetMatrix(Audience audience, float2 pos, float2 block, float4x4 xform, float time, FanlightTempoState tempo, uint seed)
        {
            var rand = new Random(seed);
            rand.NextUInt4();

            var reaction = rand.NextFloat(0.0f, reactionDelay);
            var drift = rand.NextFloat(-tempoDrift, tempoDrift);
            var phaseTime = math.max(0.0f, time - reaction);
            var legacyPhase = 2 * math.PI * math.max(0.0f, frequency + drift) * phaseTime;
            var delayedBeat = GetDelayedBeat(audience, block, tempo, reaction, ref rand);
            var beatPhase = 2 * math.PI * ((delayedBeat / math.max(0.001f, beatsPerSwing)) + beatPhaseOffset);
            var phase = math.lerp(legacyPhase, beatPhase, tempo.Enabled ? math.saturate(beatSyncAmount) : 0.0f);
            phase += rand.NextFloat(0.0f, 2 * math.PI) * randomPhase;
            phase += noise.snoise(math.float2(rand.NextFloat(-1000, 1000), time * phaseNoiseSpeed)) * phaseNoiseAmount;

            var origin = float3.zero;
            origin.xz = pos + rand.NextFloat2(-seatJitter, seatJitter) * audience.seatPitch;
            origin.y = rand.NextFloat(-heightJitter, heightJitter);

            var angle = math.cos(phase);
            var snappedAngle = math.smoothstep(-1, 1, angle) * 2 - 1;
            angle = math.lerp(angle, snappedAngle, snapAmount * rand.NextFloat());
            var heldAngle = math.sign(angle) * math.pow(math.abs(angle), math.lerp(1.0f, 0.25f, holdAmount));
            angle = math.lerp(angle, heldAngle, holdAmount);
            var flickWave = math.sin(phase * 2.0f) * (1.0f - math.abs(angle));
            angle = math.clamp(angle + flickWave * flickAmount * 0.35f, -1.0f, 1.0f);
            angle = math.clamp(angle + returnBias * 0.35f, -1.0f, 1.0f);
            angle *= rand.NextFloat(minAngle, maxAngle);

            var axisNoise = noise.snoise(math.float2(rand.NextFloat(-1000, 1000), time * axisNoiseSpeed + 100));
            var baseAxisValue = SafeNormalize(math.float3(baseAxis.x, baseAxis.y, baseAxis.z), math.float3(0.0f, 0.0f, 1.0f));
            var expressiveAxis = SafeNormalize(math.float3(axisNoise * axisNoiseAmount, verticalAmount, forwardBackAmount), baseAxisValue);
            var axis = SafeNormalize(math.lerp(baseAxisValue, expressiveAxis, axisRandomness), baseAxisValue);

            var armJitter = 1.0f + rand.NextFloat(-armLengthJitter, armLengthJitter);
            var enthusiasmFactor = enthusiasm * math.lerp(1.0f, rand.NextFloat(0.65f, 1.35f), enthusiasmVariation);
            var restFactor = GetRestFactor(time, ref rand);
            var smallMotionFactor = rand.NextFloat() < smallMotionRatio ? 0.35f : 1.0f;
            var downbeatPulse = tempo.Enabled ? math.pow(1.0f - math.saturate(tempo.BarPhase), 8.0f) : 0.0f;
            var downbeatFactor = 1.0f + downbeatPulse * downbeatAccent;
            angle *= enthusiasmFactor * restFactor * smallMotionFactor * downbeatFactor;
            var offset = armLength * armJitter * math.max(0.0f, enthusiasmFactor);

            var m1 = float4x4.Translate(origin);
            var m2 = float4x4.AxisAngle(axis, angle);
            var m3 = float4x4.Translate(math.float3(0, offset, 0));
            return math.mul(math.mul(math.mul(xform, m1), m2), m3);
        }

        private static Vector3 ValidateAxis(Vector3 value)
        {
            return value.sqrMagnitude > 0.0001f ? value.normalized : Vector3.forward;
        }

        private static float3 SafeNormalize(float3 value, float3 fallback)
        {
            return math.lengthsq(value) > 0.000001f ? math.normalize(value) : fallback;
        }

        private float GetDelayedBeat(Audience audience, float2 block, FanlightTempoState tempo, float reactionSeconds, ref Random rand)
        {
            var beatReaction = reactionSeconds * tempo.Bpm / 60.0f;
            var randomBeatReaction = rand.NextFloat(0.0f, math.max(0.0f, beatReactionDelay));
            var seatBeatJitter = rand.NextFloat(-math.max(0.0f, beatSeatJitter), math.max(0.0f, beatSeatJitter));
            var block01 = math.float2(
                audience.blockCount.x > 1 ? block.x / (audience.blockCount.x - 1.0f) : 0.5f,
                audience.blockCount.y > 1 ? block.y / (audience.blockCount.y - 1.0f) : 0.5f);
            var blockBeatDelay = math.dot(block01 - 0.5f, math.float2(beatBlockDelay.x, beatBlockDelay.y));
            return math.max(0.0f, tempo.Beat - beatReaction - randomBeatReaction - seatBeatJitter - blockBeatDelay);
        }

        private float GetRestFactor(float time, ref Random rand)
        {
            if (rand.NextFloat() >= restAmount)
            {
                return 1.0f;
            }

            var cycleDuration = math.max(restCycleDuration, 0.0f);
            var duration = math.max(restDuration, 0.0f);

            if (cycleDuration <= 0.0001f || duration <= 0.0001f)
            {
                return restIntensity;
            }

            var clampedDuration = math.min(duration, cycleDuration);
            var phaseOffset = rand.NextFloat(0.0f, cycleDuration) * restPhaseRandomness;
            var cycleTime = math.fmod(math.max(0.0f, time + phaseOffset), cycleDuration);

            if (cycleTime >= clampedDuration)
            {
                return 1.0f;
            }

            var fade = math.min(restFadeDuration, clampedDuration * 0.5f);
            var restWeight = fade > 0.0001f
                ? math.saturate(cycleTime / fade) * math.saturate((clampedDuration - cycleTime) / fade)
                : 1.0f;

            return math.lerp(1.0f, restIntensity, restWeight);
        }
    }
}

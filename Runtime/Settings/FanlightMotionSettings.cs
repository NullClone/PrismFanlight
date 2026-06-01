using System;
using Unity.Mathematics;
using UnityEngine;
using Random = Unity.Mathematics.Random;

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

        public float4x4 GetMatrix(Audience audience, float2 pos, float2 block, float4x4 xform, float time, FanlightTempoState tempo, uint seed, float3 swingTargetWorldPos = default)
        {
            var rand = new Random(seed);
            rand.NextUInt4();

            var reaction = rand.NextFloat(0f, human.reactionDelay);
            var drift = rand.NextFloat(-human.speedVariation, human.speedVariation);
            var phaseTime = math.max(0f, time - reaction);
            var freePhase = 2 * math.PI * math.max(0f, swing.swingSpeed + drift) * phaseTime;
            var delayedBeat = GetDelayedBeat(audience, block, tempo, reaction, ref rand);
            var beatPhase = 2 * math.PI * ((delayedBeat / math.max(0.001f, beatSync.beatsPerSwing)) + beatSync.beatPhaseOffset);
            var phase = math.lerp(freePhase, beatPhase, tempo.Enabled ? math.saturate(beatSync.beatSyncBlend) : 0f);
            phase += rand.NextFloat(0f, 2 * math.PI) * swing.randomPhase;

            var octaves = math.clamp(noise.noiseOctaves, 1, 4);
            var detail = math.saturate(noise.noiseDetail);
            phase += FbmNoise(math.float2(rand.NextFloat(-1000f, 1000f), time * noise.phaseIrregularitySpeed), octaves, detail) * noise.phaseIrregularity;

            var origin = float3.zero;
            origin.xz = pos + rand.NextFloat2(-human.seatJitter, human.seatJitter) * audience.seatPitch;
            origin.y = rand.NextFloat(-human.heightJitter, human.heightJitter);

            var angle = math.cos(phase);
            var snappedAngle = math.smoothstep(-1, 1, angle) * 2 - 1;
            angle = math.lerp(angle, snappedAngle, swing.crispness * rand.NextFloat());
            var heldAngle = math.sign(angle) * math.pow(math.abs(angle), math.lerp(1f, 0.25f, swing.peakHold));
            angle = math.lerp(angle, heldAngle, swing.peakHold);
            var flickWave = math.sin(phase * 2f) * (1f - math.abs(angle));
            angle = math.clamp(angle + flickWave * swing.followThrough * 0.35f, -1f, 1f);
            angle = math.clamp(angle + swing.lean * 0.35f, -1f, 1f);
            var angleAmplitude = rand.NextFloat(swing.minAngle, swing.maxAngle);
            var angleNoiseVal = FbmNoise(math.float2(rand.NextFloat(-1000f, 1000f), time * noise.axisNoiseSpeed), octaves, detail);
            angleAmplitude = math.max(0f, angleAmplitude * (1f + angleNoiseVal * swing.angleNoise));
            angle *= angleAmplitude;

            var baseAxis = ComputeBaseAxisCpu(pos, xform, swingTargetWorldPos);

            var perpU = GetSafePerp(baseAxis);
            var perpV = math.cross(baseAxis, perpU);
            var spreadDx = rand.NextFloat(-1f, 1f);
            var spreadDy = rand.NextFloat(-1f, 1f);
            var spreadLen = math.sqrt(spreadDx * spreadDx + spreadDy * spreadDy);
            var spreadDir = spreadLen > 0.001f
                ? math.float2(spreadDx, spreadDy) / spreadLen
                : math.float2(1f, 0f);
            var spreadAngle = rand.NextFloat() * direction.directionSpread * math.PI * 0.5f;
            var axis = SafeNormalize(
                baseAxis * math.cos(spreadAngle) + (perpU * spreadDir.x + perpV * spreadDir.y) * math.sin(spreadAngle),
                baseAxis);

            var nu = FbmNoise(math.float2(rand.NextFloat(-1000f, 1000f), time * noise.axisNoiseSpeed), octaves, detail);
            var nv = FbmNoise(math.float2(rand.NextFloat(-1000f, 1000f), time * noise.axisNoiseSpeed + 317.5f), octaves, detail);
            var ap1 = GetSafePerp(axis);
            var ap2 = math.cross(axis, ap1);
            axis = SafeNormalize(axis + (ap1 * nu + ap2 * nv) * noise.axisNoiseAmount, axis);

            var armLen = rand.NextFloat(swing.armLengthMin, swing.armLengthMax);
            var armJitter = 1f + rand.NextFloat(-human.armLengthJitter, human.armLengthJitter);
            var enthusiasmFactor = human.enthusiasm * math.lerp(1f, rand.NextFloat(0.65f, 1.35f), human.enthusiasmVariation);
            var restFactor = GetRestFactor(time, ref rand);
            var smallMotionFactor = rand.NextFloat() < human.lazyFanRatio ? 0.35f : 1f;
            var downbeatPulse = tempo.Enabled ? math.pow(1f - math.saturate(tempo.BarPhase), 8f) : 0f;
            var downbeatFactor = 1f + downbeatPulse * beatSync.downbeatAccent;
            angle *= enthusiasmFactor * restFactor * smallMotionFactor * downbeatFactor;
            var offset = armLen * armJitter * math.max(0f, enthusiasmFactor);

            var m1 = float4x4.Translate(origin);
            var m2 = float4x4.AxisAngle(axis, angle);
            var m3 = float4x4.Translate(math.float3(0, offset, 0));
            // Arm: rotate at shoulder — wide arc. Wrist: translate to wrist then rotate — tight motion.
            return swing.swingType == FanlightSwingType.Arm
                ? math.mul(math.mul(math.mul(xform, m1), m2), m3)
                : math.mul(math.mul(math.mul(xform, m1), m3), m2);
        }


        private float3 ComputeBaseAxisCpu(float2 pos, float4x4 localToWorld, float3 swingTargetWorldPos)
        {
            var yaw = math.radians(direction.swingYaw);
            var worldDir = SafeNormalize(
                math.float3(math.sin(yaw), 0f, math.cos(yaw)),
                math.float3(0f, 0f, 1f));

            if (direction.swingMode == FanlightSwingMode.Target && direction.aimStrength > 0.001f)
            {
                var seatWorldPos = math.transform(localToWorld, math.float3(pos.x, 0f, pos.y));
                var targetDir = swingTargetWorldPos - seatWorldPos;
                targetDir.y = 0f;
                targetDir = SafeNormalize(targetDir, worldDir);
                worldDir = SafeNormalize(math.lerp(worldDir, targetDir, direction.aimStrength), worldDir);
            }

            var worldAxis = SafeNormalize(math.cross(math.float3(0f, 1f, 0f), worldDir), math.float3(1f, 0f, 0f));
            var localAxis = math.mul((float3x3)math.inverse(localToWorld), worldAxis);
            return SafeNormalize(localAxis, math.float3(1f, 0f, 0f));
        }

        private float GetDelayedBeat(Audience audience, float2 block, FanlightTempoState tempo, float reactionSeconds, ref Random rand)
        {
            var beatReaction = reactionSeconds * tempo.Bpm / 60f;
            var randomBeatReaction = rand.NextFloat(0f, math.max(0f, beatSync.beatReactionDelay));
            var seatBeatJitter = rand.NextFloat(-math.max(0f, beatSync.beatSeatJitter), math.max(0f, beatSync.beatSeatJitter));
            var block01 = math.float2(
                audience.blockCount.x > 1 ? block.x / (audience.blockCount.x - 1f) : 0.5f,
                audience.blockCount.y > 1 ? block.y / (audience.blockCount.y - 1f) : 0.5f);
            var blockBeatDelay = math.dot(block01 - 0.5f, math.float2(beatSync.beatBlockDelay.x, beatSync.beatBlockDelay.y));
            return math.max(0f, tempo.Beat - beatReaction - randomBeatReaction - seatBeatJitter - blockBeatDelay);
        }

        private float GetRestFactor(float time, ref Random rand)
        {
            if (rand.NextFloat() >= human.restProbability)
                return 1f;

            var cycleDuration = math.max(human.restCycleDuration, 0f);
            var duration = math.max(human.restDuration, 0f);

            if (cycleDuration <= 0.0001f || duration <= 0.0001f)
                return human.restMotionLevel;

            var clampedDuration = math.min(duration, cycleDuration);
            var phaseOffset = rand.NextFloat(0f, cycleDuration) * human.restPhaseRandomness;
            var cycleTime = math.fmod(math.max(0f, time + phaseOffset), cycleDuration);

            if (cycleTime >= clampedDuration)
                return 1f;

            var fade = math.min(human.restFadeDuration, clampedDuration * 0.5f);
            var restWeight = fade > 0.0001f
                ? math.saturate(cycleTime / fade) * math.saturate((clampedDuration - cycleTime) / fade)
                : 1f;

            return math.lerp(1f, human.restMotionLevel, restWeight);
        }

        private static float3 GetSafePerp(float3 axis)
        {
            var v0 = math.cross(axis, math.float3(0f, 1f, 0f));
            var v1 = math.cross(axis, math.float3(0f, 0f, 1f));
            var v = math.dot(v0, v0) >= math.dot(v1, v1) ? v0 : v1;
            return SafeNormalize(v, math.float3(1f, 0f, 0f));
        }

        private static float FbmNoise(float2 pos, int octaves, float persistence)
        {
            var value = 0f;
            var amplitude = 1f;
            var freq = 1f;
            var maxValue = 0f;
            for (var i = 0; i < octaves; i++)
            {
                value += amplitude * Unity.Mathematics.noise.snoise(pos * freq);
                maxValue += amplitude;
                amplitude *= persistence;
                freq *= 2f;
            }

            return maxValue > 0.001f ? value / maxValue : 0f;
        }

        private static float3 SafeNormalize(float3 value, float3 fallback) =>
            math.lengthsq(value) > 0.000001f ? math.normalize(value) : fallback;
    }
}

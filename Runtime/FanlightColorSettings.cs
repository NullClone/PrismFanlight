using System;
using Unity.Mathematics;
using UnityEngine;
using Random = Unity.Mathematics.Random;

namespace PrismFanlight
{
    public enum FanlightColorMode
    {
        Solid,
        RandomHue,
        Rainbow,
        Wave,
        RadialWave,
        BlockGradient
    }

    [Serializable]
    public struct FanlightColorSettings
    {
        public FanlightColorMode mode;

        [ColorUsage(false, true)]
        public Color primaryColor;

        [ColorUsage(false, true)]
        public Color secondaryColor;

        [Min(0.0f)]
        public float baseIntensity;

        [Min(0.0f)]
        public float effectIntensity;

        [Range(0.0f, 1.0f)]
        public float randomIntensity;

        [Range(0.0f, 1.0f)]
        public float saturation;

        public float hueSpeed;

        [Range(0.0f, 1.0f)]
        public float randomHueAmount;

        public float2 waveOrigin;

        [Min(0.0f)]
        public float waveFrequency;

        public float waveSpeed;

        [Min(0.001f)]
        public float waveSharpness;


        public static FanlightColorSettings Default() => new()
        {
            mode = FanlightColorMode.RadialWave,
            primaryColor = Color.white,
            secondaryColor = Color.cyan,
            baseIntensity = 0.1f,
            effectIntensity = 50.0f,
            randomIntensity = 0.0f,
            saturation = 1.0f,
            hueSpeed = 0.83f,
            randomHueAmount = 1.0f,
            waveOrigin = math.float2(0, 16),
            waveFrequency = 0.53f,
            waveSpeed = 2.8f,
            waveSharpness = 2.0f
        };

        public FanlightColorSettings Validated() => new()
        {
            mode = mode,
            primaryColor = primaryColor,
            secondaryColor = secondaryColor,
            baseIntensity = math.max(baseIntensity, 0.0f),
            effectIntensity = math.max(effectIntensity, 0.0f),
            randomIntensity = math.saturate(randomIntensity),
            saturation = math.saturate(saturation),
            hueSpeed = hueSpeed,
            randomHueAmount = math.saturate(randomHueAmount),
            waveOrigin = waveOrigin,
            waveFrequency = math.max(waveFrequency, 0.0f),
            waveSpeed = waveSpeed,
            waveSharpness = math.max(waveSharpness, 0.001f)
        };

        public Color GetColor(Audience audience, int2 block, float2 pos, float time, uint seed)
        {
            var rand = new Random(seed);
            rand.NextUInt4();

            var randomIntensityFactor = math.lerp(1.0f, rand.NextFloat(0.65f, 1.35f), randomIntensity);
            var brightness = baseIntensity;
            var rgb = ToFloat3(primaryColor);

            switch (mode)
            {
                case FanlightColorMode.Solid:
                    brightness += effectIntensity;
                    break;

                case FanlightColorMode.RandomHue:
                    rgb = HsvToRgb(math.frac(rand.NextFloat() * randomHueAmount + time * hueSpeed), saturation, 1.0f);
                    brightness += effectIntensity;
                    break;

                case FanlightColorMode.Rainbow:
                    rgb = HsvToRgb(math.frac(pos.x * 0.035f + pos.y * 0.02f + time * hueSpeed + rand.NextFloat() * randomHueAmount), saturation, 1.0f);
                    brightness += effectIntensity;
                    break;

                case FanlightColorMode.RadialWave:
                case FanlightColorMode.Wave:
                    var wave = EvaluateWave(pos, time);
                    rgb = HsvToRgb(math.frac(rand.NextFloat() * randomHueAmount + time * hueSpeed), saturation, 1.0f);
                    brightness += math.pow(wave, waveSharpness) * effectIntensity;
                    break;

                case FanlightColorMode.BlockGradient:
                    var denom = math.max(audience.blockCount.x - 1, 1);
                    var t = (float)block.x / denom;
                    rgb = math.lerp(ToFloat3(primaryColor), ToFloat3(secondaryColor), t);
                    brightness += effectIntensity;
                    break;
            }

            var finalRgb = rgb * brightness * randomIntensityFactor;

            return new Color(finalRgb.x, finalRgb.y, finalRgb.z, primaryColor.a);
        }

        private float EvaluateWave(float2 pos, float time)
        {
            var distance = mode == FanlightColorMode.RadialWave ? math.distance(pos, waveOrigin) : pos.y - waveOrigin.y;
            return math.sin(distance * waveFrequency - time * waveSpeed) * 0.5f + 0.5f;
        }

        private static float3 ToFloat3(Color color) => math.float3(color.r, color.g, color.b);

        private static float3 HsvToRgb(float h, float s, float v)
        {
            var k = math.float3(1.0f, 2.0f / 3.0f, 1.0f / 3.0f);
            var p = math.abs(math.frac(math.float3(h, h, h) + k) * 6.0f - 3.0f);
            return v * math.lerp(math.float3(1.0f, 1.0f, 1.0f), math.saturate(p - 1.0f), s);
        }
    }
}

using System;
using Unity.Mathematics;
using UnityEngine;

namespace PrismFanlight
{
    [Serializable]
    public struct FanlightHandZoneSettings
    {
        // Fields

        public FanlightHandZone zone;

        [Range(-1f, 1.5f)]
        public float heightOffset;

        [Range(-1f, 1f)]
        public float forwardOffset;

        [Min(0.01f)]
        public float reachScale;

        [Range(0f, 0.5f)]
        public float variation;


        // Methods

        public static FanlightHandZoneSettings Default() => Preset(FanlightHandZone.Shoulder);

        public static FanlightHandZoneSettings Preset(FanlightHandZone zone)
        {
            return zone switch
            {
                FanlightHandZone.Chest => new FanlightHandZoneSettings
                {
                    zone = FanlightHandZone.Chest,
                    heightOffset = -0.32f,
                    forwardOffset = 0.08f,
                    reachScale = 0.72f,
                    variation = 0.05f
                },
                FanlightHandZone.Face => new FanlightHandZoneSettings
                {
                    zone = FanlightHandZone.Face,
                    heightOffset = 0.16f,
                    forwardOffset = 0.18f,
                    reachScale = 0.9f,
                    variation = 0.04f
                },
                FanlightHandZone.Overhead => new FanlightHandZoneSettings
                {
                    zone = FanlightHandZone.Overhead,
                    heightOffset = 0.42f,
                    forwardOffset = 0.06f,
                    reachScale = 1.1f,
                    variation = 0.06f
                },
                FanlightHandZone.High => new FanlightHandZoneSettings
                {
                    zone = FanlightHandZone.High,
                    heightOffset = 0.72f,
                    forwardOffset = 0.02f,
                    reachScale = 1.28f,
                    variation = 0.08f
                },
                _ => new FanlightHandZoneSettings
                {
                    zone = FanlightHandZone.Shoulder,
                    heightOffset = 0f,
                    forwardOffset = 0f,
                    reachScale = 1f,
                    variation = 0f
                }
            };
        }

        public FanlightHandZoneSettings Validated()
        {
            var normalizedZone = IsSupportedZone(zone) ? zone : FanlightHandZone.Shoulder;
            var source = reachScale > 0f
                ? this
                : Preset(normalizedZone);

            return new FanlightHandZoneSettings
            {
                zone = normalizedZone,
                heightOffset = math.clamp(source.heightOffset, -1f, 1.5f),
                forwardOffset = math.clamp(source.forwardOffset, -1f, 1f),
                reachScale = math.max(0.01f, source.reachScale),
                variation = math.clamp(source.variation, 0f, 0.5f)
            };
        }

        private static bool IsSupportedZone(FanlightHandZone zone)
        {
            return zone is FanlightHandZone.Shoulder
                or FanlightHandZone.Chest
                or FanlightHandZone.Face
                or FanlightHandZone.Overhead
                or FanlightHandZone.High;
        }
    }
}

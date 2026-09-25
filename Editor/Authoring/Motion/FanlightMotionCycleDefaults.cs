using System;
using PrismFanlight.Authoring;
using UnityEngine;

namespace PrismFanlight.Editor
{
    internal static class FanlightMotionCycleDefaults
    {
        internal static float Suggest(FanlightMotionAsset asset)
        {
            if (asset == null || string.IsNullOrEmpty(asset.EditorGeneratorSettings)) return 2f;

            try
            {
                var settings = new FanlightMotionGeneratorSettings();
                JsonUtility.FromJsonOverwrite(asset.EditorGeneratorSettings, settings);
                var preset = settings.BakedPreset >= 0 ? settings.BakedPreset : settings.Preset;
                return preset switch
                {
                    1 or 12 => 4f,
                    13 => 1f,
                    _ => 2f
                };
            }
            catch (ArgumentException)
            {
                return 2f;
            }
        }
    }
}

using System;
using PrismFanlight.Authoring;
using UnityEditor;
using UnityEngine;

namespace PrismFanlight.Editor
{
    internal static class FanlightDefaultMotionAssetRegenerator
    {
        // Fields

        private const string ResourceRoot = "Assets/PrismFanlight/Resources/";

        // Methods

        [MenuItem("Tools/Prism Fanlight/Regenerate Default Transform Motions")]
        public static void RegenerateAll()
        {
            Generate("Default Motion Drum Asset.asset", FanlightMotionPresetGenerator.GenerateDrum);
            Generate(
                "Default Motion Wiper Asset.asset",
                asset => FanlightMotionPresetGenerator.GenerateWiper(asset, 55f, 42f, 0.88f, 65f, 1f));
            Generate(
                "Default Motion Sasage Asset.asset",
                asset => FanlightMotionPresetGenerator.GenerateSasage(asset, 18f, 68f, 0.7f, 0.96f, 0.4f, 1f));
            Generate("Default Motion Power Pump Asset.asset", asset => FanlightMotionPresetGenerator.GeneratePowerPump(asset, 1f));
            Generate("Default Motion Double Pump Asset.asset", asset => FanlightMotionPresetGenerator.GenerateDoublePump(asset, 1f));
            Generate("Default Motion Diagonal Pump Asset.asset", asset => FanlightMotionPresetGenerator.GenerateDiagonalPump(asset, 1f));
            Generate("Default Motion Forward Thrust Asset.asset", asset => FanlightMotionPresetGenerator.GenerateForwardThrust(asset, 1f));
            Generate("Default Motion Overhead Swing Asset.asset", asset => FanlightMotionPresetGenerator.GenerateOverheadSwing(asset, 1f));
            Generate("Default Motion Circle Asset.asset", asset => FanlightMotionPresetGenerator.GenerateCircle(asset, 1f, true));
            Generate("Default Motion Figure Eight Asset.asset", asset => FanlightMotionPresetGenerator.GenerateFigureEight(asset, 1f));
            Generate("Default Motion Groove Bounce Asset.asset", asset => FanlightMotionPresetGenerator.GenerateGrooveBounce(asset, 1f));
            Generate("Default Motion Raised Sway Asset.asset", asset => FanlightMotionPresetGenerator.GenerateRaisedSway(asset, 1f));
            AssetDatabase.SaveAssets();
        }

        private static void Generate(string fileName, Action<FanlightMotionAsset> generator)
        {
            var path = ResourceRoot + fileName;
            var asset = AssetDatabase.LoadAssetAtPath<FanlightMotionAsset>(path);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<FanlightMotionAsset>();
                AssetDatabase.CreateAsset(asset, path);
            }

            generator(asset);
            EditorUtility.SetDirty(asset);
        }
    }
}

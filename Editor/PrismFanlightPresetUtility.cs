using UnityEditor;
using UnityEngine;

namespace PrismFanlight.Editor
{
    internal static class PrismFanlightPresetUtility
    {
        public static void CreateLayoutPreset(PrismFanlight fanlight, Audience audience)
        {
            var preset = CreatePresetAsset<AudienceLayoutPreset>(
                "Create Audience Layout Preset",
                $"{fanlight.name} Layout Preset",
                "Choose where to save the audience layout preset.");

            if (preset == null) return;

            preset.SetAudience(audience);
            AssignPreset(fanlight, "_layoutPreset", preset, "Assign Audience Layout Preset");
        }

        public static void CreateMotionPreset(PrismFanlight fanlight, FanlightMotionSettings settings)
        {
            var preset = CreatePresetAsset<FanlightMotionPreset>(
                "Create Fanlight Motion Preset",
                $"{fanlight.name} Motion Preset",
                "Choose where to save the motion preset.");

            if (preset == null) return;

            preset.SetSettings(settings);
            AssignPreset(fanlight, "_motionPreset", preset, "Assign Fanlight Motion Preset");
        }

        public static void CreateColorPreset(PrismFanlight fanlight, FanlightColorSettings settings)
        {
            var preset = CreatePresetAsset<FanlightColorPreset>(
                "Create Fanlight Color Preset",
                $"{fanlight.name} Color Preset",
                "Choose where to save the color preset.");

            if (preset == null) return;

            preset.SetSettings(settings);
            AssignPreset(fanlight, "_colorPreset", preset, "Assign Fanlight Color Preset");
        }

        private static T CreatePresetAsset<T>(string title, string defaultName, string message) where T : ScriptableObject
        {
            var path = EditorUtility.SaveFilePanelInProject(title, defaultName, "asset", message);

            if (string.IsNullOrEmpty(path)) return null;

            var preset = ScriptableObject.CreateInstance<T>();

            AssetDatabase.CreateAsset(preset, path);
            AssetDatabase.SaveAssets();

            return preset;
        }

        private static void AssignPreset(Object target, string propertyName, Object preset, string undoName)
        {
            Undo.RecordObject(target, undoName);

            var serializedTarget = new SerializedObject(target);
            serializedTarget.FindProperty(propertyName).objectReferenceValue = preset;
            serializedTarget.ApplyModifiedProperties();

            EditorUtility.SetDirty(preset);
            Selection.activeObject = preset;
        }
    }
}

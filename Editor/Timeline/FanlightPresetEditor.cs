using UnityEditor;
using UnityEditor.Presets;
using UnityEditor.Rendering;
using UnityEngine;

namespace PrismFanlight.Editor
{
    internal static class FanlightPresetEditor
    {
        internal static void Draw(Object[] targets)
        {
            using (new EditorGUI.DisabledScope(!HaveSameType(targets)))
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField(new GUIContent("Clip Preset"), EditorStyles.boldLabel);

                GUILayout.FlexibleSpace();

                var buttonSize = EditorGUIUtility.singleLineHeight;
                var buttonRect = EditorGUILayout.GetControlRect(
                    false,
                    buttonSize,
                    EditorStyles.iconButton,
                    GUILayout.Width(buttonSize));

                PresetSelector.DrawPresetButton(buttonRect, targets);
            }

            EditorGUILayout.Space();
            CoreEditorUtils.DrawSplitter();
            EditorGUILayout.Space();
        }

        private static bool HaveSameType(Object[] targets)
        {
            if (targets == null || targets.Length == 0 || targets[0] == null) return false;

            var targetType = targets[0].GetType();

            for (var i = 1; i < targets.Length; i++)
            {
                if (targets[i] == null || targets[i].GetType() != targetType) return false;
            }

            return true;
        }
    }
}

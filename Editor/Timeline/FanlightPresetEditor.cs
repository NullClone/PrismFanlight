using UnityEditor;
using UnityEditor.Presets;
using UnityEditor.Rendering;
using UnityEditor.Timeline;
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
                if (FanlightStateClipboard.TryGetClipKind(targets, out var kind))
                {
                    using (new EditorGUI.DisabledScope(targets.Length != 1))
                    {
                        if (GUILayout.Button("Copy", GUILayout.Width(56)))
                        {
                            FanlightStateClipboard.Copy(targets, kind, "_value");
                        }
                    }

                    using (new EditorGUI.DisabledScope(!FanlightStateClipboard.CanPaste(kind)))
                    {
                        if (GUILayout.Button("Paste", GUILayout.Width(56)))
                        {
                            if (FanlightStateClipboard.Paste(targets, kind, "_value"))
                            {
                                TimelineEditor.Refresh(RefreshReason.ContentsModified);
                            }
                        }
                    }
                }


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

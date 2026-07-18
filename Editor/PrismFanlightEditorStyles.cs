using System;
using UnityEditor;
using UnityEngine;

namespace PrismFanlight.Editor
{
    internal static class PrismFanlightEditorStyles
    {
        private static GUIStyle _section;
        private static GUIStyle _sectionTitle;
        private static GUIStyle _subGroupLabel;

        private static GUIStyle Section => _section ??= CreateSection();
        private static GUIStyle SectionTitle => _sectionTitle ??= CreateSectionTitle();


        public static void DrawSection(string title, Action draw)
        {
            EditorGUILayout.Space();

            using (new EditorGUILayout.VerticalScope(Section))
            {
                EditorGUILayout.LabelField(title, SectionTitle);
                EditorGUILayout.Space();
                draw();
            }
        }

        public static void DrawSubGroupLabel(string title)
        {
            EditorGUILayout.LabelField(title);
        }

        public static void DrawOverride(SerializedProperty property, GUIContent label, bool includeChildren = false)
        {
            using (new TimelineOverrideColorScope(true))
            {
                EditorGUILayout.PropertyField(property, label, includeChildren);
            }
        }


        private static GUIStyle CreateSection()
        {
            return new GUIStyle(EditorStyles.helpBox)
            {
                padding = new RectOffset(10, 10, 8, 10),
                margin = new RectOffset(0, 0, 4, 4)
            };
        }

        private static GUIStyle CreateSectionTitle()
        {
            return new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 14
            };
        }

    }
}

using System;
using UnityEditor;
using UnityEngine;

namespace PrismFanlight.Editor
{
    internal static class PrismFanlightEditorStyles
    {
        // Fields

        private static GUIStyle _section;
        private static GUIStyle _sectionTitle;
        private static GUIStyle _subGroupLabel;


        // Properties

        private static GUIStyle Section => _section ??= CreateSection();

        private static GUIStyle SectionTitle => _sectionTitle ??= CreateSectionTitle();

        private static GUIStyle SubGroupLabel => _subGroupLabel ??= CreateSubGroupLabel();


        // Methods

        internal static void DrawSection(string title, Action draw)
        {
            EditorGUILayout.Space();

            using (new EditorGUILayout.VerticalScope(Section))
            {
                EditorGUILayout.LabelField(title, SectionTitle);
                EditorGUILayout.Space();
                draw();
            }
        }

        internal static void DrawSubGroupLabel(string title)
        {
            EditorGUILayout.LabelField(title, SubGroupLabel);
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

        private static GUIStyle CreateSubGroupLabel()
        {
            return new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 12,
                margin = new RectOffset(0, 0, 4, 2)
            };
        }
    }
}

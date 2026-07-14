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
        private static GUIStyle _statLabel;
        private static GUIStyle _statValue;

        private static GUIStyle Section => _section ??= CreateSection();
        private static GUIStyle SectionTitle => _sectionTitle ??= CreateSectionTitle();
        private static GUIStyle StatLabel => _statLabel ??= CreateStatLabel();
        private static GUIStyle StatValue => _statValue ??= CreateStatValue();


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

        public static void DrawStat(string label, string value)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField(label, StatLabel);
                GUILayout.FlexibleSpace();
                EditorGUILayout.LabelField(value, StatValue, GUILayout.Width(110));
            }
        }

        public static void DrawOverride(SerializedProperty property, GUIContent label, bool includeChildren = false)
        {
            var previousColor = EditorStyles.label.normal.textColor;

            try
            {
                EditorStyles.label.normal.textColor = new Color(0.540f, 0.850f, 1.000f, 1.000f);

                EditorGUILayout.PropertyField(property, label, includeChildren);
            }
            finally
            {
                EditorStyles.label.normal.textColor = previousColor;
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

        private static GUIStyle CreateStatLabel()
        {
            return new GUIStyle(EditorStyles.miniLabel)
            {
                alignment = TextAnchor.MiddleLeft
            };
        }

        private static GUIStyle CreateStatValue()
        {
            return new GUIStyle(EditorStyles.miniBoldLabel)
            {
                alignment = TextAnchor.MiddleRight
            };
        }
    }
}

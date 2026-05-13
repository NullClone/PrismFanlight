using UnityEditor;
using UnityEngine;

namespace PrismFanlight.Editor
{
    internal static class PrismFanlightEditorStyles
    {
        private static GUIStyle _header;
        private static GUIStyle _section;
        private static GUIStyle _sectionTitle;
        private static GUIStyle _statLabel;
        private static GUIStyle _statValue;
        private static GUIStyle _statusPill;

        private static GUIStyle Section => _section ??= CreateSection();

        private static GUIStyle SectionTitle => _sectionTitle ??= CreateSectionTitle();

        private static GUIStyle StatLabel => _statLabel ??= CreateStatLabel();

        private static GUIStyle StatValue => _statValue ??= CreateStatValue();


        public static void DrawSection(string title, System.Action draw)
        {
            EditorGUILayout.Space(6);

            using (new EditorGUILayout.VerticalScope(Section))
            {
                EditorGUILayout.LabelField(title, SectionTitle);
                EditorGUILayout.Space(3);
                draw();
            }
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

        private static GUIStyle CreateSection()
        {
            var style = new GUIStyle(EditorStyles.helpBox)
            {
                padding = new RectOffset(10, 10, 8, 10),
                margin = new RectOffset(0, 0, 4, 4)
            };
            return style;
        }

        private static GUIStyle CreateSectionTitle()
        {
            var style = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 12
            };
            return style;
        }

        private static GUIStyle CreateStatLabel()
        {
            var style = new GUIStyle(EditorStyles.miniLabel)
            {
                alignment = TextAnchor.MiddleLeft
            };
            return style;
        }

        private static GUIStyle CreateStatValue()
        {
            var style = new GUIStyle(EditorStyles.miniBoldLabel)
            {
                alignment = TextAnchor.MiddleRight
            };
            return style;
        }
    }
}

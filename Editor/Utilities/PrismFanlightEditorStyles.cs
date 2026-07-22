using System;
using UnityEditor;
using UnityEngine;

namespace PrismFanlight.Editor
{
    internal static class PrismFanlightEditorStyles
    {
        // Fields

        private static GUIStyle _subGroupLabel;


        // Properties

        private static GUIStyle SubGroupLabel => _subGroupLabel ??= CreateSubGroupLabel();


        // Methods

        internal static void DrawSection(PrismFanlightSection section, Action draw)
        {
            section.DrawHeader();

            if (EditorGUILayout.BeginFadeGroup(section.anim.faded))
            {
                using (new EditorGUI.IndentLevelScope())
                {
                    draw();

                    EditorGUILayout.Space();
                }
            }

            EditorGUILayout.EndFadeGroup();
        }

        internal static void DrawSubGroupLabel(string title)
        {
            EditorGUILayout.LabelField(title, SubGroupLabel);
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

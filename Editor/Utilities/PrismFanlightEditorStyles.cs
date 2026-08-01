using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Timeline;

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
            if (section.DrawHeader())
            {
                using (new EditorGUI.IndentLevelScope())
                {
                    draw();

                    EditorGUILayout.Space();
                }
            }
        }

        internal static void DrawSection<TTrack>(PrismFanlightSection<TTrack> section, Action draw, PrismFanlight fanlight) where TTrack : TrackAsset, new()
        {
            if (section.DrawHeader(fanlight))
            {
                using (new EditorGUI.IndentLevelScope())
                {
                    draw();

                    EditorGUILayout.Space();
                }
            }
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

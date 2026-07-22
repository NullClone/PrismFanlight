using UnityEditor;
using UnityEngine;

namespace PrismFanlight.Editor
{
    internal static class PrismFanlightEditorUtility
    {
        internal static void DrawSplitter(bool isBoxed = false)
        {
            var rect = GUILayoutUtility.GetRect(1f, 1f);

            DrawSplitter(rect, isBoxed);
        }

        internal static void DrawSplitter(Rect rect, bool isBoxed = false)
        {
            if (!isBoxed)
            {
                rect = ToFullWidth(rect);
            }

            if (Event.current.type != EventType.Repaint) return;

            EditorGUI.DrawRect(rect, !EditorGUIUtility.isProSkin
                ? new Color(0.6f, 0.6f, 0.6f, 1.333f)
                : new Color(0.12f, 0.12f, 0.12f, 1.333f));
        }

        internal static Rect ToFullWidth(Rect rect)
        {
            rect.xMin = 0f;
            rect.width += 4f;
            return rect;
        }

        internal static bool DrawHeader(GUIContent content, bool isExpanded)
        {
            const float HEIGHT = 17f;

            DrawSplitter();

            var backgroundRect = GUILayoutUtility.GetRect(1f, HEIGHT);

            var labelRect = backgroundRect;
            labelRect.xMin += 13.5f;
            labelRect.xMax -= 43f;

            var foldoutRect = backgroundRect;
            foldoutRect.xMin += 11.5f;
            foldoutRect.y -= 1f;
            foldoutRect.width = HEIGHT;
            foldoutRect.height = HEIGHT;

            backgroundRect.xMin = 0f;
            backgroundRect.width += 4f;

            var backgroundTint = EditorGUIUtility.isProSkin ? 0.1f : 1f;

            if (backgroundRect.Contains(Event.current.mousePosition))
            {
                backgroundTint *= EditorGUIUtility.isProSkin ? 1.5f : 0.9f;
            }

            EditorGUI.DrawRect(backgroundRect, new Color(backgroundTint, backgroundTint, backgroundTint, 0.2f));
            EditorGUI.LabelField(labelRect, content, EditorStyles.boldLabel);
            isExpanded = EditorGUI.Foldout(foldoutRect, isExpanded, GUIContent.none, true, EditorStyles.foldout);

            var e = Event.current;

            if (e.type == EventType.MouseDown)
            {
                if (backgroundRect.Contains(e.mousePosition))
                {
                    if (e.button == 0)
                    {
                        isExpanded = !isExpanded;
                    }

                    e.Use();
                }
            }

            return isExpanded;
        }
    }
}

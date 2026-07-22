using PrismFanlight.Core;
using UnityEditor;
using UnityEngine;

namespace PrismFanlight.Editor
{
    [CustomPropertyDrawer(typeof(FanlightPoseState))]
    internal sealed class FanlightPoseStateDrawer : PropertyDrawer
    {
        // Fields

        private static readonly GUIContent[] AngleLabels =
        {
            new("P", "Pitch in degrees"),
            new("Y", "Yaw in degrees")
        };


        // Methods

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            var lineHeight = EditorGUIUtility.singleLineHeight;
            position.height = lineHeight;
            property.isExpanded = EditorGUI.Foldout(position, property.isExpanded, label, true);

            if (property.isExpanded)
            {
                using (new EditorGUI.IndentLevelScope())
                {
                    position.y += lineHeight + EditorGUIUtility.standardVerticalSpacing;
                    EditorGUI.PropertyField(position, property.FindPropertyRelative("_readyHandOffset"), new GUIContent("Ready Hand Offset"));
                    position.y += lineHeight + EditorGUIUtility.standardVerticalSpacing;
                    EditorGUI.PropertyField(position, property.FindPropertyRelative("_accentHandOffset"), new GUIContent("Accent Hand Offset"));
                    position.y += lineHeight + EditorGUIUtility.standardVerticalSpacing;
                    EditorGUI.PropertyField(position, property.FindPropertyRelative("_handArcOffset"), new GUIContent("Hand Arc Offset"));
                    position.y += lineHeight + EditorGUIUtility.standardVerticalSpacing;
                    DrawDirection(position, property.FindPropertyRelative("_readyPenlightDirection"), new GUIContent("Ready Penlight Direction"));
                    position.y += lineHeight + EditorGUIUtility.standardVerticalSpacing;
                    DrawDirection(position, property.FindPropertyRelative("_accentPenlightDirection"), new GUIContent("Accent Penlight Direction"));
                    position.y += lineHeight + EditorGUIUtility.standardVerticalSpacing;
                    EditorGUI.PropertyField(position, property.FindPropertyRelative("_bodyLean"), new GUIContent("Body Lean"));
                }
            }

            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var lineCount = property.isExpanded ? 7 : 1;
            return lineCount * EditorGUIUtility.singleLineHeight
                   + (lineCount - 1) * EditorGUIUtility.standardVerticalSpacing;
        }

        internal static void DrawDirection(SerializedProperty property, GUIContent label)
        {
            DrawDirection(EditorGUILayout.GetControlRect(), property, label);
        }

        private static void DrawDirection(Rect position, SerializedProperty property, GUIContent label)
        {
            var direction = property.vector3Value;
            if (!FanlightStateValidation.IsFinite(direction) || direction.sqrMagnitude <= 0.000001f) direction = Vector3.up;
            direction = FanlightStateValidation.RequireDirection(direction, nameof(direction));

            var values = new[]
            {
                Mathf.Asin(Mathf.Clamp(direction.y, -1f, 1f)) * Mathf.Rad2Deg,
                Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg
            };

            var previousMixedValue = EditorGUI.showMixedValue;
            EditorGUI.showMixedValue = property.hasMultipleDifferentValues;
            EditorGUI.BeginChangeCheck();
            var fieldPosition = EditorGUI.PrefixLabel(position, label);
            EditorGUI.MultiFloatField(fieldPosition, AngleLabels, values);

            if (EditorGUI.EndChangeCheck())
            {
                var pitch = Mathf.Clamp(values[0], -90f, 90f) * Mathf.Deg2Rad;
                var yaw = values[1] * Mathf.Deg2Rad;
                var horizontal = Mathf.Cos(pitch);
                if (Mathf.Abs(horizontal) <= 0.000001f) horizontal = 0f;
                property.vector3Value = new Vector3(
                    horizontal * Mathf.Sin(yaw),
                    Mathf.Sin(pitch),
                    horizontal * Mathf.Cos(yaw));
            }

            EditorGUI.showMixedValue = previousMixedValue;
        }
    }
}

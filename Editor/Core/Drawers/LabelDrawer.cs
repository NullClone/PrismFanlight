using PrismFanlight.Core;
using UnityEditor;
using UnityEngine;

namespace PrismFanlight.Editor
{
    [CustomPropertyDrawer(typeof(LabelAttribute))]
    internal class LabelDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (attribute is LabelAttribute labelAttribute)
            {
                EditorGUI.PropertyField(position, property, new GUIContent(labelAttribute.Label), true);
            }
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUIUtility.singleLineHeight;
        }
    }
}

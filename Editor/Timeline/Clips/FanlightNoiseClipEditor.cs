using PrismFanlight.Timeline;
using UnityEditor;
using UnityEngine;

namespace PrismFanlight.Editor
{
    [CustomEditor(typeof(FanlightNoiseClip))]
    internal sealed class FanlightNoiseClipEditor : UnityEditor.Editor
    {
        // Fields

        private SerializedProperty _value;


        // Methods

        private void OnEnable()
        {
            _value = serializedObject.FindProperty(nameof(_value));
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            PrismFanlightEditorStyles.DrawSubGroupLabel("Phase");

            DrawChild("_phaseAmount", "Amount (rad)");

            EditorGUILayout.Space();
            PrismFanlightEditorStyles.DrawSubGroupLabel("Spatial");

            DrawChild("_positionAmount", "Position (m)");
            DrawChild("_directionAmount", "Direction (rad)");

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawChild(string propertyName, string label)
        {
            var property = _value.FindPropertyRelative(propertyName);
            EditorGUILayout.PropertyField(property, new GUIContent(label));
        }
    }
}

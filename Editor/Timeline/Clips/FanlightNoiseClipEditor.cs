using PrismFanlight.Timeline;
using UnityEditor;

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

            DrawChild("_phaseAmount");
            DrawChild("_positionAmount");
            DrawChild("_directionAmount");

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawChild(string propertyName)
        {
            var property = _value.FindPropertyRelative(propertyName);
            EditorGUILayout.PropertyField(property);
        }
    }
}

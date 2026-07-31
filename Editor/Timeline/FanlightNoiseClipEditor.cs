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

            PrismFanlightEditorStyles.DrawSubGroupLabel("Phase");

            DrawSlider("_phaseAmount", "Amount (rad)", 0f, 4f);

            EditorGUILayout.Space();
            PrismFanlightEditorStyles.DrawSubGroupLabel("Spatial");

            DrawSlider("_positionAmount", "Position (m)", 0f, 0.2f);
            DrawSlider("_directionAmount", "Direction (rad)", 0f, 0.4f);

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawSlider(string propertyName, string label, float minimum, float maximum)
        {
            var property = _value.FindPropertyRelative(propertyName);
            property.floatValue = EditorGUILayout.Slider(label, property.floatValue, minimum, maximum);
        }
    }
}

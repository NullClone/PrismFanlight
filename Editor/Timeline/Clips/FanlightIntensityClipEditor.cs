using PrismFanlight.Timeline;
using UnityEditor;

namespace PrismFanlight.Editor
{
    [CustomEditor(typeof(FanlightIntensityClip))]
    internal sealed class FanlightIntensityClipEditor : UnityEditor.Editor
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

            FanlightColorIntensityEditorUtility.DrawIntensityState(_value);

            serializedObject.ApplyModifiedProperties();
        }
    }
}

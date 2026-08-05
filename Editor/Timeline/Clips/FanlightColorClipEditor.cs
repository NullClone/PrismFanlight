using PrismFanlight.Timeline;
using UnityEditor;

namespace PrismFanlight.Editor
{
    [CustomEditor(typeof(FanlightColorClip))]
    internal sealed class FanlightColorClipEditor : UnityEditor.Editor
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

            FanlightColorIntensityEditorUtility.DrawColorState(_value);

            serializedObject.ApplyModifiedProperties();
        }
    }
}

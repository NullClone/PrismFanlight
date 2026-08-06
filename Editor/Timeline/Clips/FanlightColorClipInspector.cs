using PrismFanlight.Timeline;
using UnityEditor;

namespace PrismFanlight.Editor
{
    [CustomEditor(typeof(FanlightColorClip))]
    [CanEditMultipleObjects]
    internal sealed class FanlightColorClipInspector : UnityEditor.Editor
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
            FanlightPresetEditor.Draw(targets);

            serializedObject.Update();

            FanlightColorIntensityEditorUtility.DrawColorState(_value);

            serializedObject.ApplyModifiedProperties();
        }
    }
}

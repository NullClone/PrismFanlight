using PrismFanlight.Timeline;
using UnityEditor;

namespace PrismFanlight.Editor
{
    [CustomEditor(typeof(FanlightTempoClip))]
    [CanEditMultipleObjects]
    internal sealed class FanlightTempoClipInspector : UnityEditor.Editor
    {
        // Fields

        private SerializedProperty _bpm;


        // Methods

        private void OnEnable()
        {
            _bpm = serializedObject.FindProperty(nameof(_bpm));
        }

        public override void OnInspectorGUI()
        {
            FanlightPresetEditor.Draw(targets);

            serializedObject.Update();

            EditorGUILayout.PropertyField(_bpm);

            serializedObject.ApplyModifiedProperties();
        }
    }
}

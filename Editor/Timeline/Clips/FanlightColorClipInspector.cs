using PrismFanlight.Core;
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

            var included = !FanlightTimelineFieldMaskResolver.TryResolve(targets, out var mask, out _) || mask.Color.HasFlag(FanlightColorFields.Source);

            using (new EditorGUI.DisabledScope(!included))
            {
                FanlightColorIntensityEditorUtility.DrawColorState(_value);
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}

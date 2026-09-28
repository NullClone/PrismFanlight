using PrismFanlight.Core;
using PrismFanlight.Timeline;
using UnityEditor;

namespace PrismFanlight.Editor
{
    [CustomEditor(typeof(FanlightDirectionClip))]
    [CanEditMultipleObjects]
    internal sealed class FanlightDirectionClipInspector : UnityEditor.Editor
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

            var includedFields = FanlightTimelineFieldMaskResolver.TryResolve(targets, out var mask, out _)
                ? mask.Direction
                : FanlightDirectionFields.All;

            FanlightStateGUI.DrawDirection(_value, includedFields);

            serializedObject.ApplyModifiedProperties();
        }
    }
}

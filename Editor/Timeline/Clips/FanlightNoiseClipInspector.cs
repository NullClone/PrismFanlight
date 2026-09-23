using PrismFanlight.Core;
using PrismFanlight.Timeline;
using UnityEditor;

namespace PrismFanlight.Editor
{
    [CustomEditor(typeof(FanlightNoiseClip))]
    [CanEditMultipleObjects]
    internal sealed class FanlightNoiseClipInspector : UnityEditor.Editor
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
                ? mask.Noise
                : FanlightNoiseFields.All;

            DrawChild("_phaseAmount", includedFields.HasFlag(FanlightNoiseFields.PhaseAmount));
            DrawChild("_positionAmount", includedFields.HasFlag(FanlightNoiseFields.PositionAmount));
            DrawChild("_directionAmount", includedFields.HasFlag(FanlightNoiseFields.DirectionAmount));

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawChild(string propertyName, bool included)
        {
            var property = _value.FindPropertyRelative(propertyName);

            using (new EditorGUI.DisabledScope(!included))
            {
                EditorGUILayout.PropertyField(property);
            }
        }
    }
}

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

            var mode = _value.FindPropertyRelative("_mode");

            using (new EditorGUI.DisabledScope(!includedFields.HasFlag(FanlightDirectionFields.Mode)))
            {
                EditorGUILayout.PropertyField(mode);
            }

            if (!mode.hasMultipleDifferentValues)
            {
                if (mode.enumValueIndex == (int)FanlightDirectionMode.WorldDirection)
                {
                    DrawChild("_direction", includedFields.HasFlag(FanlightDirectionFields.Direction));
                }
            }

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

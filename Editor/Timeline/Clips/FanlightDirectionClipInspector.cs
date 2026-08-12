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

            var mode = _value.FindPropertyRelative("_mode");

            EditorGUILayout.PropertyField(mode);

            if (!mode.hasMultipleDifferentValues)
            {
                if (mode.enumValueIndex == (int)FanlightDirectionMode.WorldDirection)
                {
                    DrawChild("_direction");
                }
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawChild(string propertyName)
        {
            var property = _value.FindPropertyRelative(propertyName);
            EditorGUILayout.PropertyField(property);
        }
    }
}

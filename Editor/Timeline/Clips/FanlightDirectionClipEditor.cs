using PrismFanlight.Core;
using PrismFanlight.Timeline;
using UnityEditor;

namespace PrismFanlight.Editor
{
    [CustomEditor(typeof(FanlightDirectionClip))]
    internal sealed class FanlightDirectionClipEditor : UnityEditor.Editor
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

            var mode = _value.FindPropertyRelative("_mode");

            EditorGUILayout.PropertyField(mode);

            if (!mode.hasMultipleDifferentValues)
            {
                if (mode.enumValueIndex == (int)FanlightDirectionMode.Target)
                {
                    DrawChild("_aimStrength");
                }

                DrawChild("_worldYawDegrees");
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

using PrismFanlight.Core;
using PrismFanlight.Live;
using UnityEditor;
using UnityEngine;

namespace PrismFanlight.Editor
{
    [CustomEditor(typeof(FanlightLiveLook))]
    [CanEditMultipleObjects]
    internal sealed class FanlightLiveLookEditor : UnityEditor.Editor
    {
        // Fields

        private const string FieldsSuffix = "Fields";

        private static readonly GUIContent FieldsContent = new("Fields");

        private static readonly (FanlightStateKind Kind, string PropertyName, PrismFanlightSection Section)[] Sections =
        {
            (FanlightStateKind.Motion, "_motion", new PrismFanlightSection("Motion")),
            (FanlightStateKind.Intent, "_intent", new PrismFanlightSection("Intent")),
            (FanlightStateKind.Color, "_color", new PrismFanlightSection("Color")),
            (FanlightStateKind.Intensity, "_intensity", new PrismFanlightSection("Intensity")),
            (FanlightStateKind.AudienceBody, "_audienceBody", new PrismFanlightSection("Audience")),
            (FanlightStateKind.Direction, "_direction", new PrismFanlightSection("Direction")),
            (FanlightStateKind.Variation, "_variation", new PrismFanlightSection("Variation")),
            (FanlightStateKind.Noise, "_noise", new PrismFanlightSection("Noise")),
            (FanlightStateKind.Rest, "_rest", new PrismFanlightSection("Rest"))
        };

        private SerializedProperty _quantize;


        // Methods

        private void OnEnable()
        {
            _quantize = serializedObject.FindProperty(nameof(_quantize));
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(_quantize);
            EditorGUILayout.Space();

            for (var i = 0; i < Sections.Length; i++)
            {
                DrawSection(Sections[i].Section, Sections[i].Kind, Sections[i].PropertyName);
            }

            serializedObject.ApplyModifiedProperties();
        }


        private void DrawSection(PrismFanlightSection section, FanlightStateKind kind, string propertyName)
        {
            var fields = serializedObject.FindProperty(propertyName + FieldsSuffix);
            var value = serializedObject.FindProperty(propertyName);
            var dimmed = !fields.hasMultipleDifferentValues && fields.intValue == 0;

            section.DrawSection(() =>
            {
                EditorGUILayout.PropertyField(fields, FieldsContent);
                EditorGUILayout.Space();

                FanlightStateGUI.Draw(value, kind, fields.hasMultipleDifferentValues ? null : fields.intValue);
            }, menu => FanlightStateClipboard.AddMenuItems(menu, targets, kind, propertyName, () => IncludeAllFields(kind, propertyName)), dimmed);
        }

        private void IncludeAllFields(FanlightStateKind kind, string propertyName)
        {
            var destination = new SerializedObject(targets);
            destination.Update();
            destination.FindProperty(propertyName + FieldsSuffix).intValue = FanlightStateSchema.GetAllFields(kind);
            destination.ApplyModifiedProperties();
        }
    }
}

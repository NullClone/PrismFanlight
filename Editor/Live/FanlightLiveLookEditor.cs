using System;
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

        private SerializedProperty _quantize;

        private static readonly PrismFanlightSection _motionSection = new("Motion");
        private static readonly PrismFanlightSection _intentSection = new("Intent");
        private static readonly PrismFanlightSection _colorSection = new("Color");
        private static readonly PrismFanlightSection _intensitySection = new("Intensity");
        private static readonly PrismFanlightSection _audienceSection = new("Audience");
        private static readonly PrismFanlightSection _directionSection = new("Direction");
        private static readonly PrismFanlightSection _variationSection = new("Variation");
        private static readonly PrismFanlightSection _noiseSection = new("Noise");
        private static readonly PrismFanlightSection _restSection = new("Rest");


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

            DrawSection(_motionSection, FanlightStateKind.Motion, "_motion", (int)FanlightMotionFields.All,
                (val, fields) => FanlightStateGUI.DrawMotion(val, (FanlightMotionFields)fields));

            DrawSection(_intentSection, FanlightStateKind.Intent, "_intent", (int)FanlightIntentFields.All,
                (val, fields) => FanlightStateGUI.DrawIntent(val, (FanlightIntentFields)fields));

            DrawSection(_colorSection, FanlightStateKind.Color, "_color", (int)FanlightColorFields.All,
                (val, fields) => DrawColor(val, (FanlightColorFields)fields));

            DrawSection(_intensitySection, FanlightStateKind.Intensity, "_intensity", (int)FanlightIntensityFields.All,
                (val, fields) => FanlightIntensityGUI.DrawIntensityState(val, includedFields: (FanlightIntensityFields)fields));

            DrawSection(_audienceSection, FanlightStateKind.AudienceBody, "_audienceBody", (int)FanlightAudienceBodyFields.All,
                (val, fields) => FanlightStateGUI.DrawAudienceBody(val, (FanlightAudienceBodyFields)fields));

            DrawSection(_directionSection, FanlightStateKind.Direction, "_direction", (int)FanlightDirectionFields.All,
                (val, fields) => FanlightStateGUI.DrawDirection(val, (FanlightDirectionFields)fields));

            DrawSection(_variationSection, FanlightStateKind.Variation, "_variation", (int)FanlightVariationFields.All,
                (val, fields) => FanlightStateGUI.DrawVariation(val, (FanlightVariationFields)fields));

            DrawSection(_noiseSection, FanlightStateKind.Noise, "_noise", (int)FanlightNoiseFields.All,
                (val, fields) => FanlightStateGUI.DrawNoise(val, (FanlightNoiseFields)fields));

            DrawSection(_restSection, FanlightStateKind.Rest, "_rest", (int)FanlightRestFields.All,
                (val, fields) => FanlightStateGUI.DrawRest(val, (FanlightRestFields)fields));

            serializedObject.ApplyModifiedProperties();
        }


        private void DrawSection(
            PrismFanlightSection section,
            FanlightStateKind kind,
            string valuePath,
            int allFields,
            Action<SerializedProperty, int> draw)
        {
            var fields = serializedObject.FindProperty(valuePath + "Fields");
            var value = serializedObject.FindProperty(valuePath);
            var dimmed = !fields.hasMultipleDifferentValues && fields.intValue == 0;

            section.DrawSection(() =>
            {
                EditorGUILayout.PropertyField(fields, new GUIContent("Fields"));
                EditorGUILayout.Space();

                draw(value, fields.hasMultipleDifferentValues ? allFields : fields.intValue);
            }, menu => AddClipboardItems(menu, kind, valuePath, allFields), dimmed);
        }

        private void AddClipboardItems(GenericMenu menu, FanlightStateKind kind, string valuePath, int allFields)
        {
            var copy = new GUIContent("Copy");
            var paste = new GUIContent("Paste");

            if (targets.Length == 1)
            {
                menu.AddItem(copy, false, () => FanlightStateClipboard.Copy(targets, kind, valuePath));
            }
            else
            {
                menu.AddDisabledItem(copy);
            }

            if (!FanlightStateClipboard.CanPaste(kind))
            {
                menu.AddDisabledItem(paste);
                return;
            }

            menu.AddItem(paste, false, () =>
            {
                if (!FanlightStateClipboard.Paste(targets, kind, valuePath)) return;

                var destination = new SerializedObject(targets);
                destination.Update();
                destination.FindProperty(valuePath + "Fields").intValue = allFields;
                destination.ApplyModifiedProperties();
            });
        }

        private static void DrawColor(SerializedProperty value, FanlightColorFields fields)
        {
            using (new EditorGUI.DisabledScope((fields & FanlightColorFields.Source) == 0))
            {
                FanlightColorGUI.DrawColorState(value);
            }
        }
    }
}

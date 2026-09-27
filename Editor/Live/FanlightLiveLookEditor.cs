using System;
using PrismFanlight.Core;
using PrismFanlight.Live;
using PrismFanlight.Timeline;
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

            DrawSection(_motionSection, FanlightTimelinePatchKind.Motion, "_motion", (int)FanlightMotionFields.All,
                (val, fields) => FanlightStateEditorUtility.DrawMotion(val, (FanlightMotionFields)fields));

            DrawSection(_intentSection, FanlightTimelinePatchKind.Intent, "_intent", (int)FanlightIntentFields.All,
                (val, fields) => FanlightStateEditorUtility.DrawIntent(val, (FanlightIntentFields)fields));

            DrawSection(_colorSection, FanlightTimelinePatchKind.Color, "_color", (int)FanlightColorFields.All,
                (val, fields) => DrawColor(val, (FanlightColorFields)fields));

            DrawSection(_intensitySection, FanlightTimelinePatchKind.Intensity, "_intensity", (int)FanlightIntensityFields.All,
                (val, fields) => FanlightColorIntensityEditorUtility.DrawIntensityState(val, includedFields: (FanlightIntensityFields)fields));

            DrawSection(_audienceSection, FanlightTimelinePatchKind.AudienceBody, "_audienceBody", (int)FanlightAudienceBodyFields.All,
                (val, fields) => FanlightStateEditorUtility.DrawAudienceBody(val, (FanlightAudienceBodyFields)fields));

            DrawSection(_directionSection, FanlightTimelinePatchKind.Direction, "_direction", (int)FanlightDirectionFields.All,
                (val, fields) => FanlightStateEditorUtility.DrawDirection(val, (FanlightDirectionFields)fields));

            DrawSection(_variationSection, FanlightTimelinePatchKind.Variation, "_variation", (int)FanlightVariationFields.All,
                (val, fields) => FanlightStateEditorUtility.DrawVariation(val, (FanlightVariationFields)fields));

            DrawSection(_noiseSection, FanlightTimelinePatchKind.Noise, "_noise", (int)FanlightNoiseFields.All,
                (val, fields) => FanlightStateEditorUtility.DrawNoise(val, (FanlightNoiseFields)fields));

            DrawSection(_restSection, FanlightTimelinePatchKind.Rest, "_rest", (int)FanlightRestFields.All,
                (val, fields) => FanlightStateEditorUtility.DrawRest(val, (FanlightRestFields)fields));

            serializedObject.ApplyModifiedProperties();
        }


        private void DrawSection(
            PrismFanlightSection section,
            FanlightTimelinePatchKind kind,
            string valuePath,
            int allFields,
            Action<SerializedProperty, int> draw)
        {
            var fields = serializedObject.FindProperty(valuePath + "Fields");
            var value = serializedObject.FindProperty(valuePath);

            section.DrawSection(() =>
            {
                EditorGUILayout.PropertyField(fields, new GUIContent("Fields"));
                EditorGUILayout.Space();

                draw(value, fields.hasMultipleDifferentValues ? allFields : fields.intValue);
            }, menu => AddClipboardItems(menu, kind, valuePath, allFields));
        }

        private void AddClipboardItems(GenericMenu menu, FanlightTimelinePatchKind kind, string valuePath, int allFields)
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
                FanlightColorIntensityEditorUtility.DrawColorState(value);
            }
        }
    }
}

using System;
using PrismFanlight.Authoring;
using PrismFanlight.Core;
using UnityEditor;
using UnityEngine;

namespace PrismFanlight.Editor
{
    internal static class FanlightStateGUI
    {
        internal static void Draw(
            SerializedProperty state,
            FanlightStateKind kind,
            int? includedFields = null,
            FanlightLayoutAsset layout = null)
        {
            var included = includedFields ?? FanlightStateSchema.GetAllFields(kind);

            switch (kind)
            {
                case FanlightStateKind.Motion:
                    DrawMotion(state, (FanlightMotionFields)included);
                    break;
                case FanlightStateKind.Direction:
                    DrawDirection(state, (FanlightDirectionFields)included);
                    break;
                case FanlightStateKind.Color:
                    using (new EditorGUI.DisabledScope((included & (int)FanlightColorFields.Source) == 0))
                    {
                        FanlightColorGUI.DrawColorState(state, layout);
                    }

                    break;
                case FanlightStateKind.Intensity:
                    FanlightIntensityGUI.DrawIntensityState(state, layout, includedFields: (FanlightIntensityFields)included);
                    break;
                default:
                    DrawFields(state, kind, included);
                    break;
            }
        }

        internal static void DrawFields(
            SerializedProperty state,
            FanlightStateKind kind,
            int? includedFields = null,
            bool includeBaselineFields = false)
        {
            var included = includedFields ?? FanlightStateSchema.GetAllFields(kind);
            var child = state.Copy();
            var end = state.GetEndProperty();
            var enterChildren = true;

            while (child.NextVisible(enterChildren) && !SerializedProperty.EqualContents(child, end))
            {
                enterChildren = false;

                var field = FanlightStateSchema.GetField(kind, child.name);
                if (!field.IsPatchable && !includeBaselineFields) continue;

                using (new EditorGUI.DisabledScope(field.IsPatchable && !field.IsIncluded(included)))
                {
                    if (field.Label != null)
                    {
                        EditorGUILayout.PropertyField(child, field.Label, true);
                    }
                    else
                    {
                        EditorGUILayout.PropertyField(child, true);
                    }
                }
            }
        }

        internal static void DrawMotion(
            SerializedProperty state,
            FanlightMotionFields included = FanlightMotionFields.All,
            Action<SerializedProperty> drawAssetField = null)
        {
            var motionAsset = state.FindPropertyRelative("_motionAsset");
            var beatsPerCycle = state.FindPropertyRelative("_beatsPerCycle");
            var phaseOffsetBeats = state.FindPropertyRelative("_phaseOffsetBeats");
            var sourceIncluded = (included & FanlightMotionFields.Source) != 0;

            using (new EditorGUI.DisabledScope(!sourceIncluded))
            {
                if (drawAssetField != null)
                {
                    drawAssetField(motionAsset);
                }
                else
                {
                    var previousAsset = motionAsset.objectReferenceValue;

                    EditorGUI.BeginChangeCheck();
                    EditorGUILayout.PropertyField(motionAsset);
                    if (EditorGUI.EndChangeCheck()
                        && !motionAsset.serializedObject.isEditingMultipleObjects
                        && previousAsset == null
                        && motionAsset.objectReferenceValue is FanlightMotionAsset asset)
                    {
                        beatsPerCycle.floatValue = FanlightMotionCycleDefaults.Suggest(asset);
                    }
                }

                EditorGUILayout.PropertyField(beatsPerCycle);
                EditorGUILayout.PropertyField(phaseOffsetBeats);
            }

            DrawChild(state, "_blockDelayXBeats", (included & FanlightMotionFields.BlockDelayXBeats) != 0);
            DrawChild(state, "_blockDelayYBeats", (included & FanlightMotionFields.BlockDelayYBeats) != 0);
        }

        internal static void DrawDirection(
            SerializedProperty state,
            FanlightDirectionFields included = FanlightDirectionFields.All,
            SerializedProperty swingTarget = null)
        {
            var mode = state.FindPropertyRelative("_mode");

            DrawChild(state, "_mode", (included & FanlightDirectionFields.Mode) != 0);

            if (!mode.hasMultipleDifferentValues)
            {
                if (mode.enumValueIndex == (int)FanlightDirectionMode.WorldDirection)
                {
                    DrawChild(state, "_direction", (included & FanlightDirectionFields.Direction) != 0);
                }
                else if (swingTarget != null)
                {
                    EditorGUILayout.PropertyField(swingTarget, new GUIContent("Target"));
                }
            }
        }


        internal static void DrawLocalYaw(SerializedProperty property)
        {
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(property, new GUIContent("Angle"));
            var changed = EditorGUI.EndChangeCheck();
            if (!float.IsFinite(property.floatValue)
                || (!changed && property.hasMultipleDifferentValues))
            {
                return;
            }

            property.floatValue = Mathf.Repeat(property.floatValue, 360f);
        }

        internal static bool IsFinite(Vector2 value)
        {
            return float.IsFinite(value.x) && float.IsFinite(value.y);
        }


        private static void DrawChild(SerializedProperty parent, string propertyName, bool included)
        {
            using (new EditorGUI.DisabledScope(!included))
            {
                EditorGUILayout.PropertyField(parent.FindPropertyRelative(propertyName));
            }
        }
    }
}

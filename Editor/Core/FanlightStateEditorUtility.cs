using System;
using PrismFanlight.Authoring;
using PrismFanlight.Core;
using UnityEditor;
using UnityEngine;

namespace PrismFanlight.Editor
{
    internal static class FanlightStateEditorUtility
    {
        internal static void DrawIntent(
            SerializedProperty state,
            FanlightIntentFields included = FanlightIntentFields.All)
        {
            DrawChild(state, "_energy", (included & FanlightIntentFields.Energy) != 0);
            DrawChild(state, "_participation", (included & FanlightIntentFields.Participation) != 0);
            DrawChild(state, "_synchronization", (included & FanlightIntentFields.Synchronization) != 0);
            DrawChild(state, "_transitionScatter", (included & FanlightIntentFields.TransitionScatter) != 0);
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
                    if (EditorGUI.EndChangeCheck() && previousAsset == null && motionAsset.objectReferenceValue is FanlightMotionAsset asset)
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

        internal static void DrawVariation(
            SerializedProperty state,
            FanlightVariationFields included = FanlightVariationFields.All)
        {
            DrawChild(state, "_standingPositionSpread", (included & FanlightVariationFields.StandingPositionSpread) != 0);
            DrawChild(state, "_heightVariation", (included & FanlightVariationFields.HeightVariation) != 0);
            DrawChild(state, "_armExtensionVariation", (included & FanlightVariationFields.ArmExtensionVariation) != 0);
            DrawChild(state, "_penlightDirectionSpread", (included & FanlightVariationFields.PenlightDirectionSpread) != 0);
            DrawChild(state, "_reactionDelaySeconds", (included & FanlightVariationFields.ReactionDelaySeconds) != 0);
            DrawChild(state, "_beatJitterBeats", (included & FanlightVariationFields.BeatJitterBeats) != 0);
            DrawChild(state, "_energyResponse", (included & FanlightVariationFields.EnergyResponse) != 0);
            DrawChild(state, "_handPositionSpread", (included & FanlightVariationFields.HandPositionSpread) != 0);
        }

        internal static void DrawNoise(
            SerializedProperty state,
            FanlightNoiseFields included = FanlightNoiseFields.All,
            bool includeBaselineFields = false)
        {
            DrawChild(state, "_phaseAmount", (included & FanlightNoiseFields.PhaseAmount) != 0);

            if (includeBaselineFields)
            {
                DrawChild(state, "_phaseRate", true);
            }

            DrawChild(state, "_positionAmount", (included & FanlightNoiseFields.PositionAmount) != 0);
            DrawChild(state, "_directionAmount", (included & FanlightNoiseFields.DirectionAmount) != 0);

            if (includeBaselineFields)
            {
                DrawChild(state, "_spatialRate", true);
                DrawChild(state, "_octaves", true);
                DrawChild(state, "_persistence", true);
            }
        }

        internal static void DrawRest(
            SerializedProperty state,
            FanlightRestFields included = FanlightRestFields.All)
        {
            DrawChild(state, "_probability", (included & FanlightRestFields.Probability) != 0);
            DrawChild(state, "_motionLevel", (included & FanlightRestFields.MotionLevel) != 0);
            DrawChild(state, "_cycleSeconds", (included & FanlightRestFields.CycleSeconds) != 0);
            DrawChild(state, "_durationSeconds", (included & FanlightRestFields.DurationSeconds) != 0);
            DrawChild(state, "_fadeSeconds", (included & FanlightRestFields.FadeSeconds) != 0);
            DrawChild(state, "_phaseRandomness", (included & FanlightRestFields.PhaseRandomness) != 0);
        }

        internal static void DrawAudienceBody(
            SerializedProperty state,
            FanlightAudienceBodyFields included = FanlightAudienceBodyFields.All)
        {
            DrawChild(state, "_height", (included & FanlightAudienceBodyFields.Height) != 0);
            DrawChild(state, "_width", (included & FanlightAudienceBodyFields.Width) != 0);
            DrawChild(state, "_headSize", (included & FanlightAudienceBodyFields.HeadSize) != 0);
            DrawChild(state, "_armWidth", (included & FanlightAudienceBodyFields.ArmWidth) != 0);
            DrawChild(state, "_armLengthLimit", (included & FanlightAudienceBodyFields.ArmLengthLimit) != 0);
            DrawChild(state, "_shoulderHeightRatio", (included & FanlightAudienceBodyFields.ShoulderHeightRatio) != 0);
            DrawChild(state, "_shoulderSideOffset", (included & FanlightAudienceBodyFields.ShoulderSideOffset) != 0);
            DrawChild(state, "_bounce", (included & FanlightAudienceBodyFields.Bounce) != 0);
            DrawChild(state, "_sway", (included & FanlightAudienceBodyFields.Sway) != 0);
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


        private static void DrawChild(SerializedProperty parent, string propertyName, bool included)
        {
            using (new EditorGUI.DisabledScope(!included))
            {
                EditorGUILayout.PropertyField(parent.FindPropertyRelative(propertyName));
            }
        }
    }
}

using System;
using System.Collections.Generic;
using PrismFanlight.Authoring;
using PrismFanlight.Core;
using UnityEditor;
using UnityEngine;

namespace PrismFanlight.Editor
{
    internal static class FanlightIntensityGUI
    {
        // Methods

        internal static void DrawIntensityState(
            SerializedProperty state,
            FanlightLayoutAsset layout = null,
            bool requireLayout = false,
            FanlightIntensityFields includedFields = FanlightIntensityFields.All)
        {
            using (new EditorGUI.DisabledScope(!includedFields.HasFlag(FanlightIntensityFields.BaseIntensity)))
            {
                var baseIntensity = state.FindPropertyRelative("_baseIntensity");
                EditorGUILayout.PropertyField(baseIntensity, new GUIContent("Intensity"));

                if (!baseIntensity.hasMultipleDifferentValues && (!float.IsFinite(baseIntensity.floatValue) || baseIntensity.floatValue < 0f))
                {
                    EditorGUILayout.HelpBox("Base Intensity must be a finite value of 0 or greater.", MessageType.Error);
                }
            }

            using (new EditorGUI.DisabledScope(!includedFields.HasFlag(FanlightIntensityFields.RandomIntensity)))
            {
                var randomIntensity = state.FindPropertyRelative("_randomIntensity");
                EditorGUILayout.PropertyField(randomIntensity, new GUIContent("Random Intensity"));

                if (!randomIntensity.hasMultipleDifferentValues
                    && (!float.IsFinite(randomIntensity.floatValue) || randomIntensity.floatValue < 0f || randomIntensity.floatValue > 1f))
                {
                    EditorGUILayout.HelpBox("Random Intensity must be between 0 and 1.", MessageType.Error);
                }
            }

            EditorGUILayout.Space();

            using (new EditorGUI.DisabledScope(!includedFields.HasFlag(FanlightIntensityFields.Mask)))
            {
                DrawIntensityMask(state.FindPropertyRelative("_mask"), layout, requireLayout);
            }
        }

        internal static bool IsBlockAlternatingPulse(SerializedProperty state)
        {
            if (state == null) return false;

            var mode = state.FindPropertyRelative("_mask").FindPropertyRelative("_mode");

            return !mode.hasMultipleDifferentValues && (FanlightIntensityMaskMode)mode.enumValueIndex == FanlightIntensityMaskMode.BlockAlternatingPulse;
        }

        internal static void DrawSelectedBlockPulseGroup(
            SerializedProperty state,
            FanlightLayoutAsset layout,
            int blockIndex)
        {
            if (!IsBlockAlternatingPulse(state)
                || layout == null
                || !layout.IsInitialized
                || blockIndex < 0
                || blockIndex >= layout.BlockCount)
            {
                return;
            }

            var entries = state.FindPropertyRelative("_mask").FindPropertyRelative("_blockPulseEntries");

            if (!HasCompleteBlockPulseMapping(entries, layout))
            {
                EditorGUILayout.HelpBox(
                    "Synchronize the Layout Blocks to create the complete Stable Block ID mapping.",
                    MessageType.Warning);

                if (GUILayout.Button("Synchronize"))
                {
                    SynchronizeBlockPulseEntries(entries, layout);
                }

                if (!HasCompleteBlockPulseMapping(entries, layout)) return;
            }

            var blockId = layout.GetBlock(blockIndex).BlockId;
            var entryIndex = FindBlockPulseEntry(entries, blockId);
            if (entryIndex < 0) return;

            var group = entries.GetArrayElementAtIndex(entryIndex).FindPropertyRelative("_group");
            EditorGUILayout.PropertyField(group, new GUIContent("Pulse Group"));
        }

        internal static void SynchronizeBlockPulseEntries(SerializedProperty entries, FanlightLayoutAsset layout)
        {
            var groupsByBlockId = new Dictionary<string, int>(StringComparer.Ordinal);

            for (var i = 0; i < entries.arraySize; i++)
            {
                var entry = entries.GetArrayElementAtIndex(i);
                var id = entry.FindPropertyRelative("_stableBlockId").stringValue;
                var group = entry.FindPropertyRelative("_group").enumValueIndex;
                if (!string.IsNullOrEmpty(id) && group >= 0 && group <= 1)
                {
                    groupsByBlockId.TryAdd(id, group);
                }
            }

            entries.arraySize = layout.BlockCount;

            for (var blockIndex = 0; blockIndex < layout.BlockCount; blockIndex++)
            {
                var blockId = layout.GetBlock(blockIndex).BlockId;
                var entry = entries.GetArrayElementAtIndex(blockIndex);
                entry.FindPropertyRelative("_stableBlockId").stringValue = blockId;
                entry.FindPropertyRelative("_group").enumValueIndex =
                    groupsByBlockId.TryGetValue(blockId, out var group) ? group : blockIndex & 1;
            }
        }

        private static void DrawIntensityMask(
            SerializedProperty mask,
            FanlightLayoutAsset layout,
            bool requireLayout)
        {
            var mode = mask.FindPropertyRelative("_mode");
            EditorGUILayout.PropertyField(mode, new GUIContent("Mode"));
            if (mode.hasMultipleDifferentValues) return;

            switch ((FanlightIntensityMaskMode)mode.enumValueIndex)
            {
                case FanlightIntensityMaskMode.Pulse:
                    DrawEnvelope(mask);
                    break;
                case FanlightIntensityMaskMode.TravelingWave:
                    DrawEnvelope(mask);
                    EditorGUILayout.PropertyField(mask.FindPropertyRelative("_origin"), new GUIContent("Origin"));
                    FanlightStateGUI.DrawLocalYaw(mask.FindPropertyRelative("_localYawDegrees"));
                    EditorGUILayout.PropertyField(mask.FindPropertyRelative("_wavelength"), new GUIContent("Wavelength"));
                    break;
                case FanlightIntensityMaskMode.RadialWave:
                    DrawEnvelope(mask);
                    EditorGUILayout.PropertyField(mask.FindPropertyRelative("_origin"), new GUIContent("Origin"));
                    EditorGUILayout.PropertyField(mask.FindPropertyRelative("_wavelength"), new GUIContent("Wavelength"));
                    EditorGUILayout.PropertyField(
                        mask.FindPropertyRelative("_radialWaveDirection"),
                        new GUIContent("Propagation Direction"));
                    break;
                case FanlightIntensityMaskMode.RandomSparkle:
                    DrawEnvelope(mask);
                    break;
                case FanlightIntensityMaskMode.AngularWave:
                    DrawEnvelope(mask);
                    EditorGUILayout.PropertyField(mask.FindPropertyRelative("_origin"), new GUIContent("Origin"));
                    FanlightStateGUI.DrawLocalYaw(mask.FindPropertyRelative("_localYawDegrees"));
                    EditorGUILayout.PropertyField(
                        mask.FindPropertyRelative("_angularArmCount"),
                        new GUIContent("Arm Count"));
                    EditorGUILayout.PropertyField(
                        mask.FindPropertyRelative("_angularWaveDirection"),
                        new GUIContent("Rotation Direction"));
                    break;
                case FanlightIntensityMaskMode.BlockAlternatingPulse:
                    DrawEnvelope(mask);
                    var entries = mask.FindPropertyRelative("_blockPulseEntries");
                    EditorGUILayout.PropertyField(entries, new GUIContent("Block Pulse Entries"), true);
                    if (layout != null && layout.IsInitialized && GUILayout.Button("Synchronize"))
                    {
                        SynchronizeBlockPulseEntries(entries, layout);
                    }

                    break;
            }

            var maskMode = (FanlightIntensityMaskMode)mode.enumValueIndex;
            DrawIntensityMaskValidation(mask, maskMode);
            DrawBlockPulseValidation(mask, maskMode, layout, requireLayout);
        }

        private static void DrawEnvelope(SerializedProperty mask)
        {
            EditorGUILayout.PropertyField(
                mask.FindPropertyRelative("_beatsPerCycle"),
                new GUIContent("Beats Per Cycle"));
            EditorGUILayout.PropertyField(
                mask.FindPropertyRelative("_phaseOffsetBeats"),
                new GUIContent("Phase Offset Beats"));
            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(
                mask.FindPropertyRelative("_minimumIntensityRatio"),
                new GUIContent("Minimum Ratio"));
            EditorGUILayout.PropertyField(
                mask.FindPropertyRelative("_attackRatio"),
                new GUIContent("Attack Ratio"));
            EditorGUILayout.PropertyField(
                mask.FindPropertyRelative("_holdRatio"),
                new GUIContent("Hold Ratio"));
            EditorGUILayout.PropertyField(
                mask.FindPropertyRelative("_releaseRatio"),
                new GUIContent("Release Ratio"));
            EditorGUILayout.Space();
        }

        private static void DrawIntensityMaskValidation(SerializedProperty mask, FanlightIntensityMaskMode mode)
        {
            if (mode == FanlightIntensityMaskMode.None) return;

            if (mode != FanlightIntensityMaskMode.Pulse
                && mode != FanlightIntensityMaskMode.TravelingWave
                && mode != FanlightIntensityMaskMode.RadialWave
                && mode != FanlightIntensityMaskMode.RandomSparkle
                && mode != FanlightIntensityMaskMode.AngularWave
                && mode != FanlightIntensityMaskMode.BlockAlternatingPulse)
            {
                EditorGUILayout.HelpBox("Intensity Pattern Mode is invalid.", MessageType.Error);
                return;
            }

            var beatsPerCycle = mask.FindPropertyRelative("_beatsPerCycle").floatValue;
            var phaseOffsetBeats = mask.FindPropertyRelative("_phaseOffsetBeats").floatValue;
            var minimum = mask.FindPropertyRelative("_minimumIntensityRatio").floatValue;
            var attack = mask.FindPropertyRelative("_attackRatio").floatValue;
            var hold = mask.FindPropertyRelative("_holdRatio").floatValue;
            var release = mask.FindPropertyRelative("_releaseRatio").floatValue;
            var activeRatio = attack + hold + release;
            var invalid = !float.IsFinite(beatsPerCycle)
                          || beatsPerCycle <= 0f
                          || !float.IsFinite(phaseOffsetBeats)
                          || !IsRatio(minimum)
                          || !IsRatio(attack)
                          || !IsRatio(hold)
                          || !IsRatio(release)
                          || !float.IsFinite(activeRatio)
                          || activeRatio <= 0f
                          || activeRatio > 1f;

            if (mode == FanlightIntensityMaskMode.TravelingWave
                || mode == FanlightIntensityMaskMode.RadialWave)
            {
                var origin = mask.FindPropertyRelative("_origin").vector2Value;
                var wavelength = mask.FindPropertyRelative("_wavelength").floatValue;
                invalid |= !FanlightStateGUI.IsFinite(origin)
                           || !float.IsFinite(wavelength)
                           || wavelength <= 0f;
            }

            if (mode == FanlightIntensityMaskMode.TravelingWave
                || mode == FanlightIntensityMaskMode.AngularWave)
            {
                invalid |= !float.IsFinite(mask.FindPropertyRelative("_localYawDegrees").floatValue);
            }

            if (mode == FanlightIntensityMaskMode.AngularWave)
            {
                invalid |= !FanlightStateGUI.IsFinite(mask.FindPropertyRelative("_origin").vector2Value);
            }

            if (mode == FanlightIntensityMaskMode.RadialWave)
            {
                var direction = mask.FindPropertyRelative("_radialWaveDirection").enumValueIndex;
                invalid |= direction < 0 || direction > 1;
            }

            if (mode == FanlightIntensityMaskMode.AngularWave)
            {
                var direction = mask.FindPropertyRelative("_angularWaveDirection").enumValueIndex;
                var armCount = mask.FindPropertyRelative("_angularArmCount").intValue;
                invalid |= direction < 0 || direction > 1 || armCount < 1;
            }

            if (invalid)
            {
                EditorGUILayout.HelpBox(
                    "Intensity Pattern fields are outside the valid range for the selected Mode.",
                    MessageType.Error);
            }
        }

        private static int FindBlockPulseEntry(SerializedProperty entries, string blockId)
        {
            for (var i = 0; i < entries.arraySize; i++)
            {
                var entry = entries.GetArrayElementAtIndex(i);
                if (string.Equals(entry.FindPropertyRelative("_stableBlockId").stringValue, blockId, StringComparison.Ordinal))
                {
                    return i;
                }
            }

            return -1;
        }

        private static bool HasCompleteBlockPulseMapping(SerializedProperty entries, FanlightLayoutAsset layout)
        {
            if (entries == null
                || layout == null
                || !layout.IsInitialized
                || entries.arraySize != layout.BlockCount)
            {
                return false;
            }

            var ids = new HashSet<string>(StringComparer.Ordinal);
            for (var i = 0; i < entries.arraySize; i++)
            {
                var entry = entries.GetArrayElementAtIndex(i);
                var id = entry.FindPropertyRelative("_stableBlockId").stringValue;
                var group = entry.FindPropertyRelative("_group").enumValueIndex;
                if (string.IsNullOrEmpty(id) || group < 0 || group > 1 || !ids.Add(id)) return false;
            }

            for (var blockIndex = 0; blockIndex < layout.BlockCount; blockIndex++)
            {
                if (!ids.Contains(layout.GetBlock(blockIndex).BlockId)) return false;
            }

            return true;
        }

        private static void DrawBlockPulseValidation(
            SerializedProperty mask,
            FanlightIntensityMaskMode mode,
            FanlightLayoutAsset layout,
            bool requireLayout)
        {
            if (mode != FanlightIntensityMaskMode.BlockAlternatingPulse) return;

            var entries = mask.FindPropertyRelative("_blockPulseEntries");
            if (entries.arraySize == 0)
            {
                EditorGUILayout.HelpBox(
                    "Block Alternating Pulse requires a complete Stable Block ID mapping.",
                    MessageType.Error);
                return;
            }

            var ids = new HashSet<string>(StringComparer.Ordinal);
            for (var i = 0; i < entries.arraySize; i++)
            {
                var entry = entries.GetArrayElementAtIndex(i);
                var id = entry.FindPropertyRelative("_stableBlockId").stringValue;
                var group = entry.FindPropertyRelative("_group").enumValueIndex;
                if (string.IsNullOrEmpty(id) || group < 0 || group > 1 || !ids.Add(id))
                {
                    EditorGUILayout.HelpBox(
                        "Block Pulse Entries contain an empty or duplicate Stable Block ID, or an invalid Group.",
                        MessageType.Error);
                    return;
                }
            }

            if (layout == null || !layout.IsInitialized)
            {
                if (requireLayout)
                {
                    EditorGUILayout.HelpBox(
                        "Block Alternating Pulse requires an initialized Layout.",
                        MessageType.Error);
                }

                return;
            }

            if (!HasCompleteBlockPulseMapping(entries, layout))
            {
                EditorGUILayout.HelpBox(
                    "Block Alternating Pulse must map every active Layout Block exactly once.",
                    MessageType.Error);
            }
        }

        private static bool IsRatio(float value)
        {
            return float.IsFinite(value) && value >= 0f && value <= 1f;
        }
    }
}

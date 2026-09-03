using System;
using System.Collections.Generic;
using PrismFanlight.Authoring;
using PrismFanlight.Core;
using UnityEditor;
using UnityEngine;

namespace PrismFanlight.Editor
{
    internal static class FanlightColorIntensityEditorUtility
    {
        // Fields

        private static readonly string[] PaletteSlotOptions =
        {
            "Slot 1",
            "Slot 2",
            "Slot 3",
            "Slot 4",
            "Slot 5",
            "Slot 6"
        };


        // Methods

        internal static void DrawColorState(SerializedProperty state, FanlightLayoutAsset layout = null, bool requireLayout = false)
        {
            var source = state.FindPropertyRelative("_source");

            DrawColorSource(source, layout);
            DrawBlockPaletteValidation(source, layout, requireLayout);
        }

        internal static void DrawIntensityState(
            SerializedProperty state,
            FanlightLayoutAsset layout = null,
            bool requireLayout = false)
        {
            var baseIntensity = state.FindPropertyRelative("_baseIntensity");
            EditorGUILayout.PropertyField(baseIntensity, new GUIContent("Intensity"));

            if (!baseIntensity.hasMultipleDifferentValues && (!float.IsFinite(baseIntensity.floatValue) || baseIntensity.floatValue < 0f))
            {
                EditorGUILayout.HelpBox("Base Intensity must be a finite value of 0 or greater.", MessageType.Error);
            }

            var randomIntensity = state.FindPropertyRelative("_randomIntensity");
            EditorGUILayout.PropertyField(randomIntensity, new GUIContent("Random Intensity"));

            if (!randomIntensity.hasMultipleDifferentValues
                && (!float.IsFinite(randomIntensity.floatValue) || randomIntensity.floatValue < 0f || randomIntensity.floatValue > 1f))
            {
                EditorGUILayout.HelpBox("Random Intensity must be between 0 and 1.", MessageType.Error);
            }

            EditorGUILayout.Space();

            DrawIntensityMask(state.FindPropertyRelative("_mask"), layout, requireLayout);
        }

        internal static bool IsBlockPalette(SerializedProperty state)
        {
            if (state == null) return false;

            var mode = state.FindPropertyRelative("_source").FindPropertyRelative("_mode");

            return !mode.hasMultipleDifferentValues && (FanlightColorMode)mode.enumValueIndex == FanlightColorMode.BlockPalette;
        }

        internal static bool IsBlockAlternatingPulse(SerializedProperty state)
        {
            if (state == null) return false;

            var mode = state.FindPropertyRelative("_mask").FindPropertyRelative("_mode");

            return !mode.hasMultipleDifferentValues
                   && (FanlightIntensityMaskMode)mode.enumValueIndex
                   == FanlightIntensityMaskMode.BlockAlternatingPulse;
        }

        internal static void DrawSelectedBlockColor(
            SerializedProperty state,
            FanlightLayoutAsset layout,
            IReadOnlyList<int> blockIndices,
            int activeBlockIndex)
        {
            if (!IsBlockPalette(state)
                || layout == null
                || !layout.IsInitialized
                || blockIndices == null
                || blockIndices.Count == 0
                || activeBlockIndex < 0
                || activeBlockIndex >= layout.BlockCount)
            {
                return;
            }

            var source = state.FindPropertyRelative("_source");
            var entries = source.FindPropertyRelative("_blockPaletteEntries");

            if (!HasCompleteBlockPaletteMapping(entries, layout))
            {
                EditorGUILayout.HelpBox(
                    "Synchronize the Layout Blocks to create the complete Stable Block ID mapping.",
                    MessageType.Warning);

                if (GUILayout.Button("Synchronize"))
                {
                    SynchronizeBlockPaletteEntries(entries, layout);
                }

                if (!HasCompleteBlockPaletteMapping(entries, layout)) return;
            }

            var paletteSlot = FindBlockPaletteSlot(entries, layout, activeBlockIndex);
            if (paletteSlot == null) return;

            EditorGUI.showMixedValue = HasMixedBlockPaletteSlot(
                entries,
                layout,
                blockIndices,
                paletteSlot.intValue);
            EditorGUI.BeginChangeCheck();
            var nextSlot = EditorGUILayout.Popup("Palette Slot", paletteSlot.intValue, PaletteSlotOptions);
            var slotChanged = EditorGUI.EndChangeCheck();
            EditorGUI.showMixedValue = false;
            if (slotChanged)
            {
                SetBlockPaletteSlots(entries, layout, blockIndices, nextSlot);
            }

            DrawSelectedBlockChroma(
                source,
                entries,
                layout,
                blockIndices,
                nextSlot);
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

        private static void DrawColorSource(SerializedProperty source, FanlightLayoutAsset layout)
        {
            var mode = source.FindPropertyRelative("_mode");
            EditorGUILayout.PropertyField(mode, new GUIContent("Mode"));

            if (mode.hasMultipleDifferentValues) return;

            switch ((FanlightColorMode)mode.enumValueIndex)
            {
                case FanlightColorMode.StablePalette:
                    DrawPalette(source);
                    break;
                case FanlightColorMode.LinearGradient:
                    DrawChroma(source.FindPropertyRelative("_colorA"), "Color A");
                    DrawChroma(source.FindPropertyRelative("_colorB"), "Color B");
                    EditorGUILayout.Space();
                    EditorGUILayout.PropertyField(source.FindPropertyRelative("_origin"), new GUIContent("Origin"));
                    DrawLocalYaw(source.FindPropertyRelative("_localYawDegrees"));
                    EditorGUILayout.PropertyField(source.FindPropertyRelative("_width"), new GUIContent("Width"));
                    EditorGUILayout.PropertyField(source.FindPropertyRelative("_offset"), new GUIContent("Offset"));
                    break;
                case FanlightColorMode.BlockPalette:
                    DrawPalette(source);
                    var entries = source.FindPropertyRelative("_blockPaletteEntries");
                    EditorGUILayout.Space();
                    EditorGUILayout.PropertyField(entries, new GUIContent("Block Palette Entries"), true);

                    if (layout != null && layout.IsInitialized && GUILayout.Button("Synchronize"))
                    {
                        SynchronizeBlockPaletteEntries(entries, layout);
                    }

                    break;
            }

            DrawColorSourceValidation(source, (FanlightColorMode)mode.enumValueIndex);
        }

        private static int FindBlockPaletteEntry(SerializedProperty entries, string blockId)
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

        private static SerializedProperty FindBlockPaletteSlot(
            SerializedProperty entries,
            FanlightLayoutAsset layout,
            int blockIndex)
        {
            if (blockIndex < 0 || blockIndex >= layout.BlockCount) return null;

            var blockId = layout.GetBlock(blockIndex).BlockId;
            var entryIndex = FindBlockPaletteEntry(entries, blockId);
            return entryIndex < 0
                ? null
                : entries.GetArrayElementAtIndex(entryIndex).FindPropertyRelative("_paletteSlot");
        }

        private static bool HasMixedBlockPaletteSlot(
            SerializedProperty entries,
            FanlightLayoutAsset layout,
            IReadOnlyList<int> blockIndices,
            int activeSlot)
        {
            for (var i = 0; i < blockIndices.Count; i++)
            {
                var paletteSlot = FindBlockPaletteSlot(entries, layout, blockIndices[i]);
                if (paletteSlot == null || paletteSlot.intValue != activeSlot) return true;
            }

            return false;
        }

        private static void SetBlockPaletteSlots(
            SerializedProperty entries,
            FanlightLayoutAsset layout,
            IReadOnlyList<int> blockIndices,
            int paletteSlot)
        {
            for (var i = 0; i < blockIndices.Count; i++)
            {
                var property = FindBlockPaletteSlot(entries, layout, blockIndices[i]);
                if (property != null) property.intValue = paletteSlot;
            }
        }

        private static void DrawSelectedBlockChroma(
            SerializedProperty source,
            SerializedProperty entries,
            FanlightLayoutAsset layout,
            IReadOnlyList<int> blockIndices,
            int activeSlot)
        {
            var activeColor = source.FindPropertyRelative($"_slot{activeSlot + 1}").colorValue;
            var hasMixedValue = false;
            var hasInvalidValue = false;
            for (var i = 0; i < blockIndices.Count; i++)
            {
                var paletteSlot = FindBlockPaletteSlot(entries, layout, blockIndices[i]);
                if (paletteSlot == null) continue;

                var color = source.FindPropertyRelative($"_slot{paletteSlot.intValue + 1}").colorValue;
                hasMixedValue |= !color.Equals(activeColor);
                hasInvalidValue |= !IsValidChroma(color);
            }

            EditorGUI.showMixedValue = hasMixedValue;
            EditorGUI.BeginChangeCheck();
            var colorValue = EditorGUILayout.ColorField(
                new GUIContent("Slot Color"),
                activeColor,
                true,
                false,
                false);
            var colorChanged = EditorGUI.EndChangeCheck();
            EditorGUI.showMixedValue = false;
            if (colorChanged)
            {
                colorValue = NormalizeChroma(colorValue);
                var changedSlots = new HashSet<int>();
                for (var i = 0; i < blockIndices.Count; i++)
                {
                    var paletteSlot = FindBlockPaletteSlot(entries, layout, blockIndices[i]);
                    if (paletteSlot == null || !changedSlots.Add(paletteSlot.intValue)) continue;

                    source.FindPropertyRelative($"_slot{paletteSlot.intValue + 1}").colorValue = colorValue;
                }

                hasInvalidValue = false;
            }

            if (hasInvalidValue)
            {
                EditorGUILayout.HelpBox("Slot Color must use finite HSV Value 1 and Alpha 1.", MessageType.Error);
            }
        }

        private static bool HasCompleteBlockPaletteMapping(SerializedProperty entries, FanlightLayoutAsset layout)
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
                var slot = entry.FindPropertyRelative("_paletteSlot").intValue;
                if (string.IsNullOrEmpty(id) || slot < 0 || slot > 5 || !ids.Add(id)) return false;
            }

            for (var blockIndex = 0; blockIndex < layout.BlockCount; blockIndex++)
            {
                if (!ids.Contains(layout.GetBlock(blockIndex).BlockId)) return false;
            }

            return true;
        }

        internal static void SynchronizeBlockPaletteEntries(SerializedProperty entries, FanlightLayoutAsset layout)
        {
            var slotsByBlockId = new Dictionary<string, int>(StringComparer.Ordinal);

            for (var i = 0; i < entries.arraySize; i++)
            {
                var entry = entries.GetArrayElementAtIndex(i);
                var id = entry.FindPropertyRelative("_stableBlockId").stringValue;
                var slot = entry.FindPropertyRelative("_paletteSlot").intValue;
                if (!string.IsNullOrEmpty(id) && slot >= 0 && slot <= 5)
                {
                    slotsByBlockId.TryAdd(id, slot);
                }
            }

            entries.arraySize = layout.BlockCount;

            for (var blockIndex = 0; blockIndex < layout.BlockCount; blockIndex++)
            {
                var blockId = layout.GetBlock(blockIndex).BlockId;
                var entry = entries.GetArrayElementAtIndex(blockIndex);
                entry.FindPropertyRelative("_stableBlockId").stringValue = blockId;
                entry.FindPropertyRelative("_paletteSlot").intValue =
                    slotsByBlockId.TryGetValue(blockId, out var slot) ? slot : 0;
            }
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

        private static void DrawPalette(SerializedProperty source)
        {
            DrawChroma(source.FindPropertyRelative("_slot1"), "Slot 1");
            DrawChroma(source.FindPropertyRelative("_slot2"), "Slot 2");
            DrawChroma(source.FindPropertyRelative("_slot3"), "Slot 3");
            DrawChroma(source.FindPropertyRelative("_slot4"), "Slot 4");
            DrawChroma(source.FindPropertyRelative("_slot5"), "Slot 5");
            DrawChroma(source.FindPropertyRelative("_slot6"), "Slot 6");
        }

        private static void DrawChroma(SerializedProperty property, string label)
        {
            EditorGUI.BeginChangeCheck();
            var color = EditorGUILayout.ColorField(
                new GUIContent(label),
                property.colorValue,
                true,
                false,
                false);
            if (EditorGUI.EndChangeCheck())
            {
                property.colorValue = NormalizeChroma(color);
            }

            if (!property.hasMultipleDifferentValues && !IsValidChroma(property.colorValue))
            {
                EditorGUILayout.HelpBox($"{label} must use finite HSV Value 1 and Alpha 1.", MessageType.Error);
            }
        }

        private static Color NormalizeChroma(Color color)
        {
            Color.RGBToHSV(color, out var hue, out var saturation, out _);
            color = Color.HSVToRGB(hue, saturation, 1f);
            color.a = 1f;
            return color;
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
                    DrawLocalYaw(mask.FindPropertyRelative("_localYawDegrees"));
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
                    DrawLocalYaw(mask.FindPropertyRelative("_localYawDegrees"));
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

        private static bool IsValidChroma(Color color)
        {
            if (!float.IsFinite(color.r)
                || !float.IsFinite(color.g)
                || !float.IsFinite(color.b)
                || !float.IsFinite(color.a)
                || color.r < 0f
                || color.r > 1f
                || color.g < 0f
                || color.g > 1f
                || color.b < 0f
                || color.b > 1f
                || Mathf.Abs(color.a - 1f) > 0.0001f)
            {
                return false;
            }

            Color.RGBToHSV(color, out _, out _, out var value);
            return Mathf.Abs(value - 1f) <= 0.0001f;
        }

        private static void DrawColorSourceValidation(SerializedProperty source, FanlightColorMode mode)
        {
            if (mode != FanlightColorMode.LinearGradient) return;

            var origin = source.FindPropertyRelative("_origin").vector2Value;
            var localYawDegrees = source.FindPropertyRelative("_localYawDegrees").floatValue;
            var width = source.FindPropertyRelative("_width").floatValue;
            var offset = source.FindPropertyRelative("_offset").floatValue;
            if (!IsFinite(origin)
                || !float.IsFinite(localYawDegrees)
                || !float.IsFinite(width)
                || width <= 0f
                || !float.IsFinite(offset))
            {
                EditorGUILayout.HelpBox(
                    "Linear Gradient requires finite Origin, Local Yaw Degrees, and Offset, and Width greater than 0.",
                    MessageType.Error);
            }
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
                invalid |= !IsFinite(origin)
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
                invalid |= !IsFinite(mask.FindPropertyRelative("_origin").vector2Value);
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

        private static bool IsFinite(Vector2 value)
        {
            return float.IsFinite(value.x) && float.IsFinite(value.y);
        }

        private static void DrawLocalYaw(SerializedProperty property)
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

        private static bool IsRatio(float value)
        {
            return float.IsFinite(value) && value >= 0f && value <= 1f;
        }

        private static void DrawBlockPaletteValidation(SerializedProperty source, FanlightLayoutAsset layout, bool requireLayout)
        {
            var mode = source.FindPropertyRelative("_mode");
            if (mode.hasMultipleDifferentValues
                || (FanlightColorMode)mode.enumValueIndex != FanlightColorMode.BlockPalette)
            {
                return;
            }

            var entries = source.FindPropertyRelative("_blockPaletteEntries");
            if (entries.arraySize == 0)
            {
                EditorGUILayout.HelpBox(
                    "Block Palette requires a complete Stable Block ID mapping.",
                    MessageType.Error);
                return;
            }

            var ids = new HashSet<string>(StringComparer.Ordinal);
            for (var i = 0; i < entries.arraySize; i++)
            {
                var entry = entries.GetArrayElementAtIndex(i);
                var id = entry.FindPropertyRelative("_stableBlockId").stringValue;
                var slot = entry.FindPropertyRelative("_paletteSlot").intValue;
                if (string.IsNullOrEmpty(id) || slot < 0 || slot > 5 || !ids.Add(id))
                {
                    EditorGUILayout.HelpBox(
                        "Block Palette contains an empty or duplicate Stable Block ID, or a Slot outside 0..5.",
                        MessageType.Error);
                    return;
                }
            }

            if (layout == null || !layout.IsInitialized)
            {
                if (requireLayout)
                {
                    EditorGUILayout.HelpBox(
                        "Block Palette requires an initialized Layout.",
                        MessageType.Error);
                }

                return;
            }

            if (entries.arraySize != layout.BlockCount)
            {
                EditorGUILayout.HelpBox(
                    "Block Palette must map every active Layout Block exactly once.",
                    MessageType.Error);
                return;
            }

            for (var blockIndex = 0; blockIndex < layout.BlockCount; blockIndex++)
            {
                if (!ids.Contains(layout.GetBlock(blockIndex).BlockId))
                {
                    EditorGUILayout.HelpBox(
                        "Block Palette contains an unknown Stable Block ID or omits an active Layout Block.",
                        MessageType.Error);
                    return;
                }
            }
        }
    }
}

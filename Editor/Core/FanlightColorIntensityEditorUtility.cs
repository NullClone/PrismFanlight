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

        internal static void DrawIntensityState(SerializedProperty state)
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

            DrawIntensityMask(state.FindPropertyRelative("_mask"));
        }

        internal static bool IsBlockPalette(SerializedProperty state)
        {
            if (state == null) return false;

            var mode = state.FindPropertyRelative("_source").FindPropertyRelative("_mode");

            return !mode.hasMultipleDifferentValues && (FanlightColorMode)mode.enumValueIndex == FanlightColorMode.BlockPalette;
        }

        internal static void DrawSelectedBlockColor(SerializedProperty state, FanlightLayoutAsset layout, int blockIndex)
        {
            if (!IsBlockPalette(state)
                || layout == null
                || !layout.IsInitialized
                || blockIndex < 0
                || blockIndex >= layout.TotalBlockCount)
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

            var blockId = layout.GetBlock(blockIndex).BlockId;
            var entryIndex = FindBlockPaletteEntry(entries, blockId);

            if (entryIndex < 0) return;

            var entry = entries.GetArrayElementAtIndex(entryIndex);
            var paletteSlot = entry.FindPropertyRelative("_paletteSlot");
            EditorGUI.BeginChangeCheck();
            var nextSlot = EditorGUILayout.Popup("Palette Slot", paletteSlot.intValue, PaletteSlotOptions);
            if (EditorGUI.EndChangeCheck())
            {
                paletteSlot.intValue = nextSlot;
            }

            DrawChroma(source.FindPropertyRelative($"_slot{paletteSlot.intValue + 1}"), "Slot Color");
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

        private static bool HasCompleteBlockPaletteMapping(SerializedProperty entries, FanlightLayoutAsset layout)
        {
            if (entries == null
                || layout == null
                || !layout.IsInitialized
                || entries.arraySize != layout.TotalBlockCount)
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

            for (var blockIndex = 0; blockIndex < layout.TotalBlockCount; blockIndex++)
            {
                if (!ids.Contains(layout.GetBlock(blockIndex).BlockId)) return false;
            }

            return true;
        }

        private static void SynchronizeBlockPaletteEntries(SerializedProperty entries, FanlightLayoutAsset layout)
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

            entries.arraySize = layout.TotalBlockCount;

            for (var blockIndex = 0; blockIndex < layout.TotalBlockCount; blockIndex++)
            {
                var blockId = layout.GetBlock(blockIndex).BlockId;
                var entry = entries.GetArrayElementAtIndex(blockIndex);
                entry.FindPropertyRelative("_stableBlockId").stringValue = blockId;
                entry.FindPropertyRelative("_paletteSlot").intValue =
                    slotsByBlockId.TryGetValue(blockId, out var slot) ? slot : 0;
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
                Color.RGBToHSV(color, out var hue, out var saturation, out _);
                color = Color.HSVToRGB(hue, saturation, 1f);
                color.a = 1f;
                property.colorValue = color;
            }

            if (!property.hasMultipleDifferentValues && !IsValidChroma(property.colorValue))
            {
                EditorGUILayout.HelpBox($"{label} must use finite HSV Value 1 and Alpha 1.", MessageType.Error);
            }
        }

        private static void DrawIntensityMask(SerializedProperty mask)
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
            }

            DrawIntensityMaskValidation(mask, (FanlightIntensityMaskMode)mode.enumValueIndex);
        }

        private static void DrawEnvelope(SerializedProperty mask)
        {
            EditorGUILayout.PropertyField(
                mask.FindPropertyRelative("_beatsPerCycle"),
                new GUIContent("Beats Per Cycle"));
            EditorGUILayout.PropertyField(
                mask.FindPropertyRelative("_phaseOffsetBeats"),
                new GUIContent("Phase Offset Beats"));
            EditorGUILayout.PropertyField(
                mask.FindPropertyRelative("_minimumIntensityRatio"),
                new GUIContent("Minimum Intensity Ratio"));
            EditorGUILayout.PropertyField(
                mask.FindPropertyRelative("_attackRatio"),
                new GUIContent("Attack Ratio"));
            EditorGUILayout.PropertyField(
                mask.FindPropertyRelative("_holdRatio"),
                new GUIContent("Hold Ratio"));
            EditorGUILayout.PropertyField(
                mask.FindPropertyRelative("_releaseRatio"),
                new GUIContent("Release Ratio"));
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
                && mode != FanlightIntensityMaskMode.TravelingWave)
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

            if (mode == FanlightIntensityMaskMode.TravelingWave)
            {
                var origin = mask.FindPropertyRelative("_origin").vector2Value;
                var localYawDegrees = mask.FindPropertyRelative("_localYawDegrees").floatValue;
                var wavelength = mask.FindPropertyRelative("_wavelength").floatValue;
                invalid |= !IsFinite(origin)
                           || !float.IsFinite(localYawDegrees)
                           || !float.IsFinite(wavelength)
                           || wavelength <= 0f;
            }

            if (invalid)
            {
                EditorGUILayout.HelpBox(
                    "Intensity Pattern fields are outside the valid range for the selected Mode.",
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
            EditorGUILayout.PropertyField(property, new GUIContent("Local Yaw Degrees"));
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

            if (entries.arraySize != layout.TotalBlockCount)
            {
                EditorGUILayout.HelpBox(
                    "Block Palette must map every active Layout Block exactly once.",
                    MessageType.Error);
                return;
            }

            for (var blockIndex = 0; blockIndex < layout.TotalBlockCount; blockIndex++)
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

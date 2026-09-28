using System;
using System.Collections.Generic;
using PrismFanlight.Authoring;
using PrismFanlight.Core;
using UnityEditor;
using UnityEngine;

namespace PrismFanlight.Editor
{
    internal static class FanlightColorGUI
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

        internal static bool IsBlockPalette(SerializedProperty state)
        {
            if (state == null) return false;

            var mode = state.FindPropertyRelative("_source").FindPropertyRelative("_mode");

            return !mode.hasMultipleDifferentValues && (FanlightColorMode)mode.enumValueIndex == FanlightColorMode.BlockPalette;
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
                    slotsByBlockId.GetValueOrDefault(blockId, 0);
            }
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
                    FanlightStateGUI.DrawLocalYaw(source.FindPropertyRelative("_localYawDegrees"));
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
            if (!FanlightStateGUI.IsFinite(origin)
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

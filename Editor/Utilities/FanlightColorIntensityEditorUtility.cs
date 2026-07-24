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
        // Methods

        internal static void DrawColorState(
            SerializedProperty state,
            FanlightLayoutAsset layout = null,
            bool requireLayout = false)
        {
            var source = state.FindPropertyRelative("_source");
            DrawColorSource(source);
            DrawBlockPaletteValidation(source, layout, requireLayout);
        }

        internal static void DrawIntensityState(SerializedProperty state)
        {
            var baseIntensity = state.FindPropertyRelative("_baseIntensity");
            EditorGUILayout.PropertyField(baseIntensity, new GUIContent("Base Intensity"));
            if (!baseIntensity.hasMultipleDifferentValues
                && (!float.IsFinite(baseIntensity.floatValue) || baseIntensity.floatValue < 0f))
            {
                EditorGUILayout.HelpBox("Base Intensity must be a finite value of 0 or greater.", MessageType.Error);
            }

            var randomIntensity = state.FindPropertyRelative("_randomIntensity");
            EditorGUI.BeginChangeCheck();
            var randomValue = EditorGUILayout.Slider(
                "Random Intensity",
                randomIntensity.floatValue,
                0f,
                1f);
            if (EditorGUI.EndChangeCheck()) randomIntensity.floatValue = randomValue;
            if (!randomIntensity.hasMultipleDifferentValues
                && (!float.IsFinite(randomIntensity.floatValue)
                    || randomIntensity.floatValue < 0f
                    || randomIntensity.floatValue > 1f))
            {
                EditorGUILayout.HelpBox("Random Intensity must be between 0 and 1.", MessageType.Error);
            }

            EditorGUILayout.Space();
            PrismFanlightEditorStyles.DrawSubGroupLabel("Spatial Mask");
            DrawIntensityMask(state.FindPropertyRelative("_spatialMask"));
        }

        private static void DrawColorSource(SerializedProperty source)
        {
            var mode = source.FindPropertyRelative("_mode");
            EditorGUILayout.PropertyField(mode, new GUIContent("Color Mode"));
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
                    DrawDirection(source.FindPropertyRelative("_direction"));
                    EditorGUILayout.PropertyField(source.FindPropertyRelative("_width"), new GUIContent("Width"));
                    EditorGUILayout.PropertyField(source.FindPropertyRelative("_offset"), new GUIContent("Offset"));
                    break;
                case FanlightColorMode.BlockPalette:
                    DrawPalette(source);
                    EditorGUILayout.PropertyField(
                        source.FindPropertyRelative("_blockPaletteEntries"),
                        new GUIContent("Block Palette Entries"),
                        true);
                    break;
            }

            DrawColorSourceValidation(source, (FanlightColorMode)mode.enumValueIndex);
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
            EditorGUILayout.PropertyField(mode, new GUIContent("Mask Mode"));
            if (mode.hasMultipleDifferentValues) return;

            switch ((FanlightIntensityMaskMode)mode.enumValueIndex)
            {
                case FanlightIntensityMaskMode.LinearWipe:
                    EditorGUILayout.PropertyField(mask.FindPropertyRelative("_origin"), new GUIContent("Origin"));
                    DrawDirection(mask.FindPropertyRelative("_direction"));
                    EditorGUILayout.PropertyField(mask.FindPropertyRelative("_width"), new GUIContent("Width"));
                    var progress = mask.FindPropertyRelative("_progress");
                    EditorGUI.BeginChangeCheck();
                    var progressValue = EditorGUILayout.Slider("Progress", progress.floatValue, 0f, 1f);
                    if (EditorGUI.EndChangeCheck()) progress.floatValue = progressValue;
                    EditorGUILayout.PropertyField(mask.FindPropertyRelative("_softness"), new GUIContent("Softness"));
                    EditorGUILayout.PropertyField(mask.FindPropertyRelative("_invert"), new GUIContent("Invert"));
                    break;
                case FanlightIntensityMaskMode.RadialWipe:
                    EditorGUILayout.PropertyField(mask.FindPropertyRelative("_origin"), new GUIContent("Origin"));
                    EditorGUILayout.PropertyField(mask.FindPropertyRelative("_radius"), new GUIContent("Radius"));
                    EditorGUILayout.PropertyField(mask.FindPropertyRelative("_softness"), new GUIContent("Softness"));
                    EditorGUILayout.PropertyField(mask.FindPropertyRelative("_invert"), new GUIContent("Invert"));
                    break;
            }

            DrawIntensityMaskValidation(mask, (FanlightIntensityMaskMode)mode.enumValueIndex);
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

        private static void DrawColorSourceValidation(
            SerializedProperty source,
            FanlightColorMode mode)
        {
            if (mode != FanlightColorMode.LinearGradient) return;

            var origin = source.FindPropertyRelative("_origin").vector2Value;
            var direction = source.FindPropertyRelative("_direction").vector2Value;
            var width = source.FindPropertyRelative("_width").floatValue;
            var offset = source.FindPropertyRelative("_offset").floatValue;
            if (!IsFinite(origin)
                || !IsFinite(direction)
                || direction.sqrMagnitude <= 0.000001f
                || !float.IsFinite(width)
                || width <= 0f
                || !float.IsFinite(offset))
            {
                EditorGUILayout.HelpBox(
                    "Linear Gradient requires finite Origin and Offset, a non-zero Direction, and Width greater than 0.",
                    MessageType.Error);
            }
        }

        private static void DrawIntensityMaskValidation(
            SerializedProperty mask,
            FanlightIntensityMaskMode mode)
        {
            if (mode == FanlightIntensityMaskMode.None) return;

            var origin = mask.FindPropertyRelative("_origin").vector2Value;
            var softness = mask.FindPropertyRelative("_softness").floatValue;
            var invalid = !IsFinite(origin)
                          || !float.IsFinite(softness)
                          || softness < 0f;

            if (mode == FanlightIntensityMaskMode.LinearWipe)
            {
                var direction = mask.FindPropertyRelative("_direction").vector2Value;
                var width = mask.FindPropertyRelative("_width").floatValue;
                var progress = mask.FindPropertyRelative("_progress").floatValue;
                invalid |= !IsFinite(direction)
                           || direction.sqrMagnitude <= 0.000001f
                           || !float.IsFinite(width)
                           || width <= 0f
                           || !float.IsFinite(progress)
                           || progress < 0f
                           || progress > 1f;
            }
            else
            {
                var radius = mask.FindPropertyRelative("_radius").floatValue;
                invalid |= !float.IsFinite(radius) || radius < 0f;
            }

            if (invalid)
            {
                EditorGUILayout.HelpBox(
                    "Spatial Mask fields are outside the valid range for the selected Mode.",
                    MessageType.Error);
            }
        }

        private static void DrawDirection(SerializedProperty property)
        {
            EditorGUI.BeginChangeCheck();
            var direction = EditorGUILayout.Vector2Field("Direction", property.vector2Value);
            if (!EditorGUI.EndChangeCheck()) return;

            if (IsFinite(direction) && direction.sqrMagnitude > 0.000001f)
            {
                var scale = Mathf.Max(Mathf.Abs(direction.x), Mathf.Abs(direction.y));
                direction = (direction / scale).normalized;
            }

            property.vector2Value = direction;
        }

        private static bool IsFinite(Vector2 value)
        {
            return float.IsFinite(value.x) && float.IsFinite(value.y);
        }

        private static void DrawBlockPaletteValidation(
            SerializedProperty source,
            FanlightLayoutAsset layout,
            bool requireLayout)
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

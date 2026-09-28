using PrismFanlight.Core;
using PrismFanlight.Timeline;
using UnityEditor;
using UnityEngine;

namespace PrismFanlight.Editor
{
    internal static class FanlightStateClipboard
    {
        // Fields

        private static FanlightStateKind? _kind;
        private static object _value;


        // Methods

        internal static bool CanPaste(FanlightStateKind kind) => _kind == kind && _value != null;

        internal static bool Copy(Object[] targets, FanlightStateKind kind, string propertyPath)
        {
            if (targets.Length != 1 || targets[0] == null) return false;

            var source = new SerializedObject(targets[0]);
            source.Update();
            var property = source.FindProperty(propertyPath);
            if (property == null) return false;

            _value = kind == FanlightStateKind.Noise
                ? new NoiseValues(
                    property.FindPropertyRelative("_phaseAmount").floatValue,
                    property.FindPropertyRelative("_positionAmount").floatValue,
                    property.FindPropertyRelative("_directionAmount").floatValue)
                : property.boxedValue;
            _kind = kind;
            return true;
        }

        internal static bool Paste(Object[] targets, FanlightStateKind kind, string propertyPath)
        {
            if (!CanPaste(kind) || targets.Length == 0) return false;

            for (var i = 0; i < targets.Length; i++)
            {
                if (targets[i] == null) return false;
            }

            var destination = new SerializedObject(targets);
            destination.Update();
            var property = destination.FindProperty(propertyPath);
            if (property == null) return false;

            if (kind == FanlightStateKind.Noise)
            {
                var values = (NoiseValues)_value;
                property.FindPropertyRelative("_phaseAmount").floatValue = values.PhaseAmount;
                property.FindPropertyRelative("_positionAmount").floatValue = values.PositionAmount;
                property.FindPropertyRelative("_directionAmount").floatValue = values.DirectionAmount;
            }
            else
            {
                property.boxedValue = _value;
            }

            return destination.ApplyModifiedProperties();
        }

        internal static bool TryGetClipKind(Object[] targets, out FanlightStateKind kind)
        {
            kind = default;
            if (targets == null || targets.Length == 0 || targets[0] == null) return false;

            var type = targets[0].GetType();
            for (var i = 1; i < targets.Length; i++)
            {
                if (targets[i] == null || targets[i].GetType() != type) return false;
            }

            if (type == typeof(FanlightIntentClip)) kind = FanlightStateKind.Intent;
            else if (type == typeof(FanlightMotionClip)) kind = FanlightStateKind.Motion;
            else if (type == typeof(FanlightVariationClip)) kind = FanlightStateKind.Variation;
            else if (type == typeof(FanlightNoiseClip)) kind = FanlightStateKind.Noise;
            else if (type == typeof(FanlightRestClip)) kind = FanlightStateKind.Rest;
            else if (type == typeof(FanlightAudienceBodyClip)) kind = FanlightStateKind.AudienceBody;
            else if (type == typeof(FanlightDirectionClip)) kind = FanlightStateKind.Direction;
            else if (type == typeof(FanlightColorClip)) kind = FanlightStateKind.Color;
            else if (type == typeof(FanlightIntensityClip)) kind = FanlightStateKind.Intensity;
            else return false;
            return true;
        }


        private readonly struct NoiseValues
        {
            // Properties

            internal float PhaseAmount { get; }
            internal float PositionAmount { get; }
            internal float DirectionAmount { get; }


            // Methods

            internal NoiseValues(float phaseAmount, float positionAmount, float directionAmount)
            {
                PhaseAmount = phaseAmount;
                PositionAmount = positionAmount;
                DirectionAmount = directionAmount;
            }
        }
    }
}

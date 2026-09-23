using PrismFanlight.Timeline;
using UnityEditor;
using UnityEngine;

namespace PrismFanlight.Editor
{
    internal static class FanlightStateClipboard
    {
        // Fields

        private static FanlightTimelinePatchKind? _kind;
        private static object _value;


        // Methods

        internal static bool CanPaste(FanlightTimelinePatchKind kind) => _kind == kind && _value != null;

        internal static bool Copy(Object[] targets, FanlightTimelinePatchKind kind, string propertyPath)
        {
            if (targets.Length != 1 || targets[0] == null) return false;

            var source = new SerializedObject(targets[0]);
            source.Update();
            var property = source.FindProperty(propertyPath);
            if (property == null) return false;

            _value = kind == FanlightTimelinePatchKind.Noise
                ? new NoiseValues(
                    property.FindPropertyRelative("_phaseAmount").floatValue,
                    property.FindPropertyRelative("_positionAmount").floatValue,
                    property.FindPropertyRelative("_directionAmount").floatValue)
                : property.boxedValue;
            _kind = kind;
            return true;
        }

        internal static bool Paste(Object[] targets, FanlightTimelinePatchKind kind, string propertyPath)
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

            if (kind == FanlightTimelinePatchKind.Noise)
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

        internal static bool TryGetClipKind(Object[] targets, out FanlightTimelinePatchKind kind)
        {
            kind = default;
            if (targets == null || targets.Length == 0 || targets[0] == null) return false;

            var type = targets[0].GetType();
            for (var i = 1; i < targets.Length; i++)
            {
                if (targets[i] == null || targets[i].GetType() != type) return false;
            }

            if (type == typeof(FanlightIntentClip)) kind = FanlightTimelinePatchKind.Intent;
            else if (type == typeof(FanlightMotionClip)) kind = FanlightTimelinePatchKind.Motion;
            else if (type == typeof(FanlightVariationClip)) kind = FanlightTimelinePatchKind.Variation;
            else if (type == typeof(FanlightNoiseClip)) kind = FanlightTimelinePatchKind.Noise;
            else if (type == typeof(FanlightRestClip)) kind = FanlightTimelinePatchKind.Rest;
            else if (type == typeof(FanlightAudienceBodyClip)) kind = FanlightTimelinePatchKind.AudienceBody;
            else if (type == typeof(FanlightDirectionClip)) kind = FanlightTimelinePatchKind.Direction;
            else if (type == typeof(FanlightColorClip)) kind = FanlightTimelinePatchKind.Color;
            else if (type == typeof(FanlightIntensityClip)) kind = FanlightTimelinePatchKind.Intensity;
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

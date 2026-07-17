using System;

namespace PrismFanlight.Core
{
    public enum FanlightExpertUnit
    {
        Unitless = 0,
        Normalized = 1,
        Beats = 2,
        Seconds = 3,
        Radians = 4,
        Meters = 5,
        Multiplier = 6
    }

    [Flags]
    public enum FanlightExpertBlendModeMask
    {
        None = 0,
        Replace = 1 << 0,
        Add = 1 << 1,
        Multiply = 1 << 2,
        All = Replace | Add | Multiply
    }

    public readonly struct FanlightExpertParameterDefinition
    {
        public FanlightExpertParameterDefinition(
            FanlightExpertParameterId parameterId,
            FanlightExpertValueKind valueKind,
            FanlightExpertUnit unit,
            double minimum,
            double maximum,
            double defaultValue,
            FanlightExpertBlendModeMask allowedBlendModes)
        {
            ParameterId = parameterId;
            ValueKind = valueKind;
            Unit = unit;
            Minimum = minimum;
            Maximum = maximum;
            DefaultValue = defaultValue;
            AllowedBlendModes = allowedBlendModes;
        }

        public FanlightExpertParameterId ParameterId { get; }
        public FanlightExpertValueKind ValueKind { get; }
        public FanlightExpertUnit Unit { get; }
        public double Minimum { get; }
        public double Maximum { get; }
        public double DefaultValue { get; }
        public FanlightExpertBlendModeMask AllowedBlendModes { get; }

        public FanlightExpertParameterValue DefaultParameterValue => ValueKind == FanlightExpertValueKind.Integer
            ? FanlightExpertParameterValue.Integer(ParameterId, (int)DefaultValue)
            : FanlightExpertParameterValue.Float(ParameterId, (float)DefaultValue);

        public float Clamp(float value) => (float)Math.Max(Minimum, Math.Min(Maximum, value));
        public int Clamp(int value) => (int)Math.Max(Minimum, Math.Min(Maximum, value));
    }

    public static class FanlightExpertSchema
    {
        public const int Version = 1;
        private static readonly FanlightExpertParameterId[] OrderedIds =
        {
            FanlightExpertParameterId.GestureBeatsPerCycle,
            FanlightExpertParameterId.GesturePhaseOffset,
            FanlightExpertParameterId.GestureAttackRatio,
            FanlightExpertParameterId.GestureHoldRatio,
            FanlightExpertParameterId.GestureReturnRatio,
            FanlightExpertParameterId.GestureCrispness,
            FanlightExpertParameterId.GestureFollowThrough,
            FanlightExpertParameterId.GestureDownbeatAccent,
            FanlightExpertParameterId.PoseArmLengthMinimum,
            FanlightExpertParameterId.PoseArmLengthMaximum,
            FanlightExpertParameterId.PoseAngleMinimumRadians,
            FanlightExpertParameterId.PoseAngleMaximumRadians,
            FanlightExpertParameterId.PoseHorizontalRatio,
            FanlightExpertParameterId.PoseWristFrequencyMultiplier,
            FanlightExpertParameterId.PoseWristAngleRadians,
            FanlightExpertParameterId.PoseBodyLean,
            FanlightExpertParameterId.PoseBodyBounce,
            FanlightExpertParameterId.PoseBodySway,
            FanlightExpertParameterId.PoseBodyMotionSpeed,
            FanlightExpertParameterId.PoseUpperBodyLeanMotion,
            FanlightExpertParameterId.VariationSeatPosition,
            FanlightExpertParameterId.VariationBodyHeight,
            FanlightExpertParameterId.VariationArmLength,
            FanlightExpertParameterId.VariationAngle,
            FanlightExpertParameterId.VariationDirectionSpread,
            FanlightExpertParameterId.VariationReactionDelaySeconds,
            FanlightExpertParameterId.VariationBeatJitter,
            FanlightExpertParameterId.VariationBlockDelayXBeats,
            FanlightExpertParameterId.VariationBlockDelayYBeats,
            FanlightExpertParameterId.VariationEnergyResponse,
            FanlightExpertParameterId.VariationSpeed,
            FanlightExpertParameterId.VariationBeatReactionDelaySeconds,
            FanlightExpertParameterId.VariationHandZone,
            FanlightExpertParameterId.NoisePhaseAmount,
            FanlightExpertParameterId.NoisePhaseSpeed,
            FanlightExpertParameterId.NoiseAxisAmount,
            FanlightExpertParameterId.NoiseAxisSpeed,
            FanlightExpertParameterId.NoiseOctaves,
            FanlightExpertParameterId.NoisePersistence,
            FanlightExpertParameterId.RestProbability,
            FanlightExpertParameterId.RestMotionLevel,
            FanlightExpertParameterId.RestCycleSeconds,
            FanlightExpertParameterId.RestDurationSeconds,
            FanlightExpertParameterId.RestFadeSeconds,
            FanlightExpertParameterId.RestPhaseRandomness,
            FanlightExpertParameterId.BodyHeight,
            FanlightExpertParameterId.BodyHeightVariation,
            FanlightExpertParameterId.BodyWidth,
            FanlightExpertParameterId.BodyHeadSize,
            FanlightExpertParameterId.BodyShoulderHeightRatio,
            FanlightExpertParameterId.BodyShoulderSideOffset,
            FanlightExpertParameterId.BodyArmWidth,
            FanlightExpertParameterId.BodyArmLengthLimit,
            FanlightExpertParameterId.BodyUpperBodyLeanMaximum,
            FanlightExpertParameterId.BodyUpperBodyLean
        };

        public static ReadOnlySpan<FanlightExpertParameterId> ParameterIds => OrderedIds;

        public static FanlightExpertParameterDefinition Get(FanlightExpertParameterId id)
        {
            if (!TryGet(id, out var definition))
                throw new ArgumentOutOfRangeException(nameof(id), id, "Unknown Expert parameter ID.");
            return definition;
        }

        public static bool TryGet(FanlightExpertParameterId id, out FanlightExpertParameterDefinition definition)
        {
            definition = id switch
            {
                FanlightExpertParameterId.GestureBeatsPerCycle => F(id, FanlightExpertUnit.Beats, 0.001, 64, 1),
                FanlightExpertParameterId.GesturePhaseOffset => F(id, FanlightExpertUnit.Beats, -64, 64, 0),
                FanlightExpertParameterId.GestureAttackRatio => F(id, FanlightExpertUnit.Normalized, 0, 1, 0.25),
                FanlightExpertParameterId.GestureHoldRatio => F(id, FanlightExpertUnit.Normalized, 0, 1, 0),
                FanlightExpertParameterId.GestureReturnRatio => F(id, FanlightExpertUnit.Normalized, 0, 1, 0.75),
                FanlightExpertParameterId.GestureCrispness => F(id, FanlightExpertUnit.Normalized, 0, 1, 1),
                FanlightExpertParameterId.GestureFollowThrough => F(id, FanlightExpertUnit.Normalized, 0, 1, 0),
                FanlightExpertParameterId.GestureDownbeatAccent => F(id, FanlightExpertUnit.Multiplier, 0, 4, 0),

                FanlightExpertParameterId.PoseArmLengthMinimum => F(id, FanlightExpertUnit.Meters, 0, 5, 0.2),
                FanlightExpertParameterId.PoseArmLengthMaximum => F(id, FanlightExpertUnit.Meters, 0, 5, 0.4),
                FanlightExpertParameterId.PoseAngleMinimumRadians => F(id, FanlightExpertUnit.Radians, 0, Math.PI * 2, 0.3),
                FanlightExpertParameterId.PoseAngleMaximumRadians => F(id, FanlightExpertUnit.Radians, 0, Math.PI * 2, 1),
                FanlightExpertParameterId.PoseHorizontalRatio => F(id, FanlightExpertUnit.Normalized, 0, 1, 0),
                FanlightExpertParameterId.PoseWristFrequencyMultiplier => F(id, FanlightExpertUnit.Multiplier, 1, 64, 4),
                FanlightExpertParameterId.PoseWristAngleRadians => F(id, FanlightExpertUnit.Radians, 0, Math.PI, 0.3),
                FanlightExpertParameterId.PoseBodyLean => F(id, FanlightExpertUnit.Normalized, -1, 1, 0),
                FanlightExpertParameterId.PoseBodyBounce => F(id, FanlightExpertUnit.Meters, 0, 1, 0.018),
                FanlightExpertParameterId.PoseBodySway => F(id, FanlightExpertUnit.Meters, 0, 1, 0.025),
                FanlightExpertParameterId.PoseBodyMotionSpeed => F(id, FanlightExpertUnit.Multiplier, 0.01, 16, 0.65),
                FanlightExpertParameterId.PoseUpperBodyLeanMotion => F(id, FanlightExpertUnit.Normalized, 0, 1, 0.2),

                FanlightExpertParameterId.VariationSeatPosition => F(id, FanlightExpertUnit.Normalized, 0, 1, 0.3),
                FanlightExpertParameterId.VariationBodyHeight => F(id, FanlightExpertUnit.Normalized, 0, 1, 0.2),
                FanlightExpertParameterId.VariationArmLength => F(id, FanlightExpertUnit.Normalized, 0, 1, 0.25),
                FanlightExpertParameterId.VariationAngle => F(id, FanlightExpertUnit.Normalized, 0, 1, 0),
                FanlightExpertParameterId.VariationDirectionSpread => F(id, FanlightExpertUnit.Normalized, 0, 1, 0.3),
                FanlightExpertParameterId.VariationReactionDelaySeconds => F(id, FanlightExpertUnit.Seconds, 0, 10, 0),
                FanlightExpertParameterId.VariationBeatJitter => F(id, FanlightExpertUnit.Beats, 0, 8, 0),
                FanlightExpertParameterId.VariationBlockDelayXBeats => F(id, FanlightExpertUnit.Beats, -64, 64, 0),
                FanlightExpertParameterId.VariationBlockDelayYBeats => F(id, FanlightExpertUnit.Beats, -64, 64, 0),
                FanlightExpertParameterId.VariationEnergyResponse => F(id, FanlightExpertUnit.Normalized, 0, 1, 0.15),
                FanlightExpertParameterId.VariationSpeed => F(id, FanlightExpertUnit.Multiplier, 0, 4, 0),
                FanlightExpertParameterId.VariationBeatReactionDelaySeconds => F(id, FanlightExpertUnit.Seconds, 0, 10, 0),
                FanlightExpertParameterId.VariationHandZone => F(id, FanlightExpertUnit.Meters, 0, 0.5, 0),

                FanlightExpertParameterId.NoisePhaseAmount => F(id, FanlightExpertUnit.Multiplier, 0, 4, 1),
                FanlightExpertParameterId.NoisePhaseSpeed => F(id, FanlightExpertUnit.Multiplier, 0, 16, 0.27),
                FanlightExpertParameterId.NoiseAxisAmount => F(id, FanlightExpertUnit.Multiplier, 0, 4, 1),
                FanlightExpertParameterId.NoiseAxisSpeed => F(id, FanlightExpertUnit.Multiplier, 0, 16, 0.23),
                FanlightExpertParameterId.NoiseOctaves => I(id, 1, 4, 2),
                FanlightExpertParameterId.NoisePersistence => F(id, FanlightExpertUnit.Normalized, 0, 1, 0.5),

                FanlightExpertParameterId.RestProbability => F(id, FanlightExpertUnit.Normalized, 0, 1, 0),
                FanlightExpertParameterId.RestMotionLevel => F(id, FanlightExpertUnit.Normalized, 0, 1, 0.1),
                FanlightExpertParameterId.RestCycleSeconds => F(id, FanlightExpertUnit.Seconds, 0, 3600, 0),
                FanlightExpertParameterId.RestDurationSeconds => F(id, FanlightExpertUnit.Seconds, 0, 3600, 0),
                FanlightExpertParameterId.RestFadeSeconds => F(id, FanlightExpertUnit.Seconds, 0, 60, 0.5),
                FanlightExpertParameterId.RestPhaseRandomness => F(id, FanlightExpertUnit.Normalized, 0, 1, 1),

                FanlightExpertParameterId.BodyHeight => F(id, FanlightExpertUnit.Meters, 0.1, 3, 1.5),
                FanlightExpertParameterId.BodyHeightVariation => F(id, FanlightExpertUnit.Normalized, 0, 1, 0.08),
                FanlightExpertParameterId.BodyWidth => F(id, FanlightExpertUnit.Meters, 0.01, 3, 0.55),
                FanlightExpertParameterId.BodyHeadSize => F(id, FanlightExpertUnit.Meters, 0.01, 1, 0.28),
                FanlightExpertParameterId.BodyShoulderHeightRatio => F(id, FanlightExpertUnit.Normalized, 0, 1, 0.82),
                FanlightExpertParameterId.BodyShoulderSideOffset => F(id, FanlightExpertUnit.Meters, -1, 1, 0.16),
                FanlightExpertParameterId.BodyArmWidth => F(id, FanlightExpertUnit.Meters, 0.01, 1, 0.14),
                FanlightExpertParameterId.BodyArmLengthLimit => F(id, FanlightExpertUnit.Meters, 0.01, 3, 0.55),
                FanlightExpertParameterId.BodyUpperBodyLeanMaximum => F(id, FanlightExpertUnit.Radians, 0, Math.PI / 2, 0.4),
                FanlightExpertParameterId.BodyUpperBodyLean => F(id, FanlightExpertUnit.Normalized, 0, 1, 0.5),
                _ => default
            };
            return definition.AllowedBlendModes != FanlightExpertBlendModeMask.None;
        }

        public static void ValidateInput(FanlightExpertParameterValue value)
        {
            var definition = Get(value.ParameterId);
            if (definition.ValueKind != value.ValueKind)
                throw new ArgumentException($"Expert parameter {value.ParameterId} requires {definition.ValueKind}.", nameof(value));
            if (!IsBlendAllowed(definition, value.BlendMode))
                throw new ArgumentException($"Blend mode {value.BlendMode} is not allowed for {value.ParameterId}.", nameof(value));
            if (value.ValueKind == FanlightExpertValueKind.Float && !IsFinite(value.FloatValue))
                throw new ArgumentException($"Expert parameter {value.ParameterId} must be finite.", nameof(value));
            if (!IsFinite(value.Weight))
                throw new ArgumentException($"Expert parameter {value.ParameterId} weight must be finite.", nameof(value));
        }

        public static FanlightExpertParameterValue NormalizeResolved(FanlightExpertParameterValue value)
        {
            ValidateInput(value);
            var definition = Get(value.ParameterId);
            return value.ValueKind == FanlightExpertValueKind.Integer
                ? FanlightExpertParameterValue.Integer(value.ParameterId, definition.Clamp(value.IntegerValue))
                : FanlightExpertParameterValue.Float(value.ParameterId, definition.Clamp(value.FloatValue));
        }

        private static FanlightExpertParameterDefinition F(
            FanlightExpertParameterId id,
            FanlightExpertUnit unit,
            double minimum,
            double maximum,
            double defaultValue) =>
            new(id, FanlightExpertValueKind.Float, unit, minimum, maximum, defaultValue, FanlightExpertBlendModeMask.All);

        private static FanlightExpertParameterDefinition I(
            FanlightExpertParameterId id,
            int minimum,
            int maximum,
            int defaultValue) =>
            new(id, FanlightExpertValueKind.Integer, FanlightExpertUnit.Unitless, minimum, maximum, defaultValue,
                FanlightExpertBlendModeMask.Replace);

        private static bool IsBlendAllowed(FanlightExpertParameterDefinition definition, FanlightExpertBlendMode mode)
        {
            var mask = mode switch
            {
                FanlightExpertBlendMode.Replace => FanlightExpertBlendModeMask.Replace,
                FanlightExpertBlendMode.Add => FanlightExpertBlendModeMask.Add,
                FanlightExpertBlendMode.Multiply => FanlightExpertBlendModeMask.Multiply,
                _ => FanlightExpertBlendModeMask.None
            };
            return (definition.AllowedBlendModes & mask) != 0;
        }

        private static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    }
}

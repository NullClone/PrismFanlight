using System;
using System.Collections.Generic;
using UnityEngine;

namespace PrismFanlight.Core
{
    public enum FanlightHandZoneId
    {
        Chest = 0,
        Shoulder = 1,
        Face = 2,
        Overhead = 3,
        High = 4,
        Custom = 5
    }

    public enum FanlightDirectionMode
    {
        WorldDirection = 0,
        TargetPoint = 1,
        TargetTransform = 2
    }

    public readonly struct FanlightHandZoneIntent
    {
        public FanlightHandZoneId Zone { get; }
        public float HeightOffset { get; }
        public float ForwardOffset { get; }
        public float SideOffset { get; }

        public FanlightHandZoneIntent(FanlightHandZoneId zone, float heightOffset, float forwardOffset, float sideOffset)
        {
            Zone = zone;
            HeightOffset = heightOffset;
            ForwardOffset = forwardOffset;
            SideOffset = sideOffset;
        }
    }

    public readonly struct FanlightDirectionIntent
    {
        public FanlightDirectionMode Mode { get; }
        public float WorldYawDegrees { get; }
        public float WorldPitchDegrees { get; }
        public Vector3 TargetWorldPosition { get; }
        public string TargetBindingId { get; }
        public float AimStrength { get; }
        public float FallbackWorldYawDegrees { get; }
        public float FallbackWorldPitchDegrees { get; }

        public FanlightDirectionIntent(
            FanlightDirectionMode mode,
            float worldYawDegrees,
            float worldPitchDegrees,
            Vector3 targetWorldPosition,
            string targetBindingId,
            float aimStrength,
            float fallbackWorldYawDegrees,
            float fallbackWorldPitchDegrees)
        {
            Mode = mode;
            WorldYawDegrees = worldYawDegrees;
            WorldPitchDegrees = worldPitchDegrees;
            TargetWorldPosition = targetWorldPosition;
            TargetBindingId = targetBindingId ?? string.Empty;
            AimStrength = aimStrength;
            FallbackWorldYawDegrees = fallbackWorldYawDegrees;
            FallbackWorldPitchDegrees = fallbackWorldPitchDegrees;
        }
    }

    public readonly struct FanlightPaletteIntent
    {
        public Color Slot1 { get; }
        public Color Slot2 { get; }
        public Color Slot3 { get; }
        public Color Slot4 { get; }
        public Color Slot5 { get; }
        public Color Slot6 { get; }
        public float GlobalIntensity { get; }
        public float RandomIntensity { get; }

        public FanlightPaletteIntent(
            Color slot1,
            Color slot2,
            Color slot3,
            Color slot4,
            Color slot5,
            Color slot6,
            float globalIntensity,
            float randomIntensity)
        {
            Slot1 = slot1;
            Slot2 = slot2;
            Slot3 = slot3;
            Slot4 = slot4;
            Slot5 = slot5;
            Slot6 = slot6;
            GlobalIntensity = globalIntensity;
            RandomIntensity = randomIntensity;
        }
    }

    [Flags]
    public enum FanlightPaletteFieldMask
    {
        None = 0,
        Slot1 = 1 << 0,
        Slot2 = 1 << 1,
        Slot3 = 1 << 2,
        Slot4 = 1 << 3,
        Slot5 = 1 << 4,
        Slot6 = 1 << 5,
        GlobalIntensity = 1 << 6,
        RandomIntensity = 1 << 7,
        All = Slot1 | Slot2 | Slot3 | Slot4 | Slot5 | Slot6 | GlobalIntensity | RandomIntensity
    }

    public readonly struct FanlightPalettePatch
    {
        public FanlightPaletteIntent Value { get; }
        public FanlightPaletteFieldMask Fields { get; }

        public FanlightPalettePatch(FanlightPaletteIntent value, FanlightPaletteFieldMask fields)
        {
            Value = value;
            Fields = fields;
        }
    }

    public enum FanlightExpertParameterId
    {
        GestureBeatsPerCycle = 100,
        GesturePhaseOffset = 101,
        GestureAttackRatio = 102,
        GestureHoldRatio = 103,
        GestureReturnRatio = 104,
        GestureCrispness = 105,
        GestureFollowThrough = 106,
        GestureDownbeatAccent = 107,
        PoseArmLengthMinimum = 200,
        PoseArmLengthMaximum = 201,
        PoseAngleMinimumRadians = 202,
        PoseAngleMaximumRadians = 203,
        PoseHorizontalRatio = 204,
        PoseWristFrequencyMultiplier = 205,
        PoseWristAngleRadians = 206,
        PoseBodyLean = 207,
        PoseBodyBounce = 208,
        PoseBodySway = 209,
        PoseBodyMotionSpeed = 210,
        PoseUpperBodyLeanMotion = 211,
        VariationSeatPosition = 300,
        VariationBodyHeight = 301,
        VariationArmLength = 302,
        VariationAngle = 303,
        VariationDirectionSpread = 304,
        VariationReactionDelaySeconds = 305,
        VariationBeatJitter = 306,
        VariationBlockDelayXBeats = 307,
        VariationBlockDelayYBeats = 308,
        VariationEnergyResponse = 309,
        VariationSpeed = 310,
        VariationBeatReactionDelaySeconds = 311,
        VariationHandZone = 312,
        NoisePhaseAmount = 400,
        NoisePhaseSpeed = 401,
        NoiseAxisAmount = 402,
        NoiseAxisSpeed = 403,
        NoiseOctaves = 404,
        NoisePersistence = 405,
        RestProbability = 500,
        RestMotionLevel = 501,
        RestCycleSeconds = 502,
        RestDurationSeconds = 503,
        RestFadeSeconds = 504,
        RestPhaseRandomness = 505,
        BodyHeight = 600,
        BodyHeightVariation = 601,
        BodyWidth = 602,
        BodyHeadSize = 603,
        BodyShoulderHeightRatio = 604,
        BodyShoulderSideOffset = 605,
        BodyArmWidth = 606,
        BodyArmLengthLimit = 607,
        BodyUpperBodyLeanMaximum = 608,
        BodyUpperBodyLean = 609
    }

    public enum FanlightExpertValueKind
    {
        Float = 0,
        Integer = 1
    }

    public enum FanlightExpertBlendMode
    {
        Replace = 0,
        Add = 1,
        Multiply = 2
    }

    public readonly struct FanlightExpertParameterValue
    {
        public FanlightExpertParameterId ParameterId { get; }
        public FanlightExpertValueKind ValueKind { get; }
        public float FloatValue { get; }
        public int IntegerValue { get; }
        public FanlightExpertBlendMode BlendMode { get; }
        public float Weight { get; }

        public FanlightExpertParameterValue(
            FanlightExpertParameterId parameterId,
            FanlightExpertValueKind valueKind,
            float floatValue,
            int integerValue,
            FanlightExpertBlendMode blendMode,
            float weight)
        {
            ParameterId = parameterId;
            ValueKind = valueKind;
            FloatValue = valueKind == FanlightExpertValueKind.Float ? floatValue : 0f;
            IntegerValue = valueKind == FanlightExpertValueKind.Integer ? integerValue : 0;
            BlendMode = blendMode;
            Weight = Clamp01(weight);
        }

        public static FanlightExpertParameterValue Float(FanlightExpertParameterId id, float value) =>
            new(id, FanlightExpertValueKind.Float, value, 0, FanlightExpertBlendMode.Replace, 1f);

        public static FanlightExpertParameterValue Integer(FanlightExpertParameterId id, int value) =>
            new(id, FanlightExpertValueKind.Integer, 0f, value, FanlightExpertBlendMode.Replace, 1f);

        private static float Clamp01(float value) => value < 0f ? 0f : value > 1f ? 1f : value;
    }

    public readonly struct FanlightExpertPatch
    {
        public ReadOnlyMemory<FanlightExpertParameterValue> Values { get; }

        public FanlightExpertPatch(ReadOnlyMemory<FanlightExpertParameterValue> values)
        {
            Values = values;
        }

        public static FanlightExpertPatch Empty => new(ReadOnlyMemory<FanlightExpertParameterValue>.Empty);
    }

    public readonly struct FanlightResolvedIntent
    {
        public string GestureId { get; }
        public FanlightHandZoneIntent HandZone { get; }
        public float Energy { get; }
        public float Participation { get; }
        public float Synchronization { get; }
        public float Realism { get; }
        public float Reach { get; }
        public FanlightDirectionIntent Direction { get; }
        public FanlightPaletteIntent Palette { get; }
        public bool PenlightsEnabled { get; }
        public bool AudienceBodiesEnabled { get; }
        public FanlightExpertPatch Expert { get; }

        public FanlightResolvedIntent(
            string gestureId,
            FanlightHandZoneIntent handZone,
            float energy,
            float participation,
            float synchronization,
            float realism,
            float reach,
            FanlightDirectionIntent direction,
            FanlightPaletteIntent palette,
            bool penlightsEnabled,
            bool audienceBodiesEnabled,
            FanlightExpertPatch expert)
        {
            GestureId = gestureId ?? string.Empty;
            HandZone = handZone;
            Energy = Clamp01(energy);
            Participation = Clamp01(participation);
            Synchronization = Clamp01(synchronization);
            Realism = Clamp01(realism);
            Reach = Clamp01(reach);
            Direction = direction;
            Palette = palette;
            PenlightsEnabled = penlightsEnabled;
            AudienceBodiesEnabled = audienceBodiesEnabled;
            Expert = expert;
        }

        private static float Clamp01(float value) => value < 0f ? 0f : value > 1f ? 1f : value;
    }

    public readonly struct FanlightIntentPatch
    {
        public string GestureId { get; }
        public bool HasGestureId { get; }
        public FanlightHandZoneIntent HandZone { get; }
        public bool HasHandZone { get; }
        public float Energy { get; }
        public bool HasEnergy { get; }
        public float Participation { get; }
        public bool HasParticipation { get; }
        public float Synchronization { get; }
        public bool HasSynchronization { get; }
        public float Realism { get; }
        public bool HasRealism { get; }
        public float Reach { get; }
        public bool HasReach { get; }
        public FanlightDirectionIntent Direction { get; }
        public bool HasDirection { get; }
        public FanlightPalettePatch Palette { get; }
        public bool HasPalette { get; }
        public bool PenlightsEnabled { get; }
        public bool HasPenlightsEnabled { get; }
        public bool AudienceBodiesEnabled { get; }
        public bool HasAudienceBodiesEnabled { get; }
        public FanlightExpertPatch Expert { get; }
        public bool HasExpert { get; }

        public FanlightIntentPatch(
            string gestureId,
            bool hasGestureId,
            FanlightHandZoneIntent handZone,
            bool hasHandZone,
            float energy,
            bool hasEnergy,
            float participation,
            bool hasParticipation,
            float synchronization,
            bool hasSynchronization,
            float realism,
            bool hasRealism,
            float reach,
            bool hasReach,
            FanlightDirectionIntent direction,
            bool hasDirection,
            FanlightPalettePatch palette,
            bool hasPalette,
            bool penlightsEnabled,
            bool hasPenlightsEnabled,
            bool audienceBodiesEnabled,
            bool hasAudienceBodiesEnabled,
            FanlightExpertPatch expert,
            bool hasExpert)
        {
            GestureId = gestureId ?? string.Empty;
            HasGestureId = hasGestureId;
            HandZone = handZone;
            HasHandZone = hasHandZone;
            Energy = energy;
            HasEnergy = hasEnergy;
            Participation = participation;
            HasParticipation = hasParticipation;
            Synchronization = synchronization;
            HasSynchronization = hasSynchronization;
            Realism = realism;
            HasRealism = hasRealism;
            Reach = reach;
            HasReach = hasReach;
            Direction = direction;
            HasDirection = hasDirection;
            Palette = palette;
            HasPalette = hasPalette;
            PenlightsEnabled = penlightsEnabled;
            HasPenlightsEnabled = hasPenlightsEnabled;
            AudienceBodiesEnabled = audienceBodiesEnabled;
            HasAudienceBodiesEnabled = hasAudienceBodiesEnabled;
            Expert = expert;
            HasExpert = hasExpert;
        }

        public static FanlightIntentPatch Empty => default;
    }

    public sealed class FanlightIntentPatchBuilder
    {
        private readonly List<FanlightExpertParameterValue> _expert = new();
        private string _gestureId;
        private bool _hasGestureId;
        private FanlightHandZoneIntent _handZone;
        private bool _hasHandZone;
        private float _energy;
        private bool _hasEnergy;
        private float _participation;
        private bool _hasParticipation;
        private float _synchronization;
        private bool _hasSynchronization;
        private float _realism;
        private bool _hasRealism;
        private float _reach;
        private bool _hasReach;
        private FanlightDirectionIntent _direction;
        private bool _hasDirection;
        private FanlightPalettePatch _palette;
        private bool _hasPalette;
        private bool _penlightsEnabled;
        private bool _hasPenlightsEnabled;
        private bool _audienceBodiesEnabled;
        private bool _hasAudienceBodiesEnabled;

        public FanlightIntentPatchBuilder SetGesture(string value)
        {
            _gestureId = value;
            _hasGestureId = true;
            return this;
        }

        public FanlightIntentPatchBuilder SetHandZone(FanlightHandZoneIntent value)
        {
            _handZone = value;
            _hasHandZone = true;
            return this;
        }

        public FanlightIntentPatchBuilder SetEnergy(float value)
        {
            _energy = value;
            _hasEnergy = true;
            return this;
        }

        public FanlightIntentPatchBuilder SetParticipation(float value)
        {
            _participation = value;
            _hasParticipation = true;
            return this;
        }

        public FanlightIntentPatchBuilder SetSynchronization(float value)
        {
            _synchronization = value;
            _hasSynchronization = true;
            return this;
        }

        public FanlightIntentPatchBuilder SetRealism(float value)
        {
            _realism = value;
            _hasRealism = true;
            return this;
        }

        public FanlightIntentPatchBuilder SetReach(float value)
        {
            _reach = value;
            _hasReach = true;
            return this;
        }

        public FanlightIntentPatchBuilder SetDirection(FanlightDirectionIntent value)
        {
            _direction = value;
            _hasDirection = true;
            return this;
        }

        public FanlightIntentPatchBuilder SetPalette(FanlightPalettePatch value)
        {
            _palette = value;
            _hasPalette = true;
            return this;
        }

        public FanlightIntentPatchBuilder MergePalette(FanlightPalettePatch value)
        {
            if (!_hasPalette) return SetPalette(value);
            var current = _palette.Value;
            var incoming = value.Value;
            var mask = value.Fields;
            var merged = new FanlightPaletteIntent(
                (mask & FanlightPaletteFieldMask.Slot1) != 0 ? incoming.Slot1 : current.Slot1,
                (mask & FanlightPaletteFieldMask.Slot2) != 0 ? incoming.Slot2 : current.Slot2,
                (mask & FanlightPaletteFieldMask.Slot3) != 0 ? incoming.Slot3 : current.Slot3,
                (mask & FanlightPaletteFieldMask.Slot4) != 0 ? incoming.Slot4 : current.Slot4,
                (mask & FanlightPaletteFieldMask.Slot5) != 0 ? incoming.Slot5 : current.Slot5,
                (mask & FanlightPaletteFieldMask.Slot6) != 0 ? incoming.Slot6 : current.Slot6,
                (mask & FanlightPaletteFieldMask.GlobalIntensity) != 0 ? incoming.GlobalIntensity : current.GlobalIntensity,
                (mask & FanlightPaletteFieldMask.RandomIntensity) != 0 ? incoming.RandomIntensity : current.RandomIntensity);
            _palette = new FanlightPalettePatch(merged, _palette.Fields | mask);
            return this;
        }

        public FanlightIntentPatchBuilder SetPenlightsEnabled(bool value)
        {
            _penlightsEnabled = value;
            _hasPenlightsEnabled = true;
            return this;
        }

        public FanlightIntentPatchBuilder SetAudienceBodiesEnabled(bool value)
        {
            _audienceBodiesEnabled = value;
            _hasAudienceBodiesEnabled = true;
            return this;
        }

        public FanlightIntentPatchBuilder SetExpert(FanlightExpertParameterValue value)
        {
            FanlightExpertSchema.ValidateInput(value);
            for (var i = 0; i < _expert.Count; i++)
            {
                if (_expert[i].ParameterId != value.ParameterId) continue;
                _expert[i] = value;
                return this;
            }

            _expert.Add(value);
            return this;
        }

        public FanlightIntentPatch Build()
        {
            _expert.Sort((left, right) => ((int)left.ParameterId).CompareTo((int)right.ParameterId));
            var expert = _expert.Count == 0 ? FanlightExpertPatch.Empty : new FanlightExpertPatch(_expert.ToArray());
            return new FanlightIntentPatch(
                _gestureId, _hasGestureId,
                _handZone, _hasHandZone,
                _energy, _hasEnergy,
                _participation, _hasParticipation,
                _synchronization, _hasSynchronization,
                _realism, _hasRealism,
                _reach, _hasReach,
                _direction, _hasDirection,
                _palette, _hasPalette,
                _penlightsEnabled, _hasPenlightsEnabled,
                _audienceBodiesEnabled, _hasAudienceBodiesEnabled,
                expert, _expert.Count > 0);
        }
    }
}

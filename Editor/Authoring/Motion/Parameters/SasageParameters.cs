using System;
using UnityEngine;

namespace PrismFanlight.Editor
{
    [Serializable]
    internal sealed class SasageParameters
    {
        // Fields

        [SerializeField, Range(0.1f, 0.55f)]
        private float _topHoldRatio = 0.4f;

        [SerializeField, Range(-0.1f, 0.1f)]
        private float _bodyPhaseLag = 0.03f;

        [SerializeField, Range(0f, 0.25f)]
        private float _wristPhaseLag = 0.05f;

        [SerializeField, Range(-10f, 45f)]
        private float _lowElevation = 18f;

        [SerializeField, Range(30f, 90f)]
        private float _highElevation = 80f;

        [SerializeField, Range(0.4f, 0.9f)]
        private float _lowExtension = 0.72f;

        [SerializeField, Range(0.7f, 1f)]
        private float _highExtension = 0.97f;

        [SerializeField, Range(-15f, 15f)]
        private float _baseSideAngle;

        [SerializeField, Range(0f, 15f)]
        private float _sideSwayAmplitude = 2.5f;

        [SerializeField, Range(0f, 90f)]
        private float _penlightLowElevation = 46f;

        [SerializeField, Range(-20f, 40f)]
        private float _penlightRiseArc = 32f;

        [SerializeField, Range(0f, 20f)]
        private float _penlightSideAmplitude = 3f;

        [SerializeField, Range(0f, 0.05f)]
        private float _bodyRiseLift = 0.02f;

        [SerializeField, Range(-10f, 20f)]
        private float _baseBodyLean = -1f;

        [SerializeField, Range(0f, 15f)]
        private float _bodyLeanAmplitude = 4f;

        // Properties

        internal float TopHoldRatio
        {
            get => _topHoldRatio;
            set => _topHoldRatio = value;
        }

        internal float BodyPhaseLag
        {
            get => _bodyPhaseLag;
            set => _bodyPhaseLag = value;
        }

        internal float WristPhaseLag
        {
            get => _wristPhaseLag;
            set => _wristPhaseLag = value;
        }

        internal float LowElevation
        {
            get => _lowElevation;
            set => _lowElevation = value;
        }

        internal float HighElevation
        {
            get => _highElevation;
            set => _highElevation = value;
        }

        internal float LowExtension
        {
            get => _lowExtension;
            set => _lowExtension = value;
        }

        internal float HighExtension
        {
            get => _highExtension;
            set => _highExtension = value;
        }

        internal float BaseSideAngle
        {
            get => _baseSideAngle;
            set => _baseSideAngle = value;
        }

        internal float SideSwayAmplitude
        {
            get => _sideSwayAmplitude;
            set => _sideSwayAmplitude = value;
        }

        internal float PenlightLowElevation
        {
            get => _penlightLowElevation;
            set => _penlightLowElevation = value;
        }

        internal float PenlightRiseArc
        {
            get => _penlightRiseArc;
            set => _penlightRiseArc = value;
        }

        internal float PenlightSideAmplitude
        {
            get => _penlightSideAmplitude;
            set => _penlightSideAmplitude = value;
        }

        internal float BodyRiseLift
        {
            get => _bodyRiseLift;
            set => _bodyRiseLift = value;
        }

        internal float BaseBodyLean
        {
            get => _baseBodyLean;
            set => _baseBodyLean = value;
        }

        internal float BodyLeanAmplitude
        {
            get => _bodyLeanAmplitude;
            set => _bodyLeanAmplitude = value;
        }

        // Methods

        internal static SasageParameters CreateDefault() => new();

        internal void Validate()
        {
            RequireRange(_topHoldRatio, 0.1f, 0.55f, nameof(TopHoldRatio));
            RequireRange(_bodyPhaseLag, -0.1f, 0.1f, nameof(BodyPhaseLag));
            RequireRange(_wristPhaseLag, 0f, 0.25f, nameof(WristPhaseLag));
            RequireRange(_lowElevation, -10f, 45f, nameof(LowElevation));
            RequireRange(_highElevation, 30f, 90f, nameof(HighElevation));
            RequireRange(_lowExtension, 0.4f, 0.9f, nameof(LowExtension));
            RequireRange(_highExtension, 0.7f, 1f, nameof(HighExtension));
            RequireRange(_baseSideAngle, -15f, 15f, nameof(BaseSideAngle));
            RequireRange(_sideSwayAmplitude, 0f, 15f, nameof(SideSwayAmplitude));
            RequireRange(_penlightLowElevation, 0f, 90f, nameof(PenlightLowElevation));
            RequireRange(_penlightRiseArc, -20f, 40f, nameof(PenlightRiseArc));
            RequireRange(_penlightSideAmplitude, 0f, 20f, nameof(PenlightSideAmplitude));
            RequireRange(_bodyRiseLift, 0f, 0.05f, nameof(BodyRiseLift));
            RequireRange(_baseBodyLean, -10f, 20f, nameof(BaseBodyLean));
            RequireRange(_bodyLeanAmplitude, 0f, 15f, nameof(BodyLeanAmplitude));
            if (_highElevation < _lowElevation)
                throw new ArgumentException("High elevation must not be below low elevation.");
            if (_highExtension < _lowExtension)
                throw new ArgumentException("High reach must not be below low reach.");
        }

        private static void RequireRange(float value, float minimum, float maximum, string name)
        {
            if (!float.IsFinite(value) || value < minimum || value > maximum)
                throw new ArgumentOutOfRangeException(name, $"{name} must be between {minimum} and {maximum}.");
        }
    }
}

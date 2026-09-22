using System;
using UnityEngine;

namespace PrismFanlight.Editor
{
    [Serializable]
    internal sealed class WiperParameters
    {
        // Fields

        [SerializeField, Range(0f, 1f)]
        private float _turnaroundEase = 0.35f;

        [SerializeField, Range(-0.1f, 0.1f)]
        private float _bodyPhaseLag = 0.025f;

        [SerializeField, Range(0f, 0.25f)]
        private float _wristPhaseLag = 0.045f;

        [SerializeField, Range(-30f, 30f)]
        private float _sideBias;

        [SerializeField, Range(0f, 80f)]
        private float _baseElevation = 42f;

        [SerializeField, Range(20f, 80f)]
        private float _sweepAngle = 55f;

        [SerializeField, Range(-20f, 20f)]
        private float _centerElevationArc = 6f;

        [SerializeField, Range(0.4f, 1f)]
        private float _baseExtension = 0.86f;

        [SerializeField, Range(-0.2f, 0.2f)]
        private float _centerExtensionArc = 0.06f;

        [SerializeField, Range(0f, 90f)]
        private float _penlightElevation = 68f;

        [SerializeField, Range(0f, 60f)]
        private float _penlightSideAmplitude = 18f;

        [SerializeField, Range(-20f, 20f)]
        private float _penlightCenterElevationArc = 4f;

        [SerializeField, Range(0f, 0.08f)]
        private float _bodySideShift = 0.018f;

        [SerializeField, Range(0f, 0.04f)]
        private float _bodyVerticalBounce = 0.012f;

        [SerializeField, Range(-10f, 20f)]
        private float _baseBodyLean = -1f;

        [SerializeField, Range(0f, 12f)]
        private float _bodyYawAmplitude = 2f;

        [SerializeField, Range(0f, 15f)]
        private float _bodyRollAmplitude = 4f;


        // Properties

        internal float TurnaroundEase
        {
            get => _turnaroundEase;
            set => _turnaroundEase = value;
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

        internal float SideBias
        {
            get => _sideBias;
            set => _sideBias = value;
        }

        internal float BaseElevation
        {
            get => _baseElevation;
            set => _baseElevation = value;
        }

        internal float SweepAngle
        {
            get => _sweepAngle;
            set => _sweepAngle = value;
        }

        internal float CenterElevationArc
        {
            get => _centerElevationArc;
            set => _centerElevationArc = value;
        }

        internal float BaseExtension
        {
            get => _baseExtension;
            set => _baseExtension = value;
        }

        internal float CenterExtensionArc
        {
            get => _centerExtensionArc;
            set => _centerExtensionArc = value;
        }

        internal float PenlightElevation
        {
            get => _penlightElevation;
            set => _penlightElevation = value;
        }

        internal float PenlightSideAmplitude
        {
            get => _penlightSideAmplitude;
            set => _penlightSideAmplitude = value;
        }

        internal float PenlightCenterElevationArc
        {
            get => _penlightCenterElevationArc;
            set => _penlightCenterElevationArc = value;
        }

        internal float BodySideShift
        {
            get => _bodySideShift;
            set => _bodySideShift = value;
        }

        internal float BodyVerticalBounce
        {
            get => _bodyVerticalBounce;
            set => _bodyVerticalBounce = value;
        }

        internal float BaseBodyLean
        {
            get => _baseBodyLean;
            set => _baseBodyLean = value;
        }

        internal float BodyYawAmplitude
        {
            get => _bodyYawAmplitude;
            set => _bodyYawAmplitude = value;
        }

        internal float BodyRollAmplitude
        {
            get => _bodyRollAmplitude;
            set => _bodyRollAmplitude = value;
        }


        // Methods

        internal static WiperParameters CreateDefault() => new();

        internal void Validate()
        {
            RequireRange(_turnaroundEase, 0f, 1f, nameof(TurnaroundEase));
            RequireRange(_bodyPhaseLag, -0.1f, 0.1f, nameof(BodyPhaseLag));
            RequireRange(_wristPhaseLag, 0f, 0.25f, nameof(WristPhaseLag));
            RequireRange(_sideBias, -30f, 30f, nameof(SideBias));
            RequireRange(_baseElevation, 0f, 80f, nameof(BaseElevation));
            RequireRange(_sweepAngle, 20f, 80f, nameof(SweepAngle));
            RequireRange(_centerElevationArc, -20f, 20f, nameof(CenterElevationArc));
            RequireRange(_baseExtension, 0.4f, 1f, nameof(BaseExtension));
            RequireRange(_centerExtensionArc, -0.2f, 0.2f, nameof(CenterExtensionArc));
            RequireRange(_penlightElevation, 0f, 90f, nameof(PenlightElevation));
            RequireRange(_penlightSideAmplitude, 0f, 60f, nameof(PenlightSideAmplitude));
            RequireRange(_penlightCenterElevationArc, -20f, 20f, nameof(PenlightCenterElevationArc));
            RequireRange(_bodySideShift, 0f, 0.08f, nameof(BodySideShift));
            RequireRange(_bodyVerticalBounce, 0f, 0.04f, nameof(BodyVerticalBounce));
            RequireRange(_baseBodyLean, -10f, 20f, nameof(BaseBodyLean));
            RequireRange(_bodyYawAmplitude, 0f, 12f, nameof(BodyYawAmplitude));
            RequireRange(_bodyRollAmplitude, 0f, 15f, nameof(BodyRollAmplitude));
        }

        private static void RequireRange(float value, float minimum, float maximum, string name)
        {
            if (!float.IsFinite(value) || value < minimum || value > maximum)
            {
                throw new ArgumentOutOfRangeException(name, $"{name} must be between {minimum} and {maximum}.");
            }
        }
    }
}

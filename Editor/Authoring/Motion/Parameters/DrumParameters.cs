using System;
using UnityEngine;

namespace PrismFanlight.Editor
{
    [Serializable]
    internal sealed class DrumParameters
    {
        // Fields

        [SerializeField, Range(0.3f, 0.9f)]
        private float _recoveryDuration = 0.62f;

        [SerializeField, Range(-0.1f, 0.1f)]
        private float _bodyPhaseLag = 0.025f;

        [SerializeField, Range(0f, 90f)]
        private float _baseElevation = 46f;

        [SerializeField, Range(0f, 60f)]
        private float _elevationAmplitude = 40f;

        [SerializeField, Range(0.4f, 1f)]
        private float _baseExtension = 0.88f;

        [SerializeField, Range(-0.2f, 0.3f)]
        private float _liftExtension = 0.07f;

        [SerializeField, Range(-0.2f, 0.2f)]
        private float _recoveryExtensionArc = -0.055f;

        [SerializeField, Range(-0.2f, 0.2f)]
        private float _strikeExtensionArc = 0.015f;

        [SerializeField, Range(-30f, 30f)]
        private float _baseSideAngle = 8f;

        [SerializeField, Range(-15f, 15f)]
        private float _recoverySideArc = -1.5f;

        [SerializeField, Range(-15f, 15f)]
        private float _strikeSideArc = 1f;

        [SerializeField, Range(-45f, 90f)]
        private float _basePitch = 14f;

        [SerializeField, Range(0f, 90f)]
        private float _pitchAmplitude = 26f;

        [SerializeField, Range(-30f, 30f)]
        private float _recoveryPitchArc = -4f;

        [SerializeField, Range(-30f, 30f)]
        private float _strikePitchArc = 6f;

        [SerializeField, Range(-45f, 90f)]
        private float _pitchPivot = 20f;

        [SerializeField, Range(-0.05f, 0.05f)]
        private float _bodySink = -0.010f;

        [SerializeField, Range(-0.05f, 0.05f)]
        private float _bodyPush = 0.003f;

        [SerializeField, Range(-10f, 20f)]
        private float _baseBodyLean = 1f;

        [SerializeField, Range(0f, 15f)]
        private float _bodyLeanAmplitude = 2.5f;

        [SerializeField, Range(0f, 0.25f)]
        private float _wristPhaseLag = 0.06f;


        // Properties

        internal float RecoveryDuration
        {
            get => _recoveryDuration;
            set => _recoveryDuration = value;
        }

        internal float BodyPhaseLag
        {
            get => _bodyPhaseLag;
            set => _bodyPhaseLag = value;
        }

        internal float BaseElevation
        {
            get => _baseElevation;
            set => _baseElevation = value;
        }

        internal float ElevationAmplitude
        {
            get => _elevationAmplitude;
            set => _elevationAmplitude = value;
        }

        internal float BaseExtension
        {
            get => _baseExtension;
            set => _baseExtension = value;
        }

        internal float LiftExtension
        {
            get => _liftExtension;
            set => _liftExtension = value;
        }

        internal float RecoveryExtensionArc
        {
            get => _recoveryExtensionArc;
            set => _recoveryExtensionArc = value;
        }

        internal float StrikeExtensionArc
        {
            get => _strikeExtensionArc;
            set => _strikeExtensionArc = value;
        }

        internal float BaseSideAngle
        {
            get => _baseSideAngle;
            set => _baseSideAngle = value;
        }

        internal float RecoverySideArc
        {
            get => _recoverySideArc;
            set => _recoverySideArc = value;
        }

        internal float StrikeSideArc
        {
            get => _strikeSideArc;
            set => _strikeSideArc = value;
        }

        internal float BasePitch
        {
            get => _basePitch;
            set => _basePitch = value;
        }

        internal float PitchAmplitude
        {
            get => _pitchAmplitude;
            set => _pitchAmplitude = value;
        }

        internal float RecoveryPitchArc
        {
            get => _recoveryPitchArc;
            set => _recoveryPitchArc = value;
        }

        internal float StrikePitchArc
        {
            get => _strikePitchArc;
            set => _strikePitchArc = value;
        }

        internal float PitchPivot
        {
            get => _pitchPivot;
            set => _pitchPivot = value;
        }

        internal float BodySink
        {
            get => _bodySink;
            set => _bodySink = value;
        }

        internal float BodyPush
        {
            get => _bodyPush;
            set => _bodyPush = value;
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

        internal float WristPhaseLag
        {
            get => _wristPhaseLag;
            set => _wristPhaseLag = value;
        }


        // Methods

        internal static DrumParameters CreateDefault() => new();

        internal void Validate()
        {
            RequireRange(_recoveryDuration, 0.3f, 0.9f, nameof(RecoveryDuration));
            RequireRange(_bodyPhaseLag, -0.1f, 0.1f, nameof(BodyPhaseLag));
            RequireRange(_baseElevation, 0f, 90f, nameof(BaseElevation));
            RequireRange(_elevationAmplitude, 0f, 60f, nameof(ElevationAmplitude));
            RequireRange(_baseExtension, 0.4f, 1f, nameof(BaseExtension));
            RequireRange(_liftExtension, -0.2f, 0.3f, nameof(LiftExtension));
            RequireRange(_recoveryExtensionArc, -0.2f, 0.2f, nameof(RecoveryExtensionArc));
            RequireRange(_strikeExtensionArc, -0.2f, 0.2f, nameof(StrikeExtensionArc));
            RequireRange(_baseSideAngle, -30f, 30f, nameof(BaseSideAngle));
            RequireRange(_recoverySideArc, -15f, 15f, nameof(RecoverySideArc));
            RequireRange(_strikeSideArc, -15f, 15f, nameof(StrikeSideArc));
            RequireRange(_basePitch, -45f, 90f, nameof(BasePitch));
            RequireRange(_pitchAmplitude, 0f, 90f, nameof(PitchAmplitude));
            RequireRange(_recoveryPitchArc, -30f, 30f, nameof(RecoveryPitchArc));
            RequireRange(_strikePitchArc, -30f, 30f, nameof(StrikePitchArc));
            RequireRange(_pitchPivot, -45f, 90f, nameof(PitchPivot));
            RequireRange(_bodySink, -0.05f, 0.05f, nameof(BodySink));
            RequireRange(_bodyPush, -0.05f, 0.05f, nameof(BodyPush));
            RequireRange(_baseBodyLean, -10f, 20f, nameof(BaseBodyLean));
            RequireRange(_bodyLeanAmplitude, 0f, 15f, nameof(BodyLeanAmplitude));
            RequireRange(_wristPhaseLag, 0f, 0.25f, nameof(WristPhaseLag));
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

using System;
using UnityEngine;

namespace PrismFanlight.Editor
{
    [Serializable]
    internal sealed class SasageParameters
    {
        // Fields

        [SerializeField, Range(0f, 0.3f)]
        private float _sideAmplitude = 0.12f;

        [SerializeField, Range(-0.1f, 0.75f)]
        private float _baseHandHeight = 0.37f;

        [SerializeField, Range(0.1f, 0.65f)]
        private float _verticalAmplitude = 0.49f;

        [SerializeField, Range(0.2f, 0.65f)]
        private float _baseHandDepth = 0.34f;

        [SerializeField, Range(0f, 0.3f)]
        private float _depthAmplitude = 0.18f;

        [SerializeField, Range(0f, 0.2f)]
        private float _wristPhaseLag = 0f;

        [SerializeField, Range(0f, 0.6f)]
        private float _penlightTangentWeight = 0.32f;

        [SerializeField, Range(0.1f, 0.8f)]
        private float _penlightUpwardBias = 0.38f;

        [SerializeField, Range(0f, 0.05f)]
        private float _bodyRiseLift = 0.018f;

        [SerializeField, Range(-10f, 10f)]
        private float _baseBodyLean = -2f;

        [SerializeField, Range(0f, 12f)]
        private float _bodyLeanAmplitude = 4f;

        [SerializeField, Range(0f, 8f)]
        private float _bodyRollAmplitude = 2.5f;


        // Properties

        internal float SideAmplitude
        {
            get => _sideAmplitude;
            set => _sideAmplitude = value;
        }

        internal float BaseHandHeight
        {
            get => _baseHandHeight;
            set => _baseHandHeight = value;
        }

        internal float VerticalAmplitude
        {
            get => _verticalAmplitude;
            set => _verticalAmplitude = value;
        }

        internal float BaseHandDepth
        {
            get => _baseHandDepth;
            set => _baseHandDepth = value;
        }

        internal float DepthAmplitude
        {
            get => _depthAmplitude;
            set => _depthAmplitude = value;
        }

        internal float WristPhaseLag
        {
            get => _wristPhaseLag;
            set => _wristPhaseLag = value;
        }

        internal float PenlightTangentWeight
        {
            get => _penlightTangentWeight;
            set => _penlightTangentWeight = value;
        }

        internal float PenlightUpwardBias
        {
            get => _penlightUpwardBias;
            set => _penlightUpwardBias = value;
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

        internal float BodyRollAmplitude
        {
            get => _bodyRollAmplitude;
            set => _bodyRollAmplitude = value;
        }


        // Methods

        internal static SasageParameters CreateDefault() => new();

        internal void Validate()
        {
            RequireRange(_sideAmplitude, 0f, 0.3f, nameof(SideAmplitude));
            RequireRange(_baseHandHeight, -0.1f, 0.75f, nameof(BaseHandHeight));
            RequireRange(_verticalAmplitude, 0.1f, 0.65f, nameof(VerticalAmplitude));
            RequireRange(_baseHandDepth, 0.2f, 0.65f, nameof(BaseHandDepth));
            RequireRange(_depthAmplitude, 0f, 0.3f, nameof(DepthAmplitude));
            RequireRange(_wristPhaseLag, 0f, 0.2f, nameof(WristPhaseLag));
            RequireRange(_penlightTangentWeight, 0f, 0.6f, nameof(PenlightTangentWeight));
            RequireRange(_penlightUpwardBias, 0.1f, 0.8f, nameof(PenlightUpwardBias));
            RequireRange(_bodyRiseLift, 0f, 0.05f, nameof(BodyRiseLift));
            RequireRange(_baseBodyLean, -10f, 10f, nameof(BaseBodyLean));
            RequireRange(_bodyLeanAmplitude, 0f, 12f, nameof(BodyLeanAmplitude));
            RequireRange(_bodyRollAmplitude, 0f, 8f, nameof(BodyRollAmplitude));
        }

        private static void RequireRange(float value, float minimum, float maximum, string name)
        {
            if (!float.IsFinite(value) || value < minimum || value > maximum)
                throw new ArgumentOutOfRangeException(name, $"{name} must be between {minimum} and {maximum}.");
        }
    }
}

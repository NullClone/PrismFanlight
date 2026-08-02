using System;
using UnityEngine;

namespace PrismFanlight.Authoring
{
    [CreateAssetMenu(fileName = "FanlightMotion", menuName = "Prism Fanlight/Motion Asset")]
    public sealed class FanlightMotionAsset : ScriptableObject
    {
        // Fields

        internal const int SampleCount = 64;
        private const int CurrentBakeFormatVersion = 2;


        [SerializeField, Range(-90f, 90f)]
        private float _referenceArmElevation = 65f;

        [SerializeField, Range(-90f, 90f)]
        private float _referenceArmSide;

        [SerializeField, Range(0f, 1f)]
        private float _referenceArmExtension = 0.92f;

        [SerializeField, Range(-90f, 90f)]
        private float _referencePenlightElevation = 78f;

        [SerializeField, Range(-180f, 180f)]
        private float _referencePenlightSide;

        [SerializeField, Range(-45f, 45f)]
        private float _referenceBodyLean = -2f;

        [SerializeField, Range(0f, 180f)]
        private float _armElevationAmplitude;

        [SerializeField, Range(0f, 180f)]
        private float _armSideAmplitude;

        [SerializeField, Range(0f, 1f)]
        private float _armExtensionAmplitude;

        [SerializeField, Range(0f, 180f)]
        private float _penlightElevationAmplitude;

        [SerializeField, Range(0f, 360f)]
        private float _penlightSideAmplitude;

        [SerializeField, Range(0f, 90f)]
        private float _bodyLeanAmplitude;

        [SerializeField]
        private AnimationCurve _armElevation = new();

        [SerializeField]
        private AnimationCurve _armSide = new();

        [SerializeField]
        private AnimationCurve _armExtension = new();

        [SerializeField]
        private AnimationCurve _penlightElevation = new();

        [SerializeField]
        private AnimationCurve _penlightSide = new();

        [SerializeField]
        private AnimationCurve _bodyLean = new();

        [SerializeField, HideInInspector]
        private FanlightMotionSample _bakedReferencePose;

        [SerializeField, HideInInspector]
        private FanlightMotionSample[] _bakedSamples = new FanlightMotionSample[SampleCount];

        [SerializeField, HideInInspector]
        private int _bakeRevision;

        [SerializeField, HideInInspector]
        private int _bakeFormatVersion;


        // Properties

        internal bool HasValidBake => _bakeFormatVersion == CurrentBakeFormatVersion
                                      && _bakedReferencePose.IsValid
                                      && _bakedSamples is { Length: SampleCount };

        internal int BakeRevision => _bakeRevision;

        internal FanlightMotionSample ReferencePose => _bakedReferencePose;


        // Methods

        private void Reset()
        {
            ResetToDrum();
        }

#if UNITY_EDITOR
        private void OnEnable()
        {
            if ((_armElevation == null || _armElevation.length == 0)
                && (_armSide == null || _armSide.length == 0)
                && (_armExtension == null || _armExtension.length == 0)
                && (_penlightElevation == null || _penlightElevation.length == 0)
                && (_penlightSide == null || _penlightSide.length == 0)
                && (_bodyLean == null || _bodyLean.length == 0))
            {
                ResetToDrum();
            }
        }

        private void OnValidate()
        {
            Bake();
        }
#endif

        internal void ResetToDrum()
        {
            _referenceArmElevation = 65f;
            _referenceArmSide = 0f;
            _referenceArmExtension = 0.92f;
            _referencePenlightElevation = 78f;
            _referencePenlightSide = 0f;
            _referenceBodyLean = -2f;
            _armElevationAmplitude = 120f;
            _armSideAmplitude = 0f;
            _armExtensionAmplitude = 0.1f;
            _penlightElevationAmplitude = 143f;
            _penlightSideAmplitude = 0f;
            _bodyLeanAmplitude = 6f;
            _armElevation = CreateCurve(
                new Keyframe(0f, -1f),
                new Keyframe(0.10f, -1f),
                new Keyframe(0.42f, 0f),
                new Keyframe(0.72f, 0f),
                new Keyframe(1f, -1f));
            _armSide = AnimationCurve.Linear(0f, 0f, 1f, 0f);
            _armExtension = CreateCurve(
                new Keyframe(0f, -1f),
                new Keyframe(0.10f, -0.8f),
                new Keyframe(0.42f, 0f),
                new Keyframe(0.72f, 0f),
                new Keyframe(1f, -1f));
            _penlightElevation = CreateCurve(
                new Keyframe(0f, -1f),
                new Keyframe(0.10f, -138f / 143f),
                new Keyframe(0.42f, 0f),
                new Keyframe(0.72f, 0f),
                new Keyframe(1f, -1f));
            _penlightSide = AnimationCurve.Linear(0f, 0f, 1f, 0f);
            _bodyLean = CreateCurve(
                new Keyframe(0f, 1f),
                new Keyframe(0.42f, 0f),
                new Keyframe(0.72f, 0f),
                new Keyframe(1f, 1f));

            Bake();
        }

        internal void Bake()
        {
            _bakedReferencePose = CreateSample(
                _referenceArmElevation,
                _referenceArmSide,
                _referenceArmExtension,
                _referencePenlightElevation,
                _referencePenlightSide,
                _referenceBodyLean);

            if (_bakedSamples == null || _bakedSamples.Length != SampleCount)
            {
                _bakedSamples = new FanlightMotionSample[SampleCount];
            }

            for (var i = 0; i < SampleCount; i++)
            {
                var phase = (float)i / SampleCount;
                _bakedSamples[i] = CreateSample(
                    EvaluateChannel(
                        _armElevation,
                        phase,
                        _referenceArmElevation,
                        _armElevationAmplitude,
                        -90f,
                        90f),
                    EvaluateChannel(
                        _armSide,
                        phase,
                        _referenceArmSide,
                        _armSideAmplitude,
                        -90f,
                        90f),
                    EvaluateChannel(
                        _armExtension,
                        phase,
                        _referenceArmExtension,
                        _armExtensionAmplitude,
                        0f,
                        1f),
                    EvaluateChannel(
                        _penlightElevation,
                        phase,
                        _referencePenlightElevation,
                        _penlightElevationAmplitude,
                        -90f,
                        90f),
                    EvaluateChannel(
                        _penlightSide,
                        phase,
                        _referencePenlightSide,
                        _penlightSideAmplitude,
                        -180f,
                        180f),
                    EvaluateChannel(
                        _bodyLean,
                        phase,
                        _referenceBodyLean,
                        _bodyLeanAmplitude,
                        -45f,
                        45f));
            }

            _bakeFormatVersion = CurrentBakeFormatVersion;

            var revision = 17;
            revision = unchecked(revision * 31 + _bakeFormatVersion);
            revision = unchecked(revision * 31 + _bakedReferencePose.ArmDirectionExtension.GetHashCode());
            revision = unchecked(revision * 31 + _bakedReferencePose.PenlightDirectionBodyLean.GetHashCode());
            for (var i = 0; i < _bakedSamples.Length; i++)
            {
                revision = unchecked(revision * 31 + _bakedSamples[i].ArmDirectionExtension.GetHashCode());
                revision = unchecked(revision * 31 + _bakedSamples[i].PenlightDirectionBodyLean.GetHashCode());
            }

            _bakeRevision = revision == 0 ? 1 : revision;
        }

        internal void SetReferenceFromPhase(float phase)
        {
            phase = Mathf.Repeat(phase, 1f);
            RebaseChannel(
                ref _referenceArmElevation,
                ref _armElevationAmplitude,
                ref _armElevation,
                phase,
                -90f,
                90f);
            RebaseChannel(ref _referenceArmSide, ref _armSideAmplitude, ref _armSide, phase, -90f, 90f);
            RebaseChannel(
                ref _referenceArmExtension,
                ref _armExtensionAmplitude,
                ref _armExtension,
                phase,
                0f,
                1f);
            RebaseChannel(
                ref _referencePenlightElevation,
                ref _penlightElevationAmplitude,
                ref _penlightElevation,
                phase,
                -90f,
                90f);
            RebaseChannel(
                ref _referencePenlightSide,
                ref _penlightSideAmplitude,
                ref _penlightSide,
                phase,
                -180f,
                180f);
            RebaseChannel(
                ref _referenceBodyLean,
                ref _bodyLeanAmplitude,
                ref _bodyLean,
                phase,
                -45f,
                45f);
            Bake();
        }

        internal bool CopyBakedSamples(FanlightMotionSample[] destination, int destinationIndex)
        {
            if (!HasValidBake
                || destination == null
                || destinationIndex < 0
                || destinationIndex + SampleCount > destination.Length)
            {
                return false;
            }

            Array.Copy(_bakedSamples, 0, destination, destinationIndex, SampleCount);
            return true;
        }

        private static AnimationCurve CreateCurve(params Keyframe[] keys)
        {
            var curve = new AnimationCurve(keys);
            for (var i = 0; i < curve.length; i++)
            {
                curve.SmoothTangents(i, 0f);
            }

            return curve;
        }

        private static FanlightMotionSample CreateSample(
            float armElevationDegrees,
            float armSideDegrees,
            float armExtension,
            float penlightElevationDegrees,
            float penlightSideDegrees,
            float bodyLeanDegrees)
        {
            var armDirection = CreateDirection(armElevationDegrees, armSideDegrees);
            var penlightDirection = CreateDirection(penlightElevationDegrees, penlightSideDegrees);

            return new FanlightMotionSample(
                armDirection,
                Mathf.Clamp01(armExtension),
                penlightDirection,
                Mathf.Clamp(bodyLeanDegrees, -45f, 45f) * Mathf.Deg2Rad);
        }

        private static Vector3 CreateDirection(float elevationDegrees, float sideDegrees)
        {
            var elevation = elevationDegrees * Mathf.Deg2Rad;
            var side = sideDegrees * Mathf.Deg2Rad;
            var cosElevation = Mathf.Cos(elevation);

            return new Vector3(
                Mathf.Sin(side) * cosElevation,
                Mathf.Sin(elevation),
                Mathf.Cos(side) * cosElevation).normalized;
        }

        private static float EvaluateChannel(
            AnimationCurve curve,
            float phase,
            float reference,
            float amplitude,
            float minimum,
            float maximum)
        {
            var normalized = EvaluateNormalized(curve, phase);
            return Mathf.Clamp(reference + normalized * Mathf.Max(0f, amplitude), minimum, maximum);
        }

        private static float EvaluateNormalized(AnimationCurve curve, float phase)
        {
            if (curve == null || curve.length == 0) return 0f;

            var value = curve.Evaluate(phase);

            if (float.IsNaN(value) || float.IsInfinity(value)) return 0f;

            return Mathf.Clamp(value, -1f, 1f);
        }

        private static void RebaseChannel(
            ref float reference,
            ref float amplitude,
            ref AnimationCurve curve,
            float phase,
            float minimum,
            float maximum)
        {
            amplitude = Mathf.Max(0f, amplitude);
            if (curve == null || curve.length == 0 || amplitude <= 0.000001f)
            {
                amplitude = 0f;
                curve = AnimationCurve.Linear(0f, 0f, 1f, 0f);
                return;
            }

            var previousReference = reference;
            var previousAmplitude = amplitude;
            reference = Mathf.Clamp(
                previousReference + EvaluateNormalized(curve, phase) * previousAmplitude,
                minimum,
                maximum);
            var maximumDelta = 0f;
            var keys = curve.keys;
            for (var i = 0; i < keys.Length; i++)
            {
                var value = Mathf.Clamp(
                    previousReference + Mathf.Clamp(keys[i].value, -1f, 1f) * previousAmplitude,
                    minimum,
                    maximum);
                maximumDelta = Mathf.Max(maximumDelta, Mathf.Abs(value - reference));
            }

            for (var i = 0; i < SampleCount; i++)
            {
                var samplePhase = (float)i / SampleCount;
                var value = Mathf.Clamp(
                    previousReference + EvaluateNormalized(curve, samplePhase) * previousAmplitude,
                    minimum,
                    maximum);
                maximumDelta = Mathf.Max(maximumDelta, Mathf.Abs(value - reference));
            }

            if (maximumDelta <= 0.000001f)
            {
                amplitude = 0f;
                curve = AnimationCurve.Linear(0f, 0f, 1f, 0f);
                return;
            }

            amplitude = maximumDelta;
            var tangentScale = previousAmplitude / maximumDelta;
            for (var i = 0; i < keys.Length; i++)
            {
                var key = keys[i];
                var value = Mathf.Clamp(
                    previousReference + Mathf.Clamp(key.value, -1f, 1f) * previousAmplitude,
                    minimum,
                    maximum);
                key.value = Mathf.Clamp((value - reference) / maximumDelta, -1f, 1f);
                if (float.IsFinite(key.inTangent)) key.inTangent *= tangentScale;
                if (float.IsFinite(key.outTangent)) key.outTangent *= tangentScale;
                keys[i] = key;
            }

            curve.keys = keys;
        }
    }
}

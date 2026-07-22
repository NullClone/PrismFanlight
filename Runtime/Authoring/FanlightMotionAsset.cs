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
                                      && _bakedSamples != null
                                      && _bakedSamples.Length == SampleCount;

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
            _armElevation = CreateCurve(
                new Keyframe(0f, -55f),
                new Keyframe(0.10f, -55f),
                new Keyframe(0.42f, 65f),
                new Keyframe(0.72f, 65f),
                new Keyframe(1f, -55f));
            _armSide = AnimationCurve.Linear(0f, 0f, 1f, 0f);
            _armExtension = CreateCurve(
                new Keyframe(0f, 0.82f),
                new Keyframe(0.10f, 0.84f),
                new Keyframe(0.42f, 0.92f),
                new Keyframe(0.72f, 0.92f),
                new Keyframe(1f, 0.82f));
            _penlightElevation = CreateCurve(
                new Keyframe(0f, -65f),
                new Keyframe(0.10f, -60f),
                new Keyframe(0.42f, 78f),
                new Keyframe(0.72f, 78f),
                new Keyframe(1f, -65f));
            _penlightSide = AnimationCurve.Linear(0f, 0f, 1f, 0f);
            _bodyLean = CreateCurve(
                new Keyframe(0f, 4f),
                new Keyframe(0.42f, -2f),
                new Keyframe(0.72f, -2f),
                new Keyframe(1f, 4f));
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
                    EvaluateDegrees(_armElevation, phase, -90f, 90f),
                    EvaluateDegrees(_armSide, phase, -90f, 90f),
                    Evaluate(_armExtension, phase, 0f, 1f, 0.8f),
                    EvaluateDegrees(_penlightElevation, phase, -90f, 90f),
                    EvaluateDegrees(_penlightSide, phase, -180f, 180f),
                    EvaluateDegrees(_bodyLean, phase, -45f, 45f));
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

        internal bool CopyBakedSamples(FanlightMotionSample[] destination, int destinationIndex)
        {
            if (!HasValidBake
                || destination == null
                || destinationIndex < 0
                || destinationIndex + SampleCount > destination.Length)
            {
                return false;
            }

            System.Array.Copy(_bakedSamples, 0, destination, destinationIndex, SampleCount);
            return true;
        }

        private static AnimationCurve CreateCurve(params Keyframe[] keys)
        {
            var curve = new AnimationCurve(keys);
            for (var i = 0; i < curve.length; i++) curve.SmoothTangents(i, 0f);
            return curve;
        }

        private static FanlightMotionSample CreateSample(
            float armElevationDegrees,
            float armSideDegrees,
            float armExtension,
            float penlightElevationDegrees,
            float penlightSideDegrees,
            float bodyLeanDegrees) =>
            new(
                CreateDirection(armElevationDegrees, armSideDegrees),
                Mathf.Clamp01(armExtension),
                CreateDirection(penlightElevationDegrees, penlightSideDegrees),
                Mathf.Clamp(bodyLeanDegrees, -45f, 45f) * Mathf.Deg2Rad);

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

        private static float EvaluateDegrees(AnimationCurve curve, float phase, float minimum, float maximum) =>
            Evaluate(curve, phase, minimum, maximum, 0f);

        private static float Evaluate(AnimationCurve curve, float phase, float minimum, float maximum, float fallback)
        {
            if (curve == null || curve.length == 0) return fallback;
            var value = curve.Evaluate(phase);
            if (float.IsNaN(value) || float.IsInfinity(value)) return fallback;
            return Mathf.Clamp(value, minimum, maximum);
        }
    }
}

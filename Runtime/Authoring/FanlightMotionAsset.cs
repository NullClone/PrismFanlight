using UnityEngine;

namespace PrismFanlight.Authoring
{
    [CreateAssetMenu(fileName = "FanlightMotion", menuName = "Prism Fanlight/Motion Asset")]
    public sealed class FanlightMotionAsset : ScriptableObject
    {
        // Fields

        internal const int SampleCount = 64;

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
        private FanlightMotionSample[] _bakedSamples = new FanlightMotionSample[SampleCount];

        [SerializeField, HideInInspector]
        private int _bakeRevision;


        // Properties

        internal bool HasValidBake => _bakedSamples != null && _bakedSamples.Length == SampleCount;

        internal int BakeRevision => _bakeRevision;


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
            if (_bakedSamples == null || _bakedSamples.Length != SampleCount)
            {
                _bakedSamples = new FanlightMotionSample[SampleCount];
            }

            for (var i = 0; i < SampleCount; i++)
            {
                var phase = (float)i / SampleCount;
                _bakedSamples[i] = new FanlightMotionSample(
                    EvaluateDegrees(_armElevation, phase, -90f, 90f) * Mathf.Deg2Rad,
                    EvaluateDegrees(_armSide, phase, -90f, 90f) * Mathf.Deg2Rad,
                    Evaluate(_armExtension, phase, 0f, 1f, 0.8f),
                    EvaluateDegrees(_bodyLean, phase, -45f, 45f) * Mathf.Deg2Rad,
                    EvaluateDegrees(_penlightElevation, phase, -90f, 90f) * Mathf.Deg2Rad,
                    EvaluateDegrees(_penlightSide, phase, -180f, 180f) * Mathf.Deg2Rad);
            }

            var revision = 17;
            for (var i = 0; i < _bakedSamples.Length; i++)
            {
                revision = unchecked(revision * 31 + _bakedSamples[i].Arm.GetHashCode());
                revision = unchecked(revision * 31 + _bakedSamples[i].Penlight.GetHashCode());
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

using System;
using UnityEngine;

namespace PrismFanlight.Authoring
{
    [CreateAssetMenu(fileName = "New Fanlight Motion", menuName = "Prism Fanlight/Motion Asset")]
    public sealed class FanlightMotionAsset : ScriptableObject
    {
        // Fields

        internal const int RuntimeSampleCount = 128;
        private const int CurrentBakeFormatVersion = 3;


        [SerializeField, HideInInspector]
        private FanlightMotionSample _referencePose;

        [SerializeField, HideInInspector]
        private FanlightMotionSample[] _samples = Array.Empty<FanlightMotionSample>();

        [SerializeField, HideInInspector]
        private int _bakeRevision;

        [SerializeField, HideInInspector]
        private int _bakeFormatVersion;


        // Properties

        internal bool HasValidBake => _bakeFormatVersion == CurrentBakeFormatVersion
                                      && _referencePose.IsValid
                                      && IsSupportedSampleCount(_samples?.Length ?? 0)
                                      && HasValidSamples(_samples);

        internal int BakeRevision => _bakeRevision;

        internal int SampleCount => _samples?.Length ?? 0;

        internal FanlightMotionSample ReferencePose => _referencePose;


        // Methods

        private void Reset()
        {
            ResetToDrum();
        }

        internal void ResetToDrum()
        {
            var samples = new FanlightMotionSample[RuntimeSampleCount];
            var times = new[] { 0f, 0.10f, 0.42f, 0.72f, 1f };
            var armValues = new[] { -1f, -1f, 0f, 0f, -1f };
            var extensionValues = new[] { -1f, -0.8f, 0f, 0f, -1f };
            var penlightValues = new[] { -1f, -138f / 143f, 0f, 0f, -1f };
            var bodyValues = new[] { 1f, 0.85f, 0f, 0f, 1f };

            for (var i = 0; i < samples.Length; i++)
            {
                var phase = (float)i / samples.Length;
                var armElevation = 65f + EvaluateSmoothKeys(phase, times, armValues) * 120f;
                var extension = 0.92f + EvaluateSmoothKeys(phase, times, extensionValues) * 0.1f;
                var penlightElevation = 78f + EvaluateSmoothKeys(phase, times, penlightValues) * 143f;
                var bodyLean = -2f + EvaluateSmoothKeys(phase, times, bodyValues) * 6f;
                samples[i] = CreateDirectionalSample(
                    CreateHandPosition(armElevation, 0f, extension),
                    CreateDirection(penlightElevation, 0f),
                    bodyLean);
            }

            var reference = CreateDirectionalSample(
                CreateHandPosition(65f, 0f, 0.92f),
                CreateDirection(78f, 0f),
                -2f);

            SetSamples(reference, samples);
        }

        internal void SetSamples(FanlightMotionSample referencePose, FanlightMotionSample[] samples)
        {
            if (!referencePose.IsValid)
            {
                throw new ArgumentException("Reference pose is invalid.", nameof(referencePose));
            }

            if (!IsSupportedSampleCount(samples?.Length ?? 0))
            {
                throw new ArgumentException("Motion sample count must be 64, 128, or 256.", nameof(samples));
            }

            if (!HasValidSamples(samples))
            {
                throw new ArgumentException("Motion samples contain invalid data.", nameof(samples));
            }

            _referencePose = referencePose;
            _samples = (FanlightMotionSample[])samples?.Clone();
            _bakeFormatVersion = CurrentBakeFormatVersion;

            RecalculateRevision();
        }

        internal FanlightMotionSample Sample(float phase)
        {
            if (!HasValidBake) return default;

            var samplePosition = Mathf.Repeat(phase, 1f) * _samples.Length;
            var sample0 = Mathf.FloorToInt(samplePosition) % _samples.Length;
            var sample1 = (sample0 + 1) % _samples.Length;

            return FanlightMotionSample.Interpolate(_samples[sample0], _samples[sample1], samplePosition - sample0);
        }

        internal bool CopyResampledSamples(
            FanlightMotionSample[] destination,
            int destinationIndex,
            int destinationCount)
        {
            if (!HasValidBake
                || destination == null
                || destinationCount <= 0
                || destinationIndex < 0
                || destinationIndex + destinationCount > destination.Length)
            {
                return false;
            }

            if (_samples.Length == destinationCount)
            {
                Array.Copy(_samples, 0, destination, destinationIndex, destinationCount);
                return true;
            }

            for (var i = 0; i < destinationCount; i++)
            {
                destination[destinationIndex + i] = Sample((float)i / destinationCount);
            }

            return true;
        }

        private void RecalculateRevision()
        {
            var revision = 17;
            revision = AddSampleToHash(revision, _referencePose);
            revision = unchecked(revision * 31 + _bakeFormatVersion);
            revision = unchecked(revision * 31 + _samples.Length);
            for (var i = 0; i < _samples.Length; i++)
            {
                revision = AddSampleToHash(revision, _samples[i]);
            }

            _bakeRevision = revision == 0 ? 1 : revision;
        }

        private static int AddSampleToHash(int revision, FanlightMotionSample sample)
        {
            revision = unchecked(revision * 31 + sample.BodyPositionData.GetHashCode());
            revision = unchecked(revision * 31 + sample.BodyRotationData.GetHashCode());
            revision = unchecked(revision * 31 + sample.HandPositionData.GetHashCode());
            return unchecked(revision * 31 + sample.PenlightRotationData.GetHashCode());
        }

        private static FanlightMotionSample CreateDirectionalSample(
            Vector3 handPosition,
            Vector3 penlightDirection,
            float bodyLeanDegrees)
        {
            return new FanlightMotionSample(
                Vector3.zero,
                Quaternion.Euler(bodyLeanDegrees, 0f, 0f),
                handPosition,
                Quaternion.FromToRotation(Vector3.up, penlightDirection));
        }

        private static Vector3 CreateHandPosition(float elevationDegrees, float sideDegrees, float extension)
            => CreateDirection(elevationDegrees, sideDegrees) * Mathf.Clamp01(extension);

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

        private static float EvaluateSmoothKeys(float phase, float[] times, float[] values)
        {
            phase = Mathf.Repeat(phase, 1f);
            for (var i = 0; i < times.Length - 1; i++)
            {
                if (phase > times[i + 1]) continue;

                var weight = Mathf.InverseLerp(times[i], times[i + 1], phase);
                weight = weight * weight * (3f - 2f * weight);
                return Mathf.LerpUnclamped(values[i], values[i + 1], weight);
            }

            return values[^1];
        }

        private static bool IsSupportedSampleCount(int sampleCount)
            => sampleCount == 64 || sampleCount == 128 || sampleCount == 256;

        private static bool HasValidSamples(FanlightMotionSample[] samples)
        {
            if (samples == null) return false;
            for (var i = 0; i < samples.Length; i++)
            {
                if (!samples[i].IsValid) return false;
            }

            return true;
        }
    }
}

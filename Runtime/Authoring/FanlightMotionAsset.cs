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


#if UNITY_EDITOR
        [SerializeField, HideInInspector]
        private string _editorGeneratorSettings;
#endif

        // Properties

#if UNITY_EDITOR
        internal string EditorGeneratorSettings
        {
            get => _editorGeneratorSettings;
            set => _editorGeneratorSettings = value;
        }
#endif

        internal bool HasValidBake => _bakeFormatVersion == CurrentBakeFormatVersion
                                      && _referencePose.IsValid
                                      && IsSupportedSampleCount(_samples?.Length ?? 0)
                                      && HasValidSamples(_samples);

        internal int BakeRevision => _bakeRevision;

        internal int SampleCount => _samples?.Length ?? 0;

        internal FanlightMotionSample ReferencePose => _referencePose;


        // Methods

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

        internal bool CopyResampledSamples(FanlightMotionSample[] destination, int destinationIndex, int destinationCount)
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

        private static bool IsSupportedSampleCount(int sampleCount) => sampleCount is 64 or 128 or 256;

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

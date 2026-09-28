using System;
using UnityEngine;

namespace PrismFanlight.Core
{
    [Serializable]
    internal struct FanlightRestState
    {
        // Fields

        [SerializeField, FanlightField(FanlightRestFields.Probability), Range(0f, 1f)]
        private float _probability;

        [SerializeField, FanlightField(FanlightRestFields.MotionLevel), Range(0f, 1f)]
        private float _motionLevel;

        [SerializeField, FanlightField(FanlightRestFields.CycleSeconds)]
        private float _cycleSeconds;

        [SerializeField, FanlightField(FanlightRestFields.DurationSeconds)]
        private float _durationSeconds;

        [SerializeField, FanlightField(FanlightRestFields.FadeSeconds)]
        private float _fadeSeconds;

        [SerializeField, FanlightField(FanlightRestFields.PhaseRandomness), Range(0f, 1f)]
        private float _phaseRandomness;


        // Properties

        internal float Probability => _probability;

        internal float MotionLevel => _motionLevel;

        internal float CycleSeconds => _cycleSeconds;

        internal float DurationSeconds => _durationSeconds;

        internal float FadeSeconds => _fadeSeconds;

        internal float PhaseRandomness => _phaseRandomness;


        // Methods

        internal FanlightRestState(float probability, float motionLevel, float cycleSeconds, float durationSeconds, float fadeSeconds, float phaseRandomness)
        {
            _probability = FanlightStateValidation.RequireRange(probability, 0f, 1f, nameof(probability));
            _motionLevel = FanlightStateValidation.RequireRange(motionLevel, 0f, 1f, nameof(motionLevel));
            _cycleSeconds = FanlightStateValidation.RequireRange(cycleSeconds, 0f, 3600f, nameof(cycleSeconds));
            _durationSeconds = FanlightStateValidation.RequireRange(durationSeconds, 0f, 3600f, nameof(durationSeconds));
            _fadeSeconds = FanlightStateValidation.RequireRange(fadeSeconds, 0f, 60f, nameof(fadeSeconds));
            _phaseRandomness = FanlightStateValidation.RequireRange(phaseRandomness, 0f, 1f, nameof(phaseRandomness));
        }
    }
}

using System;
using UnityEngine;

namespace PrismFanlight.Core
{
    [Serializable]
    internal struct FanlightRestState
    {
        // Fields

        [SerializeField]
        private float _probability;

        [SerializeField]
        private float _motionLevel;

        [SerializeField]
        private float _cycleSeconds;

        [SerializeField]
        private float _durationSeconds;

        [SerializeField]
        private float _fadeSeconds;

        [SerializeField]
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

using System;
using UnityEngine;

namespace PrismFanlight.Core
{
    [Serializable]
    internal struct FanlightNoiseState
    {
        // Fields

        [SerializeField]
        private float _phaseAmount;

        [SerializeField]
        private float _phaseRate;

        [SerializeField]
        private float _positionAmount;

        [SerializeField]
        private float _directionAmount;

        [SerializeField]
        private float _spatialRate;

        [SerializeField]
        private int _octaves;

        [SerializeField]
        private float _persistence;


        // Properties

        internal float PhaseAmount => _phaseAmount;

        internal float PhaseRate => _phaseRate;

        internal float PositionAmount => _positionAmount;

        internal float DirectionAmount => _directionAmount;

        internal float SpatialRate => _spatialRate;

        internal int Octaves => _octaves;

        internal float Persistence => _persistence;


        // Methods

        internal FanlightNoiseState(
            float phaseAmount,
            float phaseRate,
            float positionAmount,
            float directionAmount,
            float spatialRate,
            int octaves,
            float persistence)
        {
            _phaseAmount = FanlightStateValidation.RequireRange(phaseAmount, 0f, 4f, nameof(phaseAmount));
            _phaseRate = FanlightStateValidation.RequireRange(phaseRate, 0f, 16f, nameof(phaseRate));
            _positionAmount = FanlightStateValidation.RequireRange(positionAmount, 0f, 0.2f, nameof(positionAmount));
            _directionAmount = FanlightStateValidation.RequireRange(directionAmount, 0f, 0.4f, nameof(directionAmount));
            _spatialRate = FanlightStateValidation.RequireRange(spatialRate, 0f, 16f, nameof(spatialRate));
            if (octaves < 1 || octaves > 4) throw new ArgumentOutOfRangeException(nameof(octaves));
            _octaves = octaves;
            _persistence = FanlightStateValidation.RequireRange(persistence, 0f, 1f, nameof(persistence));
        }
    }
}

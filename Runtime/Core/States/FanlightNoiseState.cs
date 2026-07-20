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
        private float _phaseSpeed;

        [SerializeField]
        private float _axisAmount;

        [SerializeField]
        private float _axisSpeed;

        [SerializeField]
        private int _octaves;

        [SerializeField]
        private float _persistence;


        // Properties

        internal float PhaseAmount => _phaseAmount;

        internal float PhaseSpeed => _phaseSpeed;

        internal float AxisAmount => _axisAmount;

        internal float AxisSpeed => _axisSpeed;

        internal int Octaves => _octaves;

        internal float Persistence => _persistence;


        // Methods

        internal FanlightNoiseState(float phaseAmount, float phaseSpeed, float axisAmount, float axisSpeed, int octaves, float persistence)
        {
            _phaseAmount = FanlightStateValidation.RequireRange(phaseAmount, 0f, 4f, nameof(phaseAmount));
            _phaseSpeed = FanlightStateValidation.RequireRange(phaseSpeed, 0f, 16f, nameof(phaseSpeed));
            _axisAmount = FanlightStateValidation.RequireRange(axisAmount, 0f, 4f, nameof(axisAmount));
            _axisSpeed = FanlightStateValidation.RequireRange(axisSpeed, 0f, 16f, nameof(axisSpeed));
            if (octaves < 1 || octaves > 4) throw new ArgumentOutOfRangeException(nameof(octaves));
            _octaves = octaves;
            _persistence = FanlightStateValidation.RequireRange(persistence, 0f, 1f, nameof(persistence));
        }
    }
}

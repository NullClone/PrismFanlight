using System;
using UnityEngine;

namespace PrismFanlight.Core
{
    [Serializable]
    internal struct FanlightDirectionState
    {
        // Fields

        [SerializeField]
        private FanlightDirectionMode _mode;

        [SerializeField, Label("Direction (Fallback)")]
        private float _worldYawDegrees;

        [SerializeField, Label("Strength"), Range(0f, 1f)]
        private float _aimStrength;


        // Properties

        internal FanlightDirectionMode Mode => _mode;

        internal float WorldYawDegrees => _worldYawDegrees;

        internal float AimStrength => _aimStrength;


        // Methods

        internal FanlightDirectionState(FanlightDirectionMode mode, float worldYawDegrees, float aimStrength)
        {
            if (mode is not FanlightDirectionMode.WorldDirection and not FanlightDirectionMode.Target)
            {
                throw new ArgumentOutOfRangeException(nameof(mode));
            }

            _mode = mode;
            _worldYawDegrees = FanlightStateValidation.NormalizeDegrees(worldYawDegrees, nameof(worldYawDegrees));
            _aimStrength = FanlightStateValidation.RequireRange(aimStrength, 0f, 1f, nameof(aimStrength));
        }
    }
}

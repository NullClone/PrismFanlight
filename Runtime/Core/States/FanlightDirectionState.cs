using System;
using UnityEngine;

namespace PrismFanlight.Core
{
    [Serializable]
    internal struct FanlightDirectionState
    {
        // Fields

        [SerializeField, FanlightField(FanlightDirectionFields.Mode)]
        private FanlightDirectionMode _mode;

        [SerializeField, FanlightField(FanlightDirectionFields.Direction)]
        private float _direction;


        // Properties

        internal FanlightDirectionMode Mode => _mode;

        internal float Direction => _direction;


        // Methods

        internal FanlightDirectionState(FanlightDirectionMode mode, float direction)
        {
            if (mode is not FanlightDirectionMode.WorldDirection and not FanlightDirectionMode.Target)
            {
                throw new ArgumentOutOfRangeException(nameof(mode));
            }

            _mode = mode;
            _direction = FanlightStateValidation.NormalizeDegrees(direction, nameof(direction));
        }
    }
}

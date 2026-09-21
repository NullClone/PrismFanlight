using System;
using UnityEngine;

namespace PrismFanlight.Core
{
    [Serializable]
    internal struct FanlightIntentState
    {
        // Fields

        [SerializeField, Range(0f, 1f)]
        private float _energy;

        [SerializeField, Range(0f, 1f)]
        private float _participation;

        [SerializeField, Range(0f, 1f)]
        private float _synchronization;


        // Properties

        internal float Energy => _energy;

        internal float Participation => _participation;

        internal float Synchronization => _synchronization;


        // Methods

        internal FanlightIntentState(float energy, float participation, float synchronization)
        {
            _energy = FanlightStateValidation.RequireRange(energy, 0f, 1f, nameof(energy));
            _participation = FanlightStateValidation.RequireRange(participation, 0f, 1f, nameof(participation));
            _synchronization = FanlightStateValidation.RequireRange(synchronization, 0f, 1f, nameof(synchronization));
        }
    }
}

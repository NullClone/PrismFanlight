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

        [SerializeField, Range(0f, 1f)]
        private float _transitionScatter;


        // Properties

        internal float Energy => _energy;

        internal float Participation => _participation;

        internal float Synchronization => _synchronization;
        internal float TransitionScatter => _transitionScatter;


        // Methods

        internal FanlightIntentState(float energy, float participation, float synchronization, float transitionScatter)
        {
            _energy = FanlightStateValidation.RequireRange(energy, 0f, 1f, nameof(energy));
            _participation = FanlightStateValidation.RequireRange(participation, 0f, 1f, nameof(participation));
            _synchronization = FanlightStateValidation.RequireRange(synchronization, 0f, 1f, nameof(synchronization));
            _transitionScatter = FanlightStateValidation.RequireRange(transitionScatter, 0f, 1f, nameof(transitionScatter));
        }
    }
}

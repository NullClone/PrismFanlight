using System;
using UnityEngine;

namespace PrismFanlight.Core
{
    [Serializable]
    internal struct FanlightIntentState
    {
        // Fields

        [SerializeField]
        private float _energy;

        [SerializeField]
        private float _participation;

        [SerializeField]
        private float _synchronization;

        [SerializeField]
        private float _realism;

        [SerializeField]
        private float _reach;


        // Properties

        internal float Energy => _energy;

        internal float Participation => _participation;

        internal float Synchronization => _synchronization;

        internal float Realism => _realism;

        internal float Reach => _reach;


        // Methods

        internal FanlightIntentState(float energy, float participation, float synchronization, float realism, float reach)
        {
            _energy = FanlightStateValidation.RequireRange(energy, 0f, 1f, nameof(energy));
            _participation = FanlightStateValidation.RequireRange(participation, 0f, 1f, nameof(participation));
            _synchronization = FanlightStateValidation.RequireRange(synchronization, 0f, 1f, nameof(synchronization));
            _realism = FanlightStateValidation.RequireRange(realism, 0f, 1f, nameof(realism));
            _reach = FanlightStateValidation.RequireRange(reach, 0f, 1f, nameof(reach));
        }
    }
}

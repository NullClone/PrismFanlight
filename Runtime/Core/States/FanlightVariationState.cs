using System;
using UnityEngine;

namespace PrismFanlight.Core
{
    [Serializable]
    internal struct FanlightVariationState
    {
        // Fields

        [SerializeField]
        private float _standingPositionSpread;

        [SerializeField]
        private float _heightVariation;

        [SerializeField]
        private float _armExtensionVariation;

        [SerializeField]
        private float _penlightDirectionSpread;

        [SerializeField]
        private float _reactionDelaySeconds;

        [SerializeField]
        private float _beatJitterBeats;

        [SerializeField]
        private float _energyResponse;

        [SerializeField]
        private float _handPositionSpread;


        // Properties

        internal float StandingPositionSpread => _standingPositionSpread;

        internal float HeightVariation => _heightVariation;

        internal float ArmExtensionVariation => _armExtensionVariation;

        internal float PenlightDirectionSpread => _penlightDirectionSpread;

        internal float ReactionDelaySeconds => _reactionDelaySeconds;

        internal float BeatJitterBeats => _beatJitterBeats;

        internal float EnergyResponse => _energyResponse;

        internal float HandPositionSpread => _handPositionSpread;


        // Methods

        internal FanlightVariationState(
            float standingPositionSpread,
            float heightVariation,
            float armExtensionVariation,
            float penlightDirectionSpread,
            float reactionDelaySeconds,
            float beatJitterBeats,
            float energyResponse,
            float handPositionSpread)
        {
            _standingPositionSpread = FanlightStateValidation.RequireRange(standingPositionSpread, 0f, 1f, nameof(standingPositionSpread));
            _heightVariation = FanlightStateValidation.RequireRange(heightVariation, 0f, 1f, nameof(heightVariation));
            _armExtensionVariation = FanlightStateValidation.RequireRange(armExtensionVariation, 0f, 1f, nameof(armExtensionVariation));
            _penlightDirectionSpread = FanlightStateValidation.RequireRange(penlightDirectionSpread, 0f, 1f, nameof(penlightDirectionSpread));
            _reactionDelaySeconds = FanlightStateValidation.RequireRange(reactionDelaySeconds, 0f, 10f, nameof(reactionDelaySeconds));
            _beatJitterBeats = FanlightStateValidation.RequireRange(beatJitterBeats, 0f, 8f, nameof(beatJitterBeats));
            _energyResponse = FanlightStateValidation.RequireRange(energyResponse, 0f, 1f, nameof(energyResponse));
            _handPositionSpread = FanlightStateValidation.RequireRange(handPositionSpread, 0f, 0.5f, nameof(handPositionSpread));
        }
    }
}

using System;
using UnityEngine;

namespace PrismFanlight.Core
{
    [Serializable]
    internal struct FanlightGestureState
    {
        // Fields

        [SerializeField]
        private float _beatsPerCycle;

        [SerializeField]
        private float _phaseOffsetBeats;

        [SerializeField]
        private float _holdRatio;

        [SerializeField]
        private float _crispness;

        [SerializeField]
        private float _followThrough;

        [SerializeField]
        private float _downbeatAccent;


        // Properties

        internal float BeatsPerCycle => _beatsPerCycle;

        internal float PhaseOffsetBeats => _phaseOffsetBeats;

        internal float HoldRatio => _holdRatio;

        internal float Crispness => _crispness;

        internal float FollowThrough => _followThrough;

        internal float DownbeatAccent => _downbeatAccent;


        // Methods

        internal FanlightGestureState(
            float beatsPerCycle,
            float phaseOffsetBeats,
            float holdRatio,
            float crispness,
            float followThrough,
            float downbeatAccent)
        {
            _beatsPerCycle = FanlightStateValidation.RequireRange(beatsPerCycle, 0.001f, 64f, nameof(beatsPerCycle));
            _phaseOffsetBeats = FanlightStateValidation.RequireRange(phaseOffsetBeats, -64f, 64f, nameof(phaseOffsetBeats));
            _holdRatio = FanlightStateValidation.RequireRange(holdRatio, 0f, 1f, nameof(holdRatio));
            _crispness = FanlightStateValidation.RequireRange(crispness, 0f, 1f, nameof(crispness));
            _followThrough = FanlightStateValidation.RequireRange(followThrough, 0f, 1f, nameof(followThrough));
            _downbeatAccent = FanlightStateValidation.RequireRange(downbeatAccent, 0f, 4f, nameof(downbeatAccent));
        }
    }
}

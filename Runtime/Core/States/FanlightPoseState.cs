using System;
using UnityEngine;

namespace PrismFanlight.Core
{
    [Serializable]
    internal struct FanlightPoseState
    {
        // Fields

        [SerializeField]
        private Vector3 _readyHandOffset;

        [SerializeField]
        private Vector3 _accentHandOffset;

        [SerializeField]
        private Vector3 _handArcOffset;

        [SerializeField]
        private Vector3 _readyPenlightDirection;

        [SerializeField]
        private Vector3 _accentPenlightDirection;

        [SerializeField]
        private float _bodyLean;


        // Properties

        internal Vector3 ReadyHandOffset => _readyHandOffset;

        internal Vector3 AccentHandOffset => _accentHandOffset;

        internal Vector3 HandArcOffset => _handArcOffset;

        internal Vector3 ReadyPenlightDirection => _readyPenlightDirection;

        internal Vector3 AccentPenlightDirection => _accentPenlightDirection;

        internal float BodyLean => _bodyLean;


        // Methods

        internal FanlightPoseState(
            Vector3 readyHandOffset,
            Vector3 accentHandOffset,
            Vector3 handArcOffset,
            Vector3 readyPenlightDirection,
            Vector3 accentPenlightDirection,
            float bodyLean)
        {
            _readyHandOffset = FanlightStateValidation.RequireFinite(readyHandOffset, nameof(readyHandOffset));
            _accentHandOffset = FanlightStateValidation.RequireFinite(accentHandOffset, nameof(accentHandOffset));
            _handArcOffset = FanlightStateValidation.RequireFinite(handArcOffset, nameof(handArcOffset));
            _readyPenlightDirection = FanlightStateValidation.RequireDirection(readyPenlightDirection, nameof(readyPenlightDirection));
            _accentPenlightDirection = FanlightStateValidation.RequireDirection(accentPenlightDirection, nameof(accentPenlightDirection));
            _bodyLean = FanlightStateValidation.RequireRange(bodyLean, -1f, 1f, nameof(bodyLean));
        }
    }
}

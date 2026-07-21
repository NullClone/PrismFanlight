using System;
using UnityEngine;

namespace PrismFanlight.Core
{
    [Serializable]
    internal struct FanlightPoseState
    {
        // Fields

        [SerializeField]
        private FanlightHandZone _handZone;

        [SerializeField]
        private float _handHeightOffset;

        [SerializeField]
        private float _handForwardOffset;

        [SerializeField, Min(0.01f)]
        private float _handReachScale;

        [SerializeField]
        private float _armLengthMinimum;

        [SerializeField]
        private float _armLengthMaximum;

        [SerializeField]
        private float _angleMinimumRadians;

        [SerializeField]
        private float _angleMaximumRadians;

        [SerializeField]
        private float _horizontalRatio;

        [SerializeField]
        private float _wristFrequencyMultiplier;

        [SerializeField]
        private float _wristAngleRadians;

        [SerializeField]
        private float _bodyLean;


        // Properties

        internal FanlightHandZone HandZone => _handZone;

        internal float HandHeightOffset => _handHeightOffset;

        internal float HandForwardOffset => _handForwardOffset;

        internal float HandReachScale => _handReachScale;

        internal float ArmLengthMinimum => _armLengthMinimum;

        internal float ArmLengthMaximum => _armLengthMaximum;

        internal float AngleMinimumRadians => _angleMinimumRadians;

        internal float AngleMaximumRadians => _angleMaximumRadians;

        internal float HorizontalRatio => _horizontalRatio;

        internal float WristFrequencyMultiplier => _wristFrequencyMultiplier;

        internal float WristAngleRadians => _wristAngleRadians;

        internal float BodyLean => _bodyLean;


        // Methods

        internal FanlightPoseState(
            FanlightHandZone handZone,
            float handHeightOffset,
            float handForwardOffset,
            float handReachScale,
            float armLengthMinimum,
            float armLengthMaximum,
            float angleMinimumRadians,
            float angleMaximumRadians,
            float horizontalRatio,
            float wristFrequencyMultiplier,
            float wristAngleRadians,
            float bodyLean)
        {
            if (handZone is < FanlightHandZone.Shoulder or > FanlightHandZone.High)
            {
                throw new ArgumentOutOfRangeException(nameof(handZone));
            }

            _handZone = handZone;
            _handHeightOffset = FanlightStateValidation.RequireRange(handHeightOffset, -1f, 1.5f, nameof(handHeightOffset));
            _handForwardOffset = FanlightStateValidation.RequireRange(handForwardOffset, -1f, 1f, nameof(handForwardOffset));
            _handReachScale = FanlightStateValidation.RequireMinimum(handReachScale, 0.01f, nameof(handReachScale));
            _armLengthMinimum = FanlightStateValidation.RequireRange(armLengthMinimum, 0f, 5f, nameof(armLengthMinimum));
            _armLengthMaximum = FanlightStateValidation.RequireRange(armLengthMaximum, 0f, 5f, nameof(armLengthMaximum));
            _angleMinimumRadians = FanlightStateValidation.RequireRange(angleMinimumRadians, 0f, Mathf.PI * 2f, nameof(angleMinimumRadians));
            _angleMaximumRadians = FanlightStateValidation.RequireRange(angleMaximumRadians, 0f, Mathf.PI * 2f, nameof(angleMaximumRadians));
            _horizontalRatio = FanlightStateValidation.RequireRange(horizontalRatio, 0f, 1f, nameof(horizontalRatio));
            _wristFrequencyMultiplier = FanlightStateValidation.RequireRange(wristFrequencyMultiplier, 1f, 64f, nameof(wristFrequencyMultiplier));
            _wristAngleRadians = FanlightStateValidation.RequireRange(wristAngleRadians, 0f, Mathf.PI, nameof(wristAngleRadians));
            _bodyLean = FanlightStateValidation.RequireRange(bodyLean, -1f, 1f, nameof(bodyLean));
        }
    }
}

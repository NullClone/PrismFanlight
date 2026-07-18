using System;
using UnityEngine;

namespace PrismFanlight.Core
{
    [Serializable]
    internal struct FanlightVariationState
    {
        [SerializeField] private float _seatPosition;
        [SerializeField] private float _bodyHeight;
        [SerializeField] private float _armLength;
        [SerializeField] private float _angle;
        [SerializeField] private float _directionSpread;
        [SerializeField] private float _reactionDelaySeconds;
        [SerializeField] private float _beatJitter;
        [SerializeField] private float _blockDelayXBeats;
        [SerializeField] private float _blockDelayYBeats;
        [SerializeField] private float _energyResponse;
        [SerializeField] private float _speed;
        [SerializeField] private float _beatReactionDelaySeconds;
        [SerializeField] private float _handZone;

        internal float SeatPosition => _seatPosition;
        internal float BodyHeight => _bodyHeight;
        internal float ArmLength => _armLength;
        internal float Angle => _angle;
        internal float DirectionSpread => _directionSpread;
        internal float ReactionDelaySeconds => _reactionDelaySeconds;
        internal float BeatJitter => _beatJitter;
        internal float BlockDelayXBeats => _blockDelayXBeats;
        internal float BlockDelayYBeats => _blockDelayYBeats;
        internal float EnergyResponse => _energyResponse;
        internal float Speed => _speed;
        internal float BeatReactionDelaySeconds => _beatReactionDelaySeconds;
        internal float HandZone => _handZone;

        internal FanlightVariationState(
            float seatPosition,
            float bodyHeight,
            float armLength,
            float angle,
            float directionSpread,
            float reactionDelaySeconds,
            float beatJitter,
            float blockDelayXBeats,
            float blockDelayYBeats,
            float energyResponse,
            float speed,
            float beatReactionDelaySeconds,
            float handZone)
        {
            _seatPosition = FanlightStateValidation.RequireRange(seatPosition, 0f, 1f, nameof(seatPosition));
            _bodyHeight = FanlightStateValidation.RequireRange(bodyHeight, 0f, 1f, nameof(bodyHeight));
            _armLength = FanlightStateValidation.RequireRange(armLength, 0f, 1f, nameof(armLength));
            _angle = FanlightStateValidation.RequireRange(angle, 0f, 1f, nameof(angle));
            _directionSpread = FanlightStateValidation.RequireRange(directionSpread, 0f, 1f, nameof(directionSpread));
            _reactionDelaySeconds = FanlightStateValidation.RequireRange(reactionDelaySeconds, 0f, 10f, nameof(reactionDelaySeconds));
            _beatJitter = FanlightStateValidation.RequireRange(beatJitter, 0f, 8f, nameof(beatJitter));
            _blockDelayXBeats = FanlightStateValidation.RequireRange(blockDelayXBeats, -64f, 64f, nameof(blockDelayXBeats));
            _blockDelayYBeats = FanlightStateValidation.RequireRange(blockDelayYBeats, -64f, 64f, nameof(blockDelayYBeats));
            _energyResponse = FanlightStateValidation.RequireRange(energyResponse, 0f, 1f, nameof(energyResponse));
            _speed = FanlightStateValidation.RequireRange(speed, 0f, 4f, nameof(speed));
            _beatReactionDelaySeconds = FanlightStateValidation.RequireRange(beatReactionDelaySeconds, 0f, 10f, nameof(beatReactionDelaySeconds));
            _handZone = FanlightStateValidation.RequireRange(handZone, 0f, 0.5f, nameof(handZone));
        }
    }
}

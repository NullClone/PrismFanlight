using System;
using UnityEngine;

namespace PrismFanlight.Core
{
    [Serializable]
    internal struct FanlightIntensityMask
    {
        // Fields

        [SerializeField]
        private FanlightIntensityMaskMode _mode;

        [SerializeField]
        private float _beatsPerCycle;

        [SerializeField]
        private float _phaseOffsetBeats;

        [SerializeField]
        private float _minimumIntensityRatio;

        [SerializeField]
        private float _attackRatio;

        [SerializeField]
        private float _holdRatio;

        [SerializeField]
        private float _releaseRatio;

        [SerializeField]
        private Vector2 _origin;

        [SerializeField]
        private Vector2 _direction;

        [SerializeField]
        private float _wavelength;


        // Properties

        internal FanlightIntensityMaskMode Mode => _mode;

        internal float BeatsPerCycle => _beatsPerCycle;

        internal float PhaseOffsetBeats => _phaseOffsetBeats;

        internal float MinimumIntensityRatio => _minimumIntensityRatio;

        internal float AttackRatio => _attackRatio;

        internal float HoldRatio => _holdRatio;

        internal float ReleaseRatio => _releaseRatio;

        internal Vector2 Origin => _origin;

        internal Vector2 Direction => _direction;

        internal float Wavelength => _wavelength;


        // Methods

        internal FanlightIntensityMask(
            FanlightIntensityMaskMode mode,
            float beatsPerCycle,
            float phaseOffsetBeats,
            float minimumIntensityRatio,
            float attackRatio,
            float holdRatio,
            float releaseRatio,
            Vector2 origin,
            Vector2 direction,
            float wavelength)
        {
            _mode = mode;
            _beatsPerCycle = beatsPerCycle;
            _phaseOffsetBeats = phaseOffsetBeats;
            _minimumIntensityRatio = minimumIntensityRatio;
            _attackRatio = attackRatio;
            _holdRatio = holdRatio;
            _releaseRatio = releaseRatio;
            _origin = origin;
            _direction = direction;
            _wavelength = wavelength;
            ValidateAndNormalize();
        }

        internal FanlightIntensityMask Validated()
        {
            var value = this;
            value.ValidateAndNormalize();
            return value;
        }

        internal bool ContentEquals(in FanlightIntensityMask other)
        {
            if (_mode != other._mode) return false;

            return _mode switch
            {
                FanlightIntensityMaskMode.None => true,
                FanlightIntensityMaskMode.Pulse => EnvelopeEquals(other),
                FanlightIntensityMaskMode.TravelingWave => EnvelopeEquals(other)
                                                               && _origin.Equals(other._origin)
                                                               && _direction.Equals(other._direction)
                                                               && _wavelength.Equals(other._wavelength),
                _ => false
            };
        }

        private void ValidateAndNormalize()
        {
            switch (_mode)
            {
                case FanlightIntensityMaskMode.None:
                    break;
                case FanlightIntensityMaskMode.Pulse:
                    ValidateEnvelope();
                    break;
                case FanlightIntensityMaskMode.TravelingWave:
                    ValidateEnvelope();
                    _origin = FanlightStateValidation.RequireFinite(_origin, nameof(_origin));
                    _direction = FanlightStateValidation.RequireDirection(_direction, nameof(_direction));
                    _wavelength = FanlightStateValidation.RequireMinimumExclusive(
                        _wavelength,
                        0f,
                        nameof(_wavelength));
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(_mode));
            }
        }

        private bool EnvelopeEquals(in FanlightIntensityMask other)
        {
            return _beatsPerCycle.Equals(other._beatsPerCycle)
                   && _phaseOffsetBeats.Equals(other._phaseOffsetBeats)
                   && _minimumIntensityRatio.Equals(other._minimumIntensityRatio)
                   && _attackRatio.Equals(other._attackRatio)
                   && _holdRatio.Equals(other._holdRatio)
                   && _releaseRatio.Equals(other._releaseRatio);
        }

        private void ValidateEnvelope()
        {
            _beatsPerCycle = FanlightStateValidation.RequireMinimumExclusive(
                _beatsPerCycle,
                0f,
                nameof(_beatsPerCycle));
            _phaseOffsetBeats = FanlightStateValidation.RequireFinite(
                _phaseOffsetBeats,
                nameof(_phaseOffsetBeats));
            _minimumIntensityRatio = FanlightStateValidation.RequireRange(
                _minimumIntensityRatio,
                0f,
                1f,
                nameof(_minimumIntensityRatio));
            _attackRatio = FanlightStateValidation.RequireRange(
                _attackRatio,
                0f,
                1f,
                nameof(_attackRatio));
            _holdRatio = FanlightStateValidation.RequireRange(
                _holdRatio,
                0f,
                1f,
                nameof(_holdRatio));
            _releaseRatio = FanlightStateValidation.RequireRange(
                _releaseRatio,
                0f,
                1f,
                nameof(_releaseRatio));

            var activeRatio = _attackRatio + _holdRatio + _releaseRatio;
            if (!FanlightStateValidation.IsFinite(activeRatio)
                || activeRatio <= 0f
                || activeRatio > 1f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(_attackRatio),
                    "Attack, Hold, and Release Ratio must total more than 0 and no more than 1.");
            }
        }
    }
}

using System;
using System.Collections.Generic;
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

        [SerializeField, Range(0f, 1f)]
        private float _minimumIntensityRatio;

        [SerializeField, Range(0f, 1f)]
        private float _attackRatio;

        [SerializeField, Range(0f, 1f)]
        private float _holdRatio;

        [SerializeField, Range(0f, 1f)]
        private float _releaseRatio;

        [SerializeField]
        private Vector2 _origin;

        [SerializeField]
        private float _localYawDegrees;

        [SerializeField]
        private float _wavelength;

        [SerializeField]
        private FanlightRadialWaveDirection _radialWaveDirection;

        [SerializeField]
        private FanlightAngularWaveDirection _angularWaveDirection;

        [SerializeField]
        private FanlightBlockPulseEntry[] _blockPulseEntries;


        // Properties

        internal FanlightIntensityMaskMode Mode => _mode;

        internal float BeatsPerCycle => _beatsPerCycle;

        internal float PhaseOffsetBeats => _phaseOffsetBeats;

        internal float MinimumIntensityRatio => _minimumIntensityRatio;

        internal float AttackRatio => _attackRatio;

        internal float HoldRatio => _holdRatio;

        internal float ReleaseRatio => _releaseRatio;

        internal Vector2 Origin => _origin;

        internal float LocalYawDegrees => _localYawDegrees;

        internal float Wavelength => _wavelength;

        internal FanlightRadialWaveDirection RadialWaveDirection => _radialWaveDirection;

        internal FanlightAngularWaveDirection AngularWaveDirection => _angularWaveDirection;

        internal int BlockPulseEntryCount => _blockPulseEntries?.Length ?? 0;

        internal bool UsesLocalYaw => _mode == FanlightIntensityMaskMode.TravelingWave
                                      || _mode == FanlightIntensityMaskMode.AngularWave;


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
            float localYawDegrees,
            float wavelength,
            FanlightRadialWaveDirection radialWaveDirection,
            FanlightAngularWaveDirection angularWaveDirection,
            FanlightBlockPulseEntry[] blockPulseEntries)
        {
            _mode = mode;
            _beatsPerCycle = beatsPerCycle;
            _phaseOffsetBeats = phaseOffsetBeats;
            _minimumIntensityRatio = minimumIntensityRatio;
            _attackRatio = attackRatio;
            _holdRatio = holdRatio;
            _releaseRatio = releaseRatio;
            _origin = origin;
            _localYawDegrees = localYawDegrees;
            _wavelength = wavelength;
            _radialWaveDirection = radialWaveDirection;
            _angularWaveDirection = angularWaveDirection;
            _blockPulseEntries = blockPulseEntries == null
                ? Array.Empty<FanlightBlockPulseEntry>()
                : (FanlightBlockPulseEntry[])blockPulseEntries.Clone();
            ValidateAndNormalize();
        }

        internal FanlightBlockPulseEntry GetBlockPulseEntry(int index) => _blockPulseEntries[index];

        internal FanlightIntensityMask Validated()
        {
            return new FanlightIntensityMask(
                _mode,
                _beatsPerCycle,
                _phaseOffsetBeats,
                _minimumIntensityRatio,
                _attackRatio,
                _holdRatio,
                _releaseRatio,
                _origin,
                _localYawDegrees,
                _wavelength,
                _radialWaveDirection,
                _angularWaveDirection,
                _blockPulseEntries);
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
                                                           && _localYawDegrees.Equals(other._localYawDegrees)
                                                           && _wavelength.Equals(other._wavelength),
                FanlightIntensityMaskMode.RadialWave => EnvelopeEquals(other)
                                                        && _origin.Equals(other._origin)
                                                        && _wavelength.Equals(other._wavelength)
                                                        && _radialWaveDirection == other._radialWaveDirection,
                FanlightIntensityMaskMode.RandomSparkle => EnvelopeEquals(other),
                FanlightIntensityMaskMode.AngularWave => EnvelopeEquals(other)
                                                         && _origin.Equals(other._origin)
                                                         && _localYawDegrees.Equals(other._localYawDegrees)
                                                         && _angularWaveDirection == other._angularWaveDirection,
                FanlightIntensityMaskMode.BlockAlternatingPulse => EnvelopeEquals(other)
                                                                  && BlockPulseEntriesEqual(other),
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
                    _localYawDegrees = FanlightStateValidation.NormalizeDegrees(
                        _localYawDegrees,
                        nameof(_localYawDegrees));
                    _wavelength = FanlightStateValidation.RequireMinimumExclusive(
                        _wavelength,
                        0f,
                        nameof(_wavelength));
                    break;
                case FanlightIntensityMaskMode.RadialWave:
                    ValidateEnvelope();
                    _origin = FanlightStateValidation.RequireFinite(_origin, nameof(_origin));
                    _wavelength = FanlightStateValidation.RequireMinimumExclusive(
                        _wavelength,
                        0f,
                        nameof(_wavelength));
                    ValidateRadialWaveDirection();
                    break;
                case FanlightIntensityMaskMode.RandomSparkle:
                    ValidateEnvelope();
                    break;
                case FanlightIntensityMaskMode.AngularWave:
                    ValidateEnvelope();
                    _origin = FanlightStateValidation.RequireFinite(_origin, nameof(_origin));
                    _localYawDegrees = FanlightStateValidation.NormalizeDegrees(
                        _localYawDegrees,
                        nameof(_localYawDegrees));
                    ValidateAngularWaveDirection();
                    break;
                case FanlightIntensityMaskMode.BlockAlternatingPulse:
                    ValidateEnvelope();
                    ValidateBlockPulseEntries();
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(_mode));
            }
        }

        private bool BlockPulseEntriesEqual(in FanlightIntensityMask other)
        {
            if (BlockPulseEntryCount != other.BlockPulseEntryCount) return false;

            for (var i = 0; i < BlockPulseEntryCount; i++)
            {
                if (!GetBlockPulseEntry(i).Equals(other.GetBlockPulseEntry(i))) return false;
            }

            return true;
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

        private void ValidateRadialWaveDirection()
        {
            if (_radialWaveDirection != FanlightRadialWaveDirection.Outward
                && _radialWaveDirection != FanlightRadialWaveDirection.Inward)
            {
                throw new ArgumentOutOfRangeException(nameof(_radialWaveDirection));
            }
        }

        private void ValidateAngularWaveDirection()
        {
            if (_angularWaveDirection != FanlightAngularWaveDirection.Clockwise
                && _angularWaveDirection != FanlightAngularWaveDirection.Counterclockwise)
            {
                throw new ArgumentOutOfRangeException(nameof(_angularWaveDirection));
            }
        }

        private void ValidateBlockPulseEntries()
        {
            if (_blockPulseEntries == null || _blockPulseEntries.Length == 0)
            {
                throw new ArgumentException(
                    "Block Alternating Pulse requires a complete Stable Block ID mapping.",
                    nameof(_blockPulseEntries));
            }

            var ids = new HashSet<string>(StringComparer.Ordinal);
            for (var i = 0; i < _blockPulseEntries.Length; i++)
            {
                var entry = _blockPulseEntries[i];
                _ = new FanlightBlockPulseEntry(entry.StableBlockId, entry.Group);
                if (!ids.Add(entry.StableBlockId))
                {
                    throw new ArgumentException(
                        "Block Alternating Pulse Stable Block IDs must be unique.",
                        nameof(_blockPulseEntries));
                }
            }
        }
    }
}

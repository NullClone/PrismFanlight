using System;
using PrismFanlight.Core;
using UnityEngine;

namespace PrismFanlight.Live
{
    [Serializable]
    internal struct FanlightCueDefinition
    {
        // Fields

        [SerializeField]
        private string _cueId;

        [SerializeField]
        private string _displayName;

        [SerializeField]
        private FanlightShowPatch _patch;

        [SerializeField]
        private int _priority;

        [SerializeField]
        private double _attackSeconds;

        [SerializeField]
        private double _holdSeconds;

        [SerializeField]
        private double _releaseSeconds;

        [SerializeField]
        private FanlightCueRetriggerMode _retriggerMode;


        // Properties

        internal string CueId => _cueId;

        internal string DisplayName => _displayName;

        internal FanlightShowPatch Patch => _patch;

        internal int Priority => _priority;

        internal double AttackSeconds => _attackSeconds;

        internal double HoldSeconds => _holdSeconds;

        internal double ReleaseSeconds => _releaseSeconds;

        internal FanlightCueRetriggerMode RetriggerMode => _retriggerMode;


        // Methods

        internal FanlightCueDefinition(
            string cueId,
            string displayName,
            FanlightShowPatch patch,
            int priority,
            double attackSeconds,
            double holdSeconds,
            double releaseSeconds,
            FanlightCueRetriggerMode retriggerMode)
        {
            if (string.IsNullOrWhiteSpace(cueId))
            {
                throw new ArgumentException("Cue ID is required.", nameof(cueId));
            }

            if (string.IsNullOrWhiteSpace(displayName))
            {
                throw new ArgumentException("Display name is required.", nameof(displayName));
            }

            if (double.IsNaN(attackSeconds) || double.IsInfinity(attackSeconds) || attackSeconds < 0d)
            {
                throw new ArgumentOutOfRangeException(nameof(attackSeconds));
            }

            if (double.IsNaN(holdSeconds) || double.IsNegativeInfinity(holdSeconds) || holdSeconds < 0d)
            {
                throw new ArgumentOutOfRangeException(nameof(holdSeconds));
            }

            if (double.IsNaN(releaseSeconds) || double.IsInfinity(releaseSeconds) || releaseSeconds < 0d)
            {
                throw new ArgumentOutOfRangeException(nameof(releaseSeconds));
            }

            if (retriggerMode is not FanlightCueRetriggerMode.Restart
                and not FanlightCueRetriggerMode.IgnoreWhileActive
                and not FanlightCueRetriggerMode.ReplaceActive)
            {
                throw new ArgumentOutOfRangeException(nameof(retriggerMode));
            }

            _cueId = cueId;
            _displayName = displayName;
            _patch = patch;
            _priority = priority;
            _attackSeconds = attackSeconds;
            _holdSeconds = holdSeconds;
            _releaseSeconds = releaseSeconds;
            _retriggerMode = retriggerMode;
        }

        internal void Validate() => _ = new FanlightCueDefinition(
            CueId,
            DisplayName,
            Patch,
            Priority,
            AttackSeconds,
            HoldSeconds,
            ReleaseSeconds,
            RetriggerMode);
    }
}

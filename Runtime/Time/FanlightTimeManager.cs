using System;
using PrismFanlight.Core;
using UnityEngine;

namespace PrismFanlight.Time
{
    [ExecuteAlways]
    [HelpURL(PrismFanlight.HelpUrl)]
    //[AddComponentMenu("Prism Fanlight/Fanlight Time Manager")]
    public sealed class FanlightTimeManager : MonoBehaviour
    {
        // Fields

        [SerializeField, Label("Negative Time")]
        private ShowNegativeTimePolicy _negativeTimePolicy = ShowNegativeTimePolicy.ClampToZero;

        [SerializeReference]
        private IShowTimeProvider _provider = new UnityTimeProvider();

        [Space]
        [SerializeField, Min(1e-6f)]
        private double _defaultBpm = 120d;

        [SerializeField, Min(1)]
        private int _defaultBeatsPerBar = 4;

        [SerializeField]
        private FanlightBeatUnit _defaultBeatUnit = FanlightBeatUnit.u4;

        [SerializeField]
        private double _defaultMusicalOriginSeconds;


        private ShowTimeCoordinator _coordinator;
        private FanlightShowTimeFault _lastFault;
        private int _defaultTempoRevision;


        // Properties

        internal ShowNegativeTimePolicy NegativeTimePolicy => _negativeTimePolicy;

        internal bool IsFallbackActive => _coordinator?.IsFallbackActive ?? false;

        internal bool IsPrimaryAvailable => _coordinator?.IsPrimaryAvailable ?? false;

        internal double DefaultBpm => _defaultBpm;

        internal int DefaultBeatsPerBar => _defaultBeatsPerBar;

        internal int DefaultBeatUnit => (int)_defaultBeatUnit;

        internal double DefaultMusicalOriginSeconds => _defaultMusicalOriginSeconds;

        internal int DefaultTempoRevision => _defaultTempoRevision;


        // Methods

        private void OnEnable() => EnsureCoordinator();

        private void OnDisable() => _coordinator = null;

        private void OnValidate()
        {
            _defaultBpm = Math.Max(1e-6d, _defaultBpm);
            _defaultBeatsPerBar = Math.Max(1, _defaultBeatsPerBar);

            if (_defaultBeatUnit is not (FanlightBeatUnit.u1 or FanlightBeatUnit.u2 or FanlightBeatUnit.u4 or FanlightBeatUnit.u8 or FanlightBeatUnit.u16))
            {
                _defaultBeatUnit = FanlightBeatUnit.u4;
            }

            _defaultTempoRevision = _defaultTempoRevision == int.MaxValue ? 1 : _defaultTempoRevision + 1;
            _coordinator = null;
        }


        internal bool TrySampleClock(long evaluationId, out FanlightClockSample sample, out FanlightShowTimeFault fault)
        {
            EnsureCoordinator();

            if (_coordinator == null)
            {
                sample = default;
                fault = _lastFault == FanlightShowTimeFault.None ? FanlightShowTimeFault.CoordinatorUnavailable : _lastFault;
                _lastFault = fault;

                return false;
            }

            var success = _coordinator.TrySampleClock(evaluationId, out sample, out fault);
            _lastFault = fault;
            return success;
        }

        internal bool TryRequestPrimaryReacquire(out string failureCode)
        {
            EnsureCoordinator();

            if (_coordinator == null)
            {
                failureCode = "CoordinatorUnavailable";
                return false;
            }

            return _coordinator.TryRequestPrimaryReacquire(out failureCode);
        }


        private void EnsureCoordinator()
        {
            if (_coordinator != null) return;

            if (_provider == null)
            {
                _lastFault = FanlightShowTimeFault.CoordinatorUnavailable;
                _coordinator = null;
                return;
            }

            _coordinator = new ShowTimeCoordinator(NegativeTimePolicy, _provider);
            _lastFault = FanlightShowTimeFault.None;
        }
    }
}

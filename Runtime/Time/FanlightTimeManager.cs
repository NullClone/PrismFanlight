using System;
using PrismFanlight.Authoring;
using PrismFanlight.Core;
using UnityEngine;

namespace PrismFanlight.Time
{
    [ExecuteAlways]
    [HelpURL("https://github.com/NullClone/PrismFanlight")]
    [AddComponentMenu("Prism Fanlight/Fanlight Time Manager")]
    public sealed class FanlightTimeManager : MonoBehaviour
    {
        // Fields

        [SerializeField]
        private ShowNegativeTimePolicy _negativeTimePolicy = ShowNegativeTimePolicy.ClampToZero;

        [SerializeField]
        private MonoBehaviour _primaryProvider;

        [SerializeField]
        private FanlightTempoMap _tempoMap;

        [SerializeField, Min(1e-6f)]
        private double _defaultBpm = 120d;

        [SerializeField, Min(1)]
        private int _defaultBeatsPerBar = 4;

        [SerializeField]
        private int _defaultBeatUnit = 4;

        [SerializeField]
        private double _defaultOffsetSeconds;


        private ShowTimeCoordinator _coordinator;
        private FanlightShowTimeFault _lastFault;


        // Properties

        internal ShowNegativeTimePolicy NegativeTimePolicy => _negativeTimePolicy;

        internal bool IsFallbackActive => _coordinator?.IsFallbackActive ?? false;

        internal bool IsPrimaryAvailable => _coordinator?.IsPrimaryAvailable ?? false;


        // Methods

        private void OnEnable() => EnsureCoordinator();

        private void OnDisable() => _coordinator = null;

        private void OnValidate()
        {
            _defaultBpm = Math.Max(1e-6d, _defaultBpm);
            _defaultBeatsPerBar = Math.Max(1, _defaultBeatsPerBar);

            if (_defaultBeatUnit is not (1 or 2 or 4 or 8 or 16))
            {
                _defaultBeatUnit = 4;
            }

            _coordinator = null;
        }


        internal bool TrySample(long evaluationId, out FanlightShowTimeSample sample, out FanlightShowTimeFault fault)
        {
            EnsureCoordinator();

            if (_coordinator == null)
            {
                sample = default;
                fault = _lastFault == FanlightShowTimeFault.None
                    ? FanlightShowTimeFault.CoordinatorUnavailable
                    : _lastFault;
                _lastFault = fault;

                return false;
            }

            var success = _coordinator.TrySample(evaluationId, out sample, out fault);
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

            var provider = ResolveProvider();

            if (provider == null)
            {
                _lastFault = FanlightShowTimeFault.CoordinatorUnavailable;
                _coordinator = null;
                return;
            }

            IShowTempoMapResolver tempo;

            try
            {
                tempo = _tempoMap != null
                    ? new FanlightTempoMapResolver(_tempoMap)
                    : new ConstantTempoMapResolver(
                        _defaultBpm,
                        _defaultBeatsPerBar,
                        _defaultBeatUnit,
                        _defaultOffsetSeconds);
            }
            catch (Exception exception)
            {
                _lastFault = FanlightShowTimeFault.TempoMapUnavailable;
                _coordinator = null;
                return;
            }

            _coordinator = new ShowTimeCoordinator(
                NegativeTimePolicy,
                provider,
                tempo);

            _lastFault = FanlightShowTimeFault.None;
        }

        private IShowTimeProvider ResolveProvider() => _primaryProvider as IShowTimeProvider;
    }
}

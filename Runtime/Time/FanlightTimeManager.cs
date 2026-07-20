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
        private ShowTimePrimaryMode _primaryMode = ShowTimePrimaryMode.UnityTime;

        [SerializeField]
        private MonoBehaviour _primaryProvider;

        [SerializeField]
        private double _manualSeconds;

        [SerializeField]
        private double _manualRate;

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


        private readonly UnityUnscaledTimeSource _unityTime = new();
        private ShowTimeCoordinator _coordinator;
        private UnityTimeProvider _unityProvider;
        private ManualTimeProvider _manualProvider;
        private FanlightShowTimeFault _lastFault;
        private string _lastFailureCode = string.Empty;


        // Properties

        public ShowNegativeTimePolicy NegativeTimePolicy => _negativeTimePolicy;

        public bool IsFallbackActive => _coordinator?.IsFallbackActive ?? false;

        public bool IsPrimaryAvailable => _coordinator?.IsPrimaryAvailable ?? false;

        public FanlightShowTimeFault LastFault => _lastFault;

        public string LastFailureCode => _lastFailureCode;


        // Methods

        private void OnEnable() => EnsureCoordinator();

        private void OnDisable() => _coordinator = null;

        private void OnValidate()
        {
            _defaultBpm = Math.Max(1e-6d, _defaultBpm);
            _defaultBeatsPerBar = Math.Max(1, _defaultBeatsPerBar);
            if (_defaultBeatUnit is not (1 or 2 or 4 or 8 or 16)) _defaultBeatUnit = 4;
            _coordinator = null;
        }


        public bool TrySample(long evaluationId, out FanlightShowTimeSample sample, out FanlightShowTimeFault fault)
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

        public bool TryRequestPrimaryReacquire(out string failureCode)
        {
            EnsureCoordinator();

            if (_coordinator == null)
            {
                failureCode = "CoordinatorUnavailable";
                _lastFailureCode = failureCode;
                return false;
            }

            var result = _coordinator.TryRequestPrimaryReacquire(out failureCode);
            _lastFailureCode = failureCode;
            return result;
        }

        public void SetManualTime(double seconds, double rate = 0d)
        {
            _manualSeconds = seconds;
            _manualRate = rate;
            _manualProvider?.Set(seconds, rate);
        }


        private void EnsureCoordinator()
        {
            if (_coordinator != null) return;

            var provider = ResolveProvider();

            if (provider == null)
            {
                _lastFault = FanlightShowTimeFault.CoordinatorUnavailable;
                _lastFailureCode = "PrimaryProviderMissing";
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
                _lastFailureCode = exception.Message;
                _coordinator = null;
                return;
            }

            _coordinator = new ShowTimeCoordinator(
                NegativeTimePolicy,
                provider,
                tempo,
                _unityTime);

            _lastFault = FanlightShowTimeFault.None;
            _lastFailureCode = string.Empty;
        }

        private IShowTimeProvider ResolveProvider()
        {
            switch (_primaryMode)
            {
                case ShowTimePrimaryMode.Manual:
                    _manualProvider ??= new ManualTimeProvider();
                    _manualProvider.Set(_manualSeconds, _manualRate);
                    return _manualProvider;
                case ShowTimePrimaryMode.Component:
                    return _primaryProvider as IShowTimeProvider;
                default:
                    return _unityProvider ??= new UnityTimeProvider(_unityTime);
            }
        }
    }
}

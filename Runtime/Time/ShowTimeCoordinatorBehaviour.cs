using System;
using PrismFanlight.Authoring;
using PrismFanlight.Core;
using UnityEngine;

namespace PrismFanlight.Time
{
    public enum ShowTimePrimaryMode
    {
        UnityTime = 0,
        Manual = 1,
        Component = 2
    }

    [ExecuteAlways]
    [AddComponentMenu("Prism Fanlight/Show Time Coordinator")]
    public sealed class ShowTimeCoordinatorBehaviour : MonoBehaviour, IShowTimeCoordinator
    {
        [SerializeField]
        private string _timeDomainId = string.Empty;

        [SerializeField, Min(1)]
        private int _timeDomainVersion = 1;

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
        private double _compatibilityBpm = 120d;

        [SerializeField, Min(1)]
        private int _compatibilityBeatsPerBar = 4;

        [SerializeField]
        private double _compatibilityOffsetSeconds;

        private readonly UnityUnscaledTimeSource _unityTime = new();
        private ShowTimeCoordinator _coordinator;
        private UnityTimeProvider _unityProvider;
        private ManualTimeProvider _manualProvider;
        private int _configurationHash;
        private FanlightShowTimeFault _lastFault;
        private string _lastFailureCode = string.Empty;

        public string TimeDomainId => _timeDomainId ?? string.Empty;
        public int TimeDomainVersion => Math.Max(1, _timeDomainVersion);
        public ShowNegativeTimePolicy NegativeTimePolicy => _negativeTimePolicy;
        public bool IsFallbackActive => _coordinator?.IsFallbackActive ?? false;
        public bool IsPrimaryAvailable => _coordinator?.IsPrimaryAvailable ?? false;
        public FanlightShowTimeFault LastFault => _lastFault;
        public string LastFailureCode => _lastFailureCode;

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

        public void ConfigureCompatibilityTempo(double bpm, int beatsPerBar, double offsetSeconds)
        {
            if (_tempoMap != null) return;
            var validatedBpm = Math.Max(1e-6d, bpm);
            var validatedBeats = Math.Max(1, beatsPerBar);
            if (Math.Abs(_compatibilityBpm - validatedBpm) <= 1e-9
                && _compatibilityBeatsPerBar == validatedBeats
                && Math.Abs(_compatibilityOffsetSeconds - offsetSeconds) <= 1e-9) return;
            _compatibilityBpm = validatedBpm;
            _compatibilityBeatsPerBar = validatedBeats;
            _compatibilityOffsetSeconds = offsetSeconds;
            _coordinator = null;
        }

        public void ConfigureCompatibilityIdentity(string timeDomainId)
        {
            if (!string.IsNullOrEmpty(TimeDomainId) || string.IsNullOrWhiteSpace(timeDomainId)) return;
            _timeDomainId = timeDomainId;
            _coordinator = null;
        }

        public void SetManualTime(double seconds, double rate = 0d)
        {
            _manualSeconds = seconds;
            _manualRate = rate;
            _manualProvider?.Set(seconds, rate);
        }

        private void OnEnable() => EnsureCoordinator();

        private void OnValidate()
        {
            _timeDomainVersion = Math.Max(1, _timeDomainVersion);
            _compatibilityBpm = Math.Max(1e-6d, _compatibilityBpm);
            _compatibilityBeatsPerBar = Math.Max(1, _compatibilityBeatsPerBar);
            if (string.IsNullOrEmpty(_timeDomainId)) _timeDomainId = Guid.NewGuid().ToString("N");
#if UNITY_EDITOR
            foreach (var other in FindObjectsByType<ShowTimeCoordinatorBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (other != this && string.Equals(other._timeDomainId, _timeDomainId, StringComparison.Ordinal))
                {
                    _timeDomainId = Guid.NewGuid().ToString("N");
                    break;
                }
            }
#endif
            _coordinator = null;
        }

        private void EnsureCoordinator()
        {
            var hash = ComputeConfigurationHash();
            if (_coordinator != null && hash == _configurationHash) return;
            _configurationHash = hash;
            if (string.IsNullOrEmpty(TimeDomainId))
            {
                _lastFault = FanlightShowTimeFault.CoordinatorUnavailable;
                _lastFailureCode = "TimeDomainIdMissing";
                _coordinator = null;
                return;
            }

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
                        "tempo.compatibility",
                        ComputeCompatibilityTempoVersion(),
                        _compatibilityBpm,
                        _compatibilityBeatsPerBar,
                        4,
                        _compatibilityOffsetSeconds);
            }
            catch (Exception exception)
            {
                _lastFault = FanlightShowTimeFault.TempoMapUnavailable;
                _lastFailureCode = exception.Message;
                _coordinator = null;
                return;
            }

            _coordinator = new ShowTimeCoordinator(
                TimeDomainId,
                TimeDomainVersion,
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
                    _manualProvider ??= new ManualTimeProvider("manual.primary");
                    _manualProvider.Set(_manualSeconds, _manualRate);
                    return _manualProvider;
                case ShowTimePrimaryMode.Component:
                    return _primaryProvider as IShowTimeProvider;
                default:
                    return _unityProvider ??= new UnityTimeProvider("unity.unscaled.primary", _unityTime);
            }
        }

        private int ComputeConfigurationHash()
        {
            unchecked
            {
                var hash = 17;
                hash = hash * 31 + StringComparer.Ordinal.GetHashCode(TimeDomainId);
                hash = hash * 31 + TimeDomainVersion;
                hash = hash * 31 + (int)_negativeTimePolicy;
                hash = hash * 31 + (int)_primaryMode;
                hash = hash * 31 + (_primaryProvider != null ? _primaryProvider.GetInstanceID() : 0);
                hash = hash * 31 + (_tempoMap != null ? _tempoMap.GetInstanceID() : 0);
                hash = hash * 31 + _compatibilityBpm.GetHashCode();
                hash = hash * 31 + _compatibilityBeatsPerBar;
                hash = hash * 31 + _compatibilityOffsetSeconds.GetHashCode();
                return hash;
            }
        }

        private int ComputeCompatibilityTempoVersion()
        {
            unchecked
            {
                var bpmBits = BitConverter.DoubleToInt64Bits(_compatibilityBpm);
                var offsetBits = BitConverter.DoubleToInt64Bits(_compatibilityOffsetSeconds);
                var hash = 17;
                hash = hash * 31 + (int)(bpmBits ^ (bpmBits >> 32));
                hash = hash * 31 + _compatibilityBeatsPerBar;
                hash = hash * 31 + (int)(offsetBits ^ (offsetBits >> 32));
                hash &= int.MaxValue;
                return hash == 0 ? 1 : hash;
            }
        }
    }

    internal sealed class UnityUnscaledTimeSource : IUnscaledTimeSource
    {
        public double Seconds => UnityEngine.Time.unscaledTimeAsDouble;
    }

    internal sealed class UnityTimeProvider : IShowTimeProvider
    {
        private readonly IUnscaledTimeSource _time;
        private long _sequence;

        public UnityTimeProvider(string providerId, IUnscaledTimeSource time)
        {
            ProviderId = providerId;
            _time = time;
        }

        public string ProviderId { get; }

        public ShowTimeProviderSample Sample() => new(
            ProviderId,
            _time.Seconds,
            1d,
            FanlightClockStatus.Ready,
            FanlightTimeDiscontinuity.None,
            ++_sequence);
    }

    internal sealed class ManualTimeProvider : IShowTimeProvider
    {
        private double _seconds;
        private double _rate;
        private long _sequence;

        public ManualTimeProvider(string providerId) => ProviderId = providerId;
        public string ProviderId { get; }

        public void Set(double seconds, double rate)
        {
            _seconds = seconds;
            _rate = rate;
        }

        public ShowTimeProviderSample Sample() => new(
            ProviderId,
            _seconds,
            _rate,
            _rate == 0d ? FanlightClockStatus.Holding : FanlightClockStatus.Ready,
            FanlightTimeDiscontinuity.None,
            ++_sequence);
    }
}

using System;
using PrismFanlight.Core;

namespace PrismFanlight.Time
{
    public sealed class ShowTimeCoordinator : IShowTimeCoordinator
    {
        public const string FallbackProviderId = "unity.unscaled.fallback";

        private readonly IShowTimeProvider _primary;
        private readonly IShowTempoMapResolver _tempoMap;
        private readonly IUnscaledTimeSource _unscaledTime;
        private long _lastEvaluationId = long.MinValue;
        private bool _hasCachedResult;
        private bool _cachedSuccess;
        private FanlightShowTimeSample _cachedSample;
        private FanlightShowTimeFault _cachedFault;
        private bool _hasPrimaryAnchor;
        private ShowTimeProviderSample _lastPrimary;
        private bool _fallbackActive;
        private double _fallbackStartUnitySeconds;
        private bool _primaryAvailable;
        private bool _primaryAvailabilityReported;
        private ShowTimeProviderSample _availablePrimary;
        private long _sequence;
        private bool _reacquireTransitionPending;

        public ShowTimeCoordinator(
            string timeDomainId,
            int timeDomainVersion,
            ShowNegativeTimePolicy negativeTimePolicy,
            IShowTimeProvider primary,
            IShowTempoMapResolver tempoMap,
            IUnscaledTimeSource unscaledTime)
        {
            if (string.IsNullOrWhiteSpace(timeDomainId)) throw new ArgumentException("Time Domain ID is required.", nameof(timeDomainId));
            TimeDomainId = timeDomainId;
            TimeDomainVersion = Math.Max(1, timeDomainVersion);
            NegativeTimePolicy = negativeTimePolicy;
            _primary = primary ?? throw new ArgumentNullException(nameof(primary));
            _tempoMap = tempoMap ?? throw new ArgumentNullException(nameof(tempoMap));
            _unscaledTime = unscaledTime ?? throw new ArgumentNullException(nameof(unscaledTime));
        }

        public string TimeDomainId { get; }
        public int TimeDomainVersion { get; }
        public ShowNegativeTimePolicy NegativeTimePolicy { get; }
        public bool IsFallbackActive => _fallbackActive;
        public bool IsPrimaryAvailable => _primaryAvailable;

        public bool TrySample(long evaluationId, out FanlightShowTimeSample sample, out FanlightShowTimeFault fault)
        {
            if (_hasCachedResult && evaluationId == _lastEvaluationId)
            {
                sample = _cachedSample;
                fault = _cachedFault;
                return _cachedSuccess;
            }

            if (_hasCachedResult && evaluationId < _lastEvaluationId)
            {
                sample = default;
                fault = FanlightShowTimeFault.EvaluationOrderInvalid;
                return false;
            }

            _lastEvaluationId = evaluationId;
            _hasCachedResult = true;
            ShowTimeProviderSample primarySample;
            try
            {
                primarySample = _primary.Sample();
            }
            catch
            {
                primarySample = new ShowTimeProviderSample(
                    _primary.ProviderId,
                    double.NaN,
                    double.NaN,
                    FanlightClockStatus.Faulted,
                    FanlightTimeDiscontinuity.None,
                    0L);
            }

            var primaryValid = primarySample.IsValid;

            try
            {
                if (_fallbackActive && _reacquireTransitionPending)
                {
                    _reacquireTransitionPending = false;
                    if (primaryValid)
                    {
                        var reacquiredSample = CreateSample(
                            primarySample,
                            false,
                            true,
                            FanlightTimeDiscontinuity.AuthorityChanged);
                        _fallbackActive = false;
                        _hasPrimaryAnchor = true;
                        _lastPrimary = primarySample;
                        _primaryAvailable = true;
                        _primaryAvailabilityReported = true;
                        return CacheSuccess(reacquiredSample, out sample, out fault);
                    }
                }

                if (!_fallbackActive)
                {
                    if (primaryValid)
                    {
                        _hasPrimaryAnchor = true;
                        _lastPrimary = primarySample;
                        _primaryAvailable = true;
                        _primaryAvailabilityReported = true;
                        return CacheSuccess(CreateSample(primarySample, false, true, primarySample.Discontinuity), out sample, out fault);
                    }

                    _primaryAvailable = false;
                    _primaryAvailabilityReported = false;
                    if (!_hasPrimaryAnchor)
                    {
                        var invalidFault = primarySample.Status == FanlightClockStatus.Faulted
                            ? FanlightShowTimeFault.InvalidPrimarySample
                            : FanlightShowTimeFault.PrimaryUnavailable;
                        return CacheFailure(invalidFault, out sample, out fault);
                    }

                    _fallbackActive = true;
                    _fallbackStartUnitySeconds = _unscaledTime.Seconds;
                    return CacheSuccess(CreateFallback(FanlightTimeDiscontinuity.AuthorityChanged), out sample, out fault);
                }

                if (primaryValid)
                {
                    _availablePrimary = primarySample;
                    _primaryAvailable = true;
                    var discontinuity = _primaryAvailabilityReported
                        ? FanlightTimeDiscontinuity.None
                        : FanlightTimeDiscontinuity.Reconnected;
                    _primaryAvailabilityReported = true;
                    return CacheSuccess(CreateFallback(discontinuity), out sample, out fault);
                }

                _primaryAvailable = false;
                _primaryAvailabilityReported = false;
                return CacheSuccess(CreateFallback(FanlightTimeDiscontinuity.None), out sample, out fault);
            }
            catch (InvalidOperationException)
            {
                return CacheFailure(FanlightShowTimeFault.TempoMapUnavailable, out sample, out fault);
            }
            catch (ArgumentException)
            {
                return CacheFailure(FanlightShowTimeFault.TempoMapUnavailable, out sample, out fault);
            }
        }

        public bool TryRequestPrimaryReacquire(out string failureCode)
        {
            if (_reacquireTransitionPending)
            {
                failureCode = "ReacquirePending";
                return false;
            }

            if (!_fallbackActive)
            {
                failureCode = "NotFallbackActive";
                return false;
            }

            if (!_primaryAvailable || !_availablePrimary.IsValid)
            {
                failureCode = "PrimaryUnavailable";
                return false;
            }

            _reacquireTransitionPending = true;
            failureCode = string.Empty;
            return true;
        }

        private FanlightShowTimeSample CreateFallback(FanlightTimeDiscontinuity discontinuity)
        {
            var seconds = _lastPrimary.Seconds + (_unscaledTime.Seconds - _fallbackStartUnitySeconds) * _lastPrimary.Rate;
            var provider = new ShowTimeProviderSample(
                FallbackProviderId,
                seconds,
                _lastPrimary.Rate,
                _lastPrimary.Rate == 0d ? FanlightClockStatus.Holding : FanlightClockStatus.Ready,
                discontinuity,
                0L);
            return CreateSample(provider, true, _primaryAvailable, discontinuity);
        }

        private FanlightShowTimeSample CreateSample(
            ShowTimeProviderSample provider,
            bool fallback,
            bool primaryAvailable,
            FanlightTimeDiscontinuity discontinuity)
        {
            var seconds = NegativeTimePolicy == ShowNegativeTimePolicy.ClampToZero
                ? Math.Max(0d, provider.Seconds)
                : provider.Seconds;
            var musical = _tempoMap.Evaluate(seconds);
            if (!IsFinite(musical.Seconds) || Math.Abs(musical.Seconds - seconds) > 1e-9)
                throw new InvalidOperationException("Tempo Map returned an inconsistent musical position.");
            return new FanlightShowTimeSample(
                TimeDomainId,
                TimeDomainVersion,
                provider.ProviderId,
                _tempoMap.TempoMapId,
                _tempoMap.Version,
                seconds,
                provider.Rate,
                provider.Status,
                discontinuity,
                ++_sequence,
                fallback,
                primaryAvailable,
                musical);
        }

        private bool CacheSuccess(FanlightShowTimeSample value, out FanlightShowTimeSample sample, out FanlightShowTimeFault fault)
        {
            _cachedSuccess = true;
            _cachedSample = value;
            _cachedFault = FanlightShowTimeFault.None;
            sample = value;
            fault = FanlightShowTimeFault.None;
            return true;
        }

        private bool CacheFailure(FanlightShowTimeFault value, out FanlightShowTimeSample sample, out FanlightShowTimeFault fault)
        {
            _cachedSuccess = false;
            _cachedSample = default;
            _cachedFault = value;
            sample = default;
            fault = value;
            return false;
        }

        private static bool IsFinite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
    }
}

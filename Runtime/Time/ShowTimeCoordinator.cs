using System;
using PrismFanlight.Core;

namespace PrismFanlight.Time
{
    internal sealed class ShowTimeCoordinator
    {
        // Fields

        private readonly IShowTimeProvider _primary;
        private long _lastEvaluationId = long.MinValue;
        private bool _hasCachedResult;
        private bool _cachedSuccess;
        private FanlightClockSample _cachedSample;
        private FanlightShowTimeFault _cachedFault;
        private bool _hasPrimaryAnchor;
        private ShowTimeProviderSample _lastPrimary;
        private bool _fallbackActive;
        private double _fallbackStartUnitySeconds;
        private bool _primaryAvailable;
        private bool _primaryAvailabilityReported;
        private ShowTimeProviderSample _availablePrimary;
        private bool _reacquireTransitionPending;


        // Properties

        private ShowNegativeTimePolicy NegativeTimePolicy { get; }

        internal bool IsFallbackActive => _fallbackActive;

        internal bool IsPrimaryAvailable => _primaryAvailable;


        // Methods

        internal ShowTimeCoordinator(
            ShowNegativeTimePolicy negativeTimePolicy,
            IShowTimeProvider primary)
        {
            NegativeTimePolicy = negativeTimePolicy;
            _primary = primary ?? throw new ArgumentNullException(nameof(primary));
        }

        internal bool TrySampleClock(long evaluationId, out FanlightClockSample sample, out FanlightShowTimeFault fault)
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
                    double.NaN,
                    double.NaN,
                    FanlightClockStatus.Faulted,
                    FanlightTimeDiscontinuity.None);
            }

            var primaryValid = primarySample.IsValid;

            if (_fallbackActive && _reacquireTransitionPending)
            {
                _reacquireTransitionPending = false;

                if (primaryValid)
                {
                    var reacquiredSample = CreateClock(
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

                    return CacheSuccess(CreateClock(primarySample, false, true, primarySample.Discontinuity), out sample, out fault);
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
                _fallbackStartUnitySeconds = UnityEngine.Time.unscaledTimeAsDouble;

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

        internal bool TryRequestPrimaryReacquire(out string failureCode)
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


        private FanlightClockSample CreateFallback(FanlightTimeDiscontinuity discontinuity)
        {
            var seconds = _lastPrimary.Seconds + (UnityEngine.Time.unscaledTimeAsDouble - _fallbackStartUnitySeconds) * _lastPrimary.Rate;

            var provider = new ShowTimeProviderSample(
                seconds,
                _lastPrimary.Rate,
                _lastPrimary.Rate == 0d ? FanlightClockStatus.Holding : FanlightClockStatus.Ready,
                discontinuity);

            return CreateClock(provider, true, _primaryAvailable, discontinuity);
        }

        private FanlightClockSample CreateClock(
            ShowTimeProviderSample provider,
            bool fallback,
            bool primaryAvailable,
            FanlightTimeDiscontinuity discontinuity)
        {
            var seconds = 0d;

            if (NegativeTimePolicy == ShowNegativeTimePolicy.ClampToZero)
            {
                seconds = Math.Max(0d, provider.Seconds);
            }

            if (NegativeTimePolicy == ShowNegativeTimePolicy.AllowPreroll)
            {
                seconds = provider.Seconds;
            }

            return new FanlightClockSample(
                seconds,
                provider.Rate,
                provider.Status,
                discontinuity,
                fallback,
                primaryAvailable);
        }

        private bool CacheSuccess(FanlightClockSample value, out FanlightClockSample sample, out FanlightShowTimeFault fault)
        {
            _cachedSuccess = true;
            _cachedSample = value;
            _cachedFault = FanlightShowTimeFault.None;
            sample = value;
            fault = FanlightShowTimeFault.None;
            return true;
        }

        private bool CacheFailure(FanlightShowTimeFault value, out FanlightClockSample sample, out FanlightShowTimeFault fault)
        {
            _cachedSuccess = false;
            _cachedSample = default;
            _cachedFault = value;
            sample = default;
            fault = value;
            return false;
        }
    }
}

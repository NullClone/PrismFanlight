using System;
using System.Collections.Generic;

namespace PrismFanlight.Core
{
    internal sealed class FanlightShowEvaluator
    {
        // Fields

        private const int DefaultContributionCapacity = 32;

        private FanlightShowContribution[] _activeContributions;
        private readonly HashSet<int> _activeTrackOrders;


        // Methods

        internal FanlightShowEvaluator(int initialContributionCapacity = DefaultContributionCapacity)
        {
            if (initialContributionCapacity < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(initialContributionCapacity));
            }

            _activeContributions = initialContributionCapacity == 0
                ? Array.Empty<FanlightShowContribution>()
                : new FanlightShowContribution[initialContributionCapacity];

            _activeTrackOrders = new HashSet<int>(initialContributionCapacity);
        }

        internal FanlightShowSample Evaluate(in FanlightShowEvaluationRequest request)
        {
            if (!request.Time.IsComplete)
            {
                throw new ArgumentException("A complete time sample is required.", nameof(request));
            }

            var state = FanlightShowStatePatcher.Validate(request.BaseState);
            var activeCount = 0;

            try
            {
                activeCount = CollectActive(request.Contributions.Span, request.Time.Seconds);

                Array.Sort(_activeContributions, 0, activeCount, ContributionComparer.Instance);

                ValidateUniqueTrackOrders(activeCount);

                for (var i = 0; i < activeCount; i++)
                {
                    var contribution = _activeContributions[i];
                    state = FanlightShowStatePatcher.Apply(state, contribution.Patch, contribution.Weight);
                }

                state = FanlightShowStatePatcher.Validate(state);
                var animationSeconds = Quantize(request.Time.Seconds, request.Options);

                return new FanlightShowSample(
                    request.Time.Seconds,
                    animationSeconds,
                    request.Time.MusicalPosition,
                    request.Time.Discontinuity,
                    state);
            }
            finally
            {
                if (activeCount > 0)
                {
                    Array.Clear(_activeContributions, 0, activeCount);
                }

                _activeTrackOrders.Clear();
            }
        }

        private int CollectActive(ReadOnlySpan<FanlightShowContribution> contributions, double seconds)
        {
            var count = 0;

            for (var i = 0; i < contributions.Length; i++)
            {
                if (contributions[i].IsActive(seconds)) count++;
            }

            EnsureContributionCapacity(count);

            var destination = 0;
            for (var i = 0; i < contributions.Length; i++)
            {
                if (contributions[i].IsActive(seconds))
                {
                    _activeContributions[destination++] = contributions[i];
                }
            }

            return count;
        }

        private void EnsureContributionCapacity(int requiredCapacity)
        {
            if (_activeContributions.Length >= requiredCapacity) return;

            var capacity = Math.Max(DefaultContributionCapacity, _activeContributions.Length);

            while (capacity < requiredCapacity)
            {
                capacity *= 2;
            }

            Array.Resize(ref _activeContributions, capacity);
        }

        private void ValidateUniqueTrackOrders(int contributionCount)
        {
            _activeTrackOrders.EnsureCapacity(contributionCount);

            for (var i = 0; i < contributionCount; i++)
            {
                var trackOrder = _activeContributions[i].TrackOrder;

                if (!_activeTrackOrders.Add(trackOrder))
                {
                    throw new InvalidOperationException($"Duplicate active Timeline Track Order: {trackOrder}");
                }
            }
        }

        private static double Quantize(double showSeconds, FanlightEvaluationOptions options)
        {
            if (options.AnimationSampleRate <= 0d) return showSeconds;

            return Math.Floor(showSeconds * options.AnimationSampleRate + options.QuantizationEpsilon) / options.AnimationSampleRate;
        }


        private sealed class ContributionComparer : IComparer<FanlightShowContribution>
        {
            internal static readonly ContributionComparer Instance = new();

            public int Compare(FanlightShowContribution left, FanlightShowContribution right)
            {
                var priority = left.TrackPriority.CompareTo(right.TrackPriority);

                if (priority != 0) return priority;

                return left.TrackOrder.CompareTo(right.TrackOrder);
            }
        }
    }
}

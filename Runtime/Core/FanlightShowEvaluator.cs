using System;
using System.Collections.Generic;

namespace PrismFanlight.Core
{
    internal sealed class FanlightShowEvaluator
    {
        // Fields

        private const int DefaultContributionCapacity = 32;

        private FanlightShowContribution[] _activeContributions;
        private readonly HashSet<string> _activeSourceIds;


        // Properties

        internal FanlightShowEvaluator(int initialContributionCapacity = DefaultContributionCapacity)
        {
            if (initialContributionCapacity < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(initialContributionCapacity));
            }

            _activeContributions = initialContributionCapacity == 0
                ? Array.Empty<FanlightShowContribution>()
                : new FanlightShowContribution[initialContributionCapacity];

            _activeSourceIds = new HashSet<string>(initialContributionCapacity, StringComparer.Ordinal);
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

                ValidateUniqueSources(activeCount);

                for (var i = 0; i < activeCount; i++)
                {
                    var contribution = _activeContributions[i];
                    state = FanlightShowStatePatcher.Apply(state, contribution.Patch, contribution.Weight);
                }

                state = FanlightShowStatePatcher.Validate(state);
                var animationSeconds = Quantize(request.Time.Seconds, request.Options);

                return new FanlightShowSample(
                    request.Time.Sequence,
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

                _activeSourceIds.Clear();
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
            while (capacity < requiredCapacity) capacity *= 2;
            Array.Resize(ref _activeContributions, capacity);
        }

        private void ValidateUniqueSources(int contributionCount)
        {
            _activeSourceIds.EnsureCapacity(contributionCount);
            for (var i = 0; i < contributionCount; i++)
            {
                var sourceId = _activeContributions[i].SourceId;
                if (!_activeSourceIds.Add(sourceId))
                    throw new InvalidOperationException($"Duplicate active contribution source ID: {sourceId}");
            }
        }

        private static double Quantize(double showSeconds, FanlightEvaluationOptions options)
        {
            if (options.AnimationSampleRate <= 0d) return showSeconds;
            return Math.Floor(showSeconds * options.AnimationSampleRate + options.QuantizationEpsilon)
                   / options.AnimationSampleRate;
        }

        private sealed class ContributionComparer : IComparer<FanlightShowContribution>
        {
            internal static readonly ContributionComparer Instance = new();

            public int Compare(FanlightShowContribution left, FanlightShowContribution right)
            {
                var layer = left.Layer.CompareTo(right.Layer);
                if (layer != 0) return layer;
                var priority = left.Priority.CompareTo(right.Priority);
                if (priority != 0) return priority;
                return string.Compare(left.SourceId, right.SourceId, StringComparison.Ordinal);
            }
        }
    }
}

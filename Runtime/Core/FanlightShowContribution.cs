using System;

namespace PrismFanlight.Core
{
    internal readonly struct FanlightShowContribution
    {
        internal string SourceId { get; }
        internal FanlightContributionLayer Layer { get; }
        internal int Priority { get; }
        internal double StartSeconds { get; }
        internal double EndSeconds { get; }
        internal float Weight { get; }
        internal FanlightShowPatch Patch { get; }

        internal FanlightShowContribution(
            string sourceId,
            FanlightContributionLayer layer,
            int priority,
            double startSeconds,
            double endSeconds,
            float weight,
            FanlightShowPatch patch)
        {
            if (string.IsNullOrWhiteSpace(sourceId)) throw new ArgumentException("Stable source ID is required.", nameof(sourceId));
            if (layer is not FanlightContributionLayer.Base
                and not FanlightContributionLayer.Timeline
                and not FanlightContributionLayer.Cue
                and not FanlightContributionLayer.Live
                and not FanlightContributionLayer.Safety)
                throw new ArgumentOutOfRangeException(nameof(layer));
            FanlightStateValidation.RequireFinite(startSeconds, nameof(startSeconds));
            if (double.IsNaN(endSeconds) || double.IsNegativeInfinity(endSeconds) || endSeconds <= startSeconds)
                throw new ArgumentOutOfRangeException(nameof(endSeconds));
            if (!FanlightStateValidation.IsFinite(weight)) throw new ArgumentOutOfRangeException(nameof(weight));
            SourceId = sourceId;
            Layer = layer;
            Priority = priority;
            StartSeconds = startSeconds;
            EndSeconds = endSeconds;
            Weight = weight < 0f ? 0f : weight > 1f ? 1f : weight;
            Patch = patch;
        }

        internal bool IsActive(double seconds) => StartSeconds <= seconds && seconds < EndSeconds;
    }
}

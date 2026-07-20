using System;

namespace PrismFanlight.Core
{
    internal readonly struct FanlightShowContribution
    {
        // Properties

        internal string SourceId { get; }

        internal int Priority { get; }

        internal double StartSeconds { get; }

        internal double EndSeconds { get; }

        internal float Weight { get; }

        internal FanlightShowPatch Patch { get; }


        // Methods

        internal FanlightShowContribution(
            string sourceId,
            int priority,
            double startSeconds,
            double endSeconds,
            float weight,
            FanlightShowPatch patch)
        {
            if (string.IsNullOrWhiteSpace(sourceId))
            {
                throw new ArgumentException("Stable source ID is required.", nameof(sourceId));
            }

            FanlightStateValidation.RequireFinite(startSeconds, nameof(startSeconds));

            if (double.IsNaN(endSeconds) || double.IsNegativeInfinity(endSeconds) || endSeconds <= startSeconds)
            {
                throw new ArgumentOutOfRangeException(nameof(endSeconds));
            }

            if (!FanlightStateValidation.IsFinite(weight))
            {
                throw new ArgumentOutOfRangeException(nameof(weight));
            }

            SourceId = sourceId;
            Priority = priority;
            StartSeconds = startSeconds;
            EndSeconds = endSeconds;
            Weight = weight < 0f ? 0f : weight > 1f ? 1f : weight;
            Patch = patch;
        }

        internal bool IsActive(double seconds) => StartSeconds <= seconds && seconds < EndSeconds;
    }
}

using System;

namespace PrismFanlight.Core
{
    internal readonly struct FanlightShowContribution
    {
        // Properties

        internal int TrackPriority { get; }

        internal int TrackOrder { get; }

        internal double StartSeconds { get; }

        internal double EndSeconds { get; }

        internal float Weight { get; }

        internal FanlightShowPatch Patch { get; }


        // Methods

        internal FanlightShowContribution(
            int trackPriority,
            int trackOrder,
            double startSeconds,
            double endSeconds,
            float weight,
            FanlightShowPatch patch)
        {
            FanlightStateValidation.RequireFinite(startSeconds, nameof(startSeconds));

            if (double.IsNaN(endSeconds) || double.IsNegativeInfinity(endSeconds) || endSeconds <= startSeconds)
            {
                throw new ArgumentOutOfRangeException(nameof(endSeconds));
            }

            if (!FanlightStateValidation.IsFinite(weight))
            {
                throw new ArgumentOutOfRangeException(nameof(weight));
            }

            TrackPriority = trackPriority;
            TrackOrder = trackOrder;
            StartSeconds = startSeconds;
            EndSeconds = endSeconds;
            Weight = weight < 0f ? 0f : weight > 1f ? 1f : weight;
            Patch = patch;
        }

        internal bool IsActive(double seconds) => StartSeconds <= seconds && seconds < EndSeconds;
    }
}

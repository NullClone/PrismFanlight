using System;

namespace PrismFanlight.Core
{
    internal readonly struct FanlightShowContribution
    {
        // Properties

        internal FanlightSequenceContext SequenceContext { get; }

        internal int TrackPriority { get; }

        internal int TrackOrder { get; }

        internal double StartSeconds { get; }

        internal double EndSeconds { get; }

        internal float Weight { get; }

        internal FanlightShowPatch Patch { get; }


        // Methods

        internal FanlightShowContribution(
            FanlightSequenceContext sequenceContext,
            int trackPriority,
            int trackOrder,
            double startSeconds,
            double endSeconds,
            float weight,
            FanlightShowPatch patch)
        {
            if (sequenceContext == null || sequenceContext.IsReleased)
            {
                throw new ArgumentException("An active Sequence Context is required.", nameof(sequenceContext));
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

            SequenceContext = sequenceContext;
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

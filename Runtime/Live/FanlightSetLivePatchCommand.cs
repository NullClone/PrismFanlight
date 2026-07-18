using System;
using PrismFanlight.Core;

namespace PrismFanlight.Live
{
    internal readonly struct FanlightSetLivePatchCommand
    {
        internal string SourceId { get; }

        internal FanlightShowPatch Patch { get; }

        internal int Priority { get; }

        internal double TransitionSeconds { get; }

        internal FanlightSetLivePatchCommand(
            string sourceId,
            FanlightShowPatch patch,
            int priority,
            double transitionSeconds)
        {
            if (string.IsNullOrWhiteSpace(sourceId))
            {
                throw new ArgumentException("Source ID is required.", nameof(sourceId));
            }

            if (double.IsNaN(transitionSeconds) || double.IsInfinity(transitionSeconds) || transitionSeconds < 0d)
            {
                throw new ArgumentOutOfRangeException(nameof(transitionSeconds));
            }

            SourceId = sourceId;
            Patch = patch;
            Priority = priority;
            TransitionSeconds = transitionSeconds;
        }
    }
}

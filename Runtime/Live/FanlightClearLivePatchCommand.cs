using System;

namespace PrismFanlight.Live
{
    internal readonly struct FanlightClearLivePatchCommand
    {
        internal string SourceId { get; }
        internal double TransitionSeconds { get; }

        internal FanlightClearLivePatchCommand(string sourceId, double transitionSeconds)
        {
            if (string.IsNullOrWhiteSpace(sourceId)) throw new ArgumentException("Source ID is required.", nameof(sourceId));
            if (double.IsNaN(transitionSeconds) || double.IsInfinity(transitionSeconds) || transitionSeconds < 0d)
                throw new ArgumentOutOfRangeException(nameof(transitionSeconds));
            SourceId = sourceId;
            TransitionSeconds = transitionSeconds;
        }
    }
}

using System;

namespace PrismFanlight.Live
{
    internal readonly struct FanlightShowEventLogEntry
    {
        internal ulong Sequence { get; }

        internal double ShowSeconds { get; }

        internal string StableEventId { get; }

        internal FanlightShowCommand Command { get; }


        internal FanlightShowEventLogEntry(
            ulong sequence,
            double showSeconds,
            string stableEventId,
            FanlightShowCommand command)
        {
            if (sequence == 0UL)
            {
                throw new ArgumentOutOfRangeException(nameof(sequence));
            }

            if (double.IsNaN(showSeconds) || double.IsInfinity(showSeconds))
            {
                throw new ArgumentOutOfRangeException(nameof(showSeconds));
            }

            if (string.IsNullOrWhiteSpace(stableEventId))
            {
                throw new ArgumentException("Stable event ID is required.", nameof(stableEventId));
            }

            command.Validate();

            Sequence = sequence;
            ShowSeconds = showSeconds;
            StableEventId = stableEventId;
            Command = command;
        }
    }
}

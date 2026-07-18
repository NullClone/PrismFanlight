using System;

namespace PrismFanlight.Live
{
    internal readonly struct FanlightCancelCueCommand
    {
        internal string CueId { get; }

        internal FanlightCancelCueCommand(string cueId)
        {
            if (string.IsNullOrWhiteSpace(cueId)) throw new ArgumentException("Cue ID is required.", nameof(cueId));
            CueId = cueId;
        }
    }
}

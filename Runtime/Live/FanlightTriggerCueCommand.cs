using System;

namespace PrismFanlight.Live
{
    internal readonly struct FanlightTriggerCueCommand
    {
        internal string CueId { get; }


        internal FanlightTriggerCueCommand(string cueId)
        {
            if (string.IsNullOrWhiteSpace(cueId))
            {
                throw new ArgumentException("Cue ID is required.", nameof(cueId));
            }

            CueId = cueId;
        }
    }
}

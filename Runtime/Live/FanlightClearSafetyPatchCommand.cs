using System;

namespace PrismFanlight.Live
{
    internal readonly struct FanlightClearSafetyPatchCommand
    {
        internal string SourceId { get; }

        internal FanlightClearSafetyPatchCommand(string sourceId)
        {
            if (string.IsNullOrWhiteSpace(sourceId)) throw new ArgumentException("Source ID is required.", nameof(sourceId));
            SourceId = sourceId;
        }
    }
}

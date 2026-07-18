using System;
using PrismFanlight.Core;

namespace PrismFanlight.Live
{
    internal readonly struct FanlightSetSafetyPatchCommand
    {
        internal string SourceId { get; }

        internal FanlightShowPatch Patch { get; }


        internal FanlightSetSafetyPatchCommand(string sourceId, FanlightShowPatch patch)
        {
            if (string.IsNullOrWhiteSpace(sourceId))
            {
                throw new ArgumentException("Source ID is required.", nameof(sourceId));
            }

            SourceId = sourceId;
            Patch = patch;
        }
    }
}

using PrismFanlight.Core;

namespace PrismFanlight.Timeline
{
    internal readonly struct FanlightTimelineClipSample
    {
        internal string StableClipId { get; }
        internal FanlightShowPatch Patch { get; }
        internal float Weight { get; }
        internal int Priority { get; }

        internal FanlightTimelineClipSample(
            string stableClipId,
            FanlightShowPatch patch,
            float weight,
            int priority)
        {
            StableClipId = stableClipId;
            Patch = patch;
            Weight = weight;
            Priority = priority;
        }
    }
}

namespace PrismFanlight.Timeline
{
    internal readonly struct FanlightTimelineClipSample
    {
        internal string StableClipId { get; }

        internal FanlightTimelineClipValue Value { get; }

        internal float Weight { get; }


        internal FanlightTimelineClipSample(
            string stableClipId,
            FanlightTimelineClipValue value,
            float weight)
        {
            StableClipId = stableClipId;
            Value = value;
            Weight = weight;
        }
    }
}

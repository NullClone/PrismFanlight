namespace PrismFanlight.Timeline
{
    internal readonly struct FanlightTimelineClipSample
    {
        internal double StartSeconds { get; }

        internal FanlightTimelineClipValue Value { get; }

        internal float Weight { get; }


        internal FanlightTimelineClipSample(
            double startSeconds,
            FanlightTimelineClipValue value,
            float weight)
        {
            StartSeconds = startSeconds;
            Value = value;
            Weight = weight;
        }
    }
}

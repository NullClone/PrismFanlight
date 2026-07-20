namespace PrismFanlight.Timeline
{
    internal readonly struct FanlightTimelineClipRange
    {
        internal string StableClipId { get; }

        internal double EndSeconds { get; }

        internal double HoldEndSeconds { get; }


        internal FanlightTimelineClipRange(
            string stableClipId,
            double endSeconds,
            double holdEndSeconds)
        {
            StableClipId = stableClipId;
            EndSeconds = endSeconds;
            HoldEndSeconds = holdEndSeconds;
        }
    }
}

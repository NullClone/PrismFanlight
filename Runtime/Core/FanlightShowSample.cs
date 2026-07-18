namespace PrismFanlight.Core
{
    internal readonly struct FanlightShowSample
    {
        internal long SampleSequence { get; }
        internal double ShowSeconds { get; }
        internal double AnimationSampleSeconds { get; }
        internal FanlightMusicalPosition MusicalPosition { get; }
        internal FanlightTimeDiscontinuity Discontinuity { get; }
        internal FanlightShowState State { get; }

        internal FanlightShowSample(
            long sampleSequence,
            double showSeconds,
            double animationSampleSeconds,
            FanlightMusicalPosition musicalPosition,
            FanlightTimeDiscontinuity discontinuity,
            FanlightShowState state)
        {
            SampleSequence = sampleSequence;
            ShowSeconds = showSeconds;
            AnimationSampleSeconds = animationSampleSeconds;
            MusicalPosition = musicalPosition;
            Discontinuity = discontinuity;
            State = state;
        }
    }
}

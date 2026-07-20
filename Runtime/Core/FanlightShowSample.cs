namespace PrismFanlight.Core
{
    internal readonly struct FanlightShowSample
    {
        // Properties

        internal double ShowSeconds { get; }

        internal double AnimationSampleSeconds { get; }

        internal FanlightMusicalPosition MusicalPosition { get; }

        internal FanlightTimeDiscontinuity Discontinuity { get; }

        internal FanlightShowState State { get; }


        // Methods

        internal FanlightShowSample(
            double showSeconds,
            double animationSampleSeconds,
            FanlightMusicalPosition musicalPosition,
            FanlightTimeDiscontinuity discontinuity,
            FanlightShowState state)
        {
            ShowSeconds = showSeconds;
            AnimationSampleSeconds = animationSampleSeconds;
            MusicalPosition = musicalPosition;
            Discontinuity = discontinuity;
            State = state;
        }
    }
}

using System;

namespace PrismFanlight.Core
{
    [Flags]
    internal enum FanlightRestFields
    {
        None = 0,
        Probability = 1 << 0,
        MotionLevel = 1 << 1,
        CycleSeconds = 1 << 2,
        DurationSeconds = 1 << 3,
        FadeSeconds = 1 << 4,
        PhaseRandomness = 1 << 5,
        All = Probability | MotionLevel | CycleSeconds | DurationSeconds | FadeSeconds | PhaseRandomness
    }
}

using System;

namespace PrismFanlight.Core
{
    [Flags]
    internal enum FanlightMotionFields
    {
        None = 0,
        Source = 1 << 0,
        BlockDelayXBeats = 1 << 3,
        BlockDelayYBeats = 1 << 4,
        All = Source | BlockDelayXBeats | BlockDelayYBeats
    }
}

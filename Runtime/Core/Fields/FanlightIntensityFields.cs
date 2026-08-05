using System;

namespace PrismFanlight.Core
{
    [Flags]
    internal enum FanlightIntensityFields
    {
        None = 0,
        BaseIntensity = 1 << 0,
        RandomIntensity = 1 << 1,
        Mask = 1 << 2,
        All = BaseIntensity | RandomIntensity | Mask
    }
}

using System;

namespace PrismFanlight.Core
{
    [Flags]
    internal enum FanlightPaletteFields
    {
        None = 0,
        Slot1 = 1 << 0,
        Slot2 = 1 << 1,
        Slot3 = 1 << 2,
        Slot4 = 1 << 3,
        Slot5 = 1 << 4,
        Slot6 = 1 << 5,
        GlobalIntensity = 1 << 6,
        RandomIntensity = 1 << 7,
        All = Slot1 | Slot2 | Slot3 | Slot4 | Slot5 | Slot6 | GlobalIntensity | RandomIntensity
    }
}

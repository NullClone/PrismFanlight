using System;

namespace PrismFanlight.Core
{
    [Flags]
    internal enum FanlightAudienceBodyFields
    {
        None = 0,
        Height = 1 << 0,
        Width = 1 << 1,
        HeadSize = 1 << 2,
        ShoulderHeightRatio = 1 << 3,
        ShoulderSideOffset = 1 << 4,
        ArmWidth = 1 << 5,
        ArmLengthLimit = 1 << 6,
        Bounce = 1 << 7,
        Sway = 1 << 8,
        All = Height | Width | HeadSize | ShoulderHeightRatio | ShoulderSideOffset | ArmWidth | ArmLengthLimit | Bounce | Sway
    }
}

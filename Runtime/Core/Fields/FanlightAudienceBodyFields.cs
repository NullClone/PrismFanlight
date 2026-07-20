using System;

namespace PrismFanlight.Core
{
    [Flags]
    internal enum FanlightAudienceBodyFields
    {
        None = 0,
        Height = 1 << 0,
        HeightVariation = 1 << 1,
        Width = 1 << 2,
        HeadSize = 1 << 3,
        ShoulderHeightRatio = 1 << 4,
        ShoulderSideOffset = 1 << 5,
        ArmWidth = 1 << 6,
        ArmLengthLimit = 1 << 7,
        UpperBodyLeanMaximumRadians = 1 << 8,
        UpperBodyLean = 1 << 9,
        Bounce = 1 << 10,
        Sway = 1 << 11,
        MotionSpeed = 1 << 12,
        LeanMotion = 1 << 13,
        All = Height | HeightVariation | Width | HeadSize | ShoulderHeightRatio | ShoulderSideOffset | ArmWidth | ArmLengthLimit | UpperBodyLeanMaximumRadians | UpperBodyLean | Bounce | Sway | MotionSpeed | LeanMotion
    }
}

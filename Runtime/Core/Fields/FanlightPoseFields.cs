using System;

namespace PrismFanlight.Core
{
    [Flags]
    internal enum FanlightPoseFields
    {
        None = 0,
        HandZone = 1 << 0,
        HandHeightOffset = 1 << 1,
        HandForwardOffset = 1 << 2,
        HandReachScale = 1 << 3,
        ArmLengthMinimum = 1 << 4,
        ArmLengthMaximum = 1 << 5,
        AngleMinimumRadians = 1 << 6,
        AngleMaximumRadians = 1 << 7,
        HorizontalRatio = 1 << 8,
        WristFrequencyMultiplier = 1 << 9,
        WristAngleRadians = 1 << 10,
        BodyLean = 1 << 11,
        All = HandZone | HandHeightOffset | HandForwardOffset | HandReachScale | ArmLengthMinimum | ArmLengthMaximum | AngleMinimumRadians | AngleMaximumRadians | HorizontalRatio | WristFrequencyMultiplier | WristAngleRadians | BodyLean
    }
}

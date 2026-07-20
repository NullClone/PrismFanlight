namespace PrismFanlight.Core
{
    internal enum FanlightShowTimeFault
    {
        None = 0,
        PrimaryUnavailable = 1,
        InvalidPrimarySample = 2,
        TempoMapUnavailable = 3,
        CoordinatorUnavailable = 4,
        EvaluationOrderInvalid = 5
    }
}

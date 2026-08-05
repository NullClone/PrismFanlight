namespace PrismFanlight.Core
{
    internal enum FanlightShowTimeFault
    {
        None = 0,
        PrimaryUnavailable = 1,
        InvalidPrimarySample = 2,
        InvalidTempoDefinition = 3,
        TempoConflict = 4,
        CoordinatorUnavailable = 5,
        EvaluationOrderInvalid = 6
    }
}

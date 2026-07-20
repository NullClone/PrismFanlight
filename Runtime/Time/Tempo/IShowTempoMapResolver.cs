using PrismFanlight.Core;

namespace PrismFanlight.Time
{
    internal interface IShowTempoMapResolver
    {
        FanlightMusicalPosition Evaluate(double seconds);
    }
}

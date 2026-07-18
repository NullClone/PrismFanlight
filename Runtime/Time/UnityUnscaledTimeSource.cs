namespace PrismFanlight.Time
{
    internal sealed class UnityUnscaledTimeSource : IUnscaledTimeSource
    {
        public double Seconds => UnityEngine.Time.unscaledTimeAsDouble;
    }
}

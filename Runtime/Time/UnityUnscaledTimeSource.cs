namespace PrismFanlight.Time
{
    internal sealed class UnityUnscaledTimeSource
    {
        public double Seconds => UnityEngine.Time.unscaledTimeAsDouble;
    }
}

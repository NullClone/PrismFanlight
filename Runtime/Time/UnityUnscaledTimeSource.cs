namespace PrismFanlight.Time
{
    internal sealed class UnityUnscaledTimeSource
    {
        internal double Seconds => UnityEngine.Time.unscaledTimeAsDouble;
    }
}

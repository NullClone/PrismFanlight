namespace PrismFanlight.Core
{
    internal interface IFanlightContributionSource
    {
        void Collect(double seconds, FanlightContributionBuffer destination);
    }
}

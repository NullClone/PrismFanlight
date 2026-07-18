using PrismFanlight.Core;

namespace PrismFanlight
{
    internal sealed class FanlightSingleContributionSource : IFanlightContributionSource
    {
        private FanlightShowContribution _contribution;

        internal string SourceId => _contribution.SourceId;

        internal FanlightSingleContributionSource(in FanlightShowContribution contribution) => Set(contribution);

        internal void Set(in FanlightShowContribution contribution) => _contribution = contribution;

        public void Collect(double seconds, FanlightContributionBuffer destination) => destination.Add(_contribution);
    }
}

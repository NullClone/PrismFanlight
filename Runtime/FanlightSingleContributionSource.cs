using PrismFanlight.Core;

namespace PrismFanlight
{
    public sealed class FanlightSingleContributionSource : IFanlightContributionSource
    {
        // Fields

        private FanlightContribution _contribution;

        public string SourceId => _contribution.SourceId;

        public FanlightContributionLayer Layer => _contribution.Layer;

        public int Priority => _contribution.Priority;


        // Methods

        public FanlightSingleContributionSource(in FanlightContribution contribution) => Set(contribution);

        public void Set(in FanlightContribution contribution) => _contribution = contribution;

        public void Collect(double seconds, FanlightContributionBuffer destination) => destination.Add(_contribution);
    }
}

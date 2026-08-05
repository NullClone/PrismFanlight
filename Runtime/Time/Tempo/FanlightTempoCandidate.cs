namespace PrismFanlight.Time
{
    internal readonly struct FanlightTempoCandidate
    {
        // Properties

        internal double SequenceLocalSeconds { get; }

        internal FanlightTempoRuntimeDefinition Definition { get; }


        // Methods

        internal FanlightTempoCandidate(double sequenceLocalSeconds, FanlightTempoRuntimeDefinition definition)
        {
            SequenceLocalSeconds = sequenceLocalSeconds;
            Definition = definition;
        }
    }
}

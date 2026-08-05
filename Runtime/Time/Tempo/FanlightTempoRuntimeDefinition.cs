using System;

namespace PrismFanlight.Time
{
    internal sealed class FanlightTempoRuntimeDefinition
    {
        // Properties

        internal ReadOnlyMemory<FanlightTempoSection> Sections { get; }


        // Methods

        internal FanlightTempoRuntimeDefinition(ReadOnlyMemory<FanlightTempoSection> sections)
        {
            Sections = sections.Span.ToArray();
        }
    }
}

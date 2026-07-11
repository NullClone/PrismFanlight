using System;

namespace PrismFanlight
{
    public enum FanlightTimelineBlendMode
    {
        Auto,
        Angle,
        Discrete
    }

    /// <summary>Excludes a settings field from Timeline cue overrides.</summary>
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class FanlightTimelineIgnoreAttribute : Attribute
    {
    }

    /// <summary>Overrides the automatic blend mode inferred from a field type.</summary>
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class FanlightTimelineBlendAttribute : Attribute
    {
        public FanlightTimelineBlendMode Mode { get; }

        public FanlightTimelineBlendAttribute(FanlightTimelineBlendMode mode)
        {
            Mode = mode;
        }
    }
}

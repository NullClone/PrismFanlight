using System;

namespace PrismFanlight
{
    public enum FanlightTimelineBlendMode
    {
        Auto,
        Angle,
        Discrete
    }

    [AttributeUsage(AttributeTargets.Field)]
    public sealed class FanlightTimelineIgnoreAttribute : Attribute { }

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

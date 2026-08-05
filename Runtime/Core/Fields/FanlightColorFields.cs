using System;

namespace PrismFanlight.Core
{
    [Flags]
    internal enum FanlightColorFields
    {
        None = 0,
        Source = 1 << 0,
        All = Source
    }
}

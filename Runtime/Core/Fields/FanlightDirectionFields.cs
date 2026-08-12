using System;

namespace PrismFanlight.Core
{
    [Flags]
    internal enum FanlightDirectionFields
    {
        None = 0,
        Mode = 1 << 0,
        Direction = 1 << 1,
        All = Mode | Direction
    }
}

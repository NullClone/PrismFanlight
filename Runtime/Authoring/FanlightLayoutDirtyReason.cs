using System;

namespace PrismFanlight.Authoring
{
    [Flags]
    public enum FanlightLayoutDirtyReason
    {
        None = 0,
        BlockPlacement = 1 << 0,
        GlobalGeometry = 1 << 1,
        Topology = 1 << 2,
        StableIdentity = 1 << 3,
        BakeSchema = 1 << 4
    }
}

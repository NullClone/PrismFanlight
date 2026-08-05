using PrismFanlight.Core;

namespace PrismFanlight.Rendering
{
    internal readonly struct FanlightGpuDispatchContext
    {
        // Properties

        internal FanlightRuntimeLayout Layout { get; }

        internal FanlightShowSample Sample { get; }

        internal FanlightFrameContext Frame { get; }


        // Methods

        internal FanlightGpuDispatchContext(
            FanlightRuntimeLayout layout,
            in FanlightShowSample sample,
            in FanlightFrameContext frame)
        {
            Layout = layout;
            Sample = sample;
            Frame = frame;
        }
    }
}

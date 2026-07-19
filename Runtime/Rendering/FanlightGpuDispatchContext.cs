using PrismFanlight.Core;
using UnityEngine;

namespace PrismFanlight.Rendering
{
    internal readonly struct FanlightGpuDispatchContext
    {
        // Properties

        internal FanlightRuntimeLayout Layout { get; }

        internal FanlightShowSample Sample { get; }

        internal FanlightFrameContext Frame { get; }

        internal FanlightCameraContext Camera { get; }

        internal Matrix4x4 WorldToLocal { get; }

        internal Bounds WorldBounds { get; }


        // Methods

        internal FanlightGpuDispatchContext(
            FanlightRuntimeLayout layout,
            in FanlightShowSample sample,
            in FanlightFrameContext frame,
            in FanlightCameraContext camera,
            Bounds worldBounds)
        {
            Layout = layout;
            Sample = sample;
            Frame = frame;
            Camera = camera;
            WorldToLocal = frame.LocalToWorld.inverse;
            WorldBounds = worldBounds;
        }
    }
}

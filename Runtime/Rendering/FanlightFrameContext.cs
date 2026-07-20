using UnityEngine;

namespace PrismFanlight.Rendering
{
    internal readonly struct FanlightFrameContext
    {
        // Properties

        internal long FrameId { get; }

        internal Matrix4x4 LocalToWorld { get; }

        internal Vector3 SwingTargetWorldPosition { get; }


        // Methods

        internal FanlightFrameContext(long frameId, Matrix4x4 localToWorld, Vector3 swingTargetWorldPosition)
        {
            FrameId = frameId;
            LocalToWorld = localToWorld;
            SwingTargetWorldPosition = swingTargetWorldPosition;
        }
    }
}

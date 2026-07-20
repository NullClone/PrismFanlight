using System;
using UnityEngine;

namespace PrismFanlight.Rendering
{
    internal readonly struct FanlightCameraContext
    {
        // Properties

        internal string CameraId { get; }

        internal Camera Camera { get; }

        internal Matrix4x4 ViewMatrix { get; }

        internal Matrix4x4 ProjectionMatrix { get; }

        internal Vector3 WorldPosition { get; }

        internal uint RenderingLayerMask { get; }

        internal bool CullingEnabled { get; }


        // Methods

        internal FanlightCameraContext(
            string cameraId,
            Camera camera,
            Matrix4x4 viewMatrix,
            Matrix4x4 projectionMatrix,
            Vector3 worldPosition,
            uint renderingLayerMask,
            bool cullingEnabled)
        {
            if (string.IsNullOrWhiteSpace(cameraId))
            {
                throw new ArgumentException("Camera ID is required.", nameof(cameraId));
            }

            CameraId = cameraId;
            Camera = camera;
            ViewMatrix = viewMatrix;
            ProjectionMatrix = projectionMatrix;
            WorldPosition = worldPosition;
            RenderingLayerMask = renderingLayerMask;
            CullingEnabled = cullingEnabled;
        }
    }
}

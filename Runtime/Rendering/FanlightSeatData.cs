using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace PrismFanlight.Rendering
{
    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    internal struct FanlightSeatData
    {
        internal Vector4 localPositionSeed;
        internal Vector4 planePositionBlock;
        internal int blockIndex;
        internal uint placementFlags;
        internal Vector2 padding;

        internal const int Stride = sizeof(float) * 12;

        internal FanlightSeatData(
            Vector3 localPosition,
            Vector2 planePosition,
            Vector2 block,
            int blockIndex,
            uint placementFlags,
            uint seed)
        {
            localPositionSeed = new Vector4(localPosition.x, localPosition.y, localPosition.z, seed);
            planePositionBlock = new Vector4(planePosition.x, planePosition.y, block.x, block.y);
            this.blockIndex = blockIndex;
            this.placementFlags = placementFlags;
            padding = Vector2.zero;
        }
    }

    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    internal struct FanlightBlockData
    {
        internal Vector4 localCenterRadius;
        internal Vector4 indexRange;

        internal const int Stride = sizeof(float) * 8;

        internal FanlightBlockData(Vector3 localCenter, float radius, int startIndex, int count)
        {
            localCenterRadius = new Vector4(localCenter.x, localCenter.y, localCenter.z, radius);
            indexRange = new Vector4(startIndex, count, 0.0f, 0.0f);
        }
    }
}

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
        internal int blockIndex;
        internal uint padding0;
        internal uint padding1;
        internal uint padding2;

        internal const int Stride = sizeof(float) * 8;

        internal FanlightSeatData(
            Vector3 localPosition,
            int blockIndex,
            uint seed)
        {
            localPositionSeed = new Vector4(localPosition.x, localPosition.y, localPosition.z, seed);
            this.blockIndex = blockIndex;
            padding0 = 0u;
            padding1 = 0u;
            padding2 = 0u;
        }
    }

    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    internal struct FanlightBlockData
    {
        internal Vector4 localCenterRadius;
        internal Vector2 effectCoordinate;
        internal int startIndex;
        internal int count;

        internal const int Stride = sizeof(float) * 8;

        internal FanlightBlockData(
            Vector3 localCenter,
            float radius,
            int startIndex,
            int count,
            Vector2 effectCoordinate)
        {
            localCenterRadius = new Vector4(localCenter.x, localCenter.y, localCenter.z, radius);
            this.effectCoordinate = effectCoordinate;
            this.startIndex = startIndex;
            this.count = count;
        }
    }
}

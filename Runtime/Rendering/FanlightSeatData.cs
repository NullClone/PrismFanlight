using System.Runtime.InteropServices;
using UnityEngine;

namespace PrismFanlight.Rendering
{
    [StructLayout(LayoutKind.Sequential)]
    public struct FanlightSeatData
    {
        public Vector4 localPositionSeed;
        public Vector4 planePositionBlock;

        public const int Stride = sizeof(float) * 8;

        public FanlightSeatData(Vector3 localPosition, Vector2 planePosition, Vector2 block, uint seed)
        {
            localPositionSeed = new Vector4(localPosition.x, localPosition.y, localPosition.z, seed);
            planePositionBlock = new Vector4(planePosition.x, planePosition.y, block.x, block.y);
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct FanlightBlockData
    {
        public Vector4 localCenterRadius;
        public Vector4 indexRange;

        public const int Stride = sizeof(float) * 8;

        public FanlightBlockData(Vector3 localCenter, float radius, int startIndex, int count)
        {
            localCenterRadius = new Vector4(localCenter.x, localCenter.y, localCenter.z, radius);
            indexRange = new Vector4(startIndex, count, 0.0f, 0.0f);
        }
    }
}

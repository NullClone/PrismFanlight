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
}

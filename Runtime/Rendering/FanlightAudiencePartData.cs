using System.Runtime.InteropServices;
using UnityEngine;

namespace PrismFanlight.Rendering
{
    [StructLayout(LayoutKind.Sequential)]
    public struct FanlightAudiencePart
    {
        public Vector4 p0HalfWidth;
        public Vector4 p1Type;

        public const int Stride = sizeof(float) * 8;
        public const int PartsPerSeat = 3;
    }
}

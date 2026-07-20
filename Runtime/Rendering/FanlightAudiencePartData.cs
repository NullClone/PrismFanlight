using System.Runtime.InteropServices;
using UnityEngine;

namespace PrismFanlight.Rendering
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct FanlightAudiencePart
    {
        internal Vector4 p0HalfWidth;
        internal Vector4 p1Type;

        internal const int Stride = sizeof(float) * 8;
        internal const int PartsPerSeat = 3;
    }
}

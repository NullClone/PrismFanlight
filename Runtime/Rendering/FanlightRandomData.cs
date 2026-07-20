using System.Runtime.InteropServices;
using UnityEngine;

namespace PrismFanlight.Rendering
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct FanlightRandomData
    {
        internal Vector4 random0;
        internal Vector4 random1;
        internal Vector4 random2;
        internal Vector4 random3;
        internal Vector4 random4;
        internal Vector4 random5;
        internal Vector4 random6;
        internal Vector4 random7;

        internal const int Stride = sizeof(float) * 32;
    }
}

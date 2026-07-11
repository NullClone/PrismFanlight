using System.Runtime.InteropServices;
using UnityEngine;

namespace PrismFanlight.Rendering
{
    [StructLayout(LayoutKind.Sequential)]
    public struct FanlightRandomData
    {
        public Vector4 random0;
        public Vector4 random1;
        public Vector4 random2;
        public Vector4 random3;
        public Vector4 random4;
        public Vector4 random5;
        public Vector4 random6;
        public Vector4 random7;

        public const int Stride = sizeof(float) * 32;
    }
}

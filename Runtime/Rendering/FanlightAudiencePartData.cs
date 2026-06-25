using System.Runtime.InteropServices;
using UnityEngine;

namespace PrismFanlight.Rendering
{
    // 観客 1 人を構成するパーツ（席ごとに 3 つ）。
    //   part 0: 体（feet → neck のカメラ正対リボン, type 0）
    //   part 1: 腕（shoulder → hand のカメラ正対リボン, type 1）
    //   part 2: 頭（中心 + 半径の全方位ビルボード, type 2）
    [StructLayout(LayoutKind.Sequential)]
    public struct FanlightAudiencePart
    {
        public Vector4 p0HalfWidth; // xyz: 始点/中心(World), w: 半幅(World)
        public Vector4 p1Type;      // xyz: 終点(World), w: 種別 (0:体, 1:腕, 2:頭)

        public const int Stride = sizeof(float) * 8;
        public const int PartsPerSeat = 3;
    }
}

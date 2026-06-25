struct FanlightSeatData
{
    float4 localPositionSeed;
    float4 planePositionBlock;
};

struct FanlightBlockData
{
    float4 localCenterRadius;
    float4 indexRange;
};

struct FanlightAudiencePart
{
    float4 p0HalfWidth; // xyz: 始点/中心(World), w: 半幅(World)
    float4 p1Type;      // xyz: 終点(World), w: 種別 (0:体, 1:腕, 2:頭)
};

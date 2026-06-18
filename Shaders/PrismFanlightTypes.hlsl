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

struct FanlightBodyPart
{
    float4 p0HalfWidth;
    float4 p1Type;
};

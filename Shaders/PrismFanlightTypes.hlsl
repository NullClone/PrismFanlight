#ifndef PRISM_FANLIGHT_TYPES_INCLUDED
#define PRISM_FANLIGHT_TYPES_INCLUDED

struct FanlightSeatData
{
    float4 localPositionSeed;
    int blockIndex;
    uint padding0;
    uint padding1;
    uint padding2;
};

struct FanlightBlockData
{
    float4 localCenterRadius;
    float2 effectCoordinate;
    int startIndex;
    int count;
};

struct FanlightAudiencePart
{
    float4 p0HalfWidth;
    float4 p1Type;
};

struct FanlightIndirectDrawIndexedArgs
{
    uint indexCountPerInstance;
    uint instanceCount;
    uint startIndex;
    uint baseVertexIndex;
    uint startInstance;
};

struct FanlightRandomData
{
    float4 random0;
    float4 random1;
    float4 random2;
    float4 random3;
    float4 random4;
    float4 random5;
    float4 random6;
    float4 random7;
};

struct FanlightMotionSample
{
    float4 bodyPosition;
    float4 bodyRotation;
    float4 handPosition;
    float4 penlightRotation;
};

struct PrismArm
{
    float4x4 worldMatrix;
    float3 handLocal;
    float3 shoulderLocal;
};

struct PrismCrowdRhythm
{
    float cyclePhase;
    float bodyPhase;
};

struct PrismAudienceBasis
{
    float3 sideWorld;
    float3 upWorld;
    float3 forwardWorld;
    float3 sideLocal;
    float3 upLocal;
    float3 forwardLocal;
};

struct PrismHumanPose
{
    float3 anchorLocal;
    float3 feetLocal;
    float3 shoulderLocal;
    float3 neckLocal;
    float3 headCenterLocal;
    float bodyHalfWidth;
    float armHalfWidth;
    float headHalf;
};

float4 PrismNormalizeQuaternion(float4 rotation)
{
    float squareMagnitude = dot(rotation, rotation);
    return squareMagnitude > 0.000001
        ? rotation * rsqrt(squareMagnitude)
        : float4(0.0, 0.0, 0.0, 1.0);
}

float4 PrismNlerpQuaternion(float4 from, float4 to, float weight)
{
    from = PrismNormalizeQuaternion(from);
    to = PrismNormalizeQuaternion(to);
    to *= dot(from, to) < 0.0 ? -1.0 : 1.0;
    return PrismNormalizeQuaternion(lerp(from, to, saturate(weight)));
}

float3 PrismRotateByQuaternion(float4 rotation, float3 value)
{
    rotation = PrismNormalizeQuaternion(rotation);
    return value + 2.0 * cross(rotation.xyz, cross(rotation.xyz, value) + rotation.w * value);
}

#endif

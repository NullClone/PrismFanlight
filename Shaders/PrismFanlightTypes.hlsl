#ifndef PRISM_FANLIGHT_TYPES_INCLUDED
#define PRISM_FANLIGHT_TYPES_INCLUDED

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
    float4 armDirectionExtension;
    float4 penlightDirectionBodyLean;
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
    float shoulderPhase;
    float downbeatPulse;
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

#endif

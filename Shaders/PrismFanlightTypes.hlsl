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

struct PrismArm
{
    float4x4 worldMatrix;
    float3 handLocal;
    float3 shoulderLocal;
};

struct PrismCrowdRhythm
{
    float basePhase;
    float bodyPhase;
    float shoulderPhase;
    float armPhase;
    float wristPhase;
    float beatSync;
    float downbeatPulse;
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

#ifndef PRISM_FANLIGHT_COMPUTE_CONTEXT_INCLUDED
#define PRISM_FANLIGHT_COMPUTE_CONTEXT_INCLUDED

#include "PrismFanlightTypes.hlsl"

StructuredBuffer<FanlightSeatData> _Seats;
StructuredBuffer<FanlightBlockData> _Blocks;
StructuredBuffer<FanlightRandomData> _FanlightRandoms;
StructuredBuffer<FanlightMotionSample> _MotionSamples;
RWStructuredBuffer<uint> _BlockVisibility;
RWStructuredBuffer<uint> _PenlightVisibleIndices;
StructuredBuffer<uint> _PenlightVariantAssignments;
StructuredBuffer<uint> _PenlightVariantOffsets;
RWStructuredBuffer<uint> _AudienceVisibleIndices;
RWStructuredBuffer<uint> _AudienceSlots;
RWStructuredBuffer<FanlightIndirectDrawIndexedArgs> _PenlightArgs;
RWStructuredBuffer<float4x4> _FanlightMatrices;
RWStructuredBuffer<FanlightAudiencePart> _AudienceParts;
RWStructuredBuffer<FanlightIndirectDrawIndexedArgs> _AudienceArgs;

int _InstanceCount;
int _BlockCountValue;
float4x4 _LocalToWorld;
float _FanlightTime;
float4 _FanlightBeat;
float4 _FanlightTempo;
float4 _FrustumPlanes[6];
float _CullingScale;
int _EnableCulling;
int _EnableAudienceLod;
float4 _AudienceLod;
float4 _LodCameraPos;

float4 _SeatPitch;
float4 _BlockCount;
float4 _MotionTiming;
float4 _MotionCycle;
float4 _MotionParameters;
float4 _MotionReferenceArm;
float4 _MotionReferencePenlight;
int _SwingMode;
float4 _SwingAxis;
float4 _SwingTargetPos;
float4x4 _WorldToLocal;
float4 _MotionVariation;
float4 _MotionNoise;
float4 _MotionHuman;
float4 _MotionRest;
float4 _MotionRestTiming;
float4 _MotionBeatSpread;
float _HandPositionSpread;
int _PenlightVariantCount;
float4 _PenlightVariantGripPivotYs;

float PrismPenlightGripPivotY(uint seatIndex)
{
    uint variantIndex = min(_PenlightVariantAssignments[seatIndex], 3u);
    if (variantIndex == 0u) return _PenlightVariantGripPivotYs.x;
    if (variantIndex == 1u) return _PenlightVariantGripPivotYs.y;
    if (variantIndex == 2u) return _PenlightVariantGripPivotYs.z;
    return _PenlightVariantGripPivotYs.w;
}
float4 _AudienceShape;
float4 _AudienceArm;
float4 _AudienceUpperBody;
float4 _AudienceMotionBody;

uint PrismSeatIndex(FanlightSeatData seat)
{
    return (uint)round(seat.localPositionSeed.w);
}

float PrismRandom(FanlightSeatData seat, uint slot)
{
    FanlightRandomData r = _FanlightRandoms[PrismSeatIndex(seat)];
    if (slot < 4u) return r.random0[(int)slot];
    if (slot < 8u) return r.random1[(int)(slot - 4u)];
    if (slot < 12u) return r.random2[(int)(slot - 8u)];
    if (slot < 16u) return r.random3[(int)(slot - 12u)];
    if (slot < 20u) return r.random4[(int)(slot - 16u)];
    if (slot < 24u) return r.random5[(int)(slot - 20u)];
    if (slot < 28u) return r.random6[(int)(slot - 24u)];
    return r.random7[(int)min(slot - 28u, 3u)];
}

#endif

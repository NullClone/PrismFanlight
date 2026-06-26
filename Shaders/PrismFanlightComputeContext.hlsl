#ifndef PRISM_FANLIGHT_COMPUTE_CONTEXT_INCLUDED
#define PRISM_FANLIGHT_COMPUTE_CONTEXT_INCLUDED

#include "PrismFanlightTypes.hlsl"

StructuredBuffer<FanlightSeatData> _Seats;
StructuredBuffer<FanlightBlockData> _Blocks;
RWStructuredBuffer<uint> _BlockVisibility;
RWStructuredBuffer<uint> _VisibleIndices;
RWStructuredBuffer<uint> _DrawArgs;
RWStructuredBuffer<float4x4> _FanlightMatrices;
RWStructuredBuffer<float4> _FanlightColors;
RWStructuredBuffer<FanlightAudiencePart> _AudienceParts;
RWStructuredBuffer<uint> _AudienceArgs;

int _InstanceCount;
int _BlockCountValue;
float4x4 _LocalToWorld;
float _FanlightTime;
float4 _FanlightBeat;
float4 _FanlightTempo;
float4 _FrustumPlanes[6];
float _CullingScale;
int _EnableCulling;

float4 _SeatPitch;
float4 _BlockCount;
float4 _MotionTiming;
float4 _MotionSwing;
float4 _MotionShape;
int _SwingMode;
float4 _SwingWrist;
float4 _SwingAxis;
float4 _SwingTargetPos;
float4x4 _WorldToLocal;
float4 _MotionVariation;
float4 _MotionNoise;
float4 _MotionHuman;
float4 _MotionRest;
float4 _MotionRestTiming;
float4 _MotionBeat;
float4 _MotionBeatSpread;
float _GripPivotY;
int _ColorMode;
float4 _PrimaryColor;
float4 _SecondaryColor;
float4 _Brightness;
int _PaletteColorCount;
float4 _PaletteColors[16];
float4 _AudienceShape;
float4 _AudienceArm;
float4 _AudienceUpperBody;
float4 _AudienceMotionBody;
float _HandBaseHeight;

#endif

#ifndef PRISM_FANLIGHT_COLOR_KERNEL_INCLUDED
#define PRISM_FANLIGHT_COLOR_KERNEL_INCLUDED

StructuredBuffer<uint> _FanlightStableAssignments;
StructuredBuffer<uint> _RuntimeBlockPalettes;
RWStructuredBuffer<float4> _FanlightResolvedChroma;
RWStructuredBuffer<float> _FanlightResolvedMask;

float4 _ColorSourceModes[3];
float4 _ColorSourcePalette[18];
float4 _ColorSourceA[3];
float4 _ColorSourceB[3];
float4 _ColorResolvedDirection;
float4 _ColorSourceGeometry[3];
float4 _ColorSourceParameters[3];
float _MaskCompletedBeat;
float4 _MaskSourceModes[3];
float4 _MaskSourceTiming[3];
float4 _MaskSourceEnvelope[3];
float4 _MaskResolvedDirection;
float4 _MaskSourceGeometry[3];

float3 PrismEvaluateColorSource(uint sourceIndex, uint seatIndex, FanlightSeatData seat)
{
    uint mode = (uint)round(_ColorSourceModes[sourceIndex].x);
    float3 chroma = 0.0;

    if (mode == 0u)
    {
        uint paletteSlot = min(_FanlightStableAssignments[seatIndex] & 7u, 5u);
        chroma = _ColorSourcePalette[sourceIndex * 6u + paletteSlot].rgb;
    }
    else if (mode == 1u)
    {
        float2 origin = _ColorSourceGeometry[sourceIndex].xy;
        float width = max(_ColorSourceParameters[sourceIndex].x, 0.000001);
        float offset = _ColorSourceParameters[sourceIndex].y;
        float coordinate = dot(seat.localPositionSeed.xz - origin, _ColorResolvedDirection.xy) / width + 0.5 + offset;
        chroma = lerp(_ColorSourceA[sourceIndex].rgb, _ColorSourceB[sourceIndex].rgb, saturate(coordinate));
    }
    else
    {
        uint blockIndex = (uint)max(seat.blockIndex, 0);
        uint blockOffset = (uint)max(0.0, round(_ColorSourceModes[sourceIndex].z));
        uint blockPaletteSlot = min(_RuntimeBlockPalettes[blockOffset + blockIndex], 5u);
        chroma = _ColorSourcePalette[sourceIndex * 6u + blockPaletteSlot].rgb;
    }

    return chroma;
}

float PrismSmooth01(float value)
{
    value = saturate(value);
    return value * value * (3.0 - 2.0 * value);
}

float PrismEvaluateMaskEnvelope(uint sourceIndex, float phase)
{
    float minimum = _MaskSourceEnvelope[sourceIndex].x;
    float attack = _MaskSourceEnvelope[sourceIndex].y;
    float hold = _MaskSourceEnvelope[sourceIndex].z;
    float release = _MaskSourceEnvelope[sourceIndex].w;
    float attackEnd = attack;
    float holdEnd = attack + hold;
    float releaseEnd = holdEnd + release;

    if (attack > 0.0 && phase < attackEnd)
    {
        return lerp(minimum, 1.0, PrismSmooth01(phase / attack));
    }

    if (phase < holdEnd)
    {
        return 1.0;
    }

    if (release > 0.0 && phase < releaseEnd)
    {
        return lerp(1.0, minimum, PrismSmooth01((phase - holdEnd) / release));
    }

    return minimum;
}

float PrismEvaluateMaskSource(uint sourceIndex, FanlightSeatData seat)
{
    uint mode = (uint)round(_MaskSourceModes[sourceIndex].x);
    if (mode == 0u) return 1.0;

    float beatsPerCycle = max(_MaskSourceTiming[sourceIndex].x, 0.000001);
    float phaseOffsetBeats = _MaskSourceTiming[sourceIndex].y;
    float phase = (_MaskCompletedBeat + phaseOffsetBeats) / beatsPerCycle;

    if (mode == 2u)
    {
        float2 origin = _MaskSourceGeometry[sourceIndex].xy;
        float wavelength = max(_MaskSourceTiming[sourceIndex].z, 0.000001);
        float spatialPhase = dot(seat.localPositionSeed.xz - origin, _MaskResolvedDirection.xy) / wavelength;
        phase -= spatialPhase;
    }

    return saturate(PrismEvaluateMaskEnvelope(sourceIndex, frac(phase)));
}

#endif

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
float4 _ColorSourceGeometry[3];
float4 _ColorSourceParameters[3];
float4 _MaskSourceModes[3];
float4 _MaskSourceGeometry[3];
float4 _MaskSourceParameters[3];

float3 PrismEvaluateColorSource(uint sourceIndex, uint seatIndex, FanlightSeatData seat)
{
    uint mode = (uint)round(_ColorSourceModes[sourceIndex].x);
    if (mode == 0u)
    {
        uint paletteSlot = min(_FanlightStableAssignments[seatIndex] & 7u, 5u);
        return _ColorSourcePalette[sourceIndex * 6u + paletteSlot].rgb;
    }

    if (mode == 1u)
    {
        float2 origin = _ColorSourceGeometry[sourceIndex].xy;
        float2 direction = _ColorSourceGeometry[sourceIndex].zw;
        float width = max(_ColorSourceParameters[sourceIndex].x, 0.000001);
        float offset = _ColorSourceParameters[sourceIndex].y;
        float coordinate = dot(seat.localPositionSeed.xz - origin, direction) / width + 0.5 + offset;
        return lerp(_ColorSourceA[sourceIndex].rgb, _ColorSourceB[sourceIndex].rgb, saturate(coordinate));
    }

    uint blockIndex = (uint)max(seat.blockIndex, 0);
    uint blockOffset = (uint)max(0.0, round(_ColorSourceModes[sourceIndex].z));
    uint blockPaletteSlot = min(_RuntimeBlockPalettes[blockOffset + blockIndex], 5u);
    return _ColorSourcePalette[sourceIndex * 6u + blockPaletteSlot].rgb;
}

float PrismEvaluateMaskSource(uint sourceIndex, FanlightSeatData seat)
{
    uint mode = (uint)round(_MaskSourceModes[sourceIndex].x);
    float invert = _MaskSourceModes[sourceIndex].z;
    float mask = 1.0;

    if (mode == 1u)
    {
        float2 origin = _MaskSourceGeometry[sourceIndex].xy;
        float2 direction = _MaskSourceGeometry[sourceIndex].zw;
        float width = _MaskSourceParameters[sourceIndex].x;
        float progress = _MaskSourceParameters[sourceIndex].y;
        float softness = _MaskSourceParameters[sourceIndex].w;

        if (progress <= 0.0)
        {
            mask = 0.0;
        }
        else if (progress >= 1.0)
        {
            mask = 1.0;
        }
        else
        {
            float distance = dot(seat.localPositionSeed.xz - origin, direction) + width * 0.5;
            float edge = lerp(-softness, width + softness, progress);
            mask = softness > 0.0
                ? 1.0 - smoothstep(edge, edge + softness, distance)
                : distance <= edge ? 1.0 : 0.0;
        }
    }
    else if (mode == 2u)
    {
        float2 origin = _MaskSourceGeometry[sourceIndex].xy;
        float radius = _MaskSourceParameters[sourceIndex].z;
        float softness = _MaskSourceParameters[sourceIndex].w;
        float distance = length(seat.localPositionSeed.xz - origin);

        if (radius <= 0.0)
        {
            mask = 0.0;
        }
        else
        {
            mask = softness > 0.0
                ? 1.0 - smoothstep(radius, radius + softness, distance)
                : distance <= radius ? 1.0 : 0.0;
        }
    }

    if (invert >= 0.5)
    {
        mask = 1.0 - mask;
    }

    return saturate(mask);
}

#endif

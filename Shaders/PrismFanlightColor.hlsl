#ifndef PRISM_FANLIGHT_COLOR_INCLUDED
#define PRISM_FANLIGHT_COLOR_INCLUDED

StructuredBuffer<uint> _FanlightColorAssignments;
float4 _PaletteColors[6];
float _FanlightGlobalIntensity;
float _FanlightRandomIntensity;

float4 PrismFanlightSeatColor(uint seatIndex)
{
    uint assignment = _FanlightColorAssignments[seatIndex];
    uint paletteIndex = min(assignment & 7u, 5u);
    float randomValue = ((assignment >> 8u) & 65535u) / 65535.0;
    float intensityVariation = max(0.0, 1.0 + (randomValue * 2.0 - 1.0) * _FanlightRandomIntensity);
    float4 color = _PaletteColors[paletteIndex];
    color.rgb *= _FanlightGlobalIntensity * intensityVariation;
    return color;
}

#endif

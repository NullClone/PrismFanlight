#ifndef PRISM_FANLIGHT_COLOR_INCLUDED
#define PRISM_FANLIGHT_COLOR_INCLUDED

StructuredBuffer<uint> _FanlightStableAssignments;
StructuredBuffer<float4> _FanlightResolvedChroma;
StructuredBuffer<float> _FanlightResolvedMask;
float _FanlightBaseIntensity;
float _FanlightRandomIntensity;

float4 PrismFanlightSeatColor(uint seatIndex)
{
    uint assignment = _FanlightStableAssignments[seatIndex];
    float randomValue = ((assignment >> 8u) & 65535u) / 65535.0;
    float intensityVariation = max(0.0, 1.0 + (randomValue * 2.0 - 1.0) * _FanlightRandomIntensity);
    float4 color = _FanlightResolvedChroma[seatIndex];
    color.rgb *= _FanlightBaseIntensity * intensityVariation * _FanlightResolvedMask[seatIndex];
    color.a = 1.0;
    return color;
}

#endif

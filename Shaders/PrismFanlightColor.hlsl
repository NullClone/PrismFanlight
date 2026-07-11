#ifndef PRISM_FANLIGHT_COLOR_INCLUDED
#define PRISM_FANLIGHT_COLOR_INCLUDED

#include "PrismFanlightComputeContext.hlsl"
#include "PrismFanlightMath.hlsl"

float4 PrismComputeColor(FanlightSeatData seat)
{
    float2 block = seat.planePositionBlock.zw;

    float3 rgb = _PrimaryColor.rgb;

    if (_ColorMode == 0)
    {
    }
    else if (_ColorMode == 1)
    {
        int count = clamp(_PaletteColorCount, 1, 16);
        int paletteIndex = min((int)floor(PrismRandom(seat, 27u) * count), count - 1);
        rgb = _PaletteColors[paletteIndex].rgb;
    }
    else if (_ColorMode == 2)
    {
        float denom = max(_BlockCount.x - 1.0, 1.0);
        rgb = lerp(_PrimaryColor.rgb, _SecondaryColor.rgb, block.x / denom);
    }

    float randomIntensityFactor = max(0.0, 1.0 + (PrismRandom(seat, 28u) * 2.0 - 1.0) * _Brightness.y);

    return float4(rgb * randomIntensityFactor, _PrimaryColor.a);
}

#endif

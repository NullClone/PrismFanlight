#ifndef PRISM_FANLIGHT_DIRECTION_INCLUDED
#define PRISM_FANLIGHT_DIRECTION_INCLUDED

#include "PrismFanlightComputeContext.hlsl"
#include "PrismFanlightMath.hlsl"

float3 PrismWorldVectorToLocal(float3 vectorWS)
{
    return mul((float3x3)_WorldToLocal, vectorWS);
}

float3 PrismComputeWorldDirection(FanlightSeatData seat)
{
    float3 worldDirection = SafeNormalize(_SwingAxis.xyz, float3(0.0, 0.0, 1.0));
    float aimStrength = saturate(_SwingTargetPos.w);

    if (_SwingMode == 1 && aimStrength > 0.001)
    {
        float3 seatWorldPos = mul(_LocalToWorld, float4(seat.localPositionSeed.xyz, 1.0)).xyz;
        float3 targetDirection = _SwingTargetPos.xyz - seatWorldPos;
        targetDirection.y = 0.0;
        targetDirection = SafeNormalize(targetDirection, worldDirection);
        worldDirection = SafeNormalize(lerp(worldDirection, targetDirection, aimStrength), worldDirection);
    }

    return worldDirection;
}

float3 PrismComputeBaseAxis(FanlightSeatData seat, bool horizontal)
{
    float3 worldDirection = PrismComputeWorldDirection(seat);
    float3 worldAxis = horizontal
        ? worldDirection
        : SafeNormalize(cross(float3(0.0, 1.0, 0.0), worldDirection), float3(1.0, 0.0, 0.0));

    return SafeNormalize(PrismWorldVectorToLocal(worldAxis), float3(1.0, 0.0, 0.0));
}

#endif

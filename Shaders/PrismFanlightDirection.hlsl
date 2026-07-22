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

PrismAudienceBasis PrismComputeAudienceBasis(FanlightSeatData seat)
{
    PrismAudienceBasis basis = (PrismAudienceBasis)0;
    basis.upWorld = float3(0.0, 1.0, 0.0);
    basis.forwardWorld = PrismComputeWorldDirection(seat);
    basis.forwardWorld.y = 0.0;
    basis.forwardWorld = SafeNormalize(basis.forwardWorld, float3(0.0, 0.0, 1.0));
    basis.sideWorld = SafeNormalize(cross(basis.upWorld, basis.forwardWorld), float3(1.0, 0.0, 0.0));
    basis.sideLocal = SafeNormalize(PrismWorldVectorToLocal(basis.sideWorld), float3(1.0, 0.0, 0.0));
    basis.upLocal = SafeNormalize(PrismWorldVectorToLocal(basis.upWorld), float3(0.0, 1.0, 0.0));
    basis.forwardLocal = SafeNormalize(PrismWorldVectorToLocal(basis.forwardWorld), float3(0.0, 0.0, 1.0));
    return basis;
}

float3 PrismTransformAudienceOffset(PrismAudienceBasis basis, float3 offset)
{
    return basis.sideLocal * offset.x + basis.upLocal * offset.y + basis.forwardLocal * offset.z;
}

float3 PrismTransformAudienceDirectionWorld(PrismAudienceBasis basis, float3 direction)
{
    return basis.sideWorld * direction.x + basis.upWorld * direction.y + basis.forwardWorld * direction.z;
}

#endif

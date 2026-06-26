#ifndef PRISM_FANLIGHT_CULLING_INCLUDED
#define PRISM_FANLIGHT_CULLING_INCLUDED

#include "PrismFanlightComputeContext.hlsl"

bool PrismSphereInFrustum(float3 center, float radius)
{
    [unroll]
    for (uint i = 0; i < 6; i++)
    {
        float4 plane = _FrustumPlanes[i];
        
        if (dot(plane.xyz, center) + plane.w < -radius)
            return false;
    }

    return true;
}

#endif

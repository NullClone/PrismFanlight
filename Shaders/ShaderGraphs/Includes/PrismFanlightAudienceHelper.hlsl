#ifndef PRISM_FANLIGHT_AUDIENCE_HELPER_INCLUDED
#define PRISM_FANLIGHT_AUDIENCE_HELPER_INCLUDED

struct PrismFanlightAudiencePart
{
    float4 p0HalfWidth;
    float4 p1Type;
};

StructuredBuffer<uint> _VisibleIndices;
StructuredBuffer<PrismFanlightAudiencePart> _AudienceParts;

void GetAudienceBodyVertex_float(float2 uv, float instanceId, out float3 positionOS, out float partType, out float capT)
{
    uint global = (uint)max(0.0, instanceId);
    uint slot = global / 3u;
    uint part = global % 3u;
    uint seat = _VisibleIndices[slot];
    PrismFanlightAudiencePart bp = _AudienceParts[seat * 3u + part];

    float3 p0 = bp.p0HalfWidth.xyz;
    float halfWidth = bp.p0HalfWidth.w;
    float3 p1 = bp.p1Type.xyz;
    partType = bp.p1Type.w;

    float3 worldPos;
    if (partType >= 1.5)
    {
        float3 camRight = UNITY_MATRIX_V._m00_m01_m02;
        float3 camUp    = UNITY_MATRIX_V._m10_m11_m12;
        worldPos = p0 + (camRight * (uv.x - 0.5) + camUp * (uv.y - 0.5)) * (halfWidth * 2.0);
        capT = 0.5;
    }
    else
    {
        float3 center = lerp(p0, p1, uv.y);
        float3 axis = p1 - p0;
        float segLen = max(1e-4, length(axis));
        axis /= segLen;

        float3 view = _WorldSpaceCameraPos.xyz - center;
        float3 side = cross(axis, view);
        float sideLen = length(side);
        side = sideLen > 1e-4 ? side / sideLen : float3(1.0, 0.0, 0.0);

        worldPos = center + side * (uv.x - 0.5) * (halfWidth * 2.0);
        capT = saturate(halfWidth / segLen);
    }

    positionOS = mul(UNITY_MATRIX_I_M, float4(worldPos, 1.0)).xyz;
}

void GetAudienceBodyCoverage_float(float2 uv, float capT, out float coverage)
{
    float t = saturate(capT);
    float distance;

    if (t >= 0.5)
    {
        float2 sdfPoint = (uv - 0.5) * 2.0;
        distance = length(sdfPoint) - 1.0;
    }
    else
    {
        float inverseT = rcp(max(t, 1e-3));
        float2 sdfPoint = float2((uv.x - 0.5) * 2.0, uv.y * inverseT);
        float centerY = clamp(sdfPoint.y, 1.0, inverseT - 1.0);
        distance = length(float2(sdfPoint.x, sdfPoint.y - centerY)) - 1.0;
    }

    float antialias = max(fwidth(distance), 1e-4);
    coverage = saturate(0.5 - distance / antialias);
}

void GetAudienceBodyRim_float(
    float2 uv,
    float partType,
    float2 rimDirection,
    float rimThickness,
    float rimSmoothness,
    float bodyYCutoff,
    float bodyYCutoffSmoothness,
    out float rim)
{
    float x = uv.x * 2.0 - 1.0;
    float z = sqrt(saturate(1.0 - x * x));
    float directionLengthSquared = dot(rimDirection, rimDirection);
    float2 normalizedDirection = directionLengthSquared > 1e-6
        ? rimDirection * rsqrt(directionLengthSquared)
        : float2(0.0, 1.0);
    float smoothness = max(rimSmoothness, 1e-4);
    float edgeMask = 1.0 - smoothstep(
        saturate(rimThickness) - smoothness,
        saturate(rimThickness) + smoothness,
        z);
    float directionMask = saturate(dot(float2(x, z), normalizedDirection));
    float bodyMask = partType < 0.5
        ? smoothstep(bodyYCutoff, bodyYCutoff + bodyYCutoffSmoothness, uv.y)
        : 1.0;
    rim = edgeMask * directionMask * bodyMask;
}

#endif

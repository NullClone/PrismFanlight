#ifndef PRISM_FANLIGHT_BODY_SHADER_GRAPH_HELPER_INCLUDED
#define PRISM_FANLIGHT_BODY_SHADER_GRAPH_HELPER_INCLUDED

struct PrismFanlightBodyPart
{
    float4 p0HalfWidth;
    float4 p1Type;
};

StructuredBuffer<uint> _VisibleIndices;
StructuredBuffer<PrismFanlightBodyPart> _BodyParts;

StructuredBuffer<float4> _FanlightColors;
int _FanlightColorSource;
float4 _FanlightGlobalColor;
float _FanlightGlobalIntensity;

void GetAudienceBodyVertex_float(float2 uv, float instanceId, out float3 positionOS, out float partType, out float capT)
{
    uint global = (uint)max(0.0, instanceId);
    uint slot = global / 3u;
    uint part = global % 3u;
    uint seat = _VisibleIndices[slot];
    PrismFanlightBodyPart bp = _BodyParts[seat * 3u + part];

    float3 p0 = bp.p0HalfWidth.xyz;
    float halfWidth = bp.p0HalfWidth.w;
    float3 p1 = bp.p1Type.xyz;
    partType = bp.p1Type.w;

    float3 center = lerp(p0, p1, uv.y);
    float3 axis = p1 - p0;
    float segLen = max(1e-4, length(axis));
    axis /= segLen;

    float3 view = _WorldSpaceCameraPos.xyz - center;
    float3 side = cross(axis, view);
    float sideLen = length(side);
    side = sideLen > 1e-4 ? side / sideLen : float3(1.0, 0.0, 0.0);

    float3 worldPos = center + side * (uv.x - 0.5) * (halfWidth * 2.0);
    positionOS = mul(UNITY_MATRIX_I_M, float4(worldPos, 1.0)).xyz;
    capT = saturate(halfWidth / segLen);
}

void GetAudienceBodyCoverage_float(float2 uv, float capT, out float coverage)
{
    float across = abs(uv.x * 2.0 - 1.0);
    float dEnd = min(uv.y, 1.0 - uv.y);
    float t = max(1e-3, capT);

    coverage = 1.0;
    if (dEnd < t)
    {
        float yy = (t - dEnd) / t;
        coverage = (across * across + yy * yy) <= 1.0 ? 1.0 : 0.0;
    }
}

void GetAudienceBodyRim_float(
    float2 uv,
    float partType,
    float depthFake,
    float2 lightDir2D,
    float rimThickness,
    float rimSmoothness,
    float yCutoff,
    float yCutoffSmooth,
    out float rim)
{
    float nx = uv.x * 2.0 - 1.0;
    float z = sqrt(max(0.0, 1.0 - nx * nx));
    float3 fakeNormal = normalize(float3(nx, 0.0, z * depthFake));

    float2 l2 = normalize(lightDir2D);
    float3 lightDir = normalize(float3(l2.x, l2.y, 0.5));
    float ndl = saturate(dot(fakeNormal, lightDir));

    float edgeMask = 1.0 - smoothstep(rimThickness - rimSmoothness, rimThickness + rimSmoothness, z);
    float yMask = (partType < 0.5) ? smoothstep(yCutoff, yCutoff + yCutoffSmooth, uv.y) : 1.0;
    rim = ndl * edgeMask * yMask;
}

void GetAudienceBodyShade_float(float2 uv, float depthFake, float2 lightDir2D, float ambient, out float shade)
{
    float nx = uv.x * 2.0 - 1.0;
    float z = sqrt(max(0.0, 1.0 - nx * nx));
    float3 fakeNormal = normalize(float3(nx, 0.0, z * depthFake));

    float2 l2 = normalize(lightDir2D);
    float3 lightDir = normalize(float3(l2.x, l2.y, 0.5));
    float ndl = saturate(dot(fakeNormal, lightDir));

    shade = lerp(saturate(ambient), 1.0, ndl);
}

void GetAudienceBodyColor_float(float instanceId, out float4 color)
{
    if (_FanlightColorSource == 0)
    {
        color = float4(_FanlightGlobalColor.rgb * _FanlightGlobalIntensity, _FanlightGlobalColor.a);
        return;
    }

    uint global = (uint)max(0.0, instanceId);
    uint seat = _VisibleIndices[global / 3u];
    color = _FanlightColors[seat];
    color.rgb *= _FanlightGlobalIntensity;
}

#endif

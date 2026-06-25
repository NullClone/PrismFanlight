#ifndef PRISM_FANLIGHT_BODY_SHADER_GRAPH_HELPER_INCLUDED
#define PRISM_FANLIGHT_BODY_SHADER_GRAPH_HELPER_INCLUDED

struct PrismFanlightAudiencePart
{
    float4 p0HalfWidth; // xyz: 始点/中心(World), w: 半幅(World)
    float4 p1Type;      // xyz: 終点(World), w: 種別 (0:体, 1:腕, 2:頭)
};

StructuredBuffer<uint> _VisibleIndices;
StructuredBuffer<PrismFanlightAudiencePart> _AudienceParts;

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
    PrismFanlightAudiencePart bp = _AudienceParts[seat * 3u + part];

    float3 p0 = bp.p0HalfWidth.xyz;
    float halfWidth = bp.p0HalfWidth.w;
    float3 p1 = bp.p1Type.xyz;
    partType = bp.p1Type.w;

    float3 worldPos;
    if (partType >= 1.5)
    {
        // 頭: スクリーン正対の全方位ビルボード。ビュー行列の行がワールドのカメラ右/上。
        float3 camRight = UNITY_MATRIX_V._m00_m01_m02;
        float3 camUp    = UNITY_MATRIX_V._m10_m11_m12;
        worldPos = p0 + (camRight * (uv.x - 0.5) + camUp * (uv.y - 0.5)) * (halfWidth * 2.0);
        capT = 0.5; // 頭は端を丸める前提（カバレッジで円形にできる）
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

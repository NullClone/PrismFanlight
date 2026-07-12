#ifndef PRISM_FANLIGHT_SHADER_GRAPH_HELPER_INCLUDED
#define PRISM_FANLIGHT_SHADER_GRAPH_HELPER_INCLUDED

StructuredBuffer<uint> _VisibleIndices;
StructuredBuffer<float4x4> _FanlightMatrices;
StructuredBuffer<float4> _FanlightColors;

int _FanlightColorSource;
float4 _FanlightGlobalColor;
float _FanlightGlobalIntensity;

uint PrismFanlightVisibleIndex(float instanceId)
{
    return (uint)max(0.0, instanceId);
}

uint PrismFanlightSeatIndex(float instanceId)
{
    return _VisibleIndices[PrismFanlightVisibleIndex(instanceId)];
}

float3 PrismStabilizeFanlightRadius(float3 positionOS, uint seatIndex)
{
    // Thin, moving geometry can cover less than one pixel at a distance. Expanding
    // only its screen-facing radius keeps the penlight from temporally aliasing,
    // without changing its length or its hand position.
    const float minRadiusPixels = 0.75;

    float3 positionWS = mul(_FanlightMatrices[seatIndex], float4(positionOS, 1.0)).xyz;
    float3 centerWS = mul(_FanlightMatrices[seatIndex], float4(0.0, positionOS.y, 0.0, 1.0)).xyz;
    float3 toCamera = _WorldSpaceCameraPos.xyz - centerWS;
    float cameraDistance = length(toCamera);
    if (cameraDistance <= 0.0001)
        return positionWS;

    float3 viewDirection = toCamera / cameraDistance;
    float3 radiusWS = positionWS - centerWS;
    float3 screenRadiusWS = radiusWS - viewDirection * dot(radiusWS, viewDirection);
    if (dot(screenRadiusWS, screenRadiusWS) <= 0.00000001)
        return positionWS;

    float4 centerCS = mul(UNITY_MATRIX_VP, float4(centerWS, 1.0));
    float4 positionCS = mul(UNITY_MATRIX_VP, float4(positionWS, 1.0));
    if (centerCS.w <= 0.0001 || positionCS.w <= 0.0001)
        return positionWS;

    float2 radiusPixels = (positionCS.xy / positionCS.w - centerCS.xy / centerCS.w) * (0.5 * _ScreenParams.xy);
    float pixelLength = length(radiusPixels);
    if (pixelLength >= minRadiusPixels)
        return positionWS;

    return centerWS + radiusWS + screenRadiusWS * (minRadiusPixels / max(pixelLength, 0.0001) - 1.0);
}

void GetFanlightColor_float(float instanceId, out float4 color)
{
    if (_FanlightColorSource == 0)
    {
        color = float4(_FanlightGlobalColor.rgb * _FanlightGlobalIntensity, _FanlightGlobalColor.a);
        return;
    }

    uint seatIndex = PrismFanlightSeatIndex(instanceId);
    color = _FanlightColors[seatIndex];
    color.rgb *= _FanlightGlobalIntensity;
}

void GetFanlightObjectPosition_float(float3 positionOS, float instanceId, out float3 outPositionOS)
{
    uint seatIndex = PrismFanlightSeatIndex(instanceId);
    float3 positionWS = PrismStabilizeFanlightRadius(positionOS, seatIndex);
    outPositionOS = mul(UNITY_MATRIX_I_M, float4(positionWS, 1.0)).xyz;
}

void GetFanlightWorldPosition_float(float3 positionOS, float instanceId, out float3 outPositionWS)
{
    uint seatIndex = PrismFanlightSeatIndex(instanceId);
    outPositionWS = mul(_FanlightMatrices[seatIndex], float4(positionOS, 1.0)).xyz;
}

#endif

#ifndef PRISM_FANLIGHT_SHADER_GRAPH_HELPER_INCLUDED
#define PRISM_FANLIGHT_SHADER_GRAPH_HELPER_INCLUDED

StructuredBuffer<uint> _VisibleIndices;
StructuredBuffer<float4x4> _FanlightMatrices;
StructuredBuffer<float4> _FanlightColors;

uint PrismFanlightVisibleIndex(float instanceId)
{
    return (uint)max(0.0, instanceId);
}

uint PrismFanlightSeatIndex(float instanceId)
{
    return _VisibleIndices[PrismFanlightVisibleIndex(instanceId)];
}

float3 PrismFanlightObjectPosition(float3 positionOS, uint seatIndex)
{
    float4 positionWS = mul(_FanlightMatrices[seatIndex], float4(positionOS, 1.0));
    return mul(unity_WorldToObject, positionWS).xyz;
}

void GetFanlightColor_float(float instanceId, out float4 color)
{
    uint seatIndex = PrismFanlightSeatIndex(instanceId);
    color = _FanlightColors[seatIndex];
}

void GetFanlightObjectPosition_float(float3 positionOS, float instanceId, out float3 outPositionOS)
{
    uint seatIndex = PrismFanlightSeatIndex(instanceId);
    outPositionOS = PrismFanlightObjectPosition(positionOS, seatIndex);
}

void GetFanlightVertexDataObject_float(float3 positionOS, float instanceId, out float3 outPositionOS, out float4 color)
{
    uint seatIndex = PrismFanlightSeatIndex(instanceId);
    outPositionOS = PrismFanlightObjectPosition(positionOS, seatIndex);
    color = _FanlightColors[seatIndex];
}

#endif

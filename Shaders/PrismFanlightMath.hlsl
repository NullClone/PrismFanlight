static const float PRISM_FANLIGHT_PI = 3.14159265359;

float Hash11(float n)
{
    return frac(sin(n) * 43758.5453123);
}

float Hash21(float2 p)
{
    return frac(sin(dot(p, float2(12.9898, 78.233))) * 43758.5453123);
}

float Noise21(float2 p)
{
    float2 i = floor(p);
    float2 f = frac(p);
    float a = Hash21(i);
    float b = Hash21(i + float2(1, 0));
    float c = Hash21(i + float2(0, 1));
    float d = Hash21(i + float2(1, 1));
    float2 u = f * f * (3.0 - 2.0 * f);
    return lerp(lerp(a, b, u.x), lerp(c, d, u.x), u.y) * 2.0 - 1.0;
}

float3 HsvToRgb(float h, float s, float v)
{
    float3 k = float3(1.0, 2.0 / 3.0, 1.0 / 3.0);
    float3 p = abs(frac(h.xxx + k) * 6.0 - 3.0);
    return v * lerp(float3(1.0, 1.0, 1.0), saturate(p - 1.0), s);
}

float4x4 Translate(float3 t)
{
    return float4x4(
        1, 0, 0, t.x,
        0, 1, 0, t.y,
        0, 0, 1, t.z,
        0, 0, 0, 1);
}

float4x4 AxisAngle(float3 axis, float angle)
{
    axis = normalize(axis);
    float s = sin(angle);
    float c = cos(angle);
    float oc = 1.0 - c;

    return float4x4(
        oc * axis.x * axis.x + c, oc * axis.x * axis.y - axis.z * s, oc * axis.z * axis.x + axis.y * s, 0,
        oc * axis.x * axis.y + axis.z * s, oc * axis.y * axis.y + c, oc * axis.y * axis.z - axis.x * s, 0,
        oc * axis.z * axis.x - axis.y * s, oc * axis.y * axis.z + axis.x * s, oc * axis.z * axis.z + c, 0,
        0, 0, 0, 1);
}

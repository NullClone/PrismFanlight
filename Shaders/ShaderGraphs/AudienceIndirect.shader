Shader "Hidden/AudienceIndirect"
{
    Properties
    {
        _Color ("Opacity", Color) = (1, 1, 1, 1)

        [Header(Colors)]
        _SkinColor ("Skin (Head)", Color) = (0.92, 0.78, 0.69, 1)
        _ClothColor ("Clothing (Body / Arm)", Color) = (0.22, 0.26, 0.4, 1)

        [Header(Shading)]
        _LightDir ("Light Dir (billboard xyz)", Vector) = (0.35, 0.55, 0.75, 0)
        _Ambient ("Ambient Floor", Range(0, 1)) = 0.35
        _GroundShade ("Ground Shade (body)", Range(0, 1)) = 0.35
        _DepthCutoff ("Depth Cutoff", Range(0.001, 1)) = 0.35

        [Header(Rim)]
        _RimColor ("Rim Color", Color) = (0.7, 0.85, 1, 1)
        _RimPower ("Rim Power", Range(0.5, 8)) = 3
        _RimStrength ("Rim Strength", Range(0, 4)) = 1.3
        _PenlightRimTint ("Penlight Rim Tint", Range(0, 1)) = 0.6

    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
            "IgnoreProjector" = "True"
        }

        LOD 100
        Cull Off
        ZWrite On
        ZTest LEqual
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            Name "Unlit"
            Tags
            {
                "LightMode" = "UniversalForward"
            }

            ZWrite On
            ZTest LEqual

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 4.5

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "../PrismFanlightColor.hlsl"

            struct AudiencePart
            {
                float4 p0HalfWidth;
                float4 p1Type;
            };

            StructuredBuffer<AudiencePart> _AudienceParts;
            StructuredBuffer<uint> _VisibleIndices;

            CBUFFER_START(UnityPerMaterial)
                float4 _Color;
                float4 _SkinColor;
                float4 _ClothColor;
                float4 _LightDir;
                float _Ambient;
                float _GroundShade;
                float _DepthCutoff;
                float4 _RimColor;
                float _RimPower;
                float _RimStrength;
                float _PenlightRimTint;
            CBUFFER_END

            struct Attributes
            {
                float3 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float2 shape : TEXCOORD1;
                nointerpolation uint seat : TEXCOORD2;
            };

            float4 SeatPenlightColor(uint seat)
            {
                return PrismFanlightSeatColor(seat);
            }

            float SdfCoverage(float d)
            {
                float aa = max(fwidth(d), 1e-5);
                return saturate(0.5 - d / aa);
            }

            Varyings vert(Attributes IN, uint svInstanceID : SV_InstanceID)
            {
                Varyings OUT;

                uint slot = svInstanceID / 3u;
                uint part = svInstanceID % 3u;
                uint seat = _VisibleIndices[slot];
                AudiencePart bp = _AudienceParts[seat * 3u + part];

                float3 p0 = bp.p0HalfWidth.xyz;
                float halfWidth = bp.p0HalfWidth.w;
                float3 p1 = bp.p1Type.xyz;
                float type = bp.p1Type.w;

                float3 worldPos;
                float lenUnits = 1.0;

                if (type >= 1.5)
                {
                    float3 camRight = UNITY_MATRIX_V._m00_m01_m02;
                    float3 camUp = UNITY_MATRIX_V._m10_m11_m12;
                    worldPos = p0 + (camRight * (IN.uv.x - 0.5) + camUp * (IN.uv.y - 0.5)) * (halfWidth * 2.0);
                }
                else
                {
                    float3 center = lerp(p0, p1, IN.uv.y);
                    float3 axis = p1 - p0;
                    float segLen = max(1e-4, length(axis));
                    axis /= segLen;

                    float3 view = _WorldSpaceCameraPos.xyz - center;
                    float3 side = cross(axis, view);
                    float sideLen = length(side);
                    side = sideLen > 1e-4 ? side / sideLen : float3(1.0, 0.0, 0.0);

                    worldPos = center + side * (IN.uv.x - 0.5) * (halfWidth * 2.0);
                    lenUnits = segLen / max(halfWidth, 1e-4);
                }

                OUT.positionCS = TransformWorldToHClip(worldPos);
                OUT.uv = IN.uv;
                OUT.shape = float2(type, lenUnits);
                OUT.seat = seat;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float2 uv = IN.uv;
                bool isHead = IN.shape.x >= 1.5;
                bool isBody = IN.shape.x < 0.5;

                float coverage;
                float3 N;

                if (isHead)
                {
                    float2 q = (uv - 0.5) * 2.0;
                    float r2 = dot(q, q);
                    coverage = SdfCoverage(sqrt(r2) - 1.0);
                    float nz = sqrt(saturate(1.0 - r2));
                    N = normalize(float3(q.x, q.y, nz));
                }
                else
                {
                    float L = IN.shape.y;
                    float px = (uv.x - 0.5) * 2.0;
                    float py = uv.y * L;
                    float cy = clamp(py, 1.0, max(1.0, L - 1.0));
                    coverage = SdfCoverage(length(float2(px, py - cy)) - 1.0);
                    float nz = sqrt(saturate(1.0 - px * px));
                    N = normalize(float3(px, 0.0, nz));
                }

                float3 baseCol = isHead ? _SkinColor.rgb : _ClothColor.rgb;

                float3 L = normalize(_LightDir.xyz);
                float ndl = dot(N, L) * 0.5 + 0.5;
                float shade = lerp(_Ambient, 1.0, ndl);

                float ground = isBody ? lerp(1.0 - _GroundShade, 1.0, saturate(uv.y)) : 1.0;
                float3 lit = baseCol * shade * ground;

                float rim = pow(saturate(1.0 - N.z), _RimPower) * _RimStrength;
                float3 rimCol = lerp(_RimColor.rgb, SeatPenlightColor(IN.seat).rgb, _PenlightRimTint);

                float3 rgb = lit + rim * rimCol;
                float alpha = coverage * _Color.a;
                clip(alpha - max(_DepthCutoff, 0.001));
                return half4(rgb, alpha);
            }
            ENDHLSL
        }
    }
}
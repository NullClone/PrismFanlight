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

        [Header(Perceptual Crowd)]
        _EdgeSoftness ("Edge Softness", Range(0.5, 4)) = 1.4
        _RimDirectionality ("Rim Directionality", Range(0, 1)) = 0.85
        _RimVariation ("Rim Variation", Range(0, 1)) = 0.7
        _RimPixelRange ("Rim Pixel Range", Vector) = (1.5, 3, 24, 52)
        _LowerBodyAbsorption ("Lower Body Absorption", Range(0, 1)) = 0.9

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
                float _EdgeSoftness;
                float _RimDirectionality;
                float _RimVariation;
                float4 _RimPixelRange;
                float _LowerBodyAbsorption;
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
                nointerpolation float2 shape : TEXCOORD1;
                nointerpolation uint seat : TEXCOORD2;
                nointerpolation float partRadiusPixels : TEXCOORD3;
            };

            float4 SeatPenlightColor(uint seat)
            {
                return PrismFanlightSeatColor(seat);
            }

            uint AudienceHash(uint value)
            {
                value ^= value >> 16u;
                value *= 0x7FEB352Du;
                value ^= value >> 15u;
                value *= 0x846CA68Bu;
                value ^= value >> 16u;
                return value;
            }

            float AudienceRandom(uint seat, uint salt)
            {
                uint assignment = _FanlightColorAssignments[seat];
                uint value = AudienceHash(assignment ^ salt);
                return (value & 0x00FFFFFFu) / 16777215.0;
            }

            float SdfCoverage(float d)
            {
                float aa = max(fwidth(d) * max(0.5, _EdgeSoftness), 1e-5);
                return saturate(0.5 - d / aa);
            }

            float RimSizeMask(float partRadiusPixels)
            {
                float minimumEnd = max(_RimPixelRange.y, _RimPixelRange.x + 1e-3);
                float maximumEnd = max(_RimPixelRange.w, _RimPixelRange.z + 1e-3);
                float minimum = smoothstep(_RimPixelRange.x, minimumEnd, partRadiusPixels);
                float maximum = 1.0 - smoothstep(_RimPixelRange.z, maximumEnd, partRadiusPixels);
                return saturate(minimum * maximum);
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
                OUT.partRadiusPixels = halfWidth
                    * abs(UNITY_MATRIX_P._m11)
                    * (_ScreenParams.y * 0.5)
                    / max(abs(OUT.positionCS.w), 1e-4);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float2 uv = IN.uv;
                bool isHead = IN.shape.x >= 1.5;
                bool isBody = IN.shape.x < 0.5;

                float coverage;
                float3 N;
                float headRandom0 = AudienceRandom(IN.seat, 0xA511E9B3u);
                float headRandom1 = AudienceRandom(IN.seat, 0x63D83595u);
                float rimRandom = AudienceRandom(IN.seat, 0xC2B2AE35u);
                float breakupRandom = AudienceRandom(IN.seat, 0x27D4EB2Fu);

                if (isHead)
                {
                    float2 q = (uv - 0.5) * 2.0;
                    float headWidth = lerp(0.84, 0.96, headRandom0);
                    float jawWidth = lerp(0.78, 0.92, headRandom1);
                    float lowerHead = saturate(-q.y);
                    float upperHead = saturate(q.y);
                    float width = headWidth
                        * lerp(1.0, jawWidth, lowerHead)
                        * lerp(1.0, 0.96, upperHead);
                    float2 shaped = float2(q.x / max(width, 1e-3), q.y);
                    float r2 = dot(shaped, shaped);
                    coverage = SdfCoverage(sqrt(r2) - 1.0);
                    float nz = sqrt(saturate(1.0 - r2));
                    N = normalize(float3(shaped.x, shaped.y, nz));
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

                float lightLengthSquared = dot(_LightDir.xyz, _LightDir.xyz);
                float3 L = lightLengthSquared > 1e-6
                               ? _LightDir.xyz * rsqrt(lightLengthSquared)
                               : float3(0.0, 1.0, 0.0);
                float lightDot = dot(N, L);
                float ndl = lightDot * 0.5 + 0.5;
                float shade = lerp(_Ambient, 1.0, ndl);

                float ground = isBody ? lerp(1.0 - _GroundShade, 1.0, saturate(uv.y)) : 1.0;
                float bodyAbsorption = isBody
                                           ? lerp(1.0, smoothstep(0.08, 0.72, uv.y), _LowerBodyAbsorption)
                                           : 1.0;
                float fillVariation = lerp(0.88, 1.04, headRandom1);
                float3 lit = baseCol * shade * ground * bodyAbsorption * fillVariation;

                float fresnel = pow(saturate(1.0 - N.z), max(0.5, _RimPower));
                float directional = lerp(
                    1.0,
                    smoothstep(-0.15, 0.45, lightDot),
                    _RimDirectionality);
                float seatVariation = lerp(
                    1.0,
                    smoothstep(0.15, 0.9, rimRandom),
                    _RimVariation);
                float breakupWave = sin(
                    (isHead ? atan2(N.y, N.x + 1e-6) : uv.y * 3.14159265)
                    * 1.75
                    + breakupRandom * 6.28318531) * 0.5 + 0.5;
                float breakup = lerp(
                    1.0,
                    smoothstep(0.2, 0.8, breakupWave),
                    _RimVariation * 0.55);
                float partMask = isBody
                                     ? smoothstep(0.2, 0.72, uv.y)
                                     : isHead
                                     ? lerp(0.3, 1.0, smoothstep(0.15, 0.85, uv.y))
                                     : 1.0;
                float rim = fresnel
                    * directional
                    * seatVariation
                    * breakup
                    * partMask
                    * RimSizeMask(IN.partRadiusPixels)
                    * _RimStrength;
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
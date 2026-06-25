Shader "Hidden/AudienceBillboardIndirect"
{
    // 観客（体・腕・頭）のビルボードシェーダー。
    // PrismFanlightIndirect.compute が書き出した _AudienceParts を展開し、
    // フラグメントで SDF により形を出す（partType で分岐）。
    //   体・腕(type<2): p0->p1 のカメラ正対リボン  → カプセル SDF（円柱断面で陰影）
    //   頭   (type>=2): 中心 p0・半径 w の全方位ビルボード → 円 SDF（球断面で陰影）
    // 立体感は SDF から復元した擬似法線でハーフランバート＋リム発光。
    // 肌色/服色を席ごとにわずかにばらして「群衆」感を出す。リムはその席の
    // ペンライト色で染められる（ステージ逆光の雰囲気）。
    Properties
    {
        _MainTex ("Texture (RGBA)", 2D) = "white" {}
        _Color ("Opacity / Tex Tint", Color) = (1, 1, 1, 1)

        [Header(Colors)]
        _SkinColor ("Skin (Head)", Color) = (0.92, 0.78, 0.69, 1)
        _ClothColor ("Clothing (Body / Arm)", Color) = (0.22, 0.26, 0.4, 1)

        [Header(Shading)]
        _LightDir ("Light Dir (billboard xyz)", Vector) = (0.35, 0.55, 0.75, 0)
        _Ambient ("Ambient Floor", Range(0, 1)) = 0.35
        _GroundShade ("Ground Shade (body)", Range(0, 1)) = 0.35

        [Header(Rim)]
        _RimColor ("Rim Color", Color) = (0.7, 0.85, 1, 1)
        _RimPower ("Rim Power", Range(0.5, 8)) = 3
        _RimStrength ("Rim Strength", Range(0, 4)) = 1.3
        _PenlightRimTint ("Penlight Rim Tint", Range(0, 1)) = 0.6

        [Header(Per Seat Variation)]
        _HueVariation ("Hue Variation", Range(0, 1)) = 0.06
        _ValueVariation ("Value Variation", Range(0, 1)) = 0.3
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
        ZWrite Off
        ZTest LEqual
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            Name "Unlit"
            Tags
            {
                "LightMode" = "UniversalForward"
            }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 4.5

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct AudiencePart
            {
                float4 p0HalfWidth; // xyz: 始点/中心(World), w: 半幅
                float4 p1Type; // xyz: 終点(World), w: 種別 (0:体, 1:腕, 2:頭)
            };

            StructuredBuffer<AudiencePart> _AudienceParts;
            StructuredBuffer<uint> _VisibleIndices;

            // ペンライト色（DrawAudience が MaterialPropertyBlock でバインド）
            StructuredBuffer<float4> _FanlightColors;
            int _FanlightColorSource;
            float4 _FanlightGlobalColor;
            float _FanlightGlobalIntensity;

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _Color;
                float4 _SkinColor;
                float4 _ClothColor;
                float4 _LightDir;
                float _Ambient;
                float _GroundShade;
                float4 _RimColor;
                float _RimPower;
                float _RimStrength;
                float _PenlightRimTint;
                float _HueVariation;
                float _ValueVariation;
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
                float2 shape : TEXCOORD1; // x: partType, y: カプセル長/半幅
                nointerpolation uint seat : TEXCOORD2;
            };

            // 整数ハッシュ（席ごとのばらつき用、0..1）
            float Hash11(uint n)
            {
                n = (n ^ 61u) ^ (n >> 16);
                n *= 9u;
                n = n ^ (n >> 4);
                n *= 0x27d4eb2du;
                n = n ^ (n >> 15);
                return float(n & 0x00ffffffu) / float(0x01000000);
            }

            float3 RGBToHSV(float3 c)
            {
                float4 K = float4(0.0, -1.0 / 3.0, 2.0 / 3.0, -1.0);
                float4 p = lerp(float4(c.bg, K.wz), float4(c.gb, K.xy), step(c.b, c.g));
                float4 q = lerp(float4(p.xyw, c.r), float4(c.r, p.yzx), step(p.x, c.r));
                float d = q.x - min(q.w, q.y);
                float e = 1.0e-10;
                return float3(abs(q.z + (q.w - q.y) / (6.0 * d + e)), d / (q.x + e), q.x);
            }

            float3 HSVToRGB(float3 c)
            {
                float4 K = float4(1.0, 2.0 / 3.0, 1.0 / 3.0, 3.0);
                float3 p = abs(frac(c.xxx + K.xyz) * 6.0 - K.www);
                return c.z * lerp(K.xxx, saturate(p - K.xxx), c.y);
            }

            float4 SeatPenlightColor(uint seat)
            {
                if (_FanlightColorSource == 0)
                {
                    return float4(_FanlightGlobalColor.rgb * _FanlightGlobalIntensity, _FanlightGlobalColor.a);
                }
                float4 c = _FanlightColors[seat];
                c.rgb *= _FanlightGlobalIntensity;
                return c;
            }

            // 1px 程度のアンチエイリアスを掛けた被覆。d<0 が内側。
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
                    // 頭: スクリーン正対の全方位ビルボード（真上から見ても円盤として残る）。
                    float3 camRight = UNITY_MATRIX_V._m00_m01_m02;
                    float3 camUp = UNITY_MATRIX_V._m10_m11_m12;
                    worldPos = p0 + (camRight * (IN.uv.x - 0.5) + camUp * (IN.uv.y - 0.5)) * (halfWidth * 2.0);
                }
                else
                {
                    // 体・腕: p0->p1 を結ぶカメラ正対リボンに展開。
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
                float3 N; // 擬似法線（x:横, y:縦, z:カメラ方向）

                if (isHead)
                {
                    // 頭: 円 SDF + 球断面の法線
                    float2 q = (uv - 0.5) * 2.0; // [-1,1]
                    float r2 = dot(q, q);
                    coverage = SdfCoverage(sqrt(r2) - 1.0);
                    float nz = sqrt(saturate(1.0 - r2));
                    N = normalize(float3(q.x, q.y, nz));
                }
                else
                {
                    // 体・腕: 縦カプセル SDF + 円柱断面の法線
                    float L = IN.shape.y;
                    float px = (uv.x - 0.5) * 2.0; // 横 [-1,1]
                    float py = uv.y * L; // 縦 [0,L]
                    float cy = clamp(py, 1.0, max(1.0, L - 1.0));
                    coverage = SdfCoverage(length(float2(px, py - cy)) - 1.0);
                    float nz = sqrt(saturate(1.0 - px * px));
                    N = normalize(float3(px, 0.0, nz));
                }

                // ベース色：頭=肌 / 体腕=服。席ごとに色相・明度をわずかにばらす。
                float3 baseCol = isHead ? _SkinColor.rgb : _ClothColor.rgb;
                float vh = Hash11(IN.seat * 2u + 1u);
                float vv = Hash11(IN.seat * 2u + 7u);
                float3 hsv = RGBToHSV(baseCol);
                hsv.x = frac(hsv.x + (vh - 0.5) * _HueVariation);
                hsv.z = saturate(hsv.z * lerp(1.0 - _ValueVariation, 1.0 + _ValueVariation, vv));
                baseCol = HSVToRGB(hsv);

                // 擬似ライティング（ハーフランバート＋アンビエント床）
                float3 L = normalize(_LightDir.xyz);
                float ndl = dot(N, L) * 0.5 + 0.5;
                float shade = lerp(_Ambient, 1.0, ndl);

                // 接地感：体だけ足元(uv.y=0)を暗く
                float ground = isBody ? lerp(1.0 - _GroundShade, 1.0, saturate(uv.y)) : 1.0;
                float3 lit = baseCol * shade * ground;

                // リム：シルエット端（N.z→0）。その席のペンライト色で染める。
                float rim = pow(saturate(1.0 - N.z), _RimPower) * _RimStrength;
                float3 rimCol = lerp(_RimColor.rgb, SeatPenlightColor(IN.seat).rgb, _PenlightRimTint);

                half4 tex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, TRANSFORM_TEX(uv, _MainTex));
                float3 rgb = (lit + rim * rimCol) * tex.rgb;
                float alpha = coverage * tex.a * _Color.a;
                return half4(rgb, alpha);
            }
            ENDHLSL
        }
    }
}
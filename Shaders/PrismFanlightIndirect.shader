Shader "Prism Fanlight/Indirect Unlit"
{
    Properties
    {
        _BaseColor ("Base Color", Color) = (1, 1, 1, 1)
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
        }

        Pass
        {
            Name "Forward"
            Blend SrcAlpha One
            ZWrite Off
            Cull Back

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 4.5

            #include "UnityCG.cginc"

            StructuredBuffer<float4x4> _FanlightMatrices;
            StructuredBuffer<float4> _FanlightColors;
            float4 _BaseColor;

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                uint instanceID : SV_InstanceID;
            };

            struct v2f
            {
                float4 positionCS : SV_POSITION;
                float4 color : COLOR;
            };

            v2f vert(appdata v)
            {
                v2f o;
                float4 world = mul(_FanlightMatrices[v.instanceID], float4(v.vertex.xyz, 1.0));
                o.positionCS = mul(UNITY_MATRIX_VP, world);
                o.color = _FanlightColors[v.instanceID] * _BaseColor;
                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                return i.color;
            }
            ENDHLSL
        }
    }
}

Shader "CosmicSlice/SoftParticleGlow"
{
    Properties
    {
        _Color ("Tint", Color) = (0.1, 0.8, 1, 1)
        _CoreStrength ("Core Strength", Range(0.5, 4)) = 2.2
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Blend SrcAlpha One
        ZWrite Off
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            fixed4 _Color;
            float _CoreStrength;

            v2f vert(appdata input)
            {
                v2f output;
                output.vertex = UnityObjectToClipPos(input.vertex);
                output.color = input.color;
                output.uv = input.uv;
                return output;
            }

            fixed4 frag(v2f input) : SV_Target
            {
                float2 centered = input.uv * 2.0 - 1.0;
                float radius = length(centered);
                float softEdge = 1.0 - smoothstep(0.18, 1.0, radius);
                float core = pow(saturate(1.0 - radius), 3.0) * _CoreStrength;
                fixed4 color = _Color * input.color;
                color.rgb *= 1.0 + core;
                color.a *= softEdge;
                return color;
            }
            ENDCG
        }
    }
}

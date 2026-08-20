Shader "CosmicSlice/AdditiveGlow"
{
    Properties
    {
        _Color ("Color", Color) = (0.1, 0.8, 1, 1)
        _PulseSpeed ("Pulse Speed", Float) = 2.5
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
            float _PulseSpeed;

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
                float pulse = 0.78 + sin(_Time.y * _PulseSpeed + input.uv.x * 6.283) * 0.22;
                fixed4 color = _Color * input.color;
                color.rgb *= pulse * 1.7;
                return color;
            }
            ENDCG
        }
    }
}

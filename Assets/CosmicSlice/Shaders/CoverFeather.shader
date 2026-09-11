Shader "CosmicSlice/CoverFeather"
{
    Properties
    {
        _MainTex ("Cover", 2D) = "white" {}
        _Tint ("Tint", Color) = (1, 1, 1, 1)
        _EdgeColor ("Edge Color", Color) = (0, 0, 0, 1)
        _Feather ("Feather", Range(0.01, 0.25)) = 0.16
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            struct v2f
            {
                float4 vertex : SV_POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _Tint;
            fixed4 _EdgeColor;
            float _Feather;

            v2f vert(appdata input)
            {
                v2f output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_OUTPUT(v2f, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.vertex = UnityObjectToClipPos(input.vertex);
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                output.color = input.color;
                return output;
            }

            fixed4 frag(v2f input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                fixed4 cover = tex2D(_MainTex, input.uv) * input.color * _Tint;
                float2 edgeDistance = min(input.uv, 1.0 - input.uv);
                float2 softEdges = smoothstep(0.0, max(0.001, _Feather), edgeDistance);
                float interior = softEdges.x * softEdges.y;
                cover.rgb = lerp(_EdgeColor.rgb, cover.rgb, interior);
                return cover;
            }
            ENDCG
        }
    }
}

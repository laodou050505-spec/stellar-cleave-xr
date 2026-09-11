Shader "CosmicSlice/MeteorTrail"
{
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
            #pragma multi_compile_instancing
            #include "UnityCG.cginc"
            struct appdata { float4 vertex : POSITION; float4 color : COLOR; float2 uv : TEXCOORD0; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct v2f { float4 vertex : SV_POSITION; float4 color : COLOR; float2 uv : TEXCOORD0; UNITY_VERTEX_OUTPUT_STEREO };
            v2f vert(appdata input)
            {
                v2f output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_OUTPUT(v2f, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.vertex = UnityObjectToClipPos(input.vertex);
                output.color = input.color; output.uv = input.uv;
                return output;
            }
            float4 frag(v2f input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float across = abs(input.uv.y * 2 - 1);
                float plasma = exp(-10.5 * across * across) * (1 - smoothstep(0.62, 1, across));
                float spine = exp(-90 * across * across);
                float lengthFade = smoothstep(0, 0.09, input.uv.x) * (1 - smoothstep(0.54, 1, input.uv.x));
                float energy = saturate((plasma * 0.82 + spine * 0.35) * lengthFade);
                float3 saturatedTint = saturate(input.color.rgb * 1.55);
                float3 color = saturatedTint * plasma * 1.28 + lerp(saturatedTint, float3(0.9, 1, 0.98), 0.28) * spine * 0.46;
                return float4(color * lengthFade, energy * input.color.a);
            }
            ENDCG
        }
    }
}

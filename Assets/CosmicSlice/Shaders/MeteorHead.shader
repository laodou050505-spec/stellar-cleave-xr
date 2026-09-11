Shader "CosmicSlice/MeteorHead"
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
                float2 p = input.uv * 2 - 1;
                // A soft elliptical coma with a needle-bright heart. The alpha follows the
                // visible energy so PICO passthrough never composites the billboard quad.
                float ellipse = p.x * p.x * 0.72 + p.y * p.y * 1.45;
                float halo = exp(-4.8 * ellipse) * smoothstep(1.0, 0.34, ellipse);
                float core = exp(-64.0 * ellipse);
                float horizontalGlint = exp(-115.0 * p.y * p.y) * exp(-3.8 * p.x * p.x);
                float energy = saturate(halo * 0.62 + core + horizontalGlint * 0.18);
                float3 saturatedTint = saturate(input.color.rgb * 1.42);
                float3 color = saturatedTint * halo * 1.32;
                color += lerp(saturatedTint, float3(1.0, 0.985, 0.94), 0.38) * (core * 1.85 + horizontalGlint * 0.22);
                return float4(color, energy * input.color.a);
            }
            ENDCG
        }
    }
}

Shader "CosmicSlice/PortalNebula"
{
    Properties
    {
        _MainTex ("Tripo Blue Violet Nebula", 2D) = "black" {}
        _Layer ("Cloud Depth Layer", Float) = 0
        _OuterRadius ("Ceiling Bounded Radius", Float) = 2.7
        _FlowTime ("Visible Flow Time", Float) = 0
        _Reveal ("Arrival", Range(0,1)) = 0
    }
    SubShader
    {
        Tags { "Queue"="Transparent-30" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Cull Off
        ZWrite Off
        Blend One OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "UnityCG.cginc"
            sampler2D _MainTex;
            float _Layer, _OuterRadius, _FlowTime, _Reveal;
            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            struct v2f
            {
                float4 vertex : SV_POSITION;
                float3 local : TEXCOORD0;
                float2 uv : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };
            v2f vert(appdata input)
            {
                v2f output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_OUTPUT(v2f, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                float angle = input.uv.y * 6.2831853;
                float radius = lerp(1.32, _OuterRadius, input.uv.x);
                float wave = sin(angle * 3.0 + input.uv.x * 7.0 - _FlowTime * 0.16 + _Layer * 2.0);
                input.vertex.xz = float2(cos(angle), sin(angle)) * radius;
                // A soft shallow cloud skin with physical parallax, never a hanging cone.
                input.vertex.y = -0.014 - _Layer * 0.045 - (0.022 + wave * 0.020) * sin(input.uv.x * 3.1415927);
                output.local = input.vertex.xyz;
                output.uv = input.uv;
                output.vertex = UnityObjectToClipPos(input.vertex);
                return output;
            }
            float hash21(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }
            fixed4 frag(v2f input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float3 eye = mul(unity_WorldToObject, float4(_WorldSpaceCameraPos,1)).xyz;
                clip(-eye.y - 0.02);
                float radius = length(input.local.xz);
                float t = input.uv.x;
                float angle = atan2(input.local.z,input.local.x);
                float flow = _FlowTime * lerp(0.035,0.020,_Layer);
                float shear = sin(t * 7.0 - _FlowTime * 0.10 + _Layer) * 0.035;
                float a = angle - flow + _Layer * 1.9 + shear;
                float sampleRadius = lerp(0.21,0.47,t) + sin(angle*3.0+_FlowTime*0.12)*0.018;
                float2 uv = 0.5 + float2(cos(a),sin(a)) * sampleRadius;
                float3 cloud = tex2D(_MainTex, uv).rgb;
                float density = max(cloud.r,max(cloud.g,cloud.b));
                float breakup = 0.60 + 0.40 * sin(angle*3.0+t*5.0-_FlowTime*0.13+_Layer);
                float edge = smoothstep(1.38,1.65,radius) * (1.0-smoothstep(0.58,1.0,t));
                float arrival = _Reveal * _Reveal * (3.0-2.0*_Reveal);
                float opacity = edge * breakup * arrival;
                float3 tint = lerp(float3(0.68,0.84,1.0),float3(0.88,0.64,1.0),_Layer);
                float energy = lerp(0.72,0.28,_Layer);
                float3 color = cloud * tint * energy;
                // Sparse coherent dust cells drift with the gas instead of screen-space noise.
                float2 cells = float2(cos(a),sin(a)) * sampleRadius * 180.0;
                float2 id = floor(cells);
                float2 q = frac(cells)-0.5;
                float star = (1.0-smoothstep(0.035,0.16,length(q))) * step(0.989,hash21(id));
                star *= smoothstep(0.008,0.08,density) * (0.70+0.30*sin(_FlowTime*0.7+hash21(id+3.1)*6.2831853));
                color += float3(0.40,0.64,0.95) * star * (1.0-_Layer) * 0.38;
                // Source black is transparent; preserve passthrough alpha, no black rectangle.
                float alpha = saturate(density * energy * 0.58 + star*0.12) * opacity;
                return fixed4(color * opacity,alpha);
            }
            ENDCG
        }
    }
}

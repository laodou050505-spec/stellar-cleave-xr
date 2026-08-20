Shader "CosmicSlice/PortalVoid"
{
    Properties
    {
        _Accent ("Accent", Color) = (0.05, 0.85, 1, 1)
        _Hot ("Hot Edge", Color) = (1, 0.36, 0.12, 1)
        _Core ("Core", Color) = (0.001, 0.002, 0.004, 1)
        _Speed ("Speed", Float) = 1.0
    }
    SubShader
    {
        Tags { "Queue"="Geometry" "RenderType"="Opaque" }
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
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            fixed4 _Accent;
            fixed4 _Hot;
            fixed4 _Core;
            float _Speed;

            v2f vert(appdata input)
            {
                v2f output;
                output.vertex = UnityObjectToClipPos(input.vertex);
                output.uv = input.uv;
                return output;
            }

            fixed4 frag(v2f input) : SV_Target
            {
                float2 p = input.uv * 2.0 - 1.0;
                float radius = length(p);
                float angle = atan2(p.y, p.x);
                float time = _Time.y * _Speed;
                float spiralA = sin(radius * 58.0 - angle * 7.0 - time * 4.2);
                float spiralB = sin(radius * 31.0 + angle * 4.0 + time * 2.1);
                float turbulence = sin((p.x + p.y) * 23.0 - time * 1.4) * sin((p.x - p.y) * 17.0 + time * 1.8);
                float diskMask = smoothstep(0.34, 0.43, radius) * (1.0 - smoothstep(0.92, 1.0, radius));
                float stream = saturate(spiralA * 0.44 + spiralB * 0.24 + turbulence * 0.16 + 0.14) * diskMask;
                float photonRing = 1.0 - smoothstep(0.0, 0.028, abs(radius - 0.405));
                float hotRing = 1.0 - smoothstep(0.0, 0.045, abs(radius - 0.58));
                float outerLens = 1.0 - smoothstep(0.0, 0.035, abs(radius - 0.91));
                float coreMask = 1.0 - smoothstep(0.29, 0.39, radius);
                fixed3 color = _Core.rgb * (0.82 + coreMask * 0.18);
                color += _Accent.rgb * (stream * 0.92 + photonRing * 1.45 + outerLens * 0.16);
                color += _Hot.rgb * (hotRing * (0.48 + spiralB * 0.1) + stream * smoothstep(0.48, 0.82, radius) * 0.3);
                color *= 1.0 - coreMask * 0.92;
                return fixed4(color, 1.0);
            }
            ENDCG
        }
    }
}

Shader "CosmicSlice/InsideSpacePanorama"
{
    Properties
    {
        _MainTex ("Panorama", 2D) = "black" {}
        _Tint ("Tint", Color) = (0.76, 0.9, 1, 1)
        _Exposure ("Exposure", Range(0, 3)) = 0.9
        _Opacity ("MR Veil Opacity", Range(0, 1)) = 1
        _Softness ("Panorama Softness", Range(0, 2)) = 0
        _AdaptiveAlpha ("MR Content Adaptive Alpha", Range(0, 1)) = 0
    }
    SubShader
    {
        Tags { "Queue"="Background" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha
        Cull Front
        ZWrite Off
        ZTest LEqual
        Fog { Mode Off }

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

            sampler2D _MainTex;
            fixed4 _Tint;
            float _Exposure;
            float _Opacity;
            float _Softness;
            float _AdaptiveAlpha;

            v2f vert(appdata input)
            {
                v2f output;
                output.vertex = UnityObjectToClipPos(input.vertex);
                output.uv = input.uv;
                return output;
            }

            fixed4 frag(v2f input) : SV_Target
            {
                float2 blurStep = float2(0.0016, 0.0011) * _Softness;
                fixed4 panorama = tex2D(_MainTex, input.uv) * 0.4;
                panorama += tex2D(_MainTex, input.uv + float2(blurStep.x, 0)) * 0.15;
                panorama += tex2D(_MainTex, input.uv - float2(blurStep.x, 0)) * 0.15;
                panorama += tex2D(_MainTex, input.uv + float2(0, blurStep.y)) * 0.15;
                panorama += tex2D(_MainTex, input.uv - float2(0, blurStep.y)) * 0.15;
                panorama.rgb *= _Tint.rgb * _Exposure;
                float spaceSignal = max(panorama.r, max(panorama.g, panorama.b));
                float featureMask = smoothstep(0.022, 0.17, spaceSignal);
                float alpha = _Opacity * lerp(1.0, featureMask, _AdaptiveAlpha);
                return fixed4(panorama.rgb, alpha);
            }
            ENDCG
        }
    }
}

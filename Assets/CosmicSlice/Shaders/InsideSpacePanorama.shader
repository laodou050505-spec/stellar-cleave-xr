Shader "CosmicSlice/InsideSpacePanorama"
{
    Properties
    {
        _MainTex ("Panorama", 2D) = "black" {}
        _Tint ("Tint", Color) = (0.76, 0.9, 1, 1)
        _Exposure ("Exposure", Range(0, 3)) = 0.9
        _Opacity ("Immersive Opacity (locked at 1)", Range(0, 1)) = 1
        _Softness ("Panorama Softness", Range(0, 2)) = 0
        _AdaptiveAlpha ("Legacy MR Adaptive Alpha (disabled)", Range(0, 1)) = 0
    }
    SubShader
    {
        Tags { "Queue"="Background" "RenderType"="Opaque" "IgnoreProjector"="True" }
        Blend Off
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
            float _Softness;

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
                // This is the immersive outer shell, not an MR veil. Keep alpha physically
                // opaque as well as disabling blending so the compositor can never reveal
                // passthrough through dark or low-signal portions of the panorama.
                return fixed4(panorama.rgb, 1.0);
            }
            ENDCG
        }
    }
}

Shader "CosmicSlice/PortalVoid"
{
    Properties
    {
        _MouthRadius ("Opening Radius", Float) = 1.48
        _Depth ("Upward Depth", Float) = 1.85
        _Speed ("Inward Flow Speed", Float) = 0.32
        _RimOnly ("Feathered Rim", Float) = 0
        _TunnelShear ("Head Centre Refraction", Vector) = (0,0,0,0)
    }
    SubShader
    {
        Tags { "Queue"="Geometry+490" "RenderType"="Transparent" }
        Cull Off
        CGINCLUDE
        #include "UnityCG.cginc"
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
            float3 physical : TEXCOORD2;
            UNITY_VERTEX_OUTPUT_STEREO
        };
        float _MouthRadius, _Depth, _Speed, _RimOnly;
        float4 _TunnelShear;
        float hash31(float3 p)
        {
            p = frac(p * 0.1031);
            p += dot(p, p.yzx + 33.33);
            return frac((p.x + p.y) * p.z);
        }
        float noise3(float3 p)
        {
            float3 i = floor(p), f = frac(p);
            f = f*f*(3.0-2.0*f);
            return lerp(lerp(lerp(hash31(i),hash31(i+float3(1,0,0)),f.x),
                             lerp(hash31(i+float3(0,1,0)),hash31(i+float3(1,1,0)),f.x),f.y),
                        lerp(lerp(hash31(i+float3(0,0,1)),hash31(i+float3(1,0,1)),f.x),
                             lerp(hash31(i+float3(0,1,1)),hash31(i+1.0),f.x),f.y),f.z);
        }
        v2f vert(appdata input)
        {
            v2f output;
            UNITY_SETUP_INSTANCE_ID(input);
            UNITY_INITIALIZE_OUTPUT(v2f, output);
            UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
            output.local = input.vertex.xyz;
            output.uv = input.uv;
            // Shared head-centre shear keeps the vortex centre behind the aperture
            // without per-eye billboarding or moving its fixed ceiling attachment.
            if (_RimOnly < 0.5)
                input.vertex.xz += _TunnelShear.xz * max(input.vertex.y, 0.0);
            output.physical = input.vertex.xyz;
            output.vertex = UnityObjectToClipPos(input.vertex);
            return output;
        }
        float3 aperturePoint(v2f input)
        {
            float3 eye = mul(unity_WorldToObject, float4(_WorldSpaceCameraPos,1)).xyz;
            clip(-eye.y - 0.01);
            float3 ray = input.physical - eye;
            return eye + ray * (-eye.y / max(ray.y,0.00001));
        }
        fixed4 depthOnly(v2f input) : SV_Target
        {
            UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
            clip(0.5 - _RimOnly);
            clip(_MouthRadius - length(aperturePoint(input).xz));
            return 0;
        }
        fixed4 frag(v2f input) : SV_Target
        {
            UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
            float3 aperture = aperturePoint(input);
            float r = length(aperture.xz) / max(_MouthRadius,0.001);
            float d = saturate(input.uv.y);
            float angle = atan2(input.local.z,input.local.x);
            float time = _Time.y * _Speed;
            float twist = angle + d*8.6 - time;
            float3 p = float3(cos(twist)*3.8,sin(twist)*3.8,d*12.0-time*0.8);
            float cloud = noise3(p)*0.57 + noise3(p*2.07+7.2)*0.28 + noise3(p*4.13)*0.15;
            float phase = angle*3.0 + d*32.0 - time*2.6 + (cloud-0.5)*1.7;
            float broad = pow(saturate(0.5+0.5*sin(phase)),4.0);
            float strands = pow(saturate(0.5+0.5*sin(angle*11.0+d*108.0-time*8.8+cloud*6.0)),22.0);
            float thin = pow(saturate(0.5+0.5*sin(phase*8.0+cloud*8.0)),42.0);
            float illumination = 0.025+cloud*0.10+broad*0.48+strands*0.30+thin*0.14;
            float fade = pow(1.0-d,2.35)*(1.0-smoothstep(0.60,0.95,d));
            float azimuth = 0.5+0.5*sin(angle-d*5.0+0.8);
            float3 tint = lerp(float3(0.028,0.28,0.72),float3(0.34,0.065,0.42),pow(azimuth,3.0)*0.64);
            float3 color = float3(0.0002,0.0005,0.0015);
            color += tint * illumination * fade * (0.32+cloud*cloud);
            color += float3(0.30,0.67,0.95)*pow(broad,3.0)*strands*fade*1.65;
            float apertureAngle = atan2(aperture.z,aperture.x);
            float3 edgeP = float3(cos(apertureAngle)*5.0,sin(apertureAngle)*5.0,r*9.0-time*0.25);
            float edgeCloud = noise3(edgeP)*0.7 + noise3(edgeP*2.3)*0.3;
            float irregularRadius = r + (edgeCloud-0.5)*0.065;
            float alpha = 1.0-smoothstep(0.77,1.0,irregularRadius);
            if (_RimOnly > 0.5)
            {
                // Independent translucent annulus, not a luminous uniform circle.
                float envelope = smoothstep(0.72,0.88,r)*(1.0-smoothstep(0.94,1.17,irregularRadius));
                float filaments = pow(saturate(0.5+0.5*sin(apertureAngle*7.0-r*29.0+edgeCloud*5.0-time)),14.0);
                color = tint*(0.04+edgeCloud*0.14) + float3(0.20,0.48,0.75)*filaments*0.40;
                alpha = envelope*(0.26+edgeCloud*0.38);
            }
            else
            {
                clip(1.0-r);
                color += float3(0.10,0.28,0.5)*exp(-d*70.0)*broad*edgeCloud*0.24;
                float3 cells = p*35.0;
                float star = pow(saturate(1.0-length(frac(cells)-0.5)*2.0),7.0)*step(0.986,hash31(floor(cells)));
                color += float3(0.48,0.76,1.0)*star*fade*1.5;
            }
            return fixed4(color*alpha,alpha);
        }
        ENDCG
        // Nearest-surface depth first: transparent feather must not let the far-end
        // triangle fan paint a black spike over a nearer section of the tunnel.
        Pass
        {
            ColorMask 0
            ZWrite On
            Blend Off
            CGPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment depthOnly
            #pragma multi_compile_instancing
            ENDCG
        }
        Pass
        {
            ZWrite Off
            Blend One OneMinusSrcAlpha
            CGPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            ENDCG
        }
    }
}

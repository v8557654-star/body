Shader "SeaDrift/Ocean"
{
    Properties
    {
        _DeepColor ("Deep Color", Color) = (0.02, 0.15, 0.3, 1)
        _ShallowColor ("Shallow Color", Color) = (0.05, 0.5, 0.6, 1)
        _SpecColor ("Specular", Color) = (1,1,1,1)
        _Smoothness ("Smoothness", Range(0,1)) = 0.9
        _WaveScale ("Wave Scale", Float) = 1
        _FresnelPower ("Fresnel Power", Float) = 5
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        LOD 200

        CGPROGRAM
        #pragma surface surf Standard vertex:vert fullforwardshadows
        #pragma target 3.0

        struct Input
        {
            float3 worldPos;
            float3 worldNormal;
            float waveHeight;
        };

        fixed4 _DeepColor;
        fixed4 _ShallowColor;
        float _Smoothness;
        float _WaveScale;
        float _FresnelPower;

        // Wave data from script
        float4 _WaveA; // dirX, dirY, steep, wavelength
        float4 _WaveB; // amp, speed, k, c
        float _TimeScale;

        // Gerstner function
        float3 GerstnerWave(float4 waveA, float4 waveB, float3 pos, float time, inout float3 tangent, inout float3 binormal)
        {
            float2 dir = waveA.xy;
            float steep = waveA.z;
            float wavelength = waveA.w;
            float amp = waveB.x;
            float speed = waveB.y;
            float k = waveB.z;
            float c = waveB.w;

            float f = k * (dot(dir, pos.xz) - c * time * speed * 0.1);
            float sinF = sin(f);
            float cosF = cos(f);

            float3 disp;
            disp.x = -dir.x * amp * sinF * steep;
            disp.y = amp * cosF;
            disp.z = -dir.y * amp * sinF * steep;

            // derivatives for normal
            tangent.x += -k * dir.x * dir.x * steep * amp * cosF;
            tangent.y += -k * dir.x * amp * sinF;
            tangent.z += -k * dir.x * dir.y * steep * amp * cosF;

            binormal.x += -k * dir.y * dir.x * steep * amp * cosF;
            binormal.y += -k * dir.y * amp * sinF;
            binormal.z += -k * dir.y * dir.y * steep * amp * cosF;

            return disp;
        }

        void vert(inout appdata_full v, out Input o)
        {
            UNITY_INITIALIZE_OUTPUT(Input, o);
            float time = _Time.y * _TimeScale;

            float3 pos = v.vertex.xyz;
            float3 tangent = float3(1,0,0);
            float3 binormal = float3(0,0,1);
            float3 disp = float3(0,0,0);

            // 4 волны для шейдера (оптимизация)
            float4 wa0 = float4(1,0.3,0.12,60);
            float4 wb0 = float4(1.5,8,0.104,9.7);
            float4 wa1 = float4(0.7,0.7,0.08,35);
            float4 wb1 = float4(0.8,6,0.179,7.4);
            float4 wa2 = float4(-0.3,1,0.05,20);
            float4 wb2 = float4(0.4,4,0.314,5.5);
            float4 wa3 = float4(0.2,-1,0.04,12);
            float4 wb3 = float4(0.25,3,0.523,4.3);

            wa0.xy = normalize(wa0.xy); wa1.xy = normalize(wa1.xy);
            wa2.xy = normalize(wa2.xy); wa3.xy = normalize(wa3.xy);

            float3 worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;

            disp += GerstnerWave(wa0, wb0, worldPos, time, tangent, binormal);
            disp += GerstnerWave(wa1, wb1, worldPos, time, tangent, binormal);
            disp += GerstnerWave(wa2, wb2, worldPos, time, tangent, binormal);
            disp += GerstnerWave(wa3, wb3, worldPos, time, tangent, binormal);

            v.vertex.xyz += disp * _WaveScale;
            float3 normal = normalize(cross(binormal, tangent));
            v.normal = normal;

            o.waveHeight = disp.y;
            o.worldPos = worldPos + disp;
        }

        void surf(Input IN, inout SurfaceOutputStandard o)
        {
            // Цвет зависит от высоты волны и угла
            float heightFactor = saturate(IN.waveHeight * 0.3 + 0.5);
            float fresnel = pow(1 - saturate(dot(IN.worldNormal, float3(0,1,0))), _FresnelPower);
            
            fixed4 col = lerp(_DeepColor, _ShallowColor, heightFactor + fresnel * 0.3);
            
            // Пена на гребнях
            float foam = saturate(IN.waveHeight - 0.8) * 2;
            col = lerp(col, fixed4(1,1,1,1), foam);

            o.Albedo = col.rgb;
            o.Smoothness = _Smoothness;
            o.Metallic = 0;
            o.Normal = IN.worldNormal;
        }
        ENDCG
    }
    FallBack "Diffuse"
}

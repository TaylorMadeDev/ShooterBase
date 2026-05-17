Shader "Custom/ProceduralTape"
{
    Properties
    {
        _BaseColor("Base Color", Color) = (0.6, 0.6, 0.6, 1) // Gray duct tape color
        _Glossiness("Smoothness", Range(0.0, 1.0)) = 0.3
        _WrinkleScale("Wrinkle Scale", Float) = 150.0
        _WrinkleStrength("Wrinkle Strength", Range(0.0, 1.0)) = 0.15
        _ThreadScale("Thread Scale", Float) = 200.0
        _ThreadStrength("Thread Strength", Range(0.0, 1.0)) = 0.1
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Geometry" }
        LOD 100

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float3 normalOS     : NORMAL;
                float2 uv           : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS   : SV_POSITION;
                float3 positionWS   : TEXCOORD0;
                float3 normalWS     : NORMAL;
                float2 uv           : TEXCOORD1;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float _Glossiness;
                float _WrinkleScale;
                float _WrinkleStrength;
                float _ThreadScale;
                float _ThreadStrength;
            CBUFFER_END

            // Simple hash function for procedural noise
            float hash(float2 p) {
                p = frac(p * 0.3183099 + 0.1);
                p *= 17.0;
                return frac(p.x * p.y * (p.x + p.y));
            }

            // Simple value noise
            float noise(float2 x) {
                float2 i = floor(x);
                float2 f = frac(x);
                float a = hash(i);
                float b = hash(i + float2(1.0, 0.0));
                float c = hash(i + float2(0.0, 1.0));
                float d = hash(i + float2(1.0, 1.0));
                float2 u = f * f * (3.0 - 2.0 * f);
                return lerp(a, b, u.x) + (c - a) * u.y * (1.0 - u.x) + (d - b) * u.x * u.y;
            }

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS = TransformWorldToHClip(output.positionWS);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.uv = input.uv;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                Light mainLight = GetMainLight();
                
                // 1. Wrinkles (larger irregularities)
                float wrinkles = noise(input.uv * _WrinkleScale);
                
                // 2. Cloth/Tape thread patterns (fine cross-hatching)
                float threadX = sin(input.uv.x * _ThreadScale) * 0.5 + 0.5;
                float threadY = sin(input.uv.y * _ThreadScale) * 0.5 + 0.5;
                float threads = (threadX + threadY) * 0.5;

                // Combine noises
                float variation = 1.0 
                                - (wrinkles * _WrinkleStrength) 
                                - (threads * _ThreadStrength);
                
                half3 color = _BaseColor.rgb * variation;

                // Basic Lighting
                half NdotL = saturate(dot(normalize(input.normalWS), normalize(mainLight.direction)));
                
                // Simple diffuse + ambient
                half3 litColor = color * (mainLight.color * NdotL + half3(0.2, 0.2, 0.2));

                return half4(litColor, _BaseColor.a);
            }
            ENDHLSL
        }
    }
    
    // Fallback for standard/built-in pipeline just in case
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        LOD 100
        
        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows
        #pragma target 3.0

        struct Input {
            float2 uv_MainTex;
        };

        fixed4 _BaseColor;
        float _Glossiness;
        float _WrinkleScale;
        float _WrinkleStrength;
        float _ThreadScale;
        float _ThreadStrength;

        float hash(float2 p) {
            p = frac(p * 0.3183099 + 0.1);
            p *= 17.0;
            return frac(p.x * p.y * (p.x + p.y));
        }

        float noise(float2 x) {
            float2 i = floor(x);
            float2 f = frac(x);
            float a = hash(i);
            float b = hash(i + float2(1.0, 0.0));
            float c = hash(i + float2(0.0, 1.0));
            float d = hash(i + float2(1.0, 1.0));
            float2 u = f * f * (3.0 - 2.0 * f);
            return lerp(a, b, u.x) + (c - a) * u.y * (1.0 - u.x) + (d - b) * u.x * u.y;
        }

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            float wrinkles = noise(IN.uv_MainTex * _WrinkleScale);
            float threadX = sin(IN.uv_MainTex.x * _ThreadScale) * 0.5 + 0.5;
            float threadY = sin(IN.uv_MainTex.y * _ThreadScale) * 0.5 + 0.5;
            float threads = (threadX + threadY) * 0.5;

            float variation = 1.0 - (wrinkles * _WrinkleStrength) - (threads * _ThreadStrength);
            
            o.Albedo = _BaseColor.rgb * variation;
            o.Smoothness = _Glossiness;
            o.Metallic = 0.0;
        }
        ENDCG
    }
    FallBack "Diffuse"
}

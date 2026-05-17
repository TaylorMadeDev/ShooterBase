Shader "Scrapout/DuctTape"
{
    Properties
    {
        [MainTexture] _BaseMap ("Base Map", 2D) = "white" {}
        [MainColor] _BaseColor ("Base Color", Color) = (0.8, 0.8, 0.8, 1)
        _RampTex ("Lighting Ramp", 2D) = "white" {}
        _OutlineColor ("Outline Color", Color) = (0,0,0,1)
        _OutlineWidth ("Outline Width", Range(0,0.1)) = 0.015

        [Header(Halftone Settings)]
        _HalftoneScale ("Halftone Dot Scale", Float) = 0.18
        _HalftoneColor ("Halftone Color", Color) = (0.15, 0.15, 0.15, 1)

        [Header(Shading)]
        _SpecColor ("Specular Color", Color) = (0.6, 0.6, 0.6, 1)
        _SpecPower ("Spec Power", Range(1,64)) = 16
        _RimColor ("Rim Color", Color) = (0.9, 0.9, 0.85, 1)
        _RimPower ("Rim Power", Range(0.5,8)) = 2

        [Header(Edge Wear)]
        _EdgeWearColor ("Edge Wear Color", Color) = (1, 0.95, 0.85, 0.7)
        _EdgeWearStrength ("Edge Wear Strength", Range(0,1)) = 0.35
        _EdgeWearSoftness ("Edge Wear Softness", Range(0.1,2)) = 1.2

        [Header(Wrinkle)]
        _WrinkleStrength ("Wrinkle Strength", Range(0,1)) = 0.55
        _WrinkleScale ("Wrinkle Scale", Float) = 8.0
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
        LOD 250

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }
            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull Back

            HLSLPROGRAM
            #pragma vertex vertShadow
            #pragma fragment fragShadow
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_SHADOW_NON_PUNCTUAL_DIR_LIGHT_PROJECTORS

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            float3 _LightDirection;
            float3 _LightPosition;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };

            float hash(float3 p)
            {
                p = frac(p * 0.3183099 + 0.1);
                p *= 17.0;
                return frac(p.x * p.y * p.z * (p.x + p.y + p.z));
            }

            float noise(float3 x)
            {
                float3 i = floor(x);
                float3 f = frac(x);
                f = f * f * (3.0 - 2.0 * f);

                float n000 = hash(i + float3(0, 0, 0));
                float n100 = hash(i + float3(1, 0, 0));
                float n010 = hash(i + float3(0, 1, 0));
                float n110 = hash(i + float3(1, 1, 0));
                float n001 = hash(i + float3(0, 0, 1));
                float n101 = hash(i + float3(1, 0, 1));
                float n011 = hash(i + float3(0, 1, 1));
                float n111 = hash(i + float3(1, 1, 1));

                float nx00 = lerp(n000, n100, f.x);
                float nx10 = lerp(n010, n110, f.x);
                float nx01 = lerp(n001, n101, f.x);
                float nx11 = lerp(n011, n111, f.x);

                float nxy0 = lerp(nx00, nx10, f.y);
                float nxy1 = lerp(nx01, nx11, f.y);
                return lerp(nxy0, nxy1, f.z);
            }

            Varyings vertShadow(Attributes input)
            {
                Varyings output;
                float3 worldPos = TransformObjectToWorld(input.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);

                #if _CASTING_PUNCTUAL_SHADOW_NON_PUNCTUAL_DIR_LIGHT_PROJECTORS
                    float3 lightDirectionWS = normalize(_LightPosition.xyz - worldPos);
                #else
                    float3 lightDirectionWS = _LightDirection;
                #endif

                output.positionCS = TransformWorldToHClip(ApplyShadowBias(worldPos, normalWS, lightDirectionWS));
                return output;
            }

            half4 fragShadow(Varyings input) : SV_Target
            {
                return 0;
            }
            ENDHLSL
        }

        Pass
        {
            Name "Outline"
            Tags { "LightMode" = "SRPDefaultUnlit" }
            Cull Front
            ZWrite On

            HLSLPROGRAM
            #pragma vertex vertOutline
            #pragma fragment fragOutline
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _BaseColor;
                float4 _RampTex_ST;
                half4 _SpecColor;
                float _SpecPower;
                half4 _RimColor;
                float _RimPower;
                half4 _OutlineColor;
                float _OutlineWidth;
                float _HalftoneScale;
                half4 _HalftoneColor;
                float _WrinkleStrength;
                float _WrinkleScale;
                half4 _EdgeWearColor;
                float _EdgeWearStrength;
                float _EdgeWearSoftness;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };

            float hash(float3 p)
            {
                p = frac(p * 0.3183099 + 0.1);
                p *= 17.0;
                return frac(p.x * p.y * p.z * (p.x + p.y + p.z));
            }

            float noise(float3 x)
            {
                float3 i = floor(x);
                float3 f = frac(x);
                f = f * f * (3.0 - 2.0 * f);

                float n000 = hash(i + float3(0, 0, 0));
                float n100 = hash(i + float3(1, 0, 0));
                float n010 = hash(i + float3(0, 1, 0));
                float n110 = hash(i + float3(1, 1, 0));
                float n001 = hash(i + float3(0, 0, 1));
                float n101 = hash(i + float3(1, 0, 1));
                float n011 = hash(i + float3(0, 1, 1));
                float n111 = hash(i + float3(1, 1, 1));

                float nx00 = lerp(n000, n100, f.x);
                float nx10 = lerp(n010, n110, f.x);
                float nx01 = lerp(n001, n101, f.x);
                float nx11 = lerp(n011, n111, f.x);

                float nxy0 = lerp(nx00, nx10, f.y);
                float nxy1 = lerp(nx01, nx11, f.y);
                return lerp(nxy0, nxy1, f.z);
            }

            Varyings vertOutline(Attributes input)
            {
                Varyings output;
                float3 objectPosition = input.positionOS.xyz;
                float wrinkle = noise(objectPosition * _WrinkleScale) * _WrinkleStrength * 0.03;
                float3 displaced = objectPosition + input.normalOS * wrinkle;

                float3 worldPos = TransformObjectToWorld(displaced);
                float3 worldNormal = TransformObjectToWorldNormal(input.normalOS);
                
                worldPos += worldNormal * _OutlineWidth;
                output.positionCS = TransformWorldToHClip(worldPos);
                return output;
            }

            half4 fragOutline(Varyings input) : SV_Target
            {
                return _OutlineColor;
            }
            ENDHLSL
        }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _SHADOWS_SOFT
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
            TEXTURE2D(_RampTex); SAMPLER(sampler_RampTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _BaseColor;
                float4 _RampTex_ST;
                half4 _SpecColor;
                float _SpecPower;
                half4 _RimColor;
                float _RimPower;
                half4 _OutlineColor;
                float _OutlineWidth;
                float _HalftoneScale;
                half4 _HalftoneColor;
                float _WrinkleStrength;
                float _WrinkleScale;
                half4 _EdgeWearColor;
                float _EdgeWearStrength;
                float _EdgeWearSoftness;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float3 positionWS : TEXCOORD2;
                float3 objectPos : TEXCOORD3;
            };

            float hash(float3 p)
            {
                p = frac(p * 0.3183099 + 0.1);
                p *= 17.0;
                return frac(p.x * p.y * p.z * (p.x + p.y + p.z));
            }

            float noise(float3 x)
            {
                float3 i = floor(x);
                float3 f = frac(x);
                f = f * f * (3.0 - 2.0 * f);

                float n000 = hash(i + float3(0, 0, 0));
                float n100 = hash(i + float3(1, 0, 0));
                float n010 = hash(i + float3(0, 1, 0));
                float n110 = hash(i + float3(1, 1, 0));
                float n001 = hash(i + float3(0, 0, 1));
                float n101 = hash(i + float3(1, 0, 1));
                float n011 = hash(i + float3(0, 1, 1));
                float n111 = hash(i + float3(1, 1, 1));

                float nx00 = lerp(n000, n100, f.x);
                float nx10 = lerp(n010, n110, f.x);
                float nx01 = lerp(n001, n101, f.x);
                float nx11 = lerp(n011, n111, f.x);

                float nxy0 = lerp(nx00, nx10, f.y);
                float nxy1 = lerp(nx01, nx11, f.y);
                return lerp(nxy0, nxy1, f.z);
            }

            Varyings vert(Attributes input)
            {
                Varyings output;
                float3 objectPosition = input.positionOS.xyz;
                float wrinkle = noise(objectPosition * _WrinkleScale) * _WrinkleStrength * 0.03;
                float3 displaced = objectPosition + input.normalOS * wrinkle;

                output.positionWS = TransformObjectToWorld(displaced);
                output.positionCS = TransformWorldToHClip(output.positionWS);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.objectPos = objectPosition;
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                half4 albedo = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv) * _BaseColor;
                float3 N = normalize(input.normalWS);

                float4 shadowCoord = TransformWorldToShadowCoord(input.positionWS);
                Light mainLight = GetMainLight(shadowCoord);
                float3 L = normalize(mainLight.direction);
                float NdotL = saturate(dot(N, L));
                float combinedLighting = NdotL * mainLight.shadowAttenuation;

                half4 rampCol = SAMPLE_TEXTURE2D(_RampTex, sampler_RampTex, float2(combinedLighting, 0.5));

                float2 screenUV = input.positionCS.xy * _HalftoneScale;
                float2 grid = frac(screenUV) - 0.5;
                float dist = length(grid);
                float halftoneLight = (dot(N, L) * 0.5 + 0.5) * mainLight.shadowAttenuation;
                float dotRadius = lerp(0.7, 0.0, halftoneLight);
                float dotMask = 1.0 - smoothstep(dotRadius - 0.05, dotRadius + 0.05, dist);
                dotMask *= step(0.01, dotRadius);
                half3 shadedAlbedo = albedo.rgb * rampCol.rgb;
                half3 blendedDot = lerp(shadedAlbedo, _HalftoneColor.rgb, _HalftoneColor.a);
                half3 halftoneEffect = lerp(shadedAlbedo, blendedDot, dotMask);

                    float3 V = normalize(GetCameraPositionWS() - input.positionWS);
                float3 H = normalize(L + V);
                float NdotH = saturate(dot(N, H));
                float spec = pow(max(NdotH, 0.0001f), (float)_SpecPower) * mainLight.shadowAttenuation;
                float rim = pow(max(1.0f - saturate(dot(V, N)), 0.0001f), (float)_RimPower);

                // Edge wear highlight along the tape sides and seams
                float edgeNormalMask = saturate(1.0f - abs(dot(N, float3(0.0f,1.0f,0.0f))));
                edgeNormalMask = pow(max(edgeNormalMask, 0.0001f), (float)_EdgeWearSoftness);
                float edgeNoise = noise(input.objectPos * _WrinkleScale * 1.5f) * 0.25f + 0.75f;
                float edgeWear = saturate(edgeNormalMask * _EdgeWearStrength * edgeNoise);
                float3 wornEdge = lerp(halftoneEffect, _EdgeWearColor.rgb, edgeWear * _EdgeWearColor.a);

                float3 finalColor = wornEdge * mainLight.color;
                finalColor += _SpecColor.rgb * spec * mainLight.color;
                finalColor += _RimColor.rgb * rim * 0.6;

                return float4(finalColor, albedo.a);
            }
            ENDHLSL
        }
    }
    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}

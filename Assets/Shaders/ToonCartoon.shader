Shader "Scrapout/ToonCartoon"
{
    Properties
    {
        [MainTexture] _BaseMap ("Base Map", 2D) = "white" {}
        [MainColor] _BaseColor ("Base Color", Color) = (1,1,1,1)
        _RampTex ("Lighting Ramp", 2D) = "white" {}
        _SpecColor ("Specular Color", Color) = (1,1,1,1)
        _SpecPower ("Spec Power", Range(1,64)) = 16
        _RimColor ("Rim Color", Color) = (1,0.6,1,1)
        _RimPower ("Rim Power", Range(0.5,8)) = 2
        _OutlineColor ("Outline Color", Color) = (0,0,0,1)
        _OutlineWidth ("Outline Width", Range(0,0.1)) = 0.01
        
        [Header(Halftone Settings)]
        _HalftoneScale ("Halftone Dot Scale", Float) = 0.2
        _HalftoneColor ("Halftone Color", Color) = (0.2, 0.2, 0.2, 1)
    }

    SubShader
    {
        Tags 
        { 
            "RenderType"="Opaque" 
            "RenderPipeline"="UniversalPipeline"
        }
        LOD 200

        // Shadow caster pass
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

            // Unity's ShadowCaster pass requires these variables to be explicitly declared to receive the light data
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

            Varyings vertShadow(Attributes input)
            {
                Varyings output;
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);
                
                #if _CASTING_PUNCTUAL_SHADOW_NON_PUNCTUAL_DIR_LIGHT_PROJECTORS
                    float3 lightDirectionWS = normalize(_LightPosition.xyz - positionWS);
                #else
                    float3 lightDirectionWS = _LightDirection;
                #endif
                
                output.positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, lightDirectionWS));
                return output;
            }

            half4 fragShadow(Varyings input) : SV_Target
            {
                return 0;
            }
            ENDHLSL
        }

        // Outline pass (draw scaled backfaces)
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
                float4 _BaseMap_ST; // Even if not used here, required for the constant buffer layout match between passes
                half4 _BaseColor;
                half4 _SpecColor;
                float _SpecPower;
                half4 _RimColor;
                float _RimPower;
                half4 _OutlineColor;
                float _OutlineWidth;
                float _HalftoneScale;
                half4 _HalftoneColor;
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

            Varyings vertOutline(Attributes input)
            {
                Varyings output;
                float3 n = normalize(input.normalOS);
                // Expand outward along the normal
                float3 posOS = input.positionOS.xyz + n * _OutlineWidth;
                output.positionCS = TransformObjectToHClip(posOS);
                return output;
            }

            half4 fragOutline(Varyings input) : SV_Target
            {
                return _OutlineColor;
            }
            ENDHLSL
        }

        // Main lit toon pass
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            // Receive Shadows Multi-compiles
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
                half4 _SpecColor;
                float _SpecPower;
                half4 _RimColor;
                float _RimPower;
                half4 _OutlineColor;
                float _OutlineWidth;
                float _HalftoneScale;
                half4 _HalftoneColor;
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
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS = TransformWorldToHClip(output.positionWS);
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                half4 albedo = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv) * _BaseColor;

                float3 N = normalize(input.normalWS);
                
                // Get Main Light and Shadows
                float4 shadowCoord = TransformWorldToShadowCoord(input.positionWS);
                Light mainLight = GetMainLight(shadowCoord);
                
                float3 L = normalize(mainLight.direction);
                float NdotL = saturate(dot(N, L));

                // Combine light angle and shadow map attentuation into one lighting value
                float combinedLighting = NdotL * mainLight.shadowAttenuation;

                // Sample ramp texture using the combined lighting
                half4 rampCol = SAMPLE_TEXTURE2D(_RampTex, sampler_RampTex, float2(combinedLighting, 0.5));

                // Procedural Halftone dots based on Screen Space
                float2 screenUV = input.positionCS.xy * _HalftoneScale;
                float2 grid = frac(screenUV) - 0.5;
                float dist = length(grid);
                
                // To make the dots shrink smoothly across the curvature of the object instead of
                // all being the exact same size in shadows, we use a Half-Lambert wrapped lighting calculation!
                float halftoneLight = (dot(N, L) * 0.5 + 0.5) * mainLight.shadowAttenuation;
                
                // Map the smooth wrapped lighting into a dot radius (dark = big dots, light = small/no dots)
                // You can tweak 0.7 to 0.0 to control maximum dot size in pitch black!
                float dotRadius = lerp(0.7, 0.0, halftoneLight);
                
                // Use smoothstep instead of a hard cutout for nice, clean, anti-aliased dot edges!
                float dotMask = 1.0 - smoothstep(dotRadius - 0.05, dotRadius + 0.05, dist);
                dotMask *= step(0.01, dotRadius); // Ensure completely lit areas have zero points
                
                // Mix the halftone color in based on its Alpha value using the smooth mask
                half3 shadedAlbedo = albedo.rgb * rampCol.rgb;
                half3 blendedDot = lerp(shadedAlbedo, _HalftoneColor.rgb, _HalftoneColor.a);
                half3 halftoneEffect = lerp(shadedAlbedo, blendedDot, dotMask);

                // Specular
                float3 V = normalize(GetCameraPositionWS() - input.positionWS);
                float3 H = normalize(L + V);
                float NdotH = saturate(dot(N, H));
                float spec = pow(NdotH, _SpecPower) * mainLight.shadowAttenuation; // Specular affected by shadow

                // Rim lighting
                float rim = pow(1.0 - saturate(dot(V, N)), _RimPower);

                // Compose final color
                half3 finalColor = halftoneEffect * mainLight.color;
                finalColor += _SpecColor.rgb * spec * mainLight.color;
                finalColor += _RimColor.rgb * rim * 0.6; // Scale down rim intensity slightly

                return half4(finalColor, albedo.a);
            }
            ENDHLSL
        }
    }
    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}

Shader "Scrapout/ToonCartoon"
{
    Properties
    {
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
        _Color ("Color", Color) = (1,1,1,1)
        _RampTex ("Lighting Ramp", 2D) = "white" {}
        _SpecColor ("Specular Color", Color) = (1,1,1,1)
        _SpecPower ("Spec Power", Range(1,64)) = 16
        _RimColor ("Rim Color", Color) = (1,0.6,1,1)
        _RimPower ("Rim Power", Range(0.5,8)) = 2
        _OutlineColor ("Outline Color", Color) = (0,0,0,1)
        _OutlineWidth ("Outline Width", Range(0,0.1)) = 0.01
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200

        // Outline pass (draw scaled backfaces)
        Pass
        {
            Name "OUTLINE"
            Tags { "LightMode" = "Always" }
            Cull Front
            ZWrite On

            CGPROGRAM
            #pragma vertex vertOutline
            #pragma fragment fragOutline
            #include "UnityCG.cginc"

            float4 _OutlineColor;
            float _OutlineWidth;

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
            };

            v2f vertOutline(appdata v)
            {
                v2f o;
                float3 n = normalize(v.normal);
                float4 posOS = v.vertex;
                posOS.xyz += n * _OutlineWidth * -1.0; // push along -normal to expand
                o.pos = UnityObjectToClipPos(posOS);
                return o;
            }

            fixed4 fragOutline(v2f i) : SV_Target
            {
                return _OutlineColor;
            }
            ENDCG
        }

        // Main lit toon pass
        Pass
        {
            Name "TOON"
            Tags { "LightMode" = "ForwardBase" }

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _Color;
            sampler2D _RampTex;
            fixed4 _SpecColor;
            float _SpecPower;
            fixed4 _RimColor;
            float _RimPower;

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 worldNormal : TEXCOORD1;
                float3 worldPos : TEXCOORD2;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.worldNormal = UnityObjectToWorldNormal(v.normal);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // Albedo
                fixed4 albedo = tex2D(_MainTex, i.uv) * _Color;

                // Normalized lighting
                float3 N = normalize(i.worldNormal);
                float3 L = normalize(_WorldSpaceLightPos0.xyz);
                float NdotL = saturate(dot(N, L));

                // Sample ramp texture using NdotL as u coordinate
                float rampU = NdotL;
                fixed4 rampCol = tex2D(_RampTex, float2(rampU, 0.5));

                // Simple Blinn specular (quantized by ramp brightness)
                float3 V = normalize(_WorldSpaceCameraPos - i.worldPos);
                float3 H = normalize(L + V);
                float NdotH = saturate(dot(N, H));
                float spec = pow(NdotH, _SpecPower);

                // Rim lighting
                float rim = pow(1.0 - saturate(dot(V, N)), _RimPower);

                fixed3 final = albedo.rgb * rampCol.rgb;
                final += _SpecColor.rgb * spec;
                final += _RimColor.rgb * rim * 0.6;

                return fixed4(final, albedo.a);
            }
            ENDCG
        }
    }

    FallBack "Diffuse"
}

Shader "Scrapout/Hologram"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Base Color", Color) = (0.6,0.1,0.9,1)
        _EmissionColor ("Emission Color", Color) = (0.8,0.2,1,1)
        _FresnelPower ("Fresnel Power", Float) = 2.0
        _FresnelIntensity ("Fresnel Intensity", Float) = 1.0
        _ScanlineTiling ("Scanline Tiling", Float) = 40.0
        _ScanSpeed ("Scan Speed", Float) = 1.0
        _ScanOffset ("Scan Offset", Vector) = (0,0,0,0)
        _GridStrength ("Grid Strength", Float) = 0.6
        _Alpha ("Alpha", Range(0,1)) = 0.9
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        LOD 100

        Cull Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _Color;
            fixed4 _EmissionColor;
            float _FresnelPower;
            float _FresnelIntensity;
            float _ScanlineTiling;
            float _ScanSpeed;
            float4 _ScanOffset;
            float _GridStrength;
            float _Alpha;

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
                // basic texture
                fixed4 baseCol = tex2D(_MainTex, i.uv) * _Color;

                // view direction
                float3 viewDir = normalize(_WorldSpaceCameraPos - i.worldPos);
                float fresnel = pow(saturate(1.0 - saturate(dot(normalize(i.worldNormal), viewDir))), _FresnelPower);

                // scanline effect (horizontal)
                float scanUV = i.uv.y * _ScanlineTiling + (_Time.y * _ScanSpeed) + _ScanOffset.y;
                float scan = smoothstep(0.2, 0.8, 0.5 + 0.5 * sin(scanUV * 6.28318));

                // grid / hologram cross hatch using both u and v
                float grid = (sin(i.uv.x * 30.0 + _Time.y * 0.5) * 0.5 + 0.5) * (sin(i.uv.y * 30.0) * 0.5 + 0.5);

                fixed4 emission = _EmissionColor * (fresnel * _FresnelIntensity * (0.5 + 0.5 * scan));

                // Mix base color and emissive scanlines and grid overlay
                fixed3 colorOut = lerp(baseCol.rgb, _EmissionColor.rgb, 0.45 * scan) + emission.rgb * 0.8;

                // apply subtle grid mask to alpha for holographic scan-lines
                float alphaMask = lerp(1.0, grid, _GridStrength) * _Alpha;

                return fixed4(colorOut, alphaMask);
            }
            ENDCG
        }
    }

    FallBack "Transparent/Diffuse"
}

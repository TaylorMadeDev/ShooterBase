Shader "VFX/MuzzleFlash"
{
    Properties
    {
        [HDR] _CoreColor ("Core Color", Color) = (1.0, 0.9, 0.2, 1.0)
        [HDR] _EdgeColor ("Edge Color", Color) = (1.0, 0.4, 0.0, 1.0)
        _MainTex ("Texture Mask (Grayscale)", 2D) = "white" {}
        
        _Erosion ("Erosion Cutoff", Range(0, 1)) = 0.05
        _EdgeWidth ("Edge Width", Range(0.01, 1)) = 0.2
        _Smoothness ("Stylized Smoothness", Range(0, 0.5)) = 0.02
    }
    SubShader
    {
        Tags 
        { 
            "Queue"="Transparent" 
            "RenderType"="Transparent" 
            "IgnoreProjector"="True" 
            "PreviewType"="Plane"
        }

        // Restored Additive Blending
        Blend SrcAlpha One
        ZWrite Off
        Cull Off
        Lighting Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_particles
            #pragma multi_compile_fog

            #include "UnityCG.cginc"

            struct appdata_t
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float4 color : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_FOG_COORDS(1)
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            
            float4 _CoreColor;
            float4 _EdgeColor;
            float _Erosion;
            float _EdgeWidth;
            float _Smoothness;

            v2f vert (appdata_t v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
                o.color = v.color;
                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float shapeMask = tex2D(_MainTex, i.texcoord).r;
                float baseMask = shapeMask * i.color.a;

                float alpha = smoothstep(_Erosion, _Erosion + _Smoothness, baseMask);
                float coreBlend = smoothstep(_Erosion + _EdgeWidth, _Erosion + _EdgeWidth + _Smoothness, baseMask);
                
                float4 finalColor = lerp(_EdgeColor, _CoreColor, coreBlend);
                
                // Pre-multiply alpha into RGB so the black background stays invisible in Additive blend!
                finalColor.rgb *= alpha * i.color.rgb;
                finalColor.a = alpha;
                
                UNITY_APPLY_FOG(i.fogCoord, finalColor);
                return finalColor;
            }
            ENDCG
        }
    }
}

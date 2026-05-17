Shader "VFX/MuzzleFlash"
{
    Properties
    {
        [HDR] _CoreColor ("Core Color", Color) = (1.0, 0.9, 0.2, 1.0)
        [HDR] _EdgeColor ("Edge Color", Color) = (1.0, 0.4, 0.0, 1.0)
        _MainTex ("Texture Mask (Grayscale)", 2D) = "white" {}
        
        [Header(Noise and Distortion)]
        _NoiseTex ("Noise Texture", 2D) = "gray" {}
        _NoiseErosion ("Fiery Noise Erosion", Range(0, 1)) = 0.3
        _NoiseSpeed ("Noise Pan Speed (X, Y)", Vector) = (0, 5, 0, 0)
        
        [Header(Stylized Settings)]
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
                float2 noiseUV : TEXCOORD1;
                UNITY_FOG_COORDS(2)
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            
            sampler2D _NoiseTex;
            float4 _NoiseTex_ST;
            
            float4 _CoreColor;
            float4 _EdgeColor;
            float _Erosion;
            float _EdgeWidth;
            float _Smoothness;
            
            float _NoiseErosion;
            float2 _NoiseSpeed;

            v2f vert (appdata_t v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
                
                // Calculate moving noise UVs using Unity's built-in _Time
                o.noiseUV = TRANSFORM_TEX(v.texcoord, _NoiseTex) + (_Time.y * _NoiseSpeed);
                
                o.color = v.color;
                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // Sample the noise texture
                float noise = tex2D(_NoiseTex, i.noiseUV).r;
                
                // Read the shape from the UN-warped texture so it doesn't move/downshift
                float shapeMask = tex2D(_MainTex, i.texcoord).r;
                
                // FIERY EROSION: Combine base shape, vertex alpha, and noise
                // We subtract noise so the edges dissolve irregularly into fiery licks
                float modifiedShape = shapeMask - (noise * _NoiseErosion);
                
                // Allow controlling alpha purely via the slider or the quad's mesh color
                float baseMask = modifiedShape * i.color.a;

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

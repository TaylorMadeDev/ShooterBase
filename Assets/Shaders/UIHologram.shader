Shader "Scrapout/UI/HologramPanel"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (0, 0.6, 1, 0.5)
        _ScanlineDensity ("Scanline Density", Float) = 60
        _ScanlineSpeed ("Scanline Speed", Float) = 2
        _GlowIntensity ("Global Glow Intensity", Float) = 1.2
        _NeonBoost ("Neon Highlight Boost", Float) = 2.5
        _NeonThreshold ("Neon Threshold", Range(0, 1)) = 0.4
        
        // Required for Unity UI masking and Canvas groups
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            Name "Default"
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex   : SV_POSITION;
                fixed4 color    : COLOR;
                float2 texcoord  : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            fixed4 _Color;
            fixed4 _TextureSampleAdd;
            float4 _ClipRect;
            
            float _ScanlineDensity;
            float _ScanlineSpeed;
            float _GlowIntensity;
            float _NeonBoost;
            float _NeonThreshold;

            v2f vert(appdata_t v)
            {
                v2f OUT;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
                OUT.worldPosition = v.vertex;
                OUT.vertex = UnityObjectToClipPos(OUT.worldPosition);
                OUT.texcoord = v.texcoord;
                OUT.color = v.color * _Color;
                return OUT;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                half4 color = (tex2D(_MainTex, IN.texcoord) + _TextureSampleAdd) * IN.color;

                #ifdef UNITY_UI_CLIP_RECT
                color.a *= UnityGet2DClipping(IN.worldPosition.xy, _ClipRect);
                #endif

                #ifdef UNITY_UI_ALPHACLIP
                clip (color.a - 0.001);
                #endif

                // 1. Scrolling Scanlines using World Position 
                // (Using world position stops 9-sliced UI sprites from distorting the lines)
                float scanline = sin(IN.worldPosition.y * _ScanlineDensity - _Time.y * _ScanlineSpeed);
                scanline = (scanline * 0.15) + 0.85; // Soften the lines so they aren't pure black
                
                // 2. Identify the brightest parts of the image (the bright blue lines)
                // We find the brightest color channel (usually blue in your sprite)
                float luminance = max(color.r, max(color.g, color.b));
                // Create a mask that isolates only the bright parts based on the threshold
                float neonMask = smoothstep(_NeonThreshold, 1.0, luminance);
                
                // 3. Apply effects
                color.rgb *= scanline * _GlowIntensity;
                // Supercharge the bright edges to push them into HDR bloom range!
                color.rgb += color.rgb * neonMask * _NeonBoost * color.a;

                return color;
            }
            ENDCG
        }
    }
}
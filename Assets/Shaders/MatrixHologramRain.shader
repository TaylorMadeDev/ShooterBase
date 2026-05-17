Shader "Scrapout/VFX/MatrixRain"
{
    Properties
    {
        [HDR] _Color ("Main Color", Color) = (0, 1, 0, 1)
        _GridSize ("Grid Size (X, Y)", Vector) = (8, 20, 0, 0)
        _Speed ("Fall Speed", Float) = 3.0
        _Glow ("Glow Multiplier", Float) = 2.5
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Blend One One // Additive blending for holograms!
        ZWrite Off
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR; // Reads Particle System or LineRenderer colors
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float4 color : COLOR;
            };

            float4 _Color;
            float4 _GridSize;
            float _Speed;
            float _Glow;

            // Fast pseudo-random number generator
            float rand(float2 co) {
                return frac(sin(dot(co.xy, float2(12.9898, 78.233))) * 43758.5453);
            }

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.color = v.color; // Pass vertex color to fragment
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float2 uv = i.uv;
                
                // Create the grid
                float2 gridUV = uv * _GridSize.xy;
                
                // Offset Y scrolling speed based on X position to make columns fall out of sync
                float xRand = rand(floor(gridUV.x));
                float scrollY = gridUV.y - _Time.y * _Speed * (0.5 + xRand);
                
                // Lock cells into a strict grid
                float2 cellID = float2(floor(gridUV.x), floor(scrollY));
                float2 localUV = frac(float2(gridUV.x, scrollY));
                
                // Random value for this specific cell (to randomly choose 0 or 1)
                float cellRand = rand(cellID);
                
                // PROCEDURAL '1': Center vertical line with a tiny nub
                float draw1 = step(abs(localUV.x - 0.5), 0.1) * step(abs(localUV.y - 0.5), 0.35);
                
                // PROCEDURAL '0': Hollow box outline
                float boxDistX = abs(localUV.x - 0.5);
                float boxDistY = abs(localUV.y - 0.5);
                float boxMax = max(boxDistX, boxDistY);
                float draw0 = step(0.15, boxMax) * step(boxMax, 0.35);
                
                // Randomly pick '0' or '1'
                float shape = lerp(draw0, draw1, step(0.5, cellRand));
                
                // Add a random blinking brightness per number
                float brightness = rand(cellID + float2(13.0, 37.0));
                
                // Fade out at the top and bottom edges so it looks clean
                float alpha = smoothstep(0.0, 0.15, uv.y) * smoothstep(1.0, 0.6, uv.y);
                
                // Combine it all! (Multiply by i.color so it syncs with our C# rarity color)
                float3 finalColor = _Color.rgb * i.color.rgb * shape * brightness * _Glow * alpha;
                
                return float4(finalColor, 1.0);
            }
            ENDCG
        }
    }
}
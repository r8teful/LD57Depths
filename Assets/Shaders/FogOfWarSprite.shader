Shader "Custom/WorldGenSprite"
{
    Properties
    {
        _MainTex ("Sprite Texture", 2D) = "white" {}
        _GlobalSeed ("Global Seed", Float) = 1234.0
        _WorldLayerNoiseScale ("World Noise Scale", Float) = 1.0
        _WorldLayerEdgeAmp ("World Edge Amp", Float) = 1.0
        _WorldUVScale ("World UV Scale", Float) = 0.1
        _TotalLayers ("World Layers", Float) = 5
        _LayerIndex ("Layer Index", Float) = 1
        _WobbleLength ("Wobble Length", Float) = 1
        _WobbleStrength ("Wobble Strength", Float) = 0.5
        _WobbleSpeed ("Wobble Speed", Float) = 1
        _PixelSize ("Pixel Size", Float) = 1
        
        _TrenchBaseWiden ("Trench Base Widen", Float) = 0.02
        _TrenchBaseWidth ("Trench Base Width", Float) = 12.0
        
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" }
        LOD 100

        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        ZWrite Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0

            #include "UnityCG.cginc"
            #include "RandomnesHelpers.hlsl"

            // ---- CONFIG ----
            #define MAX_LAYERS 6
            // ------------------

            sampler2D _MainTex;
            float4 _MainTex_ST;

            float _GlobalSeed;
            // For max depth
            float _TrenchBaseWiden;
            float _TrenchBaseWidth;


            float _WorldUVScale;
            float _TotalLayers;
            float _WorldLayerNoiseScale;
            float _WorldLayerEdgeAmp;

            float _WobbleLength;
            float _WobbleStrength;
            float _WobbleSpeed;
            float _PixelSize;
            float _LayerIndex;
            
           float CloudMask(float2 uv, float maxDepth) 
           {
                int layers = max(1, min(_TotalLayers, MAX_LAYERS));
                int layer = max(0, min(_LayerIndex, layers - 1));
                
                // compute only the two borders relevant for this layer: b = layer and b = layer+1
                // but still use the exact same formula so border offsets match other shaders.
                float b0 = maxDepth * (abs((float)layer - (float)layers) / (float)layers);
                float n0 = EdgeNoise_Smooth(uv, _GlobalSeed, layer, _WorldLayerNoiseScale);
                //float n0 = perlin2D(uv, _GlobalSeed);
                // Perlin is already [-1,1]
                float offset0 = (n0 - 0.5) * 2.0 * _WorldLayerEdgeAmp;
                //float offset0 = n0 * _WorldLayerEdgeAmp;
                if (layer == 0) // bottom special-case
                    b0 += maxDepth * 0.1;
                b0 += offset0;
                
                // the upper border index (layer+1) always exists because layer <= layers-1 and we know layers>=1
                int upperIndex = layer + 1;
                float b1 = maxDepth * (abs((float)upperIndex - (float)layers) / (float)layers);
                float n1 = EdgeNoise_Smooth(uv, _GlobalSeed, upperIndex, _WorldLayerNoiseScale);
                //float n1 = perlin2D(uv, _GlobalSeed);
                float offset1 = (n1 - 0.5) * 2.0 * _WorldLayerEdgeAmp;
                //float offset1 = n1 * _WorldLayerEdgeAmp;
                // no bottom adjust for b1 (only index 0 got the extra depth)
                b1 += offset1;
                
                float bandLow  = min(b0, b1);
                float bandHigh = max(b0, b1);
                
                bool isInside= (uv.y >= bandLow && (layer < layers - 1 ? uv.y < bandHigh : uv.y <= bandHigh));
                if(isInside) {
                    return 1;
                } else {
                    return 0;
                }
            }   
            
            float2 PixelateUV(float2 UV, float Resolution) {
                return floor(UV * Resolution) / Resolution;
            }

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
                float3 worldPos : TEXCOORD1;
            };

            v2f vert (appdata_t v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.texcoord, _MainTex);
                o.color = v.color;
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float2 worldUV = i.worldPos.xy * _WorldUVScale;
                float2 pixelUV = PixelateUV(worldUV,_PixelSize);
                float2 wobbleOffset = float2(
                    sin(pixelUV.y * _WobbleLength + _Time.y* _WobbleSpeed), 
                    cos(pixelUV.x *_WobbleLength + _Time.y* _WobbleSpeed)
                ) * _WobbleStrength; // Multiply by 0.05 to keep the distortion small
                
                // Add the wobble to the static world UV
                float2 distortedUV = pixelUV + wobbleOffset;
                float4 resultColor = float4(1, 1, 1, 1);
                
                float maxDepth = -1 * abs(_TrenchBaseWidth / _TrenchBaseWiden) * 0.7;
                float layerMask = CloudMask(distortedUV,maxDepth);
                resultColor.a *= layerMask;
                // Multiply by sprite vertex color
                resultColor.rgb *= i.color.rgb;
                resultColor.a *= i.color.a;
                return resultColor;
            }

            ENDHLSL
        } // Pass
    } // SubShader

    FallBack "Sprites/Default"
}

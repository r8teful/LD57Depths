Shader "Custom/WorldGenSprite"
{
    Properties
    {
        _MainTex ("Sprite Texture", 2D) = "white" {}
        _GlobalSeed ("Global Seed", Float) = 1234.0

        // Cave parameters (editable in inspector)
        _CaveNoiseScale ("Cave Noise Scale", Float) = 1.0
        _CaveAmp ("Cave Amp", Float) = 1.0
        _CaveCutoff ("Cave Cutoff", Float) = 0.5
        _BaseOctaves("Base Octaves",int) = 5
        _RidgeOctaves("Ridge Octaves",int) = 4
        _WarpAmp("Warp Amp",Float) = 0.8
        _WorleyWeight("Worley Weight",Float) = 0.6

        // Trench parameters (editable)
        _TrenchBaseWiden ("Trench Base Widen", Float) = 0.02
        _TrenchBaseWidth ("Trench Base Width", Float) = 12.0
        _TrenchNoiseScale ("Trench Noise Scale", Float) = 1.0
        _TrenchEdgeAmp ("Trench Edge Amp", Float) = 1.0
        _WorldUVScale ("World UV Scale", Float) = 0.1
        
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
            #define NUM_BIOMES 5
            // ------------------

            sampler2D _MainTex;
            float4 _MainTex_ST;

            // Global / inspector floats
            float _GlobalSeed;
            float _CaveNoiseScale;
            float _CaveAmp;
            float _CaveCutoff;
            int _BaseOctaves;
            int _RidgeOctaves;
            float _WarpAmp;
            float _WorleyWeight;
            float _TrenchBaseWiden;
            float _TrenchBaseWidth;
            float _TrenchNoiseScale;
            float _TrenchEdgeAmp;
            float _WorldUVScale;

            // Generation details
            float _edgeNoiseScale[NUM_BIOMES];
            float _edgeNoiseAmp[NUM_BIOMES];
            float _blockNoiseScale[NUM_BIOMES];
            float _blockNoiseAmp[NUM_BIOMES];
            float _blockCutoff[NUM_BIOMES];
            float _baseOctaves[NUM_BIOMES];
            float _ridgeOctaves[NUM_BIOMES];
            float _warpAmp[NUM_BIOMES];
            float _worldeyWeight[NUM_BIOMES];
            float _caveType[NUM_BIOMES];
            
            // Pos
            float _YStart[NUM_BIOMES];
            float _YHeight[NUM_BIOMES];
            float _horSize[NUM_BIOMES];
            float _XOffset[NUM_BIOMES];
            float4 _tileColor[NUM_BIOMES];
            float4 _airColor[NUM_BIOMES];

            float PerBiomeNoise(float2 uv, float globalSeed, int biomeIndex, float scale, int caveType,   
                int baseOctaves, int ridgeOctaves, float warpAmp, float worleyWeight)
            {
                if(caveType <= 0){
                    return CaveDensity_Combined(uv,globalSeed,biomeIndex,scale,baseOctaves,ridgeOctaves,warpAmp,worleyWeight);
                } 
                // Tunnels
                if(caveType >= 1){
                    return CaveDensity_Tunnels(uv,globalSeed,biomeIndex,scale,baseOctaves,warpAmp);
                }
                return CaveDensity_Combined(uv,globalSeed,biomeIndex,scale,baseOctaves,ridgeOctaves,warpAmp,worleyWeight);
            }

            int PickBiome2D(float2 uv) {
                for (int i = 0; i < NUM_BIOMES; i++) { 
                    // Fetch per-biome parameters
                    float b_edgeScale = _edgeNoiseScale[i];
                    float b_edgeAmp = _edgeNoiseAmp[i];
                    float b_YStart = _YStart[i];
                    float b_YHeight = _YHeight[i];
                    float b_horSize = _horSize[i];
                    float b_XOffset = _XOffset[i];

                    // Compute biome bounds with noise
                    float edgeNoiseX = (EdgeNoise_Smooth(uv, _GlobalSeed,i, b_edgeScale) - 0.5) * 2.0;
                    float edgeNoiseY = (EdgeNoise_Smooth(uv, _GlobalSeed,i, b_edgeScale) - 0.5) * 2.0;

                    float width = max(0.0, b_horSize + edgeNoiseX * b_edgeAmp);
                    float heightTop = b_YStart + b_YHeight + edgeNoiseY * b_edgeAmp;
                    float heightBottom = b_YStart + edgeNoiseY * b_edgeAmp;
                   
                    // Check if UV is inside this biome's region
                    bool isInBiomeRegion = (width > abs(uv.x - b_XOffset)) && (uv.y >= heightBottom && uv.y < heightTop);

                    if (isInBiomeRegion) {
                        return i;
                    }
                }
                return -1;  // Default or fallback biome index if none found (e.g., void or background)
            }
            // Trench logic (kept similar to original)
            float GenerateTrenchAndSurface(float2 uv, float baseWiden, float baseWidth, float noiseFreq, float edgeAmp, float parallax ,bool useEdge, float seed)
            {
                float halfTrenchWidth = (baseWidth + abs(uv.y) * baseWiden) / 2.0;
                float edgeNoise = (EdgeNoise_Smooth(uv, _GlobalSeed,0,noiseFreq) - 0.5) * 2.0;
                float noisyHalfWidth = max(0.0, halfTrenchWidth + edgeNoise * edgeAmp);

                float maxDepth = abs(-1 * baseWidth / baseWiden) * 0.7 * (1 + parallax);
                // Todo the edge noise freq needs to be set manually here, make a nice number, just 4 for now
                float surfaceNoise = (EdgeNoise_Smooth(uv, _GlobalSeed,2,4) - 0.5) * 2.0;
                if (uv.y > surfaceNoise)
                    return 0;
                if (abs(uv.y) > maxDepth)
                    return 1;
                if (!useEdge)
                {
                    bool mask2 = (abs(uv.x) < noisyHalfWidth);
                    return mask2 ? 0.0 : 1.0;
                }
                float distanceToEdge = abs(abs(uv.x) - noisyHalfWidth);
                bool mask = distanceToEdge < 0.1;
                return mask ? 1.0 : 0.0;
            }

            float4 WorldGenFull(float2 uv)
            {
                // Start the world as solid
                float4 Color = float4(1, 1, 0, 255) / 255.0;
                // CAVES - use global seed so caves change with seed
                
                // ---------- 2D normalized-distance biome pick (cheap) ----------
                //float caveNoise = fbm(float2(uv.x * _CaveNoiseScale + _GlobalSeed * 2.79, uv.y * _CaveNoiseScale + _GlobalSeed * 8.69)) * _CaveAmp;
                // Best one:
                float caveNoise = CaveDensity_Combined(uv,_GlobalSeed,0,_CaveNoiseScale,_BaseOctaves, _RidgeOctaves,_WarpAmp,_WorleyWeight)* _CaveAmp;
                //float caveNoise = CaveDensity_Tunnels(uv,_GlobalSeed,biomeIndex,_CaveNoiseScale,_BaseOctaves,_WarpAmp);
                int biomeIndex = PickBiome2D(uv);
                if (caveNoise <_CaveCutoff) {
                    // Cave
                    Color = float4(0, 1, 1, 1);
                }
                
                
                // fetch biome parameters from arrays
                float b_edgeScale  = _edgeNoiseScale[biomeIndex];
                float b_edgeAmp    = _edgeNoiseAmp[biomeIndex];
                float b_blockScale = _blockNoiseScale[biomeIndex];
                float b_blockAmp   = _blockNoiseAmp[biomeIndex];

                float b_baseOctaves   = _baseOctaves[biomeIndex];
                float b_ridgeOctaves  = _ridgeOctaves[biomeIndex];
                float b_warpAmp       = _warpAmp[biomeIndex];
                float b_worldeyWeight = _worldeyWeight[biomeIndex];
                float b_caveType      = _caveType[biomeIndex];

                float b_YStart     = _YStart[biomeIndex];
                float b_blockCut   = _blockCutoff[biomeIndex];
                float b_YHeight    = _YHeight[biomeIndex];
                float b_horSize    = _horSize[biomeIndex];
                float b_XOffset    = _XOffset[biomeIndex];
                float4 b_tileColor = _tileColor[biomeIndex];
                float4 b_airColor  = _airColor[biomeIndex];

                // Edge noise for shape (per-biome noise fields)
                float edgeNoiseX = (EdgeNoise_Smooth(uv, _GlobalSeed, biomeIndex, b_edgeScale) - 0.5) * 2.0;
                float edgeNoiseY = (EdgeNoise_Smooth(uv, _GlobalSeed, biomeIndex, b_edgeScale) - 0.5) * 2.0;

                float width = max(0.0, b_horSize + edgeNoiseX * b_edgeAmp);
                float heightTop = b_YStart + b_YHeight + edgeNoiseY * b_edgeAmp;
                float heightBottom = b_YStart + edgeNoiseY * b_edgeAmp;

                bool isInBiomeRegion = (width > abs(uv.x - b_XOffset)) && (uv.y >= heightBottom && uv.y < heightTop);

                if (isInBiomeRegion)
                {
                    // block vs air within the biome using per-biome block noise
                    float blockVal = PerBiomeNoise(uv,_GlobalSeed, biomeIndex, b_blockScale,b_caveType,
                        b_baseOctaves,b_ridgeOctaves,b_warpAmp,b_worldeyWeight) * b_blockAmp;
                    if (blockVal < b_blockCut)
                        Color = b_tileColor;
                    else
                        Color = b_airColor;
                }
                else
                {
                    // fallback (not in any biome) - keep the base Color
                    // Could be replaced with a global surface palette
                    
                }
                
                float trenchMask = GenerateTrenchAndSurface(uv, _TrenchBaseWiden, _TrenchBaseWidth, _TrenchNoiseScale, _TrenchEdgeAmp, 0.0, false, _GlobalSeed);
                // //  if (trenchMask < 0.5 && uv.y < b_YStart)
                if (trenchMask < 0.5)
                {
                Color = float4(255, 254.0, 255, 255) / 255.0;
                }

                return Color;
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
                // read UV. World space mapping or local UV mapping depends on how you feed uv to shader.
                // Here we assume uv.xy is the sprite UV, but WorldGenFull expects world-like coords.
                // If your world coordinates differ, multiply/offset uv appropriately from shader inputs.
                //float2 uv = i.uv * 100.0 - float2(50.0, 50.0); // example transform -> adjust to your coordinate system
                // NOTE: the line above is an example scaling so the procedural world fits in UV space.
                // In practice you should pass true world coords (or a conversion) from C# via material properties.
                float2 worldUV = i.worldPos.xy * _WorldUVScale;
                
                float4 worldColor = WorldGenFull(worldUV);
                // Multiply by sprite vertex color (so sprite tint works)
                worldColor.rgb *= i.color.rgb;
                worldColor.a *= i.color.a;
                return worldColor;
            }

            ENDHLSL
        } // Pass
    } // SubShader

    FallBack "Sprites/Default"
}

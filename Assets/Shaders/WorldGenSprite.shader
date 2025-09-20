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

            // ---- CONFIG ----
            #define NUM_BIOMES 3
            // ------------------

            sampler2D _MainTex;
            float4 _MainTex_ST;

            // Global / inspector floats
            float _GlobalSeed;
            float _CaveNoiseScale;
            float _CaveAmp;
            float _CaveCutoff;

            float _TrenchBaseWiden;
            float _TrenchBaseWidth;
            float _TrenchNoiseScale;
            float _TrenchEdgeAmp;
            float _WorldUVScale;

            // ----------------------------
            // Per-biome arrays (set from C# with SetFloatArray / SetVectorArray)
            // Note: these don't show up in the inspector; you must set them from script.
            // ----------------------------
            float _edgeNoiseScale[NUM_BIOMES];
            float _edgeNoiseAmp[NUM_BIOMES];
            float _blockNoiseScale[NUM_BIOMES];
            float _blockNoiseAmp[NUM_BIOMES];
            float _blockCutoff[NUM_BIOMES];
            float _YStart[NUM_BIOMES];
            float _YHeight[NUM_BIOMES];
            float _horSize[NUM_BIOMES];
            float _XOffset[NUM_BIOMES];
            float4 _tileColor[NUM_BIOMES];
            float4 _airColor[NUM_BIOMES];

            // ----------------------------
            // Lightweight deterministic hash & value-noise (returns 0..1)
            // ----------------------------
            static const float2 HASH_VEC2 = float2(127.1, 311.7);
            static const float2 HASH_VEC2_B = float2(269.5, 183.3);

            float hash1(float n)
            {
                return frac(sin(n) * 43758.5453123);
            }

            float hash2(float2 p)
            {
                // dot + sin hash to get pseudo-random
                float v = dot(p, HASH_VEC2);
                return frac(sin(v) * 43758.5453);
            }

            float valueNoise2D(float2 p)
            {
                // simple value noise / bilinear interpolation with smoothstep
                float2 i = floor(p);
                float2 f = frac(p);

                float a = hash2(i);
                float b = hash2(i + float2(1.0, 0.0));
                float c = hash2(i + float2(0.0, 1.0));
                float d = hash2(i + float2(1.0, 1.0));

                float2 u = f * f * (3.0 - 2.0 * f); // smoothstep interpolation
                float lerpX1 = lerp(a, b, u.x);
                float lerpX2 = lerp(c, d, u.x);
                return lerp(lerpX1, lerpX2, u.y);
            }
            inline float unity_noise_randomValue(float2 uv) {
                 return frac(sin(dot(uv, float2(12.9898, 78.233))) * 43758.5453);
            }
            inline float unity_noise_interpolate(float a, float b, float t) {
                 return (1.0 - t) * a + (t * b);
            }
            inline float unity_valueNoise(float2 uv)
            {
                float2 i = floor(uv);
                float2 f = frac(uv);
                f = f * f * (3.0 - 2.0 * f);

                uv = abs(frac(uv) - 0.5);
                float2 c0 = i + float2(0.0, 0.0);
                float2 c1 = i + float2(1.0, 0.0);
                float2 c2 = i + float2(0.0, 1.0);
                float2 c3 = i + float2(1.0, 1.0);
                float r0 = unity_noise_randomValue(c0);
                float r1 = unity_noise_randomValue(c1);
                float r2 = unity_noise_randomValue(c2);
                float r3 = unity_noise_randomValue(c3);

                float bottomOfGrid = unity_noise_interpolate(r0, r1, f.x);
                float topOfGrid = unity_noise_interpolate(r2, r3, f.x);
                float t = unity_noise_interpolate(bottomOfGrid, topOfGrid, f.y);
                return t;
            }

            float Unity_SimpleNoise_float(float2 UV, float Scale)
            {
                float t = 0.0;

                float freq = pow(2.0, float(0));
                float amp = pow(0.5, float(3 - 0));
                t += unity_valueNoise(float2(UV.x * Scale / freq, UV.y * Scale / freq)) * amp;

                freq = pow(2.0, float(1));
                amp = pow(0.5, float(3 - 1));
                t += unity_valueNoise(float2(UV.x * Scale / freq, UV.y * Scale / freq)) * amp;

                freq = pow(2.0, float(2));
                amp = pow(0.5, float(3 - 2));
                t += unity_valueNoise(float2(UV.x * Scale / freq, UV.y * Scale / freq)) * amp;

                return t;
            }

            // small utility for deterministic per-shader hashing
            float unity_hash(float s)
            {
                return hash1(s);
            }

            // Per-biome noise sampling: keeps parameters authored in C# (unchanged)
            // but decorrelates noise per-biome to get different patterns in each biome.
            float PerBiomeNoise(float2 uv, int biomeIndex, float scale, float seedOffset)
            {
                float perBiomeSeed = _GlobalSeed + (float)biomeIndex * 137.731; // constant to separate biomes
                float2 samplePos = float2(uv.x * scale + perBiomeSeed * 13.19, uv.y * scale + perBiomeSeed * 7.31 + seedOffset);
                return Unity_SimpleNoise_float(samplePos, 1.0);
            }

            // Trench logic (kept similar to original)
            float GenerateTrenchAndSurface(float2 uv, float baseWiden, float baseWidth, float noiseFreq, float edgeAmp, float parallax ,bool useEdge, float seed)
            {
                float halfTrenchWidth = (baseWidth + abs(uv.y) * baseWiden) / 2.0;
                float edgeNoise = (Unity_SimpleNoise_float(float2(uv.x, uv.y + seed), noiseFreq) - 0.5) * 2.0;
                float noisyHalfWidth = max(0.0, halfTrenchWidth + edgeNoise * edgeAmp);

                float maxDepth = abs(-1 * baseWidth / baseWiden) * 0.9 * (1 + parallax);
                float surfaceNoise = ((Unity_SimpleNoise_float(float2(uv.x, uv.y + 2000.0), 1.32) - 0.5) * 2.0) * 3.0;
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

            // World generation core - reads parameters from arrays, noise uses _GlobalSeed & PerBiomeNoise
            float4 WorldGenFull(float2 uv)
            {
                // Start the world as solid
                float4 Color = float4(1, 0, 0, 255) / 255.0;
                // CAVES - use global seed so caves change with seed
                
                float caveNoise = step(Unity_SimpleNoise_float(float2(uv.x, uv.y + _GlobalSeed * 4000.0), _CaveNoiseScale) * _CaveAmp, _CaveCutoff);
                if (caveNoise < 0.5) {
                    // Cave
                    Color = float4(0, 1, 1, 1);
                }
                
                // ---------- 2D normalized-distance biome pick (cheap) ----------
                int biomeIndex = 0;
                float bestScore = 1e20;
                
                // iterate biomes and pick the one with smallest normalized squared distance
                // This is so we know what calculations to use!
                for (int bi = 0; bi < NUM_BIOMES; ++bi)
                {
                    // center of biome in Y = middle of the vertical band
                    float centerX = _XOffset[bi];
                    float centerY = _YStart[bi] + 0.5 * _YHeight[bi];
                
                    // dx, dy from pixel to biome center
                    float dx = uv.x - centerX;
                    float dy = uv.y - centerY;
                
                    // normalization factors: horizontal "radius" and vertical "radius"
                    // interpret _horSize as half-width (same semantic as other existing width check).
                    // If your _horSize is full-width, divide by 2 here instead.
                    float radiusX = max(0.0001, _horSize[bi]);              // avoid div0
                    float radiusY = max(0.0001, 0.5 * _YHeight[bi]);       // half-height as vertical radius
                
                    // normalized squared distance (no sqrt)
                    float nx = dx / radiusX;
                    float ny = dy / radiusY;
                    float score = nx*nx + ny*ny;
                
                    if (score < bestScore)
                    {
                        bestScore = score;
                        biomeIndex = bi;
                    }
                }
                // fetch biome parameters from arrays
                float b_edgeScale  = _edgeNoiseScale[biomeIndex];
                float b_edgeAmp    = _edgeNoiseAmp[biomeIndex];
                float b_blockScale = _blockNoiseScale[biomeIndex];
                float b_blockAmp   = _blockNoiseAmp[biomeIndex];
                float b_blockCut   = _blockCutoff[biomeIndex];
                float b_YStart     = _YStart[biomeIndex];
                float b_YHeight    = _YHeight[biomeIndex];
                float b_horSize    = _horSize[biomeIndex];
                float b_XOffset    = _XOffset[biomeIndex];
                float4 b_tileColor = _tileColor[biomeIndex];
                float4 b_airColor  = _airColor[biomeIndex];

                // Edge noise for shape (per-biome noise fields)
                float edgeNoiseX = (PerBiomeNoise(uv, biomeIndex, b_edgeScale, 5000.0) - 0.5) * 2.0;
                float edgeNoiseY = (PerBiomeNoise(uv, biomeIndex, b_edgeScale, 2000.0) - 0.5) * 2.0;

                float width = max(0.0, b_horSize + edgeNoiseX * b_edgeAmp);
                float heightTop = b_YStart + b_YHeight + edgeNoiseY * b_edgeAmp;
                float heightBottom = b_YStart + edgeNoiseY * b_edgeAmp;

                bool isInBiomeRegion = (width > abs(uv.x - b_XOffset)) && (uv.y >= heightBottom && uv.y < heightTop);

                if (isInBiomeRegion)
                {
                    // block vs air within the biome using per-biome block noise
                    float blockVal = PerBiomeNoise(uv, biomeIndex, b_blockScale, 7000.0) * b_blockAmp;
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

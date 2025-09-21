Shader "Custom/BackgroundWorldGenLayer"
{
    Properties
    {
        _MainTex ("Dummy (not used)", 2D) = "white" {}
        _EdgeTex ("Edge Texture (set from C# per-biome)", 2D) = "white" {}
        _FillTex ("Fill Texture (set from C# per-biome)", 2D) = "white" {}

        // world mapping & parallax
        _WorldUVScale ("World UV Scale", Float) = 1.0
        _CameraPos ("Camera World Pos", Vector) = (0,0,0,0)
        _ParallaxFactor ("Parallax Factor", Float) = 0.5

        // pixelization
        _PixelSize ("Pixels per Unit (pixelization)", Float) = 8.0
        _ScreenRatio ("Screen Ratio (H/W)", Float) = 1.0
        
        _WorldUVScale ("World UV Scale", Float) = 0.1
        // debug
        _DebugMode ("Debug Mode (0=off,1=mask,2=edge)", Float) = 0.0
        _TrenchBaseWiden ("Widen)", Float) = 0.0
        _TrenchBaseWidth ("Width)", Float) = 0.0
        _TrenchNoiseScale ("Scale)", Float) = 0.0
        _TrenchEdgeAmp ("EdgeAmp)", Float) = 0.0
    }

    SubShader
    {
        Tags { "Queue"="Background" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Pass
        {
            ZWrite Off
            Blend SrcAlpha OneMinusSrcAlpha
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0

            #include "UnityCG.cginc"

            sampler2D _EdgeTex;
            sampler2D _FillTex;

            float _WorldUVScale;
            float4 _CameraPos;
            float _ParallaxFactor;
            float _PixelSize;
            float _ScreenRatio;
            float _DebugMode;   
            
            // Caves
            float _CaveNoiseScale;
            float _CaveAmp;
            float _CaveCutoff;

            // Trench
            float _TrenchBaseWiden;
            float _TrenchBaseWidth;
            float _TrenchNoiseScale;
            float _TrenchEdgeAmp;

            // ---------- Per-biome param arrays (set from C# like in your WorldGen) ----------
            // Make sure NUM_BIOMES here matches what your C# uploader uses.
            #define NUM_BIOMES 6

            float _edgeNoiseScale[NUM_BIOMES];
            float _edgeNoiseAmp[NUM_BIOMES];
            float _blockNoiseScale[NUM_BIOMES];
            float _blockNoiseAmp[NUM_BIOMES];
            float _blockCutoff[NUM_BIOMES];
            float _YStart[NUM_BIOMES];
            float _YHeight[NUM_BIOMES];
            float _horSize[NUM_BIOMES];
            float _XOffset[NUM_BIOMES];

            float _GlobalSeed; // set from C#

            // --- Minimal value-noise & hash (same idea as WorldGen) ---
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

            float PerBiomeNoiseSimple(float2 uv, int biomeIndex, float scale, float seedOffset)
            {
                float perBiomeSeed = _GlobalSeed + (float)biomeIndex * 137.731;
                float2 samplePos = float2(uv.x * scale + perBiomeSeed * 13.19, uv.y * scale + perBiomeSeed * 7.31 + seedOffset);
                return Unity_SimpleNoise_float(samplePos, 1.0);
            }
            // pick nearest biome in normalized 2D space (smaller score = closer)
            int PickBiome2D(float2 uv)
            {
                int chosen = 0;
                float bestScore = 1e20;
                for (int bi = 0; bi < NUM_BIOMES; ++bi)
                {
                    float cx = _XOffset[bi];
                    float cy = _YStart[bi] + 0.5 * _YHeight[bi];
            
                    float dx = uv.x - cx;
                    float dy = uv.y - cy;
            
                    float radiusX = max(0.0001, _horSize[bi]); // adjust if horSize is full-width
                    float radiusY = max(0.0001, 0.5 * _YHeight[bi]);
            
                    float nx = dx / radiusX;
                    float ny = dy / radiusY;
                    float score = nx*nx + ny*ny;
                    if (score < bestScore)
                    {
                        bestScore = score;
                        chosen = bi;
                    }
                }
                return chosen;
            }
            
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
            // ---------- mask generator: returns 0..1 alpha for a given world-space coord ----------
            // Modified/simplified from WorldGen: only evaluates the currently selected biome (by index).
            // It returns 1 for solid (tile) and 0 for empty (air).
            float GetWorldMask(float2 uv)
            {
                // caves (global) - explicit compare
                 float caveNoise = Unity_SimpleNoise_float(float2(uv.x * _CaveNoiseScale + _GlobalSeed * 2.79, uv.y * _CaveNoiseScale + _GlobalSeed * 8.69),1) * _CaveAmp;
                 // Use a fixed threshold for caves; you can tune or expose per-biome if you want.
                if (caveNoise < _CaveCutoff) {
                    return 0.0;
                }
                // Trench
                float trenchMask = GenerateTrenchAndSurface(uv, _TrenchBaseWiden, _TrenchBaseWidth, _TrenchNoiseScale, _TrenchEdgeAmp, 0.0, false, _GlobalSeed);
                if (trenchMask < 0.5) {
                    return 0.0;
                }
                int biomeIndex = PickBiome2D(uv);

                // get basic authored params for the selected biome
                float b_edgeScale  = _edgeNoiseScale[biomeIndex];
                float b_edgeAmp    = _edgeNoiseAmp[biomeIndex];
                float b_blockScale = _blockNoiseScale[biomeIndex];
                float b_blockAmp   = _blockNoiseAmp[biomeIndex];
                float b_blockCut   = _blockCutoff[biomeIndex];
                float b_YStart     = _YStart[biomeIndex];
                float b_YHeight    = _YHeight[biomeIndex];
                float b_horSize    = _horSize[biomeIndex];
                float b_XOffset    = _XOffset[biomeIndex];

                // edge noise for shape
                float edgeNoiseX = (PerBiomeNoiseSimple(uv, biomeIndex, b_edgeScale, 5000.0) - 0.5) * 2.0;
                float edgeNoiseY = (PerBiomeNoiseSimple(uv, biomeIndex, b_edgeScale, 2000.0) - 0.5) * 2.0;
                float width = max(0.0, b_horSize + edgeNoiseX * b_edgeAmp);
                float heightTop = b_YStart + b_YHeight + edgeNoiseY * b_edgeAmp;
                float heightBottom = b_YStart + edgeNoiseY * b_edgeAmp;

                // membership
                if ((width > abs(uv.x - b_XOffset)) && (uv.y >= heightBottom && uv.y < heightTop))
                {
                    // block/air via block-noise
                    float blockVal = PerBiomeNoiseSimple(uv, biomeIndex, b_blockScale, 7000.0) * b_blockAmp;
                    if (blockVal < b_blockCut)
                        return 1.0;
                    else
                        return 0.0;
                }

                return 1.0;
            }

            // very small helper to integer-cast biome index safely
            int IntBiomeIndex(float f) { return max(0, min(NUM_BIOMES - 1, (int)f)); }

            struct appdata {
                float4 vertex : POSITION; 
                float2 uv : TEXCOORD0; 
                float4 color : COLOR; 
            };
            struct v2f { 
                float4 pos : SV_POSITION; 
                float2 uv : TEXCOORD0; 
                float4 color : COLOR; 
                float3 worldPos : TEXCOORD1;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv; // 0..1 across the sprite
                o.color = v.color;
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                return o;
            }

            // pixelization helper: floor to pixel grid (keeps blocky edges square even on non-square pixels)
            float2 PixelizeUV(float2 uv, float pixelSize, float screenRatio)
            {
                // pixelSize = number of "blocks" per world unit (or per sprite width depending on mapping)
                // To keep square blocks on non-square viewport we scale Y by screenRatio before floor.
                float2 scaled = uv * pixelSize;
                scaled.y *= screenRatio;
                scaled = floor(scaled);
                scaled.y /= screenRatio;
                return scaled / pixelSize;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // 1) Map sprite UV (0..1) to world coords (same approach as WorldGen)
                float2 worldUV = i.worldPos.xy * _WorldUVScale;

                // 2) Parallax: shift the world coord based on camera position and parallax factor
                // Camera pos passed in _CameraPos.xy
                float2 cam = _CameraPos.xy;
                float par = _ParallaxFactor;
                float2 parUV = ((worldUV - cam) * par) + worldUV; // your proposed formula ((UV - CameraPos) * Par) + UV

                // 3) Pixelize (blocky look) - do on parUV so parallax keeps blocks-stable
                float2 pixUV = PixelizeUV(parUV, _PixelSize, _ScreenRatio);

                // We'll sample the mask at pixUV. For edge detection we do a simple erosion (min of neighbors).

                // a) compute mask at center pixel
                float centerMask = GetWorldMask(pixUV);

                // b) compute eroded mask by sampling the 4-neighbors inside the pixel grid
                // use neighbor offsets of one pixel in world-space (inverse of pixel size)
                float2 pxStep = float2(1.0 / _PixelSize, (1.0 / _PixelSize) / _ScreenRatio);

                float m1 = GetWorldMask(pixUV + float2(pxStep.x, 0.0));
                float m2 = GetWorldMask(pixUV + float2(-pxStep.x, 0.0));
                float m3 = GetWorldMask(pixUV + float2(0.0, pxStep.y));
                float m4 = GetWorldMask(pixUV + float2(0.0, -pxStep.y));

                float eroded = min(centerMask, min(min(m1,m2), min(m3,m4)));

                // edgeMask: pixels that are solid (centerMask==1) but lost after erosion -> edge band
                float edgeMask = saturate(centerMask - eroded); // 1 on thin edges, 0 elsewhere
                float fillMask = centerMask * (1.0 - edgeMask);

                // sample textures: prefer edge if edgeMask>0 else fill
                float4 edgeCol = tex2D(_EdgeTex, worldUV); // sample using original sprite uv so textures map to screen
                float4 fillCol = tex2D(_FillTex, worldUV);

                // combine with masks to compute final color/alpha
                float4 outCol = lerp(fillCol, edgeCol, edgeMask); // pick edge color where edgeMask==1
                float outAlpha = saturate(centerMask); // 1 if inside, 0 otherwise

                // Debugging modes
                if (_DebugMode > 0.5)
                {
                    if (_DebugMode < 1.5) return float4(centerMask, centerMask, centerMask, 1.0); // mask visualization
                    if (_DebugMode < 2.5) return float4(edgeMask, edgeMask, edgeMask, 1.0); // edge visual
                }

                outCol.a = outAlpha;
                // multiply by vertex color if desired:
                outCol.rgb *= i.color.rgb;
                outCol.a *= i.color.a;
                return outCol;
            }
            ENDHLSL
        }
    }
    FallBack "Sprites/Default"
}
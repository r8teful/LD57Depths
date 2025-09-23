Shader "Custom/BackgroundWorldGenLayer"
{
    Properties
    {
        _MainTex ("Dummy (not used)", 2D) = "white" {}
        _EdgeTex ("Edge Texture (set from C# per-biome)", 2D) = "white" {}
        _FillTexArray ("Fill Texture Array", 2DArray) = "" {}
        // world mapping & parallax
        _WorldUVScale ("World UV Scale", Float) = 1.0
        _CameraPos ("Camera World Pos", Vector) = (0,0,0,0)
        _ParallaxFactor ("Parallax Factor", Float) = 0.5

        // pixelization
        _PixelSize ("Pixels per Unit (pixelization)", Float) = 8.0
        _ScreenRatio ("Screen Ratio (H/W)", Float) = 1.0
        
        _EdgeThickness ("EdgeThickness", Float) = 0.1
        _TrenchEdgeSens ("TrenchEdgeSensitivity", Float) = 0.1

        _TextureTiling ("Backgroun Texture Tiling", Float) = 1.0
        _TintColor ("EdgeColor", Color) = (1, 1, 1, 1)
        _NonEdgeDarkness("NonEdgeBackgroundColor", Color) = (0, 0, 0, 0)
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
            float _TextureTiling;
            float _PixelSize;
            float _ScreenRatio;
            float _DebugMode;   
            float _TrenchEdgeSens;   
            float _EdgeThickness;     
            float _EdgeInnerRim;            // 0 = off, 1 = on (if you want a rim inside hole)
            float _EdgeOuterRim;            // 0 = off, 1 = on (rim on the filled side)
            float4 _NonEdgeDarkness;
            float4 _TintColor;
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
            // Background array
            UNITY_DECLARE_TEX2DARRAY(_FillTexArray);
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

            float PerBiomeNoise(float2 uv, int biomeIndex, float scale, float seedOffset)
            {
                float perBiomeSeed = _GlobalSeed + (float)biomeIndex * 137.731;
                float2 samplePos = float2(uv.x * scale + perBiomeSeed * 13.19, uv.y * scale + perBiomeSeed * 7.31 + seedOffset);
                return Unity_SimpleNoise_float(samplePos, 1.0);
            }
            // pick nearest biome in normalized 2D space (smaller score = closer)
            int PickBiome2D(float2 uv)
            {
                int chosen = NUM_BIOMES; 
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
            int PickBiome2DBetter(float2 uv) {
                for (int i = 0; i < NUM_BIOMES; i++) { 
                    // Fetch per-biome parameters
                    float b_edgeScale = _edgeNoiseScale[i];
                    float b_edgeAmp = _edgeNoiseAmp[i];
                    float b_YStart = _YStart[i];
                    float b_YHeight = _YHeight[i];
                    float b_horSize = _horSize[i];
                    float b_XOffset = _XOffset[i];

                    // Compute biome bounds with noise TODO, could make the noise scale less to have a more natural wave
                    float edgeNoiseX = (PerBiomeNoise(uv, i, b_edgeScale, 5000.0) - 0.5) * 2.0;
                    float edgeNoiseY = (PerBiomeNoise(uv, i, b_edgeScale, 2000.0) - 0.5) * 2.0;

                    float width = max(0.0, b_horSize + edgeNoiseX * b_edgeAmp);
                    float heightTop = b_YStart + b_YHeight + edgeNoiseY * b_edgeAmp;
                    float heightBottom = b_YStart + edgeNoiseY * b_edgeAmp;
                    
                    /*
                    // Trying simple now without edge noise
                    float width = max(0.0, b_horSize);
                    float heightTop = b_YStart + b_YHeight;
                    float heightBottom = b_YStart;
                    */

                    // Check if UV is inside this biome's region
                    bool isInBiomeRegion = (width > abs(uv.x - b_XOffset)) && (uv.y >= heightBottom && uv.y < heightTop);

                    if (isInBiomeRegion) {
                        return i + 1; // Shifting here, and will shift back in world mask
                    }
                }
                return 0;
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
            // --- Utility: turn a signed scalar into fill + edge masks ---
            // s: signed value where s>0 = filled, s<=0 = hole
            // thickness: width of edge band (in the same units as s). If <=0 -> no edge.
            // Returns: fillMask (0..1), edgeMask (0..1 where 1 == at inner rim)
            void MakeMasksFromSigned(float s, float edgeThicknessPixels, out float fillMask, out float edgeMask)
            {
                // anti-alias around zero using the derivative of s.
                // This value scales `s` so that 1 unit of `s` corresponds to 1 pixel.
                float screenSpaceScale = fwidth(s); 
                float aa = screenSpaceScale * 0.5; // Half a pixel for anti-aliasing

                // fillMask: smooth step from hole (s<=0) to filled (s>0)
                fillMask = smoothstep(-aa, aa, s);

                if (edgeThicknessPixels <= 0.0)
                {
                    edgeMask = 0.0;
                    return;
                }

                // Convert desired pixel thickness to 's' units using the screenSpaceScale.
                // So, 'edgeThicknessSDFUnits' is the raw 's' value corresponding to the desired pixel thickness.
                float edgeThicknessSDFUnits = edgeThicknessPixels * screenSpaceScale;

                // The 'band' defines the region of the edge.
                // It goes from 0 at s=0 (inner rim) to 1 at s=edgeThicknessSDFUnits (outer edge of band).
                float band = saturate(s / edgeThicknessSDFUnits);
                
                // edgeMask is strong when band is near 0, fades to 0 when band -> 1.
                // Use smoothstep for soft falloff and multiply by fillMask so holes get zero edge.
                // The smoothstep operates over the full width of the edge band.
                // Using fwidth(band) for anti-aliasing ensures smooth transition within the edge.
                float edgeSmoothness = fwidth(band); // Derivative of band, to smooth the fade
                edgeMask = (1.0 - smoothstep(0.0, 1.0, band)) * fillMask; // Original implementation: fade from 1 to 0 across the band
            }
            void MakeMasksFromSigned2(float s, float thickness, out float fillMask, out float edgeMask)
            {
                // anti-alias around zero using the derivative of s
                float aa = max(1e-5, fwidth(s) * 0.5);
            
                // fillMask: smooth step from hole (s<=0) to filled (s>0)
                fillMask = smoothstep(-aa, aa, s);
            
                if (thickness <= 0.0)
                {
                    edgeMask = 0.0;
                    return;
                }
            
                // band: normalized [0..1] across [s=0 .. s=thickness]
                float band = saturate(s / thickness); // 0 at s=0 (rim), 1 at s>=thickness
                // edgeMask is strong when band is near 0, fades to 0 when band -> 1.
                // Use smoothstep for soft falloff and multiply by fillMask so holes get zero edge.
                
                //edgeMask = (1.0 - smoothstep(0.0, 1.0, band)) * fillMask;
                edgeMask = pow(1.0 - smoothstep(0.0, 1.0, band), 1.8) * fillMask; //sharper peak at the inner rim
                //edgeMask = 1.0 - smoothstep(_EdgeThickness, _EdgeThickness+ fwidth(s), abs(s));
            }

            // caves: s = caveNoise - cutoff  (caveNoise < cutoff -> hole because s < 0)
            float SampleCaveSigned(float2 uv)
            {
                float caveNoise = Unity_SimpleNoise_float(
                    float2(uv.x * _CaveNoiseScale + _GlobalSeed * 2.79,
                            uv.y * _CaveNoiseScale + _GlobalSeed * 8.69), 1.0) * _CaveAmp;
                return caveNoise - _CaveCutoff;
            }

            // trench: signed distance-ish: s = abs(uv.x) - noisyHalfWidth
            // Also respects surfaceNoise and maxDepth like your original function.
            // Positive s -> outside trench (filled). Negative s -> inside trench (hole).
            float SampleTrenchSigned(float2 uv, float baseWiden, float baseWidth, float noiseFreq, float edgeAmp, float parallax, float seed)
            {
                // --- 1. Calculate the noisy, widening trench walls ---
                float halfTrenchWidth = (baseWidth + abs(uv.y) * baseWiden) / 2.0;
                float edgeNoise = (Unity_SimpleNoise_float(float2(uv.x, uv.y + seed), noiseFreq) - 0.5) * 2.0;
                float noisyHalfWidth = max(0.0, halfTrenchWidth + edgeNoise * edgeAmp);

                // SDF for the vertical trench walls.
                // This is the horizontal distance from the current UV to the noisy edge.
                // It's negative inside and positive outside.
                float trenchWallsSDF = abs(uv.x) - noisyHalfWidth;

                // --- 2. Calculate the noisy top surface ---
                float surfaceNoise = ((Unity_SimpleNoise_float(float2(uv.x, uv.y + 2000.0), 1.32) - 0.5) * 2.0) * 3.0;
                
                // SDF for the top surface. We want the area *above* this surface to be part of the trench.
                // 'surfaceNoise - uv.y' is negative above the surface line and positive below.
                float topSurfaceSDF = surfaceNoise - uv.y;

                // --- 3. Combine the trench and surface (Union) ---
                // The final "empty" area is the union of the space between the walls AND the space above the surface.
                // The union operation for SDFs is min().
                float combinedTrenchSDF = min(trenchWallsSDF, topSurfaceSDF);

                // --- 4. Apply the maximum depth floor (Intersection) ---
                // This acts as a hard floor. The world is solid below this depth.
                // Note: Assuming uv.y is negative for depth.
                float maxDepth = abs(-1 * baseWidth / baseWiden) * 0.9 * (1 + parallax);

                // SDF for the floor. We want the area *above* y = -maxDepth.
                // This is negative above the floor and positive below it.
                float floorSDF = -uv.y - maxDepth;
                
                // We want the intersection of our trench shape and the area "above the floor".
                // The intersection operation for SDFs is max().
                float finalSDF = max(combinedTrenchSDF, floorSDF);
                
                // Divide the final distance field by the characteristic size of the trench.
                // This changes the output from absolute world units to a scale-independent ratio.
                // We add a small epsilon to baseWidth to prevent division by zero.
                return finalSDF / max(baseWidth*_TrenchEdgeSens, 0.0001);
            }

            // ---------- Biome block sampler: option to normalize by amplitude ----------
            float SampleBiomeBlockSigned(float2 uv, int shiftedIndex, float b_blockScale, float b_blockAmp, float b_blockCut, bool normalizeByAmp)
            {
                float blockVal = PerBiomeNoise(uv, shiftedIndex, b_blockScale, 7000.0) * b_blockAmp;
                //float s = b_blockCut - blockVal; // >0 filled as before
                float s = blockVal- b_blockCut ; // >0 filled as before

                if (normalizeByAmp)
                {
                    // Normalize per-biome so edge-thickness has similar numeric meaning across biomes.
                    // Be careful if b_blockAmp is near zero.
                    float denom = max(1e-4, b_blockAmp);
                    s = s / denom;
                }

                return s;
            }
            // ---------- mask generator: returns 0..1 alpha for a given world-space coord ----------
            // Modified/simplified from WorldGen: only evaluates the currently selected biome (by index).
            // It returns 1 for solid (tile) and 0 for empty (air).
            float GetWorldMask(float2 uv,int  biomeIndex,float extraCutoff) {
                if(biomeIndex < 1) {
                    // Not in any biome reagon, do world stuff
                    // caves (global) - explicit compare
                    float caveNoise = Unity_SimpleNoise_float(float2(uv.x * _CaveNoiseScale + _GlobalSeed * 2.79, uv.y * _CaveNoiseScale + _GlobalSeed * 8.69),1) * _CaveAmp;
                    // Use a fixed threshold for caves; you can tune or expose per-biome if you want.
                    if (caveNoise < _CaveCutoff+extraCutoff) {
                        return 0.0;
                    }
                    // Trench
                    float trenchMask = GenerateTrenchAndSurface(uv, _TrenchBaseWiden, _TrenchBaseWidth, _TrenchNoiseScale, _TrenchEdgeAmp, 0.0, false, _GlobalSeed);
                    if (trenchMask < 0.5+extraCutoff) {
                        return 0.0;
                    }
                    return 1.0;
                } else {
                    int shiftedIndex = biomeIndex - 1; // We shifted it forward because 0 is default, but now shift it back to get the right index for the array values
                    float b_blockCut   = _blockCutoff[shiftedIndex];
                    float b_blockScale = _blockNoiseScale[shiftedIndex];
                    float b_blockAmp   = _blockNoiseAmp[shiftedIndex];
                     float blockVal = PerBiomeNoise(uv, shiftedIndex, b_blockScale, 7000.0) * b_blockAmp;
                    if (blockVal < b_blockCut+extraCutoff)
                        return 1.0;
                    else
                        return 0.0;
                }
            }
               
            void GetWorldFillAndEdge(float2 uv, int biomeIndex, float edgeThickness, out float fillMask, out float edgeMask) {
                // default s = large positive (filled)
                float s = 1e2;

                if (biomeIndex < 1)
                {
                    // world logic: cave and trench both can create holes; if either indicates a hole, treat as hole.
                    float sCave = SampleCaveSigned(uv);
                    float sTrench = SampleTrenchSigned(uv, _TrenchBaseWiden, _TrenchBaseWidth, _TrenchNoiseScale, _TrenchEdgeAmp, 0.0, _GlobalSeed);

                    // combine so that any hole-producing feature wins (min). If you have more features, min() them too.
                    s = min(sCave, sTrench);
                }
                else
                {
                    int shiftedIndex = biomeIndex - 1;
                    float b_blockCut   = _blockCutoff[shiftedIndex];
                    float b_blockScale = _blockNoiseScale[shiftedIndex];
                    float b_blockAmp   = _blockNoiseAmp[shiftedIndex];
                    s = SampleBiomeBlockSigned(uv, shiftedIndex, b_blockScale, b_blockAmp, b_blockCut,false);
                }

                // produce smooth masks from signed scalar
                MakeMasksFromSigned(s, edgeThickness, fillMask, edgeMask);
            }
            void GetWorldFill(float2 uv, int biomeIndex, out float fillMask)
            {
                float s = 1e2; // Default to solid
            
                if (biomeIndex < 1)
                {
                    float sCave = SampleCaveSigned(uv);
                    float sTrench = SampleTrenchSigned(uv, _TrenchBaseWiden, _TrenchBaseWidth, _TrenchNoiseScale, _TrenchEdgeAmp, 0.0, _GlobalSeed);
                    s = min(sCave, sTrench);
                }
                else
                {
                    int shiftedIndex = biomeIndex - 1;
                    float b_blockCut   = _blockCutoff[shiftedIndex];
                    float b_blockScale = _blockNoiseScale[shiftedIndex];
                    float b_blockAmp   = _blockNoiseAmp[shiftedIndex];
                    s = SampleBiomeBlockSigned(uv, shiftedIndex, b_blockScale, b_blockAmp, b_blockCut, false);
                }
                
                // We only need the fill mask. Use a simpler version of MakeMasksFromSigned.
                // Use fwidth for anti-aliasing to prevent jagged edges even on the blocks.
                //float aa = fwidth(s) * 0.5;
                //fillMask = smoothstep(-aa, aa, s);
                fillMask = step(0.0, s);
            }
            // Don't know why we need 3 of these but I'm just trying to make it work!!
            // Replaces GetWorldFill, GetWorldFillAndEdge, and GetWorldFill_HardAlpha
            float GetWorldSDF(float2 uv, int biomeIndex)
            {
                float s = 1e2; // Default to solid
            
                if (biomeIndex < 1)
                {
                    float sCave = SampleCaveSigned(uv);
                    float sTrench = SampleTrenchSigned(uv, _TrenchBaseWiden, _TrenchBaseWidth, _TrenchNoiseScale, _TrenchEdgeAmp, 0.0, _GlobalSeed);
                    s = min(sCave, sTrench);
                }
                else
                {
                    int shiftedIndex = biomeIndex - 1;
                    float b_blockCut   = _blockCutoff[shiftedIndex];
                    float b_blockScale = _blockNoiseScale[shiftedIndex];
                    float b_blockAmp   = _blockNoiseAmp[shiftedIndex];
                    s = SampleBiomeBlockSigned(uv, shiftedIndex, b_blockScale, b_blockAmp, b_blockCut, true);
                }
            
                return s;
            }
               

                /*
                float b_edgeScale  = _edgeNoiseScale[biomeIndex];
                float b_edgeAmp    = _edgeNoiseAmp[biomeIndex];
                float b_YStart     = _YStart[biomeIndex];
                float b_YHeight    = _YHeight[biomeIndex];
                float b_horSize    = _horSize[biomeIndex];
                float b_XOffset    = _XOffset[biomeIndex];

                // My tought here is that we don't need to do this edge shit again and check if we are in the biome
                // because the biome index already SAYS that we are in the biome, so why check it twice
                float edgeNoiseX = (PerBiomeNoise(uv, biomeIndex, b_edgeScale, 5000.0) - 0.5) * 2.0;
                float edgeNoiseY = (PerBiomeNoise(uv, biomeIndex, b_edgeScale, 2000.0) - 0.5) * 2.0;
                float width = max(0.0, b_horSize + edgeNoiseX * b_edgeAmp);
                float heightTop = b_YStart + b_YHeight + edgeNoiseY * b_edgeAmp;
                float heightBottom = b_YStart + edgeNoiseY * b_edgeAmp;

                // membership
                if ((width > abs(uv.x - b_XOffset)) && (uv.y >= heightBottom && uv.y < heightTop))
                {
                    // block/air via block-noise
                    float blockVal = PerBiomeNoise(uv, biomeIndex, b_blockScale, 7000.0) * b_blockAmp;
                    if (blockVal < b_blockCut)
                        return 1.0;
                    else
                        return 0.0;
                }
                */

            

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
                //scaled = round(scaled);
                scaled = floor(scaled + 0.5);
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
                float2 parUV = ((worldUV - cam) * _ParallaxFactor) + worldUV; // your proposed formula ((UV - CameraPos) * Par) + UV

                // 3) Pixelize (blocky look) - do on parUV so parallax keeps blocks-stable
                float2 pixUV = PixelizeUV(parUV, _PixelSize, _ScreenRatio);
                
                int biomeIndex = PickBiome2DBetter(parUV);
                float s_HighRes = GetWorldSDF(parUV, biomeIndex);
                float s_LowRes  = GetWorldSDF(pixUV, biomeIndex);
                float s_ForColoring = max(s_HighRes, s_LowRes);
                // a) compute mask at center pixel
                float fillMask_HighRes, edgeMask_HighRes;
                //GetWorldFillAndEdge(parUV, biomeIndex, _EdgeThickness, fillMask_HighRes, edgeMask_HighRes);
                MakeMasksFromSigned2(s_ForColoring, _EdgeThickness, fillMask_HighRes, edgeMask_HighRes);
                
                // b) Low-Res HARD mask for pixelated alpha, using the original low-res SDF
                float fillMask_Pixelated = step(0.0, s_LowRes);
                // --- LOW-RESOLUTION PASS for ALPHA ---
                // Calculate a blocky fill mask for the final alpha channel.
                //float fillMask_Pixelated;
                //GetWorldFill(pixUV, biomeIndex, fillMask_Pixelated);

                float3 uvw = float3(parUV * _TextureTiling, biomeIndex); // ParallaxUV + biome index is shifted to +1 because texture array is setup to have 0 as default, and rest as biomes
                fixed4 baseCol = UNITY_SAMPLE_TEX2DARRAY(_FillTexArray, uvw);
                // combine with masks to compute final color/alpha
                //float4 outCol = lerp(fillCol, edgeCol, edgeMask); // pick edge color where edgeMask==1
                //float outAlpha = saturate(fillMask); // 1 if inside, 0 otherwise
                
                // a) Darken the filled area, but NOT the edge area.
                float nonEdgeFilled = fillMask_HighRes * (1.0 - edgeMask_HighRes);
                float3 darkenedColor = baseCol.rgb * _NonEdgeDarkness.rgb * nonEdgeFilled; 
                
                float3 darkenedBase = lerp(baseCol.rgb, _TintColor.rgb,_TintColor.a);
                //float nonEdgeFilled = fillMask * (1.0 - edgeMask);
                //float darkened = baseCol * (1.0 - _NonEdgeDarkness * nonEdgeFilled); 
                
                // Edge color blending
                //float4 edgeColor = _EdgeColor; // set in material
                float3 finalColor = lerp(darkenedColor, darkenedBase, edgeMask_HighRes);
                float4 result = float4(finalColor,fillMask_Pixelated);
                
                // Debugging modes
                if (_DebugMode > 0.5)
                {
                    if (_DebugMode < 1.5) return float4(fillMask_Pixelated, fillMask_Pixelated, fillMask_Pixelated, 1.0); // mask visualization
                    if (_DebugMode < 2.5) return float4(edgeMask_HighRes, edgeMask_HighRes, edgeMask_HighRes, 1.0); // edge visual
                }

                return result;
                
                // multiply by vertex color if desired:
                //outCol.rgb *= i.color.rgb;
                //outCol.a *= i.color.a;
                //return outCol;
            }
            ENDHLSL
        }
    }
    FallBack "Sprites/Default"
}
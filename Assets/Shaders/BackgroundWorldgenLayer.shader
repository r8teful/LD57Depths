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
        _DarkenMax ("_DarkenMax", Float) = 1.0
        _DarkenCurve("_DarkenCurve", Float) = 1.0
        _EdgeTintColor ("EdgeColor", Color) = (1, 1, 1, 1)
        // debug
        _DebugMode ("Debug Mode (0=off,1=mask,2=edge)", Float) = 0.0
        _TrenchBaseWiden ("Widen)", Float) = 0.0
        _TrenchBaseWidth ("Width)", Float) = 0.0
        _TrenchNoiseScale ("Scale)", Float) = 0.0
        _FillMaskStep ("_FillMaskStep)", Float) = 0.0
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
            #include "RandomnesHelpers.hlsl"

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
            float4 _EdgeTintColor;
            float _DarkenMax;
            float _DarkenCurve;
            // Caves
            float _CaveNoiseScale;
            float _CaveAmp;
            float _CaveCutoff;
            float _BaseOctaves;
            float _RidgeOctaves;
            float _WarpAmp;
            float _WorleyWeight;

            float _GlobalSeed; 
            // Trench
            float _TrenchBaseWiden;
            float _TrenchBaseWidth;
            float _TrenchNoiseScale;
            float _TrenchEdgeAmp;

            float _FillMaskStep;

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

            float _baseOctaves[NUM_BIOMES];
            float _ridgeOctaves[NUM_BIOMES];
            float _warpAmp[NUM_BIOMES];
            float _worldeyWeight[NUM_BIOMES];
            float _caveType[NUM_BIOMES];

            float4 _ColorArray[NUM_BIOMES];
            // Background array
            UNITY_DECLARE_TEX2DARRAY(_FillTexArray);

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
            int PickBiome2DBetter(float2 uv) {
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

            void MakeMasksFromSigned2(float s, float thickness, out float fillMask, out float edgeMask)
            {
                
                // Compute screen-space derivative
                float ds = fwidth(s);
                ds = max(ds, 1e-5);

                // Convert desired world thickness to screen space
                float screenThickness = thickness / ds; // now in "sdf units per pixel"

                // Anti-aliasing width: ~1 pixel
                float aa = 1.0;

                // Fill mask: sharp at SDF=0
                fillMask = saturate((s / ds + aa) / (2.0 * aa)); // or use smoothstep
                //fillMask = step(0,s); 

                // anti-alias around zero using the derivative of s
                //float aa = max(1e-5, fwidth(s) * 0.5);
            
                // fillMask: smooth step from hole (s<=0) to filled (s>0)
                //fillMask = smoothstep(-aa, aa, s);
            
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
                edgeMask = pow(1.0 - smoothstep(0.0, 1.0, band), 3) * fillMask; //sharper peak at the inner rim
                //edgeMask = 1.0 - smoothstep(_EdgeThickness, _EdgeThickness+ fwidth(s), abs(s));
            
            }
            void MakeMasksFromSigned(float s, float thickness, out float fillMask, out float edgeMask)
            {
                fillMask = step(0,s); 
                if (thickness <= 0.0)
                {
                    edgeMask = 0.0;
                    return;
                }
            
                // Compute local gradient length of s. Use ddx/ddy for more accurate gradient direction
                float2 grad = float2(ddx(s), ddy(s));
                float gradLen = length(grad);
            
                // If ddx/ddy aren't available or for platforms that prefer, fallback to fwidth-based approx:
                // float gradLen = max(length(float2(ddx(s), ddy(s))), max(1e-6, fwidth(s))); // optional hybrid
            
                // Avoid division by zero and clamp to reasonable range
                gradLen = max(gradLen, 1e-6);
            
                // Convert desired 'thickness' (in SDF/world units) into SDF-space scale at this pixel:
                // If the SDF is not a true distance field, s grows at rate 'gradLen', so the effective sdf-thickness must be scaled up:
                float localThickness = thickness * gradLen;
            
                // band: 0 at s=0 (rim), 1 at s >= localThickness
                float band = saturate(s / localThickness);
            
                // edge shape: keep the fillMask to ensure edges appear only inside filled area
                // sharpen the inner rim with a power, as you did
                edgeMask = pow(1.0 - smoothstep(0.0, 1.0, band), 3) * fillMask;
            }

            // caves: s = caveNoise - cutoff  (caveNoise < cutoff -> hole because s < 0)
            float SampleCaveSigned(float2 uv)
            {
                float caveNoise = CaveDensity_Combined(uv,_GlobalSeed,0,_CaveNoiseScale,_BaseOctaves, _RidgeOctaves,_WarpAmp,_WorleyWeight)* _CaveAmp;
                return caveNoise - _CaveCutoff;
            }

            // trench: signed distance-ish: s = abs(uv.x) - noisyHalfWidth
            // Also respects surfaceNoise and maxDepth like your original function.
            // Positive s -> outside trench (filled). Negative s -> inside trench (hole).
            float SampleTrenchSigned(float2 uv, float baseWiden, float baseWidth, float noiseFreq, float edgeAmp, float parallax, float seed)
            {
                // --- 1. Calculate the noisy, widening trench walls ---
                float halfTrenchWidth = (baseWidth + abs(uv.y) * baseWiden) / 2.0;
                float edgeNoise = (EdgeNoise_Smooth(uv, _GlobalSeed,0,noiseFreq) - 0.5) * 2.0;
                float noisyHalfWidth = max(0.0, halfTrenchWidth + edgeNoise * edgeAmp);

                // SDF for the vertical trench walls.
                // This is the horizontal distance from the current UV to the noisy edge.
                // It's negative inside and positive outside.
                float trenchWallsSDF = abs(uv.x) - noisyHalfWidth;

                // --- 2. Calculate the noisy top surface ---
                float surfaceNoise = (EdgeNoise_Smooth(uv, _GlobalSeed,2,4) - 0.5) * 2.0;
                
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
            float SampleBiomeBlockSigned(
                float2 uv, float globalSeed, int biomeIndex, float scale, float amp, float blockCut,int caveType,   
                int baseOctaves, int ridgeOctaves, float warpAmp, float worleyWeight, bool normalizeByAmp)
            {
                 float blockVal = PerBiomeNoise(uv,globalSeed, biomeIndex, scale,caveType,
                        baseOctaves,ridgeOctaves,warpAmp,worleyWeight) * amp;
                //float s = b_blockCut - blockVal; // >0 filled as before
                float s = blockVal- blockCut ; // >0 filled as before

                if (normalizeByAmp)
                {
                    // Normalize per-biome so edge-thickness has similar numeric meaning across biomes.
                    // Be careful if b_blockAmp is near zero.
                    float denom = max(1e-4, amp);
                    s = s / denom;
                }

                return s;
            }
            float GetWorldSDF(float2 uv, int biomeIndex)
            {
                float s = 1e2; // Default to solid
            
                if (biomeIndex < 1)
                {
                    float sCave = SampleCaveSigned(uv);
                    float sTrench = SampleTrenchSigned(uv, _TrenchBaseWiden, _TrenchBaseWidth, _TrenchNoiseScale, _TrenchEdgeAmp, 0.0, _GlobalSeed);
                    //return sCave;
                    s = min(sCave, sTrench);
                }
                else
                {
                    int shiftedIndex = biomeIndex - 1;
                    float b_blockCut   = _blockCutoff[shiftedIndex];
                    float b_blockScale = _blockNoiseScale[shiftedIndex];
                    float b_blockAmp   = _blockNoiseAmp[shiftedIndex];
                    float b_baseOctaves   = _baseOctaves[biomeIndex];
                    float b_ridgeOctaves  = _ridgeOctaves[biomeIndex];
                    float b_warpAmp       = _warpAmp[biomeIndex];
                    float b_worldeyWeight = _worldeyWeight[biomeIndex];
                    float b_caveType      = _caveType[biomeIndex];
                    s = SampleBiomeBlockSigned(uv,_GlobalSeed, biomeIndex, b_blockScale,b_blockAmp,b_blockCut, b_caveType,
                        b_baseOctaves,b_ridgeOctaves,b_warpAmp,b_worldeyWeight, true);
                }
            
                return s;
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
                
                int biomeIndex = PickBiome2DBetter(parUV);
                float s_HighRes = GetWorldSDF(parUV, biomeIndex);
                // a) compute mask at center pixel
                float fillMask_HighRes, edgeMask_HighRes;
                MakeMasksFromSigned(s_HighRes, _EdgeThickness, fillMask_HighRes, edgeMask_HighRes);
                
                float fillMask = step(0, s_HighRes);

                float3 uvw = float3(parUV * _TextureTiling, biomeIndex); // ParallaxUV + biome index is shifted to +1 because texture array is setup to have 0 as default, and rest as biomes
                fixed4 baseCol = UNITY_SAMPLE_TEX2DARRAY(_FillTexArray, uvw);
                
                // a) Darken the filled area, but NOT the edge area.
                float nonEdgeFilled = fillMask_HighRes * (1.0 - edgeMask_HighRes);
                //float3 darkenedColor = baseCol.rgb * _NonEdgeDarkness.rgb * nonEdgeFilled; 

                //float3 nonEdgeDarkness = _ColorArray[biomeIndex].rgb * (1.0 - _ParallaxFactor*2);
                //float3 nonEdgeDarkness = _ColorArray[biomeIndex].rgb;
                // Scale parallax into [0,1]
                // float t = saturate(_ParallaxFactor / 0.4); // Should be dividing by the MAX parralex effect that will results in fully black  if 
                // t = pow(t, _DarkenCurve);
                // t *= _DarkenMax;
                //float t = saturate((_ParallaxFactor / 0.4) * _DarkenMax);
                float t = saturate(_DarkenMax);
                float4 backgroundColor;
                if(biomeIndex < 1){
                    // This means its 0, so not in any biome, use 
                    backgroundColor = float4(0,0,0.007,1); // World color
                } else {
                    backgroundColor = _ColorArray[biomeIndex-1]; // shifting BACK the index again, because first biome color is at position 0 
                }
                float3 nonEdgeDarkness = lerp(backgroundColor, float3(0,0,0), t);
                //float3 nonEdgeDarkness = _NonEdgeDarkness.rgb; // This is pulled from the color array now
                float3 darkenedColor =  nonEdgeDarkness * nonEdgeFilled; 
                
                float3 darkenedBase = lerp(baseCol.rgb, _EdgeTintColor.rgb,_EdgeTintColor.a);
                //float nonEdgeFilled = fillMask * (1.0 - edgeMask);
                //float darkened = baseCol * (1.0 - _NonEdgeDarkness * nonEdgeFilled); 
                
                // Edge color blending
                //float4 edgeColor = _EdgeColor; // set in material
                float3 finalColor = lerp(darkenedColor, darkenedBase, edgeMask_HighRes);
                float4 result = float4(finalColor,fillMask);
                
                // Debugging modes
                if (_DebugMode > 0.5)
                {
                    if (_DebugMode < 1.5) return float4(s_HighRes*_FillMaskStep, 0, 0, 1); // red = positive, blue = negative
                    if (_DebugMode < 2.5) return float4(edgeMask_HighRes, edgeMask_HighRes, edgeMask_HighRes, 1.0); // edge visual
                }

                return result;
            }
            ENDHLSL
        }
    }
    FallBack "Sprites/Default"
}
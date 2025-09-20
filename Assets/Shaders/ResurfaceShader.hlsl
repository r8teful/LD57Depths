// ---- Config ----
#define NUM_BIOMES 10      // compile-time max. Use small numbers for perf & compatibility.
#define BIOME_PLACEMENT_SCALE 1   // controls how wide/zoomed the biome partition map is
#define BIOME_PLACEMENT_SPAN 200.0   // horizontal spread used when generating per-biome XOffset
// ------------------------------------
struct BiomeParams
{
    float edgeNoiseScale;
    float edgeNoiseAmp;
    float blockNoiseScale;
    float blockNoiseAmp;
    float blockCutoff;
    float YStart;
    float YHeight;
    float horSize;
    float XOffset; // center X for this biome
    float4 tileColor; // tile color (rgba 0..1)
    float4 airColor; // air color (rgba 0..1)
};

inline float2 randomVector (float2 UV, float offset)
{
    float2x2 m = float2x2(15.27, 47.63, 99.41, 89.98);
    UV = frac(sin(mul(UV, m)) * 46839.32);
    return float2(sin(UV.y*+offset)*0.5+0.5, cos(UV.x*offset)*0.5+0.5);
}

inline float unity_noise_randomValue(float2 uv)
{
    return frac(sin(dot(uv, float2(12.9898, 78.233))) * 43758.5453);
}

inline float unity_noise_interpolate(float a, float b, float t)
{
    return (1.0 - t) * a + (t * b);
}

float ByteToLinear(uint B)
{
    float sRGB = B / 255.0;
    if (sRGB <= 0.04045)
    {
        return sRGB / 12.92;
    }
    else
    {
        return pow((sRGB + 0.055) / 1.055, 2.4);
    }
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
float unity_hash(float p) {
    uint v = (uint) (int) round(p);
    v ^= 1103515245U;
    v += v;
    v *= v;
    v ^= v >> 5u;
    v *= 0x27d4eb2du;
    return v * (1.0 / float(0xffffffff));
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
// utility: deterministic float in [0,1) from a seed/idx
float Rand01FromSeed(float seed, int idx)
{
    // small trick to alter the input seed per idx
    return frac(unity_hash(seed + (float) idx * 37.13));
}
// deterministic pick from a palette (returns 0..paletteSize-1)
int PickFromSeed(float seed, int idx, int paletteSize)
{
    return (int) floor(Rand01FromSeed(seed, idx) * paletteSize);
}
// Derive a biome's parameters from the global seed and the biome index.
// If you prefer, replace this whole function by reading arrays/uniforms from CPU.
BiomeParams MakeBiomeFromSeed(float seed, int idx)
{
    BiomeParams b;

    // small variety in noise scales / amps
    float r0 = Rand01FromSeed(seed, idx * 11 + 1);
    float r1 = Rand01FromSeed(seed, idx * 11 + 2);
    float r2 = Rand01FromSeed(seed, idx * 11 + 3);
    float r3 = Rand01FromSeed(seed, idx * 11 + 4);
    float r4 = Rand01FromSeed(seed, idx * 11 + 5);

    // Edge noise scale typically lower-freq than block noise
    b.edgeNoiseScale = lerp(0.2, 4.0, r0); // tweak ranges to taste
    b.edgeNoiseAmp = lerp(0.05, 0.8, r1);

    b.blockNoiseScale = lerp(0.5, 8.0, r2);
    b.blockNoiseAmp = lerp(0.3, 1.6, r3);
    b.blockCutoff = lerp(0.2, 0.8, r4);

    // Vertical band (Y)
    float r5 = Rand01FromSeed(seed, idx * 11 + 6);
    float r6 = Rand01FromSeed(seed, idx * 11 + 7);
    b.YStart = lerp(-40.0, 40.0, r5); // world Y baseline for biome (choose range as your world)
    b.YHeight = lerp(8.0, 32.0, r6);

    // Horizontal size & offset
    float r7 = Rand01FromSeed(seed, idx * 11 + 8);
    float r8 = Rand01FromSeed(seed, idx * 11 + 9);
    b.horSize = lerp(8.0, 60.0, r7);
    b.XOffset = (r8 * 2.0 - 1.0) * BIOME_PLACEMENT_SPAN; // spread around origin +/- span

    // Tile / air colors — simple seeded palette pick
    // You can replace this palette with whatever you want; values must be 0..1
    static const float4 paletteTiles[6] =
    {
        float4(90, 253, 255, 255) / 255.0, // cyan-ish
        float4(180, 200, 120, 255) / 255.0, // green-ish
        float4(200, 120, 220, 255) / 255.0, // purple-ish
        float4(225, 190, 150, 255) / 255.0, // sand-ish
        float4(120, 140, 200, 255) / 255.0, // blue-ish
        float4(200, 100, 80, 255) / 255.0 // red-orange
    };
    static const float4 paletteAirs[6] =
    {
        float4(255, 253, 255, 255) / 255.0,
        float4(220, 240, 255, 255) / 255.0,
        float4(255, 230, 255, 255) / 255.0,
        float4(255, 255, 240, 255) / 255.0,
        float4(245, 250, 255, 255) / 255.0,
        float4(255, 240, 230, 255) / 255.0
    };

    int pick = PickFromSeed(seed, idx * 13 + 2, 6);
    b.tileColor = paletteTiles[pick];
    b.airColor = paletteAirs[pick];

    return b;
}
// =================================================================================
// SECTION 1: YOUR CUSTOMIZABLE MASK LOGIC
// =================================================================================
//
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
    // Distance to trench edge
    if (!useEdge)
    {
        // Don't use epsilon
        bool mask2 = (abs(uv.x) < noisyHalfWidth);
        return mask2 ? 0.0 : 1.0;
    }
    float distanceToEdge = abs(abs(uv.x) - noisyHalfWidth);
    //MaskYes = 0.0;
    bool mask = distanceToEdge < 0.1;
    return mask ? 1.0 : 0.0;
}

float4 WorldGenFull(
float2 uv,
// CAVES
float caveNoiseScale,
float caveAmp,
float caveCutoff,
// BIOME
float edgeNoiseScale,
float edgeNoiseAmp,
float blockNoiseScale,
float blockNoiseAmp,
float blockCutoff,
float YStart,
float YHeight,
float XOffset,
float horSize,
// TRENCH
float trenchBaseWiden,
float trenchBaseWidth,
float trenchNoiseScale,
float trenchEdgeAmp,
// OTHER
float seed)
{
    float uniqueSeed = unity_hash(seed);
    // Start the world as solid
    float4 Color = float4(1, 0, 0, 0) / 255.0;
    
    // Add caves
    float caveNoise = step(Unity_SimpleNoise_float(float2(uv.x, uv.y + uniqueSeed *4000), caveNoiseScale) * caveAmp, caveCutoff);
    if (caveNoise < 0.5)
    {
        // Cave
        Color = float4(0, 1, 1, 1);
    }
    
    // Add biomes (just one supported for now)
    float edgeNoiseX = (Unity_SimpleNoise_float(float2(uv.x, uv.y + uniqueSeed * 5000), edgeNoiseScale) - 0.5) * 2.0;
    float edgeNoiseY = (Unity_SimpleNoise_float(float2(uv.x, uv.y + uniqueSeed * 2000), edgeNoiseScale) - 0.5) * 2.0;
    float width = max(0.0, horSize + edgeNoiseX * edgeNoiseAmp);
    
    float heightTop = YStart + YHeight + edgeNoiseY * edgeNoiseAmp;
    float heightBottom = YStart + edgeNoiseY * edgeNoiseAmp;
    if ((width > abs(uv.x - XOffset)) && (uv.y >= heightBottom && uv.y < heightTop))
    {
        // Inside the biome 
        float biomeBlocks = step(Unity_SimpleNoise_float(float2(uv.x, uv.y + uniqueSeed * 5000), blockNoiseScale) * blockNoiseAmp, blockCutoff);
        if (biomeBlocks < 0.5)
        {
            // biome tile
            Color = float4(90, 253, 255, 255) / 255.0;
        }
        else
        {
            Color = float4(255, 253, 255, 255) / 255.0;
            // biome air tile
        }
    }
    
    // Add thrench TODO this is generating both trench and surface which will just assign the same trench biome to all of it, which is not what we ultimatelly want
    float trenchAndSurface = GenerateTrenchAndSurface(uv, trenchBaseWiden, trenchBaseWidth, trenchNoiseScale, trenchEdgeAmp, 0, false, uniqueSeed);
    if (trenchAndSurface < 0.5)
    {
        Color = float4(255, 254.0, 255, 255) / 255.0;
    }
    return Color;
}
// Order doesn't matter in this one
float WorldGenMaskOnly(
float2 uv,
// CAVES
float caveNoiseScale,
float caveAmp,
float caveCutoff,
// BIOME
float edgeNoiseScale,
float edgeNoiseAmp,
float blockNoiseScale,
float blockNoiseAmp,
float blockCutoff,
float YStart,
float YHeight,
float XOffset,
float horSize,
// TRENCH
float trenchBaseWiden,
float trenchBaseWidth,
float trenchNoiseScale,
float trenchEdgeAmp,
// OTHER
float parallax, // Used for background only for proper max depth 
float seed)
{
    float uniqueSeed = unity_hash(seed);
    // Add caves
    float caveNoise = step(Unity_SimpleNoise_float(float2(uv.x, uv.y + uniqueSeed * 4000), caveNoiseScale) * caveAmp, caveCutoff);
    if (caveNoise < 0.5)
    {
        // Cave
        return 1;
    }
    // Add thrench TODO this is generating both trench and surface which will just assign the same trench biome to all of it, which is not what we ultimatelly want
    float trenchAndSurface = GenerateTrenchAndSurface(uv, trenchBaseWiden, trenchBaseWidth, trenchNoiseScale, trenchEdgeAmp, parallax, false, uniqueSeed);
    if (trenchAndSurface < 0.5)
    {
        return 1;
    }
    // Add biomes (just one supported for now)
    float edgeNoiseX = (Unity_SimpleNoise_float(float2(uv.x, uv.y + uniqueSeed * 5000), edgeNoiseScale) - 0.5) * 2.0;
    float edgeNoiseY = (Unity_SimpleNoise_float(float2(uv.x, uv.y + uniqueSeed * 2000), edgeNoiseScale) - 0.5) * 2.0;
    float width = max(0.0, horSize + edgeNoiseX * edgeNoiseAmp);
    
    float heightTop = YStart + YHeight + edgeNoiseY * edgeNoiseAmp;
    float heightBottom = YStart + edgeNoiseY * edgeNoiseAmp;
    if ((width > abs(uv.x - XOffset)) && (uv.y >= heightBottom && uv.y < heightTop))
    {
        // Inside the biome 
        float biomeBlocks = step(Unity_SimpleNoise_float(float2(uv.x, uv.y + uniqueSeed * 5000), blockNoiseScale) * blockNoiseAmp, blockCutoff);
        if (biomeBlocks < 0.5)
        {
            // biome tile
            return 0; // solid
        }
        else
        {
            return 1;
        }
    }
    return 0;
}
// Used as a custom node
void WorldGenFull_float (
float2 uv,
// CAVES
float caveNoiseScale, 
float caveAmp, 
float caveCutoff,
// BIOME
float edgeNoiseScale, 
float edgeNoiseAmp, 
float blockNoiseScale, 
float blockNoiseAmp, 
float blockCutoff, 
float YStart, 
float YHeight, 
float XOffset,
float horSize,
// TRENCH
float trenchBaseWiden, 
float trenchBaseWidth, 
float trenchNoiseScale, 
float trenchEdgeAmp,
// OTHER
float seed, 
out float4 Color)
{
    Color = WorldGenFull(uv, caveNoiseScale, caveAmp, caveCutoff, edgeNoiseScale, edgeNoiseAmp, blockNoiseScale, blockNoiseAmp, 
    blockCutoff, YStart, YHeight, XOffset,horSize, trenchBaseWiden, trenchBaseWidth, trenchNoiseScale, trenchEdgeAmp, seed);
    //float gen = WorldGenMaskOnly(uv, caveNoiseScale, caveAmp, caveCutoff, edgeNoiseScale, edgeNoiseAmp, blockNoiseScale, blockNoiseAmp,
    //                              blockCutoff, YStart, YHeight, horSize, trenchBaseWiden, trenchBaseWidth, edgeNoiseScale, edgeNoiseAmp, seed);
    //Color = float4(gen, gen, gen, 1);
}
// Used as a custom node
void WorldGenMask_float(
float2 uv,
// CAVES
float caveNoiseScale,
float caveAmp,
float caveCutoff,
// BIOME
float edgeNoiseScale,
float edgeNoiseAmp,
float blockNoiseScale,
float blockNoiseAmp,
float blockCutoff,
float YStart,
float YHeight,
float XOffset,
float horSize,
// TRENCH
float trenchBaseWiden,
float trenchBaseWidth,
float trenchNoiseScale,
float trenchEdgeAmp,
// OTHER
float parallax,
float seed,
out float Mask)
{
    // Also this now is FUCKED because the background need unique seeds for the trench, but not the other world gen stuff THIS IS SOMETHING FOR LATER
    // BECAUSE I CAN'T BE FUCKED AND LUCY SAID IM NOT ALLOWED TO DO ANY SHADER CODING TODAY!!
    Mask = WorldGenMaskOnly(uv, caveNoiseScale, caveAmp, caveCutoff, edgeNoiseScale, edgeNoiseAmp, blockNoiseScale, blockNoiseAmp,
                                  blockCutoff, YStart, YHeight, XOffset,horSize, trenchBaseWiden, trenchBaseWidth, edgeNoiseScale, edgeNoiseAmp, parallax, seed);
}
float4 WorldGenFullBiomes (
    float2 uv,
    // CAVES
    float caveNoiseScale,
    float caveAmp,
    float caveCutoff,
    // TRENCH
    float trenchBaseWiden,
    float trenchBaseWidth,
    float trenchNoiseScale,
    float trenchEdgeAmp,
    // OTHER
    float seed
)
{
    float uniqueSeed = unity_hash(seed);
    // Start the world as solid base color
    float4 Color = float4(1, 0, 0, 0) / 255.0; // default fallback

    // 1) CAVES (cut holes everywhere)
    float caveNoise = step(Unity_SimpleNoise_float(float2(uv.x, uv.y + uniqueSeed * 4000.0), caveNoiseScale) * caveAmp, caveCutoff);
    bool isCave = (caveNoise < 0.5);
    if (isCave)
    {
        // Cave color / empty space
        return float4(0, 1, 1, 1); // return early for caves (transparent/air)
    }

    // 2) Decide which biome applies at this X (use a low-frequency noise to partition X)
    float selNoise = Unity_SimpleNoise_float(float2(uv.x * BIOME_PLACEMENT_SCALE, uniqueSeed * 1000.0), 1.0);
    selNoise = saturate(selNoise); // 0..1
    int biomeIndex = min(NUM_BIOMES - 1, (int) floor(selNoise * (float) NUM_BIOMES));
    // Create biome params from seed + index:
    BiomeParams biome = MakeBiomeFromSeed(uniqueSeed, biomeIndex);

    // 3) Biome horizontal/vertical membership with edge noise (per-biome params)
    float edgeNoiseX = (Unity_SimpleNoise_float(float2(uv.x, uv.y + uniqueSeed * 5000.0 + (float) biomeIndex * 11.0), biome.edgeNoiseScale) - 0.5) * 2.0;
    float edgeNoiseY = (Unity_SimpleNoise_float(float2(uv.x, uv.y + uniqueSeed * 2000.0 + (float) biomeIndex * 7.0), biome.edgeNoiseScale) - 0.5) * 2.0;
    float width = max(0.0, biome.horSize + edgeNoiseX * biome.edgeNoiseAmp);
    float heightTop = biome.YStart + biome.YHeight + edgeNoiseY * biome.edgeNoiseAmp;
    float heightBottom = biome.YStart + edgeNoiseY * biome.edgeNoiseAmp;

    bool isInBiomeRegion = (width > abs(uv.x - biome.XOffset)) && (uv.y >= heightBottom && uv.y < heightTop);

    if (isInBiomeRegion)
    {
        // block vs air within the biome
        float blockNoise = step(Unity_SimpleNoise_float(float2(uv.x, uv.y + uniqueSeed * (5000.0 + (float) biomeIndex * 3.0)), biome.blockNoiseScale) * biome.blockNoiseAmp, biome.blockCutoff);
        if (blockNoise < 0.5)
        {
            // biome tile
            Color = biome.tileColor;
        }
        else
        {
            // biome air
            Color = biome.airColor;
        }
    }
    else
    {
        // Not in any biome region -> keep original default or assign global surface color
        // example fallback:
        Color = float4(0.8, 0.8, 0.75, 1.0); // plain ground
    }

    // 4) Trench: only apply under the surface of the current biome (prevent surface recolor)
    float trenchMask = GenerateTrenchAndSurface(uv, trenchBaseWiden, trenchBaseWidth, trenchNoiseScale, trenchEdgeAmp, 0.0, false, uniqueSeed);
    // trenchMask == 0.0 means inside trench (per your original function)
    if (trenchMask < 0.5)
    {
        // Only let trench overwrite the biome if this position is below the biome's surface
        if (uv.y < biome.YStart)
        {
            // You can pick a trench color per-biome if desired; for now we use a neutral trench color
            Color = float4(255, 254.0, 255, 255) / 255.0;
        }
    }

    return Color;
}
void WorldGenFullBiomes_float(
    float2 uv,
    // CAVES
    float caveNoiseScale,
    float caveAmp,
    float caveCutoff,
    // TRENCH
    float trenchBaseWiden,
    float trenchBaseWidth,
    float trenchNoiseScale,
    float trenchEdgeAmp,
    // OTHER
    float seed,
    out float4 Color
) 
{
    Color = WorldGenFullBiomes(uv, caveNoiseScale, caveAmp, caveCutoff, trenchBaseWiden, trenchBaseWidth, trenchNoiseScale, trenchEdgeAmp, seed);
}

void CustomVoronoi_Edge_Procedural_float(
    // INPUTS
    float2 UV,
    float AngleOffset,
    float CellDensity,
    // CAVES
    float caveNoiseScale,
    float caveAmp,
    float caveCutoff,
    // BIOME
    float edgeNoiseScale,
    float edgeNoiseAmp,
    float blockNoiseScale,
    float blockNoiseAmp,
    float blockCutoff,
    float YStart,
    float YHeight,
    float XOffset,
    float horSize,
    // TRENCH
    float baseWiden,
    float baseWidth,
    float noiseFreq,
    float edgeAmp,
    // OTHER
    float parallax, // needed for correct calculation of maxdepth
    float seedWorld,
    float seedBackground,
    // OUTPUTS
    out float DistFromCenter,
    out float TrenchMask,
    out float2 EdgeDirection,
    out float2 ClosestPoint)
{
    int2 cell = floor(UV * CellDensity);
    float2 posInCell = frac(UV * CellDensity);
    const float epsilon2 = 0.002;
    // Initialize distances for our prioritized search
    float edgeDist = 8.0f;
    float2 edgeClosestOffset;
    float insideDist = 8.0f;
    float2 insideClosestOffset;
    float absoluteDist = 8.0f;
    float2 absoluteClosestOffset;
    float2 chosenEdgeDirection = float2(0, 0);
    float uniqueSeedWorld = unity_hash(seedWorld);
    float uniqueSeedBackground = unity_hash(seedWorld+seedBackground);
    // The standard 3x3 cell search loop
    for (int y = -1; y <= 1; ++y)
    {
        for (int x = -1; x <= 1; ++x)
        {
            int2 cellToCheck = int2(x, y);
            float2 randomPointOffset = randomVector(cell + cellToCheck, AngleOffset);
            float2 totalOffset = float2(cellToCheck) - posInCell + randomPointOffset;
            float distToPoint = dot(totalOffset, totalOffset);
            
            // Calculate the world-space UV of the random point itself to test against the mask
            float2 pointWorldUV = (cell + cellToCheck + randomPointOffset) / CellDensity;

            // --- Prioritized Logic ---

            // --- Step 1: Minimum calculation. Is the point inside the mask at all?
            float centerMaskValue = GenerateTrenchAndSurface(pointWorldUV, baseWiden, baseWidth, noiseFreq, edgeAmp, parallax,true, uniqueSeedBackground);
            bool isInside = centerMaskValue > 0.5;
            float2 currentEdgeDir;
            if (edgeDist < 0.1)
                break;
            // --- Step 2: If it is inside, check if it's an edge and get its direction.
            if (isInside)
            {
                // Check neighbors
                //float top = GenerateMaskValue(pointWorldUV + float2(0, epsilon2), baseWiden, baseWidth, noiseFreq, edgeAmp, epsilon, seed);
                //float bottom = GenerateMaskValue(pointWorldUV - float2(0, epsilon2), baseWiden, baseWidth, noiseFreq, edgeAmp, epsilon, seed);
                float right = GenerateTrenchAndSurface(pointWorldUV + float2(epsilon2, 0), baseWiden, baseWidth, noiseFreq, edgeAmp, parallax,true, uniqueSeedBackground);
                float left = GenerateTrenchAndSurface(pointWorldUV - float2(epsilon2, 0), baseWiden, baseWidth, noiseFreq, edgeAmp, parallax, true, uniqueSeedBackground);

                bool isEdge = (left < 0.5 || right < 0.5);

                if (isEdge)
                {
                    // It's an edge point. Update the highest priority tier.
                    if (distToPoint < edgeDist)
                    {
                        edgeDist = distToPoint;
                        edgeClosestOffset = totalOffset;
                        // Calculate direction and store it
                        //float gradX = right - left;
                        //float gradY = top - bottom;
                        //chosenEdgeDirection = -normalize(float2(gradX, gradY));
                    }
                }

                // It's an inside point (could be an edge or not). Update the second priority tier.
                if (distToPoint < insideDist)
                {
                    insideDist = distToPoint;
                    insideClosestOffset = totalOffset;
                }
            }
            // Tier 3: Always check for the absolute closest point as a failsafe
            if (distToPoint < absoluteDist)
            {
                absoluteDist = 1;
                absoluteClosestOffset = float2(0, 0);
                //totalOffset;
            }
        }
    }

    // --- Final Selection ---
    if (edgeDist < 8.0f)
    {
        DistFromCenter = edgeDist;
        ClosestPoint = UV + edgeClosestOffset / CellDensity;
        EdgeDirection = chosenEdgeDirection;
    }
    else if (insideDist < 8.0f)
    {
        DistFromCenter = insideDist;
        ClosestPoint = UV + insideClosestOffset / CellDensity;
        EdgeDirection = chosenEdgeDirection; // Not on an edge, so output a zero vector
    }
    else
    {
        //DistFromCenter = absoluteDist;
        //ClosestPoint = UV + absoluteClosestOffset / CellDensity;
        DistFromCenter = 0;
        ClosestPoint = float2(0, 0);
        EdgeDirection = float2(0, 0);
    }
    if (parallax > 0.3)
    {
        // Kind of uggly to test it like this, but we only folllow the world seed with the two first layers, then it is just random, otherwise it becomes very trippy and looks
        // Too unrealistic
        uniqueSeedWorld = uniqueSeedBackground;
    }
    
    // OKAY FOR FUTURE RIK: 
    /*
    Right now the WorldGenMaskOnly does the whole of the world gen, I tried putting it instead of GenerateTrenchAndSurface above but then it runs at like 30 fps. Which is not good
    We could still do it with just the trench I supose, but right now using WorldGenMaskOnly for the TrenchMask variable doesn't work. Then thing is, we need to do it once for the 
    biomes and caves, and another time for the trench. We want the uniqueTrenchSeed for the trench, and the worldseed for the other things. This way, the trench is unique for each 
    layer, while the world background stays similar to how the actual world generation works, which allows us to have this extra layer that follows the world generation nicely.
    I like the unique look, but it will be quite complicated ( and comptitationally heavy ) to ALSO have plants spawn on those different layers aswel, the  ones for the world that is.
    There might be another way to place plants, or other object, in the background  like that, but I have no clue. For now, we just have the background without plants 
    
    */
    TrenchMask = WorldGenMaskOnly(UV, caveNoiseScale, caveAmp, caveCutoff, edgeNoiseScale, edgeNoiseAmp, blockNoiseScale, blockNoiseAmp, 
                                  blockCutoff, YStart, YHeight, XOffset ,horSize, baseWiden, baseWidth, edgeNoiseScale, edgeNoiseAmp, parallax, uniqueSeedWorld);
}
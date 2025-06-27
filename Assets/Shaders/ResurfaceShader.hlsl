
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
inline float3 Unity_ColorspaceConversion_RGB_Linear(float3 In)
{
    float3 linearRGBLo = In / 12.92;;
    float3 linearRGBHi = pow(max(abs((In + 0.055) / 1.055), 1.192092896e-07), float3(2.4, 2.4, 2.4));
    return float3(In <= 0.04045) ? linearRGBLo : linearRGBHi;
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
// =================================================================================
// SECTION 1: YOUR CUSTOMIZABLE MASK LOGIC
// =================================================================================
//
float GenerateMaskValue(float2 uv, float baseWiden, float baseWidth, float noiseFreq, float edgeAmp, float epsilon,float seed)
{

    float halfTrenchWidth = (baseWidth + abs(uv.y) * baseWiden) / 2.0;
    float edgeNoise = (Unity_SimpleNoise_float(float2(uv.x, uv.y + seed), noiseFreq) - 0.5) * 2.0;
    float noisyHalfWidth = max(0.0, halfTrenchWidth + edgeNoise * edgeAmp);
    
    
    float maxDepth = abs( -1 * baseWidth / baseWiden) * 0.9;
    float surfaceNoise = ((Unity_SimpleNoise_float(float2(uv.x, uv.y + 2000.0), 1.32) - 0.5) * 2.0) * 3.0;
    if (uv.y > surfaceNoise)
        return 0;
    if (abs(uv.y) > maxDepth)
        return 1;
    // Distance to trench edge
    if (epsilon < 0) {
        // Don't use epsilon
        bool mask2 = (abs(uv.x) < noisyHalfWidth);
        return mask2 ? 0.0 : 1.0;
    }
    float distanceToEdge = abs(abs(uv.x) - noisyHalfWidth);
    //MaskYes = 0.0;
    bool mask = distanceToEdge<epsilon;
    return mask ? 1.0 : 0.0;
}
void BiomeNear_float(float2 uv, float edgeNoiseScale, float edgeNoiseAmp, float blockNoiseScale, float blockNoiseAmp, float blockCutoff, float YStart, float YHeight, float horSize, float seed, out
float4 Color)
{
    float edgeNoiseX = (Unity_SimpleNoise_float(float2(uv.x, uv.y + seed * 5000), edgeNoiseScale) - 0.5) * 2.0;
    float edgeNoiseY = (Unity_SimpleNoise_float(float2(uv.x, uv.y + seed * 2000), edgeNoiseScale) - 0.5) * 2.0;
    float width = max(0.0, horSize + edgeNoiseX * edgeNoiseAmp);
    
    float heightTop = YStart + YHeight + edgeNoiseY * edgeNoiseAmp;
    float heightBottom = YStart + edgeNoiseY * edgeNoiseAmp;
    if ((width > abs(uv.x)) && (uv.y >= heightBottom && uv.y < heightTop))
    {
        // Inside the biome 
        float biomeBlocks = step(Unity_SimpleNoise_float(float2(uv.x, uv.y + seed * 5000), blockNoiseScale) * blockNoiseAmp, blockCutoff);
        if (biomeBlocks < 0.5)
        {
            // biome tile
            Color = float4(90,253, 1, 1) /255;
        }
        else
        {
            Color = float4(255, 253, 255, 255) /255;
            // biome air tile
        }
    }
    else
    {
        Color = float4(0, 0, 0, 1); // black
    }
}

// Procedural edge detection. It calls GenerateMaskValue() for the point and its neighbors.
bool IsOnEdgeProcedural(float2 uv, float baseWiden, float baseWidth, float noiseFreq, float edgeAmp, float epsilon2,float seed, out float2 edgeDirection)
{
    // The point itself must be inside the mask.
    if (GenerateMaskValue(uv, baseWiden, baseWidth, noiseFreq, edgeAmp, epsilon2, seed) < 0.5)
    {
        return false;
    }

    // Define a small offset to check "neighboring" positions.
    // This is the procedural equivalent of looking at the next texel.
    const float epsilon = 0.002;

    // Check the four neighbors by re-running the mask logic at offset positions.
    float top = GenerateMaskValue(uv + float2(0, epsilon), baseWiden, baseWidth, noiseFreq, edgeAmp, epsilon2, seed);
    float bottom = GenerateMaskValue(uv - float2(0, epsilon), baseWiden, baseWidth, noiseFreq, edgeAmp, epsilon2, seed);
    float right = GenerateMaskValue(uv + float2(epsilon, 0), baseWiden, baseWidth, noiseFreq, edgeAmp, epsilon2, seed);
    float left = GenerateMaskValue(uv - float2(epsilon, 0), baseWiden, baseWidth, noiseFreq, edgeAmp, epsilon2, seed);

    // An edge exists if any neighbor is outside the mask.
    bool isEdge = (top < 0.5 || bottom < 0.5 || left < 0.5 || right < 0.5);

    if (isEdge)
    {
        // Calculate the gradient. It points towards higher values (inward).
        float gradX = right - left;
        float gradY = top - bottom;
        float2 gradient = float2(gradX, gradY);
        
        // We want the direction pointing AWAY from the mask, so we negate the gradient.
        // We also normalize it to get a clean direction vector.
        // We add a tiny value in length() to prevent division by zero.
        edgeDirection = -normalize(gradient);
        return true;
    }

    edgeDirection = float2(0, 0);
    return false;
    return false;
}
void WorldGenTrench_float(float2 UV, float baseWiden, float baseWidth, float noiseFreq, float edgeAmp, float seed, out bool IsTrench)
{
    if (GenerateMaskValue(UV, baseWiden, baseWidth, noiseFreq, edgeAmp, -1, seed) < 0.5) {
        // Trench
        IsTrench = true;// = float4(255, 254, 255, 255) / 255;
    } else {
        IsTrench = false; //= float4(255, 254, 255, 255) / 255;
    }
}

void CustomVoronoi_Edge_Procedural_float(
    // INPUTS
    float2 UV,
    float AngleOffset,
    float CellDensity,
    float baseWiden,
    float baseWidth,
    float noiseFreq,
    float edgeAmp,
    float epsilon,
    float seed,
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
            float centerMaskValue = GenerateMaskValue(pointWorldUV, baseWiden, baseWidth, noiseFreq, edgeAmp, epsilon,seed);
            bool isInside = centerMaskValue > 0.5;
            float2 currentEdgeDir;
            
                 // --- Step 2: If it is inside, check if it's an edge and get its direction.
            if (isInside)
            {
                // Check neighbors
                float top = GenerateMaskValue(pointWorldUV + float2(0, epsilon2), baseWiden, baseWidth, noiseFreq, edgeAmp, epsilon, seed);
                float bottom = GenerateMaskValue(pointWorldUV - float2(0, epsilon2), baseWiden, baseWidth, noiseFreq, edgeAmp, epsilon, seed);
                float right = GenerateMaskValue(pointWorldUV + float2(epsilon2, 0), baseWiden, baseWidth, noiseFreq, edgeAmp, epsilon, seed);
                float left = GenerateMaskValue(pointWorldUV - float2(epsilon2, 0), baseWiden, baseWidth, noiseFreq, edgeAmp, epsilon, seed);

                bool isEdge = (top < 0.5 || bottom < 0.5 || left < 0.5 || right < 0.5);

                if (isEdge)
                {
                    // It's an edge point. Update the highest priority tier.
                    if (distToPoint < edgeDist)
                    {
                        edgeDist = distToPoint;
                        edgeClosestOffset = totalOffset;
                        // Calculate direction and store it
                        float gradX = right - left;
                        float gradY = top - bottom;
                        chosenEdgeDirection = -normalize(float2(gradX, gradY));
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
    TrenchMask = GenerateMaskValue(UV, baseWiden, baseWidth, noiseFreq, edgeAmp, -1, seed);

}
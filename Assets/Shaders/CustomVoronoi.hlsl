
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
float GenerateMaskValue(float2 uv, float baseWiden, float baseWidth, float noiseFreq, float edgeAmp, float epsilon)
{

    float halfTrenchWidth = (baseWidth + abs(uv.y) * baseWiden) / 2.0;
    float edgeNoise = (Unity_SimpleNoise_float(float2(uv.x, uv.y + 5000.0), noiseFreq) - 0.5) * 2.0;
    float noisyHalfWidth = max(0.0, halfTrenchWidth + edgeNoise * edgeAmp);
    
    // TODO calculate max depth
    
    // Distance to trench edge
    float distanceToEdge = abs(abs(uv.x) - noisyHalfWidth);
    //MaskYes = 0.0;
    bool mask = distanceToEdge<epsilon;
    bool mask2 = abs(uv.x) < noisyHalfWidth;
    //return mask2 ? 1.0 : 0.0;
    return mask ? 1.0 : 0.0;
}
float GenerateMaskValue2(float2 uv, float baseWiden, float baseWidth, float noiseFreq, float edgeAmp)
{

    float halfTrenchWidth = (baseWidth + abs(uv.y) * baseWiden) / 2.0;
    float edgeNoise = (Unity_SimpleNoise_float(float2(uv.x, uv.y + 5000.0), noiseFreq) - 0.5) * 2.0;
    float noisyHalfWidth = max(0.0, halfTrenchWidth + edgeNoise * edgeAmp);
    
    // TODO calculate max depth
    float surfaceNoise = ((Unity_SimpleNoise_float(float2(uv.x, uv.y + 2000.0), 1.32) - 0.5) * 2.0) * 3.0;
    if (uv.y > surfaceNoise)
        return 0;
    bool mask = (abs(uv.x) < noisyHalfWidth);
    //return mask2 ? 1.0 : 0.0;
    return mask ? 0.0 : 1.0;
}
// Procedural edge detection. It calls GenerateMaskValue() for the point and its neighbors.
bool IsOnEdgeProcedural(float2 uv, float baseWiden, float baseWidth, float noiseFreq, float edgeAmp, float epsilon2, out float2 edgeDirection)
{
    // The point itself must be inside the mask.
    if (GenerateMaskValue(uv, baseWiden, baseWidth, noiseFreq, edgeAmp, epsilon2) < 0.5)
    {
        return false;
    }

    // Define a small offset to check "neighboring" positions.
    // This is the procedural equivalent of looking at the next texel.
    const float epsilon = 0.002;

    // Check the four neighbors by re-running the mask logic at offset positions.
    float top = GenerateMaskValue(uv + float2(0, epsilon), baseWiden, baseWidth, noiseFreq, edgeAmp, epsilon2);
    float bottom = GenerateMaskValue(uv - float2(0, epsilon), baseWiden, baseWidth, noiseFreq, edgeAmp, epsilon2);
    float right = GenerateMaskValue(uv + float2(epsilon, 0), baseWiden, baseWidth, noiseFreq, edgeAmp, epsilon2);
    float left = GenerateMaskValue(uv - float2(epsilon, 0), baseWiden, baseWidth, noiseFreq, edgeAmp, epsilon2);

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
            float centerMaskValue = GenerateMaskValue(pointWorldUV, baseWiden, baseWidth, noiseFreq, edgeAmp, epsilon);
            bool isInside = centerMaskValue > 0.5;
            float2 currentEdgeDir;
            
                 // --- Step 2: If it is inside, check if it's an edge and get its direction.
            if (isInside)
            {
                // Check neighbors
                float top = GenerateMaskValue(pointWorldUV + float2(0, epsilon2), baseWiden, baseWidth, noiseFreq, edgeAmp, epsilon);
                float bottom = GenerateMaskValue(pointWorldUV - float2(0, epsilon2), baseWiden, baseWidth, noiseFreq, edgeAmp, epsilon);
                float right = GenerateMaskValue(pointWorldUV + float2(epsilon2, 0), baseWiden, baseWidth, noiseFreq, edgeAmp, epsilon);
                float left = GenerateMaskValue(pointWorldUV - float2(epsilon2, 0), baseWiden, baseWidth, noiseFreq, edgeAmp, epsilon);

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
    TrenchMask = GenerateMaskValue2(UV, baseWiden, baseWidth, noiseFreq, edgeAmp);

}
void CustomVoronoi_Edge_Procedural_Fast_float(
     // INPUTS
    float2 UV,
    float AngleOffset,
    float CellDensity,
    float baseWiden,
    float baseWidth,
    float noiseFreq,
    float edgeAmp,
    float epsilon,
    // OUTPUTS
    out float DistFromCenter,
    out float TrenchMask,
    out float2 EdgeDirection,
    out float2 ClosestPoint)
{
    int2 cell = floor(UV * CellDensity);
    float2 posInCell = frac(UV * CellDensity);

    float closestDistSq = 8.0f;
    float2 closestOffset;

    // *** STEP 1: FAST, 'DUMB' VORONOI SEARCH ***
    // This loop does no mask checks at all. It just finds the single
    // closest random point, making it extremely fast.
    for (int y = -1; y <= 1; ++y)
    {
        for (int x = -1; x <= 1; ++x)
        {
            int2 cellToCheck = int2(x, y);
            float2 randomPointOffset = randomVector(cell + cellToCheck, AngleOffset);
            float2 totalOffset = float2(cellToCheck) - posInCell + randomPointOffset;
            float distSq = dot(totalOffset, totalOffset);

            if (distSq < closestDistSq)
            {
                closestDistSq = distSq;
                closestOffset = totalOffset;
            }
        }
    }

    // --- We now have the single closest point and its distance ---
    DistFromCenter = closestDistSq; // This is still the squared distance
    ClosestPoint = UV + closestOffset / CellDensity;
    EdgeDirection = float2(0, 0); // Default to no direction

    // *** STEP 2: VALIDATE THE CHOSEN POINT ***
    // Now we do a small, fixed number of mask checks ONLY on the point we found.
    float centerMaskValue = GenerateMaskValue(ClosestPoint, baseWiden, baseWidth, noiseFreq, edgeAmp, epsilon);
    
    // If the closest point is OUTSIDE the mask, we are done. This will create a 'hole'.
    // We return the point anyway but it can be culled later by checking if EdgeDirection is zero.
    if (centerMaskValue < 0.5)
    {
        // To signify this is an invalid point, you can set DistFromCenter to a magic number
        // your graph can check, e.g., DistFromCenter = -1. For now, we leave it.
        return;
    }
    TrenchMask = GenerateMaskValue2(UV, baseWiden, baseWidth, noiseFreq, edgeAmp);

    // --- The point is inside the mask. Now check if it's an edge. ---
    const float epsilon2 = 0.002;
    float top = GenerateMaskValue(ClosestPoint + float2(0, epsilon2), baseWiden, baseWidth, noiseFreq, edgeAmp, epsilon);
    float bottom = GenerateMaskValue(ClosestPoint - float2(0, epsilon2), baseWiden, baseWidth, noiseFreq, edgeAmp, epsilon);
    float right = GenerateMaskValue(ClosestPoint + float2(epsilon2, 0), baseWiden, baseWidth, noiseFreq, edgeAmp, epsilon);
    float left = GenerateMaskValue(ClosestPoint - float2(epsilon2, 0), baseWiden, baseWidth, noiseFreq, edgeAmp, epsilon);
    
    bool isEdge = (top < 0.5 || bottom < 0.5 || left < 0.5 || right < 0.5);

    if (isEdge)
    {
        float gradX = right - left;
        float gradY = top - bottom;
        EdgeDirection = -normalize(float2(gradX, gradY));
    }
}

// Based on code by Inigo Quilez: https://iquilezles.org/articles/voronoilines/
void CustomVoronoi_float(float2 UV, float AngleOffset, float CellDensity, out float DistFromCenter, out float DistFromEdge, out float2 ClosestPoint)
{
    int2 cell = floor(UV * CellDensity);
    float2 posInCell = frac(UV * CellDensity);

    DistFromCenter = 8.0f;
    float2 closestOffset;

    for (int y = -1; y <= 1; ++y)
    {
        for (int x = -1; x <= 1; ++x)
        {
            int2 cellToCheck = int2(x, y);
            float2 cellOffset = float2(cellToCheck) - posInCell + randomVector(cell + cellToCheck, AngleOffset);
            float distToPoint = dot(cellOffset, cellOffset);

            if (distToPoint < DistFromCenter)
            {
                DistFromCenter = distToPoint;
                closestOffset = cellOffset;
            }
        }
    }

    ClosestPoint = UV + closestOffset / CellDensity;

    DistFromEdge = 8.0f;

    for (int y = -1; y <= 1; ++y)
    {
        for (int x = -1; x <= 1; ++x)
        {
            int2 cellToCheck = int2(x, y);
            float2 cellOffset = float2(cellToCheck) - posInCell + randomVector(cell + cellToCheck, AngleOffset);
            float distToEdge = dot(0.5f * (closestOffset + cellOffset), normalize(cellOffset - closestOffset));
            DistFromEdge = min(DistFromEdge, distToEdge);
        }
    }
}
void CustomVoronoiC_float(float2 UV, float AngleOffset, float CellDensity, float epsilon, float noisyHalfWidth, out float DistFromCenter, out float2 ClosestPoint)
{
    int2 cell = floor(UV * CellDensity);
    float2 posInCell = frac(UV * CellDensity);

    DistFromCenter = 8.0f; // Large initial value
    float2 closestOffset;
    bool foundValidPoint = false;

    // Loop over neighboring cells
     
    float closestDist = 8.0; // Large initial distance
    float2 closestSeedPos = float2(0, 0);
    
    // Check neighboring cells (e.g., 3x3 grid)
    for (int y = -1; y <= 1; y++)
    {
        for (int x = -1; x <= 1; x++)
        {
            int2 cellToCheck = int2(x, y);
            float2 seedOffset = randomVector(cell + cellToCheck, AngleOffset);
            float2 seedPos = (float2(cell + cellToCheck) + seedOffset) / CellDensity;
            
            // Filter seed points near trench edge and within depth
            float distanceToEdge = abs(abs(seedPos.x) - noisyHalfWidth);
            if (abs(seedPos.y) < 5000 && distanceToEdge < epsilon)
            {
                float2 cellOffset = float2(cellToCheck) - posInCell + seedOffset;
                float distToPoint = dot(cellOffset, cellOffset);
                
                if (distToPoint < closestDist)
                {
                    closestDist = distToPoint;
                    closestSeedPos = seedPos; // Store exact seed position
                    foundValidPoint = true;
                }
            }
        }
    }
    
    if (foundValidPoint)
    {
        ClosestPoint = closestSeedPos;
        DistFromCenter = closestDist; // Squared distance, scaled by CellDensity^2
        //FoundValid = 1.0;
    }
    else
    {
        ClosestPoint = float2(0, 0);
        DistFromCenter = 8.0;
        //FoundValid = 0.0;
    }
}
void CustomVoronoiFalidOnly_float(float2 UV, float AngleOffset, float CellDensity, float minX, float maxX, out float DistFromCenter, out float DistFromEdge, out float2 ClosestPoint)
{
    int2 cell = floor(UV * CellDensity);
    float2 posInCell = frac(UV * CellDensity);

    DistFromCenter = 8.0f;
    float2 closestOffset;

    for (int y = -1; y <= 1; ++y)
    {
        for (int x = -1; x <= 1; ++x)
        {
            int2 cellToCheck = int2(x, y);
            float2 cellOffset = float2(cellToCheck) - posInCell + randomVector(cell + cellToCheck, AngleOffset);
            float distToPoint = dot(cellOffset, cellOffset);

            if (distToPoint < DistFromCenter)
            {
                DistFromCenter = distToPoint;
                closestOffset = cellOffset;
            }
        }
    }

    float2 candidate = UV + closestOffset / CellDensity;

    // Check if the closest point is within the X-value range
    bool inRange = (candidate.x >= minX) && (candidate.x <= maxX);
    ClosestPoint = inRange ? candidate : float2(0.0, 0.0);

    DistFromEdge = 8.0f;

    for (int y = -1; y <= 1; ++y)
    {
        for (int x = -1; x <= 1; ++x)
        {
            int2 cellToCheck = int2(x, y);
            float2 cellOffset = float2(cellToCheck) - posInCell + randomVector(cell + cellToCheck, AngleOffset);
            float distToEdge = dot(0.5f * (closestOffset + cellOffset), normalize(cellOffset - closestOffset));
            DistFromEdge = min(DistFromEdge, distToEdge);
        }
    }
} 

// New helper function to detect if a point is on the mask's edge
// It checks a point's mask value against its immediate neighbors.
// A point is on an edge if it's '1' and at least one neighbor is '0'.
bool IsOnEdge(float2 uv, Texture2D mask, SamplerState maskSampler)
{
    // Get the dimensions of the mask texture to calculate texel size for accurate neighbor sampling
    float width, height;
    mask.GetDimensions(width, height);
    float2 texelSize = float2(1.0 / width, 1.0 / height);

    // Sample the center point. We assume the mask data is in the red channel.
    float centerValue = mask.Sample(maskSampler, uv).r;

    // If the point itself is not in the valid area (mask is 0), it can't be an edge point.
    if (centerValue < 0.5)
    {
        return false;
    }

    // Sample the four direct neighbors
    float top = mask.Sample(maskSampler, uv + float2(0, texelSize.y)).r;
    float bottom = mask.Sample(maskSampler, uv - float2(0, texelSize.y)).r;
    float right = mask.Sample(maskSampler, uv + float2(texelSize.x, 0)).r;
    float left = mask.Sample(maskSampler, uv - float2(texelSize.x, 0)).r;

    // If any neighbor is outside the mask (value is 0), this is an edge.
    if (top < 0.5 || bottom < 0.5 || left < 0.5 || right < 0.5)
    {
        return true;
    }

    return false;
}

void CustomVoronoi_Edge_float(
    // INPUTS
    float2 UV,
    float AngleOffset,
    float CellDensity,
    Texture2D Mask,
    SamplerState MaskSampler,
    
    // OUTPUTS
    out float DistFromCenter,
    out float2 ClosestPoint)
{
    int2 cell = floor(UV * CellDensity);
    float2 posInCell = frac(UV * CellDensity);

    // Initialize distances for our prioritized search
    // Tier 1: Closest point on an edge
    float edgeDist = 8.0f;
    float2 edgeClosestOffset;

    // Tier 2: Closest point inside the mask (fallback)
    float insideDist = 8.0f;
    float2 insideClosestOffset;
    
    // Tier 3: Absolute closest point (failsafe)
    float absoluteDist = 8.0f;
    float2 absoluteClosestOffset;

    // The standard 3x3 cell search loop
    for (int y = -1; y <= 1; ++y)
    {
        for (int x = -1; x <= 1; ++x)
        {
            int2 cellToCheck = int2(x, y);
            // Get the random point's position within its cell
            float2 randomPointOffset = randomVector(cell + cellToCheck, AngleOffset);
            
            // Calculate the total offset from the current pixel (UV) to this random point
            float2 totalOffset = float2(cellToCheck) - posInCell + randomPointOffset;
            float distToPoint = dot(totalOffset, totalOffset);
            
            // Calculate the world-space UV of the random point itself to sample the mask
            float2 pointWorldUV = (cell + cellToCheck + randomPointOffset) / CellDensity;

            // --- Prioritized Logic ---

            // Tier 1: Check if the point is on an edge
            if (IsOnEdge(pointWorldUV, Mask, MaskSampler))
            {
                if (distToPoint < edgeDist)
                {
                    edgeDist = distToPoint;
                    edgeClosestOffset = totalOffset;
                }
            }

            // Tier 2: Check if the point is inside the mask
            // We use SampleLevel to prevent using derivatives (like ddx/ddy) inside a loop
            if (Mask.SampleLevel(MaskSampler, pointWorldUV, 0).r > 0.5)
            {
                if (distToPoint < insideDist)
                {
                    insideDist = distToPoint;
                    insideClosestOffset = totalOffset;
                }
            }
            
            // Tier 3: Always check for the absolute closest point as a failsafe
            if (distToPoint < absoluteDist)
            {
                absoluteDist = distToPoint;
                absoluteClosestOffset = totalOffset;
            }
        }
    }

    // --- Final Selection ---
    // Select the best result based on our priority system

    if (edgeDist < 8.0f) // Priority 1: An edge point was found
    {
        DistFromCenter = edgeDist;
        ClosestPoint = UV + edgeClosestOffset / CellDensity;
    }
    else if (insideDist < 8.0f) // Priority 2: No edge point, but an inside point was found
    {
        DistFromCenter = insideDist;
        ClosestPoint = UV + insideClosestOffset / CellDensity;
    }
    else // Priority 3: Failsafe, no valid points found, use the absolute closest
    {
        DistFromCenter = absoluteDist;
        ClosestPoint = UV + absoluteClosestOffset / CellDensity;
    }
}
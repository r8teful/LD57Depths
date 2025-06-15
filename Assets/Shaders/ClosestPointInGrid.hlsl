#ifndef FIND_CLOSEST_POINT_NODE
#define FIND_CLOSEST_POINT_NODE

// A standard, simple hashing function to turn an integer grid coordinate
// into a pseudo-random Vector2 offset.
float2 hash2D(float2 p)
{
    // Use strange, prime-like numbers to create chaos.
    // The frac() function keeps the result between 0 and 1.
    float x = frac(sin(dot(p, float2(12.9898, 78.233))) * 43758.5453);
    float y = frac(sin(dot(p, float2(34.876, 62.345))) * 53758.3453);
    return float2(x, y);
}


// Main function for the Shader Graph node.
// It finds the closest point by checking a 3x3 grid of cells.
void FindClosestPointInGrid_float(float2 PixelPos, out float2 ClosestPointPos, out float MinDistSquared, out float2 ClosestCellID)
{
    // 1. Get the integer ID of the cell the current pixel is in.
    float2 currentCellID = floor(PixelPos);
    
    // 2. Initialize tracking variables with a large distance.
    MinDistSquared = 100.0;
    ClosestPointPos = float2(0.0, 0.0);
    ClosestCellID = float2(0.0, 0.0);
    
    // 3. Loop through the 3x3 grid of cells (-1 to +1 on both axes).
    for (int y = -1; y <= 1; y++)
    {
        for (int x = -1; x <= 1; x++)
        {
            // Get the ID of the neighboring cell we are currently checking.
            float2 neighborCellID = currentCellID + float2(x, y);
            
            // Generate the position of the decal point within this neighboring cell.
            // It's the cell's corner (its ID) plus a random offset.
            float2 pointPos = neighborCellID + hash2D(neighborCellID);
            
            // Calculate the squared distance from our pixel to this point.
            // Using squared distance is faster because it avoids a square root.
            float2 vecToPoint = pointPos - PixelPos;
            float distSq = dot(vecToPoint, vecToPoint);
            
            // If this point is closer than any we've found so far, save it.
            if (distSq < MinDistSquared)
            {
                MinDistSquared = distSq;
                ClosestPointPos = pointPos;
                ClosestCellID = neighborCellID;
            }
        }
    }
}

#endif
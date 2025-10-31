#ifndef COMMON_FUNCS_CGINC
#define COMMON_FUNCS_CGINC
// -----------------------------
// Deterministic integer hash (Wang / mix)
// -----------------------------
uint wang_hash(uint x)
{
    x = (x ^ 61u) ^ (x >> 16);
    x *= 9u;
    x = x ^ (x >> 4);
    x *= 0x27d4eb2du;
    x = x ^ (x >> 15);
    return x;
}

// combine integer coords + seed to a 32-bit hash
uint grid_hash(int ix, int iy, uint seed)
{
    // mix coords with large primes then run Wang hash
    uint x = (uint) ix;
    uint y = (uint) iy;
    // prime multipliers are chosen to reduce correlation
    uint h = x * 73856093u ^ y * 19349663u ^ seed * 83492791u;
    return wang_hash(h);
}

// convert 32-bit uint hash to float in [0,1)
float uintToFloat01(uint v)
{
    // dividing by 2^32 to get uniform [0,1)
    return (float) v / 4294967296.0;
}
// -----------------------------
// Small deterministic pseudo-random float per integer lattice point
// returns in [0,1)
// -----------------------------
float cellRandom(int ix, int iy, uint seed)
{
    return uintToFloat01(grid_hash(ix, iy, seed));
}
// -----------------------------
// Perlin-style gradient noise (deterministic)
// -----------------------------
static const float2 GRAD2[8] =
{
    float2(1.0, 0.0),
    float2(-1.0, 0.0),
    float2(0.0, 1.0),
    float2(0.0, -1.0),
    float2(0.70710678, 0.70710678),
    float2(-0.70710678, 0.70710678),
    float2(0.70710678, -0.70710678),
    float2(-0.70710678, -0.70710678)
};

float fade(float t)
{
    return t * t * t * (t * (t * 6.0 - 15.0) + 10.0);
}

float perlin2D(float2 p, uint seed)
{
    int ix = (int) floor(p.x);
    int iy = (int) floor(p.y);
    float fx = p.x - floor(p.x);
    float fy = p.y - floor(p.y);

    // four corners
    uint h00 = grid_hash(ix + 0, iy + 0, seed);
    uint h10 = grid_hash(ix + 1, iy + 0, seed);
    uint h01 = grid_hash(ix + 0, iy + 1, seed);
    uint h11 = grid_hash(ix + 1, iy + 1, seed);

    float2 g00 = GRAD2[h00 & 7u];
    float2 g10 = GRAD2[h10 & 7u];
    float2 g01 = GRAD2[h01 & 7u];
    float2 g11 = GRAD2[h11 & 7u];

    float n00 = dot(g00, float2(fx - 0.0, fy - 0.0));
    float n10 = dot(g10, float2(fx - 1.0, fy - 0.0));
    float n01 = dot(g01, float2(fx - 0.0, fy - 1.0));
    float n11 = dot(g11, float2(fx - 1.0, fy - 1.0));

    float u = fade(fx);
    float v = fade(fy);

    float nx0 = lerp(n00, n10, u);
    float nx1 = lerp(n01, n11, u);
    float nxy = lerp(nx0, nx1, v);

    // scale approximate range to [-1,1]
    return saturate(nxy * 1.41421356) * 2.0 - 1.0;
}

// -----------------------------
// fBM (fractal Brownian motion) — deterministic
// returns in [0,1]
// -----------------------------
float fbm(float2 p, uint seed, int octaves, float lacunarity, float gain)
{
    float amplitude = 1.0;
    float frequency = 1.0;
    float sum = 0.0;
    float maxAmp = 0.0;

    // use different seeds per octave to decorrelate
    for (int i = 0; i < octaves; ++i)
    {
        uint octaveSeed = wang_hash(seed + (uint) i * 0x9E3779B9u); // cheap per-octave variation
        float v = perlin2D(p * frequency, octaveSeed) * 0.5 + 0.5; // map to [0,1]
        sum += v * amplitude;
        maxAmp += amplitude;
        amplitude *= gain;
        frequency *= lacunarity;
    }
    return sum / maxAmp;
}

// -----------------------------
// Ridged fBM (useful for mountains / sharp features)
// -----------------------------
float ridgedFbm(float2 p, uint seed, int octaves, float lacunarity, float gain)
{
    float amplitude = 0.5;
    float frequency = 1.0;
    float sum = 0.0;
    float weight = 1.0;
    for (int i = 0; i < octaves; ++i)
    {
        uint octaveSeed = wang_hash(seed + (uint) i * 0x9E3779B9u);
        float v = perlin2D(p * frequency, octaveSeed);
        v = 1.0 - abs(v); // ridge transform
        v *= v; // sharpen
        v *= weight;
        sum += v * amplitude;

        weight = saturate(v * 2.0); // feedback
        frequency *= lacunarity;
        amplitude *= gain;
    }
    // result in roughly [0,1]
    return saturate(sum);
}
// -----------------------------
// Deterministic 2D Worley (cellular) noise - returns distance in [0, ~1] (approx)
// Use for bubble/cavern spots, pond-like clusters.
// -----------------------------
float worley2D(float2 p, uint seed)
{
    // cell size 1.0 assumed. p in any scale.
    int ix = (int) floor(p.x);
    int iy = (int) floor(p.y);
    float minDist = 10.0;
    // check 3x3 neighborhood
    for (int oy = -1; oy <= 1; ++oy)
    {
        for (int ox = -1; ox <= 1; ++ox)
        {
            int cx = ix + ox;
            int cy = iy + oy;
            // random point within cell (deterministic)
            float rx = cellRandom(cx, cy, seed);
            float ry = cellRandom(cx, cy, seed ^ 0x9e3779b9u);
            float2 featurePos = float2(cx, cy) + float2(rx, ry);
            float d = length(p - featurePos);
            if (d < minDist)
                minDist = d;
        }
    }
    // minDist can be >1 if sparse; normalize by a reasonable factor
    // you can tweak divisor to change scale; keep as-is to get ~0..1
    return saturate(minDist);
}

// -----------------------------
// Domain warp helper: warp p by two fbm channels
// -----------------------------
float2 domainWarp(float2 p, uint seed, float warpAmp, int warpOctaves)
{
    float qx = fbm(p + float2(17.0, -29.0), seed ^ 0xABC123u, warpOctaves, 2.0, 0.5);
    float qy = fbm(p + float2(-43.0, 7.0), seed ^ 0xC0FFEEu, warpOctaves, 2.0, 0.5);
    return p + (float2(qx, qy) - 0.5) * warpAmp;
}

// -----------------------------
// Cave density helpers
// All density functions return a value roughly in [0,1]
// Interpreting density:
//   density >= cutoff -> solid rock
//   density <  cutoff -> cave (empty)
// Provide blending/feather to control sharpness near edges.
// -----------------------------

// Example combined density using fbm + ridged + worley
float CaveDensity_Combined(float2 uv, float globalSeed, int biomeIndex, float scale,
                           int baseOctaves, int ridgeOctaves, float warpAmp, float worleyWeight)
{
    // Build deterministic uint seed from inputs (quantize seed to avoid float jitter)
    uint gSeed = (uint) floor(globalSeed);
    uint biomeSeed = (uint) biomeIndex * 0x9E3779B1u;
    uint seed = wang_hash(gSeed + biomeSeed + 0x13579BDFu);

    float2 p = uv * scale;

    // domain warp helps avoid grid-aligned artifacts
    float2 wp = domainWarp(p, seed, warpAmp, 3);

    // base rolling noise
    float base = fbm(wp * 0.6, seed, baseOctaves, 2.0, 0.5);

    // ridged detail for overhangs and cliffs
    float ridge = ridgedFbm(wp * 1.6, seed ^ 0xDEADBEEFu, ridgeOctaves, 2.0, 0.7);

    // worley to create pockets/lakes inside caverns (inverted influence)
    float w = worley2D(wp * 1.2, seed ^ 0xBEEF1337u);

    // Combine: base * (1 - worleyInfluence) + ridge*ridgeWeight + worley * worleyWeight
    // designers can tweak weights for desired look
    float combined = base * (1.0 - w * worleyWeight) + ridge * 0.45 + (1.0 - w) * 0.25;

    return saturate(combined);
}

// A simpler tunnel-style density (thin winding caves): high-frequency fbm + domain warp + thresholding
float CaveDensity_Tunnels(float2 uv, float globalSeed, int biomeIndex, float scale, int octaves, float warpAmp)
{
    uint gSeed = (uint) floor(globalSeed);
    uint seed = wang_hash(gSeed + (uint) biomeIndex * 0x1337BEEF);

    float2 p = uv * scale;
    float2 wp = domainWarp(p, seed, warpAmp, 2);

    // high frequency, lower octaves for thin tunnels
    float v = fbm(wp * 2.5, seed, octaves, 2.0, 0.45);

    // emphasize narrow passages by sharpening contrast
    //v = smoothstep(0.35, 0.65, v); // acts like a contrast boost
    return v;
}

float EdgeNoise_Smooth(float2 uv, int seed, int biomeIndex, float scale)
{
                // quantize and build an integer seed to avoid tiny float jitter differences
    uint gSeed = (uint) seed;
    uint biomeSeed = (uint) biomeIndex * 0x9E3779B1u;
    uint uniqueSeed = wang_hash(gSeed + biomeSeed);
            
                // scale coordinates to control feature size
    float2 sp = uv * scale;
            
    int ix = (int) floor(sp.x);
    int iy = (int) floor(sp.y);
    float fx = frac(sp.x);
    float fy = frac(sp.y);
            
                // corners: deterministic random per lattice point
    float r00 = cellRandom(ix + 0, iy + 0, uniqueSeed);
    float r10 = cellRandom(ix + 1, iy + 0, uniqueSeed);
    float r01 = cellRandom(ix + 0, iy + 1, uniqueSeed);
    float r11 = cellRandom(ix + 1, iy + 1, uniqueSeed);
            
                // smoothstep interpolation (better than linear; reduces grid artifacts)
    float ux = fx * fx * (3.0 - 2.0 * fx);
    float uy = fy * fy * (3.0 - 2.0 * fy);
            
    float lx0 = lerp(r00, r10, ux);
    float lx1 = lerp(r01, r11, ux);
    float value = lerp(lx0, lx1, uy);
            
    return saturate(value); // ensure within [0,1]
}
#endif // COMMON_FUNCS_CGINC
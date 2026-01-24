using UnityEngine;

[CreateAssetMenu(fileName = "WorldGenOreSO", menuName = "ScriptableObjects/WorldGen/WorldGenOreSO")]
public class WorldGenOreSO : ScriptableObject {
    public TileSO oreTile; // We pull ID and name from this

    [Header("Depth-Based Rarity")]
    public int CircleLayer = 0; //The circle index where this ore is MOST common
    [Range(0.01f, 1)] public float maxChance = 0.2f;

    // Width of the ore band relative to the ring size.
    [Range(0.01f, 1)] public float widthPercent = 0.2f;

    [Header("Noise-Based Clustering")]
    [Tooltip("Higher scale = larger, less frequent ore veins.")]
    public float noiseScale = 0.1f;
    [Tooltip("The noise value threshold (0 to 1). Ore is considered only if noise is above this. Higher = smaller, rarer veins.")]
    [Range(0, 1)] public float noiseThreshold = 0.6f;

    // A unique offset for this ore's noise to prevent all ores from spawning in the same spots.
    public Vector2 noiseOffset;

    // Richness
    public bool useRichness;
    public float richnessNoiseScale;
    public float minRichnessMultiplier, maxRichnessMultiplier;

    // Domain Warp
    public bool useDomainWarp;
    public float warpNoiseScale, warpStrength;
}
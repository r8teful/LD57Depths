using UnityEngine;

[CreateAssetMenu(fileName = "WorldGenOreSO", menuName = "ScriptableObjects/WorldGen/WorldGenOreSO")]
public class WorldGenOreSO : ScriptableObject {
    public TileSO oreTile; // We pull ID and name from this

    [Header("Depth-Based Rarity")]
    // Ores spawn in a semi circle around the center of the world, 
    // 0 is at the bottom, 0.5 would be in the middle of the trench, 1 at the very top
    [Range(0.05f, 0.9f)] public float WorldDepthBandProcent = 0; 
    [Range(0.01f, 1)] public float maxChance = 0.2f;

    // Width of the ore band relative to the ring size.
    [Range(0.01f, 6)] public float widthPercent = 0.2f;

    [Header("Noise-Based Clustering")]
    [Tooltip("Higher scale = larger, less frequent ore veins.")]
    [Range(0.02f, 0.4f)]  public float noiseScale = 0.1f;
    [Tooltip("The noise value threshold (0 to 1). Ore is considered only if noise is above this. Higher = smaller, rarer veins.")]
    [Range(0.1f, 0.9f)] public float noiseThreshold = 0.6f;

    // A unique offset for this ore's noise to prevent all ores from spawning in the same spots.
    public Vector2 noiseOffset;
    public Color DebugColor = Color.white;
}
using UnityEngine;

[CreateAssetMenu(fileName = "WorldGenOreSO", menuName = "ScriptableObjects/WorldGen/WorldGenOreSO")]
public class WorldGenOreSO : ScriptableObject {
    public TileSO oreTile; // We pull ID and name from this
    public ushort replaceableTileID = 1;

    [Header("Depth-Based Rarity")]
    public int LayerStartSpawn = 0; //The biome index where this ore STARTS appearing.
    public int LayerStopCommon = 1;
    public int LayerMostCommon = 0;
    [Tooltip("The chance to spawn (0 to 1) at the 'Spawn Depth'.")]
    [Range(0, 1)] public float minChance = 0.01f; // At the start layer, this is what the chances start at
    [Tooltip("The chance to spawn (0 to 1) at the 'Max Rarity Depth'.")]
    [Range(0, 1)] public float maxChance = 0.2f;

    [Header("Noise-Based Clustering")]
    [Tooltip("Higher scale = larger, less frequent ore veins.")]
    public float noiseScale = 0.1f;
    [Tooltip("The noise value threshold (0 to 1). Ore is considered only if noise is above this. Higher = smaller, rarer veins.")]
    [Range(0, 1)] public float noiseThreshold = 0.6f;

    // A unique offset for this ore's noise to prevent all ores from spawning in the same spots.
    public Vector2 noiseOffset;
}
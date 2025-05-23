// --- Biome Specific Cave Settings ---
using System.Collections.Generic;
using UnityEngine.Tilemaps;
using UnityEngine;

[System.Serializable]
public struct BiomeCaveSettings {
    public bool overrideGlobalCaveSettings; // Use settings below instead of WorldGenerator global ones?

    [Header("Overrides (if above is true)")]
    public bool generateCavesInBiome; //= true;      // Generate caves AT ALL in this biome?

    [Tooltip("Lower frequency = larger cave systems/caverns")]
    [Range(0f, 0.4f)] public float baseCaveFrequency; //= 0.05f; // Frequency for the main cave noise

    [Tooltip("Value BELOW which becomes cave. Lower value = Less cave space overall.")]
    [Range(0f, 1f)] public float caveThreshold;// = 0.4f;

    [Header("Domain Warp Settings")]
    [Tooltip("How strongly the warp noise distorts the cave shapes. 0 = no warp. Higher = more swirling/tunnels.")]
    public float warpAmplitude;// = 15f; // Controls the magnitude of the coordinate offset
    [Tooltip("Frequency of the distortion pattern itself. Often similar to or slightly higher than base cave frequency.")]
    public float warpFrequency;// = 0.06f; // Frequency for the noise generating the offsets

    public bool useDetailNoise; //= false;
    public float detailFrequency;// = 0.2f;
    public float detailInfluence;// = 0.1f; // How much detail noise affects the final value before threshold
}

// --- Biome Specific Ore Settings ---
[System.Serializable]
public struct BiomeOreSettings {
    public bool overrideGlobalOreSettings; // Use settings below instead of WorldGenerator global ones?
    [Header("Overrides (if above is true)")]
    public List<OreType> allowedOres; // List of ore definitions SPECIFICALLY for this biome
                                      // You could add frequency multipliers here too if desired later
}
[System.Serializable]
public struct OreType {
    public string name;
    public TileBase tile;
    public List<string> allowedBiomeNames; // Names of biomes where this ore can spawn
    public float frequency;     // Noise frequency for this ore
    [Range(0f, 1f)] public float threshold; // Noise value above which ore spawns (higher = rarer)
    public float clusterFrequency; // Lower frequency noise for controlling large clusters
    [Range(0f, 1f)] public float clusterThreshold; // Threshold for cluster noise
    public bool requireCluster; // Must the cluster noise also be above threshold?
}


[CreateAssetMenu(fileName = "BiomeLayerSO", menuName = "ScriptableObjects/BiomeLayerSO", order = 6)]
public class BiomeLayerSO : ScriptableObject {
    [Header("Identification & Base")]
    public BiomeType biomeType;
    public TileBase defaultGroundTile;

    [Header("Vertical Placement")]
    public int startY; // Center Y where the biome ideally starts
    public int endY;   // Center Y where the biome ideally ends
    public float verticalEdgeNoiseFrequency;// = 0.06f; Noise frequency for vertical boundaries
    public float verticalEdgeNoiseAmplitude;// = 4f;   Max units the boundary can shift up/down

    [Header("Horizontal Placement")]
    public int maxHorizontalDistanceFromTrenchCenter;// = 100;  Max X extent from world 0
    public float horizontalEdgeNoiseFrequency;// = 0.04f;  Noise frequency for horizontal edges
    public float horizontalEdgeNoiseAmplitude;// = 8f;  Max units the horizontal edge can shift in/out

    [Header("Biome-Specific Generation Rules")]
    public BiomeCaveSettings caveSettings;
    public BiomeOreSettings oreSettings;
    // public BiomeEntitySettings entitySettings; // Could add this later
}
public enum BiomeType : byte {
    None = 0,
    Trench = 1,
    Surface = 2,
    Cave = 3,
    Algea = 4,
    Coral = 5,
    Ocean = 6,
    Bioluminescent = 7
}
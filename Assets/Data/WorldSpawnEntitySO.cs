using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "WorldSpawnEntitySO", menuName = "ScriptableObjects/WorldSpawnEntitySO", order = 4)]
public class WorldSpawnEntitySO : EntityBaseSO {
    [Header("Placement Rules")]
    public float placementFrequency = 0.1f; // Noise frequency for density/clustering control
    [Range(0f, 1f)] public float placementThreshold = 0.7f; // Noise value needed at anchor point
    public List<string> requiredBiomeNames; // Optional: Only place in these biomes

    [Header("Spawn Conditions at Anchor Point")]
    public bool requireSolidGroundBelow = true; // Must the anchor tile be 'rock'?
    public bool requireWaterAdjacent = false;   // Must be next to MainWater or CaveWater?
    public bool requireCeilingSpace = true;   // Must the tile above be water/air? (Prevent spawning inside solid ground)
    public int minCeilingHeight = 1;         // If requireCeilingSpace, how many tiles above must be non-solid?
    // public float maxSlope = 30f; // Optional: Add slope check later if needed

    [Header("Placement Fine-tuning")]
    public Vector3 positionOffset = Vector3.zero; // Offset from the anchor tile center
    public bool randomYRotation = true;
    public Vector2 scaleVariation = Vector2.one; // Min/Max uniform scale multiplier
}
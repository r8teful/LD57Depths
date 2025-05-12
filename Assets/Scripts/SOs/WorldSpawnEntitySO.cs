using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "WorldSpawnEntitySO", menuName = "ScriptableObjects/WorldSpawnEntitySO", order = 4)]
// An entity that is predetermined to spawn from within the world generation
public class WorldSpawnEntitySO : EntityBaseSO {
    [Header("Placement Rules")]
    public float placementFrequency = 0.1f; // Noise frequency for density/clustering control
    [Range(0f, 1f)] public float placementThreshold = 0.7f; // Noise value needed at anchor point
    public List<AttachmentType> allowedAttachmentTypes;
    [Header("Spawn Conditions at Anchor Point")]
    public bool requireSolidGround = true; // Must the anchor tile be 'rock'?
    public bool requireWaterAdjacent = false;   // Must be next to MainWater or CaveWater?
    public int minHeightSpace = 1;         // How many tiles above must be non-solid?
    public int minWidthSpace = 1;         // How many tiles to the sides must be non-solid??

    [Header("Placement Fine-tuning")]
    public bool randomYRotation = true;
    public Vector2 scaleVariation = Vector2.one; // Min/Max uniform scale multiplier
}

public enum AttachmentType {
    None,
    Ground, Ceiling, WallRight, WallLeft
}
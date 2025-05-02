using UnityEngine;

public class RuntimeSpawnEntitySO : EntityBaseSO {

    [Range(0f, 1f)] public float spawnChance = 0.1f; // Base chance per spawn tick check
    public int maxConcurrent = 10;         // Max of THIS entity type allowed near player/in world
    [Header("Spawn Behavior")]
    public int spawnGroupSizeMin = 1;
    public int spawnGroupSizeMax = 1;
}
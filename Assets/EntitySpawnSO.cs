using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Tilemaps;

[CreateAssetMenu(fileName = "EntitySpawnSO", menuName = "ScriptableObjects/EntitySpawnSO")]
public class EntitySpawnSO : ScriptableObject {
    [Header("Identification")]
    public string entityName = "Generic Entity";
    public GameObject entityPrefab; // Must have NetworkObject!

    [Header("Spawn Conditions")]
    public List<BiomeType> requiredBiomes; // Spawns if CURRENT biome is one of these
    public float minBiomeRate = 0.5f;      // Required rate of one of the requiredBiomes
    [Range(0f, 1f)] public float spawnChance = 0.1f; // Base chance per spawn tick check
    public int maxConcurrent = 10;         // Max of THIS entity type allowed near player/in world
    public bool requireWater = true;      
    public int minYLevel = -1000;         
    public int maxYLevel = 2000;     
    public List<TileBase> specificSpawnTiles; 

    [Header("Spawn Behavior")]
    public int spawnGroupSizeMin = 1;
    public int spawnGroupSizeMax = 1;
}
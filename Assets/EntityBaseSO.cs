using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Tilemaps;

[CreateAssetMenu(fileName = "EntitySpawnSO", menuName = "ScriptableObjects/EntitySpawnSO")]
// An entity that can be spawned, either dynamically, or with the world generation 
public class EntityBaseSO : ScriptableObject {
    [Header("Identification")]
    public string entityName = "Generic Entity";
    public GameObject entityPrefab; // Must have NetworkObject!

    [Header("Spawn Conditions")]
    public List<BiomeType> requiredBiomes; // Spawns if CURRENT biome is one of these
    public float minBiomeRate = 0.5f;      // Required rate of one of the requiredBiomes
    public int minY = -1000;         
    public int maxY = 2000;     
    public List<TileBase> specificSpawnTiles;
}

[System.Serializable]
public struct EntitySpawnInfo {
    public GameObject prefab; // The prefab to instantiate
    public Vector3 position; // World position
    public Quaternion rotation; // Rotation variaton
    public Vector3 scale; // Scale variation
    public EntitySpawnInfo(GameObject prefab, Vector3 position, Quaternion rotation,  Vector3 scale) {
        this.prefab = prefab;
        this.position = position;
        this.rotation = rotation;
        this.scale = scale;
    }
}
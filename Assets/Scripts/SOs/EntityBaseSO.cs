using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Tilemaps;
using FishNet.Object;
using Sirenix.OdinInspector;

[CreateAssetMenu(fileName = "EntitySpawnSO", menuName = "ScriptableObjects/EntitySpawnSO")]
// An entity that can be spawned, either dynamically, or with the world generation 
public class EntityBaseSO : ScriptableObject, IIdentifiable {
    [Header("Identification")]
    public string entityName = "Generic Entity";
    public ushort entityID; 
    public GameObject entityPrefab; // Must have NetworkObject!

    [Header("Spawn Conditions")]
    public List<BiomeType> requiredBiomes; // Spawns if CURRENT biome is one of these
    public float minBiomeRate = 0.5f;      // Required rate of one of the requiredBiomes
    public int minY = -1000;         
    public int maxY = 2000;     
    public List<TileBase> specificSpawnTiles;

    public ushort ID => entityID;
}

[System.Serializable]
public class PersistentEntityData {
    // --- Identification ---
    public ulong persistentId; // A unique ID for this specific instance across sessions
    public ushort entityID { get; set; } // We also need it here because we lose the prefab data when where passing things around
    // --- Core State ---
    public Vector3Int cellPos;
    public Quaternion rotation;
    public Vector3 scale;

    // --- Runtime Link (Server Only, Not Saved) ---
    [System.NonSerialized] public NetworkObject activeInstance = null; // Link to the live NetworkObject when active
    public PersistentEntityData(ulong persistentId, ushort entityID,Vector3Int cellPos, Quaternion rotation,  Vector3 scale) {
        this.persistentId = persistentId;
        this.entityID = entityID;
        this.cellPos = cellPos;
        this.rotation = rotation;
        this.scale = scale;
    }
}
public struct EntitySpawnInfo {
    public GameObject prefab; // The prefab to instantiate
    public ushort entityID; // So I don't have to set each entity into the inspector
    public Vector3Int cellPos; // Cell position
    public Quaternion rotation; // Rotation variaton
    public Vector3 scale; // Scale variation
    public EntitySpawnInfo(GameObject prefab, ushort entityID, Vector3Int cellPos, Quaternion rotation, Vector3 scale) {
        this.prefab = prefab;
        this.entityID = entityID;
        this.cellPos = cellPos;
        this.rotation = rotation;
        this.scale = scale;
    }
}
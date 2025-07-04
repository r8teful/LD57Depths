using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Tilemaps;
using FishNet.Object;
using Sirenix.OdinInspector;
using System;
using Newtonsoft.Json;

[CreateAssetMenu(fileName = "EntityBaseSO", menuName = "ScriptableObjects/Entities/EntityBaseSO")]
// An entity that can be spawned, either dynamically, or with the world generation 
public class EntityBaseSO : SerializedScriptableObject, IIdentifiable {
    [TitleGroup("Identification")]
    [HorizontalGroup("Identification/Split")]
    [VerticalGroup("Identification/Split/Right")]
    public ushort entityID; 
    [VerticalGroup("Identification/Split/Right")]
    public string entityName = "Generic Entity";
    [VerticalGroup("Identification/Split/Right")]
#if UNITY_EDITOR
    [OnValueChanged(nameof(UpdatePreview))]
#endif
    public GameObject entityPrefab; // Must have NetworkObject!
#if UNITY_EDITOR
    [VerticalGroup("Identification/Split/Left")]
    [PreviewField(75, ObjectFieldAlignment.Left)]
    [ReadOnly,HideLabel]
    [ShowInInspector]
    private Sprite sprite;

    [OnInspectorInit]
    private void UpdatePreview() {
        sprite = null;

        if (entityPrefab != null) {
            var spriteRenderer = entityPrefab.GetComponentInChildren<SpriteRenderer>();
            if (spriteRenderer != null) {
                sprite = spriteRenderer.sprite;
            }
        }
    }
#endif
    [Header("Spawn Conditions (Optional)")]
    public SpawnConditions spawnConditions; // Null for non-spawning entities
    public ushort ID => entityID;
}
[Serializable]
public class SpawnConditions {
    public List<BiomeType> requiredBiomes; // Spawns if current biome is one of these
    public int minY = -1000;
    public int maxY = 2000;
    public List<TileBase> specificSpawnTiles;
}

[System.Serializable]
public class PersistentEntityData {
    // --- Identification ---
    public ulong persistentId; // A unique ID for this specific instance across sessions
    public ushort entityID { get; set; } // We also need it here because we lose the prefab data when where passing things around
    // --- Core State ---
    public Vector3Int cellPos;
    public Quaternion rotation;

    [JsonProperty(TypeNameHandling = TypeNameHandling.Auto)]
    public EntitySpecificData specificData; // Polymorphic field for specific data

    // --- Runtime Link (Server Only, Not Saved) ---
    [System.NonSerialized] public NetworkObject activeInstance = null; // Link to the live NetworkObject when active
    public PersistentEntityData(ulong persistentId, ushort entityID,Vector3Int cellPos, Quaternion rotation) {
        this.persistentId = persistentId;
        this.entityID = entityID;
        this.cellPos = cellPos;
        this.rotation = rotation;
    }
    public PersistentEntityData(ulong persistentId, ushort entityID, Vector3Int cellPos, Quaternion rotation,EntitySpecificData entitySpecific) {
        this.persistentId = persistentId;
        this.entityID = entityID;
        this.cellPos = cellPos;
        this.rotation = rotation;
        specificData = entitySpecific;
    }
}
public struct EntitySpawnInfo {
    public ushort entityID; // So I don't have to set each entity into the inspector
    public Vector3Int cellPos; // Cell position
    public Quaternion rotation; // Rotation variaton
    public EntitySpawnInfo(ushort entityID, Vector3Int cellPos, Quaternion rotation) {
        this.entityID = entityID;
        this.cellPos = cellPos;
        this.rotation = rotation;
    }
}
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;       // For InstanceFinder
using UnityEngine;

public class EntityManager : StaticInstance<EntityManager> {
    [Header("References")]
    [SerializeField] private ChunkManager chunkManager;
    [SerializeField] private WorldManager worldManager;
    [SerializeField] private BiomeManager biomeManager;
    [SerializeField] private List<RuntimeSpawnEntitySO> entitySpawnList;


    // Key: Persistent Entity ID, Value: How many active chunks require it
    private Dictionary<Vector2Int, List<ulong>> entityIdsByByChunkCoord = new Dictionary<Vector2Int, List<ulong>>(); // Like worldChunks but for entities
    private Dictionary<ulong, PersistentEntityData> persistentEntityDatabase = new Dictionary<ulong, PersistentEntityData>();
    private ulong nextPersistentEntityId = 1; // Counter for assigning unique IDs

    private ulong GetNextPersistentEntityId() { return nextPersistentEntityId++; }
    public List<ulong> GetEntityIDsByChunkCoord(Vector2Int chunkCoord) {
        if (entityIdsByByChunkCoord.TryGetValue(chunkCoord, out var Idlist)) {
            return Idlist;
        } else {
            return null;
        }
    }

    public void Start() {
        if (chunkManager == null) chunkManager = FindFirstObjectByType<ChunkManager>();
        if (biomeManager == null) biomeManager = worldManager.BiomeManager;
        if (chunkManager == null || biomeManager == null) {
            Debug.LogError("EntitySpawner cannot find WorldGenerator or BiomeManager! Disabling.");
            enabled = false; // Disable the spawner component
            return;
        }
    }

    // Adds data to a persistent database, doesn't get spawned yet because that is client only
    public List<ulong> AddGeneratedEntityData(Vector2Int chunkCoord, List<EntitySpawnInfo> entityList) {
        if (entityList == null || entityList.Count == 0) return new List<ulong>();
        if (!entityIdsByByChunkCoord.ContainsKey(chunkCoord)) {
            entityIdsByByChunkCoord[chunkCoord] = new List<ulong>();
        }
        foreach (EntitySpawnInfo info in entityList) {
            // Add to the main persistent database
            PersistentEntityData newEntityData = ServerAddNewPersistentEntity(info.entityID, info.cellPos, info.rotation);
            if (newEntityData != null) {
                // Add the ID to this chunk's list
                entityIdsByByChunkCoord[chunkCoord].Add(newEntityData.persistentId);
            }
        }
        return entityIdsByByChunkCoord[chunkCoord];
    }
    // Called by WorldGenerator's TargetRPC
    public void ProcessReceivedEntityIds(Vector2Int chunkCoord, List<ulong> entityIds) {
        if (entityIds == null)
            return;
        if (entityIds.Count == 0) return;
        // Note that we don't need to add it to the entityIdsByByChunkCoord because we've already called AddGeneratedEntityData.
        // Basically the generation/registration part is separated from the activation part

        foreach (ulong id in entityIds) {
            RequestEntityActivation(id); // Assuming we're always going to want to activate it and its not already activated
        }
    }
    public void RemoveEntitieAtChunk(Vector2Int chunkCoord) {
        if (entityIdsByByChunkCoord.TryGetValue(chunkCoord, out List<ulong> entityIds)) {
            foreach (ulong id in entityIds) {
                RequestEntityDeactivation(id);
            }
        }
    }

    public PersistentEntityData ServerAddNewPersistentEntity(ushort id, Vector3Int pos, Quaternion rot, EntitySpecificData entityData = null) {
        ulong uniqueID = GetNextPersistentEntityId();
        // Instead of creating the data like this, you can let the entity themeselves handle the setup of the data
        // Right now its not worth the effort because we just have 2 entities that actually store specific data, but could be good for later
        //EntitySpecificData e = App.ResourceSystem.GetEntityByID(id).CreateDefaultSpecificData(); // Something like that

        EntitySpecificData data = CreateEntitySpecificDataByID(id); // This obviously doesn't scale properly
        PersistentEntityData newEntityData = new(uniqueID, id, pos, rot, entityData);
        persistentEntityDatabase.Add(uniqueID, newEntityData);
        //Debug.Log($"Added new persistent entity ID:{uniqueID} at {pos}");
        return newEntityData;
    }
    private EntitySpecificData CreateEntitySpecificDataByID(ushort id) {
        if (ResourceSystem.IsGrowEntity(id)) {
            return new GrowthEntityData(0);
        }
        return null;
    }

    public void RequestEntityActivation(ulong persistentId) {

        PersistentEntityData data = persistentEntityDatabase[persistentId];
        if (data == null) {
            Debug.LogWarning($"Server: Client requested activation for unknown entity ID {persistentId}");
            // Optionally: Send error back to client? TargetRpc...?
            return;
        }
        GameObject prefab = App.ResourceSystem.GetEntityByID(data.entityID).entityPrefab;
        if (prefab == null) {
            Debug.LogError($"Cannot activate entity {persistentId}: Prefab missing for type {data.entityID}");
            return;
        }
        // Instantiate and apply data
        Vector3 spawnPos = new Vector3(data.cellPos.x + 0.5f, data.cellPos.y + 0.5f, 0f); // Spawn in the centre of the tile
        GameObject instance = Instantiate(prefab, spawnPos, data.rotation);
        data.activeInstance = instance; // LINK
        //instance.transform.localScale = data.scale;
        ApplyDataToInstance(instance, data); // Apply health, growth etc.
        // Link instance BEFORE spawn
        if (instance.TryGetComponent<DestroyEntityCallback>(out var entityDestroyCallback)) {
            entityDestroyCallback.OnDestroyed += HandleEntityDestroyed;
        }
    }


    public void RequestEntityDeactivation(ulong persistentId) {
        PersistentEntityData data = persistentEntityDatabase[persistentId];
        // It's possible data is null if entity was just removed permanently
        if (data == null) {
            return;
        }
        var nob = data.activeInstance;
        if (nob != null) {
            // Save state just before despawning
            UpdateDataFromInstance(nob, data);

            // Unsubscribe FIRST
            if (nob.TryGetComponent<DestroyEntityCallback>(out var entityDestroyCallback)) {
                entityDestroyCallback.OnDestroyed -= HandleEntityDestroyed;
            }
            Destroy(nob); // This could also return it to the object pool or something
        }
        // Clear link in persistent data regardless
        data.activeInstance = null;
    }

    // --- Modify entity removal to also clear from chunk map ---
    public void ServerRemovePersistentEntity(ulong persistentId) {
        if (persistentEntityDatabase.TryGetValue(persistentId, out PersistentEntityData data)) {
            // --- Remove from chunk map ---
            Vector2Int chunkCoord = chunkManager.CellToChunkCoord(data.cellPos);
            if (entityIdsByByChunkCoord.TryGetValue(chunkCoord, out List<ulong> list)) {
                list.Remove(persistentId);
            }
            persistentEntityDatabase.Remove(persistentId);
        }
    }

    private void HandleEntityDestroyed(GameObject nob) {
        if (nob.TryGetComponent<DestroyEntityCallback>(out var entityDestroyCallback)) {
            entityDestroyCallback.OnDestroyed -= HandleEntityDestroyed;
            if (entityDestroyCallback.IsDestroyedPermanently) {
                ulong persistentId = FindIdForInstance(nob);
                ServerRemovePersistentEntity(persistentId);
            }
        }
    }

    private void ApplyDataToInstance(GameObject instance, PersistentEntityData data) {
        if (instance == null || data == null) return;
        /* TODO obviously
        // Apply Health (Example: using a standard Health component)
        HealthComponent health = instance.GetComponent<HealthComponent>(); // Assume you have this
        if (health != null) {
            health.ServerSetCurrentHealth(data.currentHealth); // Method needed on HealthComponent
        }

        // Apply Growth (Example)
        PlantGrowthComponent growth = instance.GetComponent<PlantGrowthComponent>();
        if (growth != null) {
            growth.ServerSetGrowth(data.growthStage); // Method needed on growth component
        }
        */
        // Apply custom name, inventory, etc. to corresponding components
    }
    // Update persistent data FROM an active NetworkObject instance (before deactivating)
    private void UpdateDataFromInstance(GameObject nob, PersistentEntityData data) {
        if (nob == null || data == null) return;
        // Update Core State
        //data.cellPos = Mathf.FloorToInt(nob.transform.position);
        data.rotation = nob.transform.rotation;
        //data.scale = nob.transform.localScale;
        /* todo
        // Update Health
        HealthComponent health = nob.GetComponent<HealthComponent>();
        if (health != null) {
            data.currentHealth = health.CurrentHealth; // Assume property/getter exists
        }

        // Update Growth
        PlantGrowthComponent growth = nob.GetComponent<PlantGrowthComponent>();
        if (growth != null) {
            data.growthStage = growth.CurrentGrowth; // Assume property/getter exists
        }*/
    }

    // Helper needed for HandleUnexpectedDespawn
    private ulong FindIdForInstance(GameObject nob) {
        foreach (var kvp in persistentEntityDatabase) {
            if (kvp.Value.activeInstance == nob) return kvp.Key;
        }
        return 0;
    }
    public void NotifyTileChanged(Vector3Int changedCellPosition, Vector2Int chunkCoord, int newTileID) {
        List<ulong> candidateIds = GetEntityIDsByChunkCoord(chunkCoord);
        if (candidateIds == null) return; // No entities registered in this chunk
        candidateIds = candidateIds.ToList();
        float sqrNotifyRadius = 5 * 5; //default of 5 tiles now
        Vector3 changeWorldPos = worldManager.GetCellCenterWorld(changedCellPosition);

        foreach (ulong entityId in candidateIds) {
            PersistentEntityData entityData = persistentEntityDatabase[entityId];
            // Check if entity is ACTIVE and within notification radius
            if (entityData != null && entityData.activeInstance != null) {
                if (Vector3.SqrMagnitude(entityData.activeInstance.transform.position - changeWorldPos) <= sqrNotifyRadius) {
                    // Found a nearby active entity, notify its components
                    NotifyEntityComponents<ITileChangeReactor>(
                         entityData.activeInstance,
                         (reactor) => reactor.OnTileChangedNearby(changedCellPosition, newTileID)
                     );
                }
            }
        }
    }
    private void NotifyEntityComponents<T>(GameObject targetNobo, Action<T> action) where T : class // Interface constraint
    {
        if (targetNobo == null || action == null) return;

        // GetComponentsInChildren also includes components on the root object
        T[] reactors = targetNobo.GetComponentsInChildren<T>();
        foreach (T reactor in reactors) {
            try {
                action(reactor); // Execute the provided action (e.g., calling the interface method)
            } catch (System.Exception e) {
                Debug.LogError($"Error notifying component {typeof(T).Name} on entity {targetNobo.name}: {e.Message}", targetNobo);
            }
        }
    }

    public void NotifyNearbyPlayers(Vector3Int changedCellPosition, Vector2Int chunkCoord, int newTileID) {
        List<ulong> candidateIds = GetEntityIDsByChunkCoord(chunkCoord);
        if (candidateIds == null)
            return; // No entities registered in this chunk
        candidateIds = candidateIds.ToList();
        float sqrNotifyRadius = 5 * 5; //default of 5 tiles now
        Vector3 changeWorldPos = worldManager.GetCellCenterWorld(changedCellPosition);

        foreach (ulong entityId in candidateIds) {
            PersistentEntityData entityData = persistentEntityDatabase[entityId];
            // Check if entity is ACTIVE and within notification radius
            if (entityData != null && entityData.activeInstance != null) {
                if (Vector3.SqrMagnitude(entityData.activeInstance.transform.position - changeWorldPos) <= sqrNotifyRadius) {
                    // Found a nearby active entity, notify its components
                    NotifyEntityComponents<ITileChangeReactor>(
                         entityData.activeInstance,
                         (reactor) => reactor.OnTileChangedNearby(changedCellPosition, newTileID)
                     );
                }
            }
        }
    }
}
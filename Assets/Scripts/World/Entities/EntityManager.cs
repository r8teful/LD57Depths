using r8teful;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class EntityManager : StaticInstance<EntityManager>, ISaveable {
    [Header("References")]
    [SerializeField] private ChunkManager chunkManager;
    [SerializeField] private WorldManager worldManager;
    [SerializeField] private BiomeManager biomeManager;


    // Key: Persistent Entity ID, Value: How many active chunks require it
    private Dictionary<Vector2Int, List<ulong>> entityIdsByByChunkCoord = new Dictionary<Vector2Int, List<ulong>>(); // Like worldChunks but for entities
    private Dictionary<ulong, PersistentEntityData> persistentEntityDatabase = new Dictionary<ulong, PersistentEntityData>();
    private ulong nextPersistentEntityId = 1; // Counter for assigning unique IDs
    
    // runtime
    private HashSet<ulong> _currentSpawnedEntities = new HashSet<ulong>();
    
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

    public void AddGeneratedEntityData(Vector2Int chunkCoord, List<EntitySpawnInfo> entityList) {
        if (entityList == null || entityList.Count == 0) return;
        if (!entityIdsByByChunkCoord.ContainsKey(chunkCoord)) {
            entityIdsByByChunkCoord[chunkCoord] = new List<ulong>();
        }
        foreach (EntitySpawnInfo info in entityList) {
            // Add to the main persistent database
            PersistentEntityData newEntityData = AddNewPersistentEntity(info.entityID, info.cellPos, info.rotation);
            if (newEntityData != null) {
                // Add the ID to this chunk's list
                entityIdsByByChunkCoord[chunkCoord].Add(newEntityData.persistentId);
            }
        }
    }
    public void AddGeneratedEntityData(Vector2Int chunkCoord,EntitySpawnInfo info, EntitySpecificData specificData = null) {
        if (info.entityID == ResourceSystem.InvalidID) return;
        if (!entityIdsByByChunkCoord.ContainsKey(chunkCoord)) {
            entityIdsByByChunkCoord[chunkCoord] = new List<ulong>();
        }
        // Add to the main persistent database
        PersistentEntityData newEntityData = AddNewPersistentEntity(info.entityID, info.cellPos, info.rotation, specificData);
        if (newEntityData != null) {
            // Add the ID to this chunk's list
            entityIdsByByChunkCoord[chunkCoord].Add(newEntityData.persistentId);
        }
    }

    // Called by WorldGenerator's TargetRPC
    public void ProcessReceivedEntityIds(Vector2Int chunkCoord, List<ulong> entityIds) {
        if (entityIds == null)
            return;
        if (entityIds.Count == 0) return;
        // Note that we don't need to add it to the entityIdsByByChunkCoord because we've already called AddGeneratedEntityData.
        // Basically the generation/registration part is separated from the activation part

        foreach (ulong id in entityIds) {
            RequestEntityActivation(id); // This only spawns if data.activeInstance is null
        }
    }
    public void RemoveEntitieAtChunk(Vector2Int chunkCoord) {
        if (entityIdsByByChunkCoord.TryGetValue(chunkCoord, out List<ulong> entityIds)) {
            foreach (ulong id in entityIds) {
                RequestEntityDeactivation(id);
            }
        }
    }
    public void ActivateEntitiesAtChunk(Vector2Int chunkCoord) {
        if (entityIdsByByChunkCoord.TryGetValue(chunkCoord, out List<ulong> entityIds)) {
            foreach (ulong id in entityIds) {
                RequestEntityActivation(id);
            }
        }
    }

    public PersistentEntityData AddNewPersistentEntity(ushort id, Vector3Int pos, Quaternion rot, EntitySpecificData entityData = null) {
        
        ulong uniqueID = GetNextPersistentEntityId();
        

        PersistentEntityData newEntityData = new(uniqueID, id, pos, rot, entityData);
        persistentEntityDatabase.Add(uniqueID, newEntityData);
        //Debug.Log($"Added new persistent entity ID:{uniqueID} at {pos}");
        return newEntityData;
    }
    

    public void RequestEntityActivation(ulong persistentId) {

        PersistentEntityData data = persistentEntityDatabase[persistentId];
        if (data == null) {
            Debug.LogWarning($"Server: Client requested activation for unknown entity ID {persistentId}");
            // Optionally: Send error back to client? TargetRpc...?
            return;
        }
        if(data.activeInstance != null) {
            // Already spawned
            return;
        }
        var entitySO = App.ResourceSystem.GetEntityByID(data.entityID);
        GameObject prefab = entitySO.entityPrefab;
        if (prefab == null) {
            Debug.LogError($"Cannot activate entity {persistentId}: Prefab missing for type {data.entityID}");
            return;
        }
        // Instantiate and apply data
        Vector3 spawnPos = new Vector3(data.cellPos.x + 0.5f, data.cellPos.y + 0.5f, 0f); // Spawn in the centre of the tile
        GameObject instance = Instantiate(prefab, spawnPos, data.rotation,transform);
        //Debug.Log($"Instsantiting instance {prefab} with ID: {data.entityID} at {spawnPos}");
        data.activeInstance = instance; // LINK
        _currentSpawnedEntities.Add(data.persistentId);

        ApplyDataToInstance(instance, data); // Apply health, growth etc.
        // Link instance on destroyed so we can remove it from our registry
        if (instance.TryGetComponent<DestroyEntityCallback>(out var entityDestroyCallback)) {
            entityDestroyCallback.OnDestroyed += HandleEntityDestroyed;
        } else {
            Debug.LogWarning($"Entity {entitySO.entityName} doesn't have destroyEntityCallback! You should add it just in case the object gets destroyed");
        }
        if (instance.TryGetComponent<SaveEntityCallback>(out var entitySaveCallback)) {
            // tell what ID the save has
            entitySaveCallback.persistantID = data.persistentId;
            entitySaveCallback.OnSave += HandleEntitySaveChange;
        } 
    }

    private void HandleEntitySaveChange(GameObject obj,ulong ID) {
        persistentEntityDatabase.TryGetValue(ID, out var persistentEntity);
        SaveDataFromInstance(obj, persistentEntity);
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
            SaveDataFromInstance(nob, data);

            // Unsubscribe FIRST
            if (nob.TryGetComponent<DestroyEntityCallback>(out var entityDestroyCallback)) {
                entityDestroyCallback.OnDestroyed -= HandleEntityDestroyed;
            }
            if (nob.TryGetComponent<SaveEntityCallback>(out var entitySaveCallback)) {
                entitySaveCallback.OnSave -= HandleEntitySaveChange;
            }
            Destroy(nob); // This could also return it to the object pool or something
            _currentSpawnedEntities.Remove(data.persistentId);
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
        if (nob.TryGetComponent<SaveEntityCallback>(out var entitySaveCallback)) {
            entitySaveCallback.OnSave -= HandleEntitySaveChange;
        }
    }

    private void ApplyDataToInstance(GameObject instance, PersistentEntityData data) {
        if (instance == null || data == null) return;
        if(data.specificData == null) {
            return;
        }
        // Specific data already exists, apply it 
        data.specificData.ApplyTo(instance);
    }

    // Update persistent data FROM an entity
    private void SaveDataFromInstance(GameObject nob, PersistentEntityData data) {
        if (nob == null || data == null) return;
        // Update Core State
        data.cellPos = new(Mathf.FloorToInt(nob.transform.position.x), Mathf.FloorToInt(nob.transform.position.y));
        data.rotation = nob.transform.rotation;

        if (data.specificData == null) return;
        if(data.specificData is ArtifactData a){
            // Biome data doeesn't change at runtime so no need to save or change the persistanententityData
            return;
        }
        if(data.specificData is IsUsedData i) {
            i.TrySave(nob);
            return;
        }
        Debug.LogError("Entity had specific Data but it could not be applied!");
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
    public PersistentEntityData FindClosestExplorationEntity(Transform playerTransform) {
        if (playerTransform == null || persistentEntityDatabase == null || persistentEntityDatabase.Count == 0)
            return null;

        PersistentEntityData closestEntity = null;
        float closestSqrDistance = float.MaxValue;
        Vector3 playerPos = playerTransform.position;

        foreach (var entity in persistentEntityDatabase.Values) {
            if (entity == null)continue;
            if (entity.specificData == null)continue;
            if (entity.specificData is IsUsedData used) {
                if(used.IsEntityUsed) continue;
            } else {
                continue;
            }
            var data = App.ResourceSystem.GetEntityByID(entity.entityID);
            if (data == null || data.entityType != EntityType.Exploration) continue;
            
            Vector3 entityPos = (Vector3)entity.cellPos;
            float sqrDistance = (playerPos - entityPos).sqrMagnitude;

            if (sqrDistance < closestSqrDistance) {
                closestSqrDistance = sqrDistance;
                closestEntity = entity;
            }
        }

        return closestEntity;
    }
    public void OnSave(SaveData data) {
        if (data == null|| data.worldData == null) return;
        // Make sure we save entity specific data in the database of all active entities
        foreach (var spawnedEntity in _currentSpawnedEntities) { 
            if(persistentEntityDatabase.TryGetValue(spawnedEntity, out var entityData)){
                SaveDataFromInstance(entityData.activeInstance, entityData);
            } else {
                Debug.LogError("Could not find persistant entity in database, did we remove it?");
            }
        }
        data.worldData.savedEntities = persistentEntityDatabase; // same data as in save, don't have to do anything complicated here 
        data.worldData.nextPersistentEntityId = nextPersistentEntityId;
        //var entities = JsonConvert.SerializeObject(pEntities, Formatting.Indented, new JsonSerializerSettings {
        //    TypeNameHandling = TypeNameHandling.Auto
        //});
        // write entity string to data
    }

    public void OnLoad(SaveData data) {
        //string dataString = data.entities;
        foreach (var entity in data.worldData.savedEntities) {
            // Create persistent database from save
            if (!persistentEntityDatabase.ContainsKey(entity.Value.persistentId)) {
                persistentEntityDatabase.Add(entity.Value.persistentId, entity.Value);
            } else {
                Debug.LogWarning("Persistant ID already exists in data base! Did you forget to clear the database?");
            }
            // Create chunk coord mapping 
            if (chunkManager != null) {
                var chunkCoord = chunkManager.CellToChunkCoord(entity.Value.cellPos);

                if (!entityIdsByByChunkCoord.ContainsKey(chunkCoord)) {
                    entityIdsByByChunkCoord.Add(chunkCoord, new() { entity.Value.persistentId }); // Chunk choord doesn't exist yet, add new
                } else {
                    entityIdsByByChunkCoord[chunkCoord].Add(entity.Value.persistentId); // Add to the already existing list
                }
            }
        }
        nextPersistentEntityId = data.worldData.nextPersistentEntityId;
    }
}
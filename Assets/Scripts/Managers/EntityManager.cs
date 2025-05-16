using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using FishNet.Object; // For NetworkBehaviour and ServerManager access
using FishNet;
using FishNet.Connection;
using System.Linq;       // For InstanceFinder

public class EntityManager : NetworkBehaviour // Needs to be NetworkBehaviour to use ServerManager etc.
{
    [Header("References")]
    [SerializeField] private ChunkManager chunkManager;
    [SerializeField] private WorldManager worldManager;
    [SerializeField] private BiomeManager biomeManager;
    [SerializeField] private List<RuntimeSpawnEntitySO> entitySpawnList;

    [Header("Spawner Settings")]
    [SerializeField] private float spawnCheckInterval = 2.0f; // How often to check for spawns
    [SerializeField] private int spawnCheckRadius = 10; // In chunks, around each player
    [SerializeField] private int checksPerInterval = 5;  // How many random locations to check each interval
    [SerializeField] private int maxTotalEntities = 99999; // Global cap for all spawned entities managed by this spawner
    [SerializeField] private float playerDespawnRange = 120f; // Despawn entities further than this from *any* player

    // Server-side tracking
    private List<NetworkObject> spawnedEntities = new List<NetworkObject>(); // Track instances for despawning
    private Dictionary<GameObject, int> currentEntityCounts = new Dictionary<GameObject, int>(); // Key: Prefab, Value: Count
    // Key: Persistent Entity ID, Value: Number of CLIENTS currently needing it active
    private Dictionary<ulong, int> entityClientRefCount = new Dictionary<ulong, int>();

    // Key: Persistent Entity ID, Value: How many active chunks require it
    private Dictionary<Vector2Int, List<ulong>> entityIdsByByChunkCoord = new Dictionary<Vector2Int, List<ulong>>(); // Like worldChunks but for entities
    private Dictionary<ulong, PersistentEntityData> persistentEntityDatabase = new Dictionary<ulong, PersistentEntityData>();
    private ulong nextPersistentEntityId = 1; // Counter for assigning unique IDs

    // Local entity data
    private HashSet<ulong> locallyActiveEntityIds = new HashSet<ulong>();
    private Dictionary<Vector2Int, List<ulong>> cachedEntityIdsByChunk = new Dictionary<Vector2Int, List<ulong>>();
    private Dictionary<ulong, Vector2Int> cachedEntityInChunk = new Dictionary<ulong, Vector2Int>(); // Specifies which chunk an cached enemy is
    private ulong GetNextPersistentEntityId() { return nextPersistentEntityId++; }
    public List<ulong> GetEntityIDsByChunkCoord(Vector2Int chunkCoord) {
        if (entityIdsByByChunkCoord.TryGetValue(chunkCoord, out var Idlist)) {
            return Idlist;
        } else {
            return null;
        }
    }
    public override void OnStartServer() {
        base.OnStartServer();
        if (chunkManager == null) chunkManager = FindFirstObjectByType<ChunkManager>();
        if (biomeManager == null) biomeManager = worldManager.BiomeManager;
        if (chunkManager == null || biomeManager == null) {
            Debug.LogError("EntitySpawner cannot find WorldGenerator or BiomeManager! Disabling.");
            enabled = false; // Disable the spawner component
            return;
        }
        // Only using these two for runtime spawning
        //StartCoroutine(SpawnCheckLoop());
        //StartCoroutine(DespawnCheckLoop());
    }

    #region Runtime spawning
    IEnumerator SpawnCheckLoop() {
        // Safety check: Ensure this only runs on the server.
        if (!IsServerInitialized) yield break;

        while (true) {
            yield return new WaitForSeconds(spawnCheckInterval);

            if (InstanceFinder.ServerManager == null || !InstanceFinder.ServerManager.Started) continue; // Server not ready
            if (spawnedEntities.Count >= maxTotalEntities) continue; // Global cap reached

            // Iterate through all connected players
            foreach (var conn in InstanceFinder.ServerManager.Clients.Values) {
                if (conn.FirstObject == null) continue; // Player object not spawned/found?
                Vector3 playerPos = conn.FirstObject.transform.position; // Get player's position
                Vector2Int playerChunk = chunkManager.WorldToChunkCoord(playerPos);

                // Perform several checks around this player
                for (int i = 0; i < checksPerInterval; i++) {
                    if (spawnedEntities.Count >= maxTotalEntities) break; // Re-check cap

                    // Find a random chunk near the player
                    Vector2Int randomChunkOffset = new Vector2Int(Random.Range(-spawnCheckRadius, spawnCheckRadius + 1), Random.Range(-spawnCheckRadius, spawnCheckRadius + 1));
                    Vector2Int targetChunkCoord = playerChunk + randomChunkOffset;

                    // Pick a random tile location within that chunk
                    Vector3Int chunkOriginCell = chunkManager.ChunkCoordToCellOrigin(targetChunkCoord);
                    Vector3Int randomCell = chunkOriginCell + new Vector3Int(Random.Range(0, chunkManager.GetChunkSize()), Random.Range(0, chunkManager.GetChunkSize()), 0);

                    // --- Check Conditions at randomCell ---
                    TrySpawnEntityAt(randomCell);
                }
            }
        }
    }

    void TrySpawnEntityAt(Vector3Int cellPos) {
        if (!IsServerInitialized) return; // Ensure server only

        // 1. Get Biome Info
        Vector2Int chunkCoord = chunkManager.CellToChunkCoord(cellPos);
        BiomeChunkInfo biomeInfo = biomeManager.GetBiomeInfo(chunkCoord);
        if (biomeInfo == null || biomeInfo.totalTilesCounted == 0) return; // No biome data or empty chunk

        // 2. Check Tile Info (from WorldGenerator DATA, not visual tilemap)
        // We need a way to get the TileBase from server data. Let's assume WorldGenerator has a server-side method:
        // TileBase currentTile = worldGenerator.GetTileBaseFromServerData(cellPos);
        // For simplicity now, we might have to skip specific tile checks or add that method to WG.
        // Let's focus on biome for now. We also need the world pos Y.
        int worldY = cellPos.y; // Assuming tilemap Y corresponds to world Y

        // 3. Iterate through potential entities
        foreach (var data in entitySpawnList) {
            // Basic chance check
            if (Random.value > data.spawnChance) continue;

            // Concurrent limit check for this entity type
            currentEntityCounts.TryGetValue(data.entityPrefab, out int currentCount);
            if (currentCount >= data.maxConcurrent) continue;


            // Y Level Check
            if (worldY < data.minY || worldY > data.maxY) continue;

            // Biome Check
            bool biomeMatch = false;
            foreach (BiomeType requiredBiome in data.requiredBiomes) {
                if (biomeInfo.GetBiomeRate(requiredBiome) >= data.minBiomeRate) {
                    biomeMatch = true;
                    break;
                }
            }
            if (!biomeMatch && data.requiredBiomes.Count > 0) continue; // Skip if biome doesn't match

            // TODO: Add checks for Surface/Water based on querying WorldGenerator server data for cellPos and cellPos + Up

            // --- All checks passed, attempt spawn ---
            SpawnEntity(data, worldManager.GetCellCenterWorld(cellPos)); // Spawn at cell center
            return; // Spawned one entity this check, move to next check location
        }
    }


    void SpawnEntity(RuntimeSpawnEntitySO data, Vector3 position) {
        if (!IsServerInitialized) return;
        if (data.entityPrefab == null) return;

        int groupSize = Random.Range(data.spawnGroupSizeMin, data.spawnGroupSizeMax + 1);

        for (int i = 0; i < groupSize; ++i) {
            if (spawnedEntities.Count >= maxTotalEntities) break; // Check global cap

            // Check concurrent cap for this type again just before spawning
            currentEntityCounts.TryGetValue(data.entityPrefab, out int currentCount);
            if (currentCount >= data.maxConcurrent) break;

            Vector3 spawnPos = position + (Vector3)Random.insideUnitCircle * 0.5f; // Slightly randomize group pos
            GameObject prefabToSpawn = data.entityPrefab;

            // Instantiate PREFAB on SERVER -> Spawn INSTANCE over network
            GameObject instance = Instantiate(prefabToSpawn, spawnPos, Quaternion.identity, worldManager.GetWorldRoot());
            NetworkObject nob = instance.GetComponent<NetworkObject>();

            if (nob != null) {
                InstanceFinder.ServerManager.Spawn(nob); // Spawn with default ownership (server)
                spawnedEntities.Add(nob); // Track it

                // Increment count for this prefab
                currentEntityCounts[data.entityPrefab] = currentCount + 1;

                // Add listener to despawn event to update count
                // nob.OnStopServer += HandleEntityDespawned; // Use += operator

                Debug.Log($"Spawned {data.entityName} at {spawnPos}");
            } else {
                Debug.LogError($"Entity prefab {data.entityName} is missing NetworkObject component!");
                Destroy(instance); // Clean up unusable instance
            }
        }
    }
    IEnumerator DespawnCheckLoop() {
        if (!IsServerInitialized) yield break;

        while (true) {
            yield return new WaitForSeconds(spawnCheckInterval * 2.5f); // Check less often than spawning

            if (InstanceFinder.ServerManager == null || !InstanceFinder.ServerManager.Started) continue;

            // Build list of active player positions
            List<Vector3> playerPositions = new List<Vector3>();
            foreach (var conn in InstanceFinder.ServerManager.Clients.Values) {
                if (conn.FirstObject != null) { playerPositions.Add(conn.FirstObject.transform.position); }
            }

            if (playerPositions.Count == 0) continue; // No players to check against

            // Iterate backwards through spawned entities list (safe for removal)
            for (int i = spawnedEntities.Count - 1; i >= 0; i--) {
                NetworkObject nob = spawnedEntities[i];
                if (nob == null || !nob.IsSpawned) { // Check if object already destroyed or not spawned
                    spawnedEntities.RemoveAt(i);
                    continue;
                }

                Vector3 entityPos = nob.transform.position;
                bool nearAPlayer = false;
                foreach (Vector3 playerPos in playerPositions) {
                    if (Vector3.Distance(entityPos, playerPos) < playerDespawnRange) {
                        nearAPlayer = true;
                        break;
                    }
                }

                if (!nearAPlayer) {
                    // Too far from ALL players, despawn it
                    Debug.Log($"Despawning {nob.name} due to distance.");
                    // IMPORTANT: OnStopServer listener (HandleEntityDespawned) will handle removal from list & count updates
                    InstanceFinder.ServerManager.Despawn(nob);
                    // Do NOT remove from spawnedEntities list here, HandleEntityDespawned does it.
                }
            }
        }
    }
    #endregion

    // Called by WorldGenerator after a chunk's data (including entities) is ready on the server.
    public bool ServerSpawnPredefinedEntity(GameObject prefab, Vector3 position, Quaternion rotation, Vector3 scale) {
        // --- SERVER ONLY ---
        if (!IsServerInitialized) {
            Debug.LogWarning("Client attempted to call ServerSpawnPredefinedEntity!");
            return false;
        }

        if (prefab == null) {
            Debug.LogError("ServerSpawnPredefinedEntity called with null prefab!");
            return false;
        }

        // --- Respect Global Spawn Cap ---
        if (spawnedEntities.Count >= maxTotalEntities) {
            // Debug.Log($"Global entity cap ({maxTotalEntities}) reached. Cannot spawn predefined {prefab.name}.");
            return false; // Optionally log less verbosely
        }

        // --- Respect Concurrent Spawn Cap for THIS Type ---
        currentEntityCounts.TryGetValue(prefab, out int currentCount);
        // Find the EntitySpawnData associated with this prefab to get its cap
        // This assumes the prefab might ALSO be in the dynamic spawn list.
        // If predefined prefabs are totally separate, you might need another way to define their caps.
        int maxConcurrentForThis = maxTotalEntities; // Default to global if no specific data found

        if (currentCount >= maxConcurrentForThis) {
            Debug.Log($"Concurrent cap ({maxConcurrentForThis}) for {prefab.name} reached. Cannot spawn predefined entity.");
            return false;
        }

        // --- Spawn the Entity ---
        GameObject instance = Instantiate(prefab, position, rotation);
        instance.transform.localScale = scale; // Apply scale *after* instantiation
        NetworkObject nob = instance.GetComponent<NetworkObject>();

        if (nob != null) {
            // Spawn over network (server owned by default)
            InstanceFinder.ServerManager.Spawn(nob);
            spawnedEntities.Add(nob); // Add to tracking list

            // Increment count for this prefab
            currentEntityCounts[prefab] = currentCount + 1;

            // Add listener to despawn event to update count
            //nob.OnStopServer += HandleEntityDespawned; // Ensure this handler exists and works

            // Assign EntityIdentity if needed by HandleEntityDespawned
            EntityIdentity identity = instance.GetComponent<EntityIdentity>();
            if (identity != null) {
                //identity.spawnData = spawnData; // Assign SO if found
            }


            // Debug.Log($"Successfully spawned predefined entity: {prefab.name} at {position}");
            return true;
        } else {
            Debug.LogError($"Predefined entity prefab {prefab.name} is missing NetworkObject component! Destroying instance.");
            Destroy(instance); // Clean up
            return false;
        }
    }
    // Adds data to a persistent database, doesn't get spawned yet because that is client only
    public List<ulong> AddGeneratedEntityData(Vector2Int chunkCoord, List<EntitySpawnInfo> entityList) {
        if (!IsServerInitialized || entityList == null || entityList.Count == 0) return new List<ulong>();
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
        if(entityIds.Count == 0) return;
        // Cache the received IDs
        cachedEntityIdsByChunk[chunkCoord] = entityIds;
        // Request activation for entities in this list that we aren't already tracking
        foreach (ulong id in entityIds) {
            if (locallyActiveEntityIds.Add(id)) // If added (wasn't previously needed)
            {
                // Request activation from server
                CmdRequestEntityActivation(id);
            }
            cachedEntityInChunk.TryAdd(id, chunkCoord);
            
            // Add to reverse lookup
        }
    }
    public void RemoveEntitieAtChunk(Vector2Int chunkCoord) {
        if (cachedEntityIdsByChunk.TryGetValue(chunkCoord, out List<ulong> entityIds)) {
            foreach (ulong id in entityIds) {
                if (locallyActiveEntityIds.Contains(id)) {
                    // Only decrement ref if no *other* active local chunk still contains this ID
                    if (!IsEntityStillNeededByOtherChunks(id, chunkCoord)) {
                        CmdRequestEntityDeactivation(id);
                        locallyActiveEntityIds.Remove(id); // Assume deactivation request will succeed
                    }
                }
            }
        }
    }
    public PersistentEntityData ServerAddNewPersistentEntity(ushort id, Vector3Int pos, Quaternion rot) {
        ulong unqiueID = GetNextPersistentEntityId();
        PersistentEntityData newEntityData = new PersistentEntityData(unqiueID, id, pos, rot);
        persistentEntityDatabase.Add(unqiueID, newEntityData);
        Debug.Log($"Added new persistent entity ID:{unqiueID} at {pos}");
        return newEntityData; // Return the created data
    }
    // --- Client RPCs for Activation/Deactivation ---
    [ServerRpc(RequireOwnership = false)] // Any client can request
    public void CmdRequestEntityActivation(ulong persistentId, NetworkConnection requester = null) {
        if (!IsServerInitialized) return;
        if (requester == null) return; // Should not happen

        PersistentEntityData data = persistentEntityDatabase[persistentId];
        if (data == null) {
            Debug.LogWarning($"Server: Client {requester.ClientId} requested activation for unknown entity ID {persistentId}");
            // Optionally: Send error back to client? TargetRpc...?
            return;
        }

        entityClientRefCount.TryGetValue(persistentId, out int currentRefCount);
        currentRefCount++;
        entityClientRefCount[persistentId] = currentRefCount;

        NetworkObject nob = data.activeInstance;

        // --- Activate SERVER instance if ref count is now 1 ---
        if (currentRefCount == 1) {
            // Get prefab based on type ID
            GameObject prefab = App.ResourceSystem.GetEntityByID(data.entityID).entityPrefab;
            if (prefab == null) {
                Debug.LogError($"Cannot activate entity {persistentId}: Prefab missing for type {data.entityID}");
                return;
            }

            // Instantiate and apply data
            Vector3 spawnPos = new Vector3(data.cellPos.x + 0.5f, data.cellPos.y + 0.5f, 0f); // Spawn in the centre of the tile
            GameObject instance = Instantiate(prefab, spawnPos, data.rotation);
            //instance.transform.localScale = data.scale;

            nob = instance.GetComponent<NetworkObject>();
            if (nob != null) {
                ApplyDataToInstance(instance, data); // Apply health, growth etc.
                // Link instance BEFORE spawn
                // Spawn server-side FIRST (observers added after)
                InstanceFinder.ServerManager.Spawn(nob);
                var entity = instance.GetComponent<DestroyEntityCallback>();
                entity.OnServerStopped += HandleEntityDespawned;
                data.activeInstance = nob;
            } else {
                Debug.LogError($"Entity prefab {prefab.name} is missing NetworkObject component!");
                Destroy(instance);
                entityClientRefCount[persistentId] = 0; // Failed, reset ref count
                if (entityClientRefCount[persistentId] == 0) entityClientRefCount.Remove(persistentId);
                return;
            }
        }

        // --- Add requesting client as observer ---
        if (nob != null && nob.IsSpawned) // Check if instance exists and is spawned
        {
            //nob.AddObserver(requester);
            nob.Observers.Add(requester);
            //nob.NetworkObserver = requester;
            //Debug.Log($"Server: Added client {requester.ClientId} as observer for entity {persistentId}. Ref count: {currentRefCount}");
        } else if (nob == null && currentRefCount == 1) {
            // Should not happen if spawn logic above worked
            Debug.LogError($"Server: Entity {persistentId} instance failed to spawn or link correctly.");
        } else if (nob == null) {
            // Instance doesn't exist even though ref count > 1? Problem!
            Debug.LogError($"Server: Entity {persistentId} instance missing despite ref count {currentRefCount}!");
        }
    }


    [ServerRpc(RequireOwnership = false)]
    public void CmdRequestEntityDeactivation(ulong persistentId, NetworkConnection requester = null) {
        if (!IsServerInitialized) return;
        if (requester == null) return;

        PersistentEntityData data = persistentEntityDatabase[persistentId];
        // It's possible data is null if entity was just removed permanently
        if (data == null) {
            // Clean up ref count if somehow it exists for a removed entity
            entityClientRefCount.Remove(persistentId);
            return;
        }

        if (entityClientRefCount.TryGetValue(persistentId, out int currentRefCount)) {
            // --- Remove requesting client as observer ---
            NetworkObject nob = data.activeInstance;
            if (nob != null && nob.IsSpawned) {

                //Debug.Log($"Server: Removed client {requester.ClientId} as observer for entity {persistentId}.");
            }

            // --- Decrement ref count ---
            currentRefCount--;
            if (currentRefCount < 0) currentRefCount = 0; // Safety
            entityClientRefCount[persistentId] = currentRefCount;

            // --- Deactivate SERVER instance if ref count hits 0 ---
            if (currentRefCount == 0) {
                entityClientRefCount.Remove(persistentId); // Remove entry

                if (nob != null && nob.IsSpawned) {
                    // Save state just before despawning
                    UpdateDataFromInstance(nob, data);

                    // Unsubscribe FIRST
                    nob.GetComponent<DestroyEntityCallback>().OnServerStopped -= HandleEntityDespawned;

                    // Despawn server instance
                    Debug.Log($"Despawning {nob.name} for client {requester.ClientId}");
                    nob.Despawn(DespawnType.Destroy);
                    //InstanceFinder.ServerManager.Despawn(nob);
                    // Debug.Log($"Server: Despawned instance for entity {persistentId} as ref count hit 0.");
                    nob.Observers.Remove(requester);
                }
                // Clear link in persistent data regardless
                data.activeInstance = null;
            }
        } else {
            // Client requested deactivation for an entity server wasn't tracking refs for. Maybe already hit 0? Ignore.
            // Debug.LogWarning($"Server: Client {requester.ClientId} requested deactivation for entity {persistentId} with no active refs tracked.");
        }
    }

    // --- Modify entity removal to also clear from chunk map ---
    [Server]
    public void ServerRemovePersistentEntity(ulong persistentId) {
        if (!IsServerInitialized) return;
        if (persistentEntityDatabase.TryGetValue(persistentId, out PersistentEntityData data)) {
            // ... (Despawn active instance if needed) ...

            // --- Remove from chunk map ---
            //Vector2Int chunkCoord = chunkManager.WorldToChunkCoord(data.position);
            Vector2Int chunkCoord = chunkManager.CellToChunkCoord(data.cellPos);
            if (entityIdsByByChunkCoord.TryGetValue(chunkCoord, out List<ulong> list)) {
                list.Remove(persistentId);
            }
            // ---------------------------

            persistentEntityDatabase.Remove(persistentId);
            // Notify Entity Manager in case it needs to clean up ref count
            ForceDeactivation(persistentId);
        }
    }
    // --- Force Deactivation (e.g., when entity is permanently removed) ---
    [Server]
    public void ForceDeactivation(ulong persistentId) {
        // Deactivate instance if present
        if (entityClientRefCount.ContainsKey(persistentId)) {
            DeactivateEntity(persistentId);
        }
        // Ensure ref count is removed
        entityClientRefCount.Remove(persistentId);
        // TODO need to remove from this list: 
        if(cachedEntityInChunk.TryGetValue(persistentId, out var chunk)){
            cachedEntityIdsByChunk[chunk].Remove(persistentId);
        }
    }
    // Called when a NetworkObject tracked by this spawner is despawned/destroyed on the server
    private void HandleEntityDespawned(NetworkObject nob) {
        if (!IsServerInitialized) return; // Should only be invoked on server
        Debug.Log("StoppedTracking: " + nob.name);
        spawnedEntities.Remove(nob); // Stop tracking
        // Decrement the count for its prefab type
        // This requires knowing the original prefab. You might need to store this
        // association when spawning, or add a component to the entity telling its type.
        // Simplified Example: Assuming you add a component `EntityIdentity` to the prefab
        EntityIdentity identity = nob.GetComponent<EntityIdentity>(); // Create this component
        if (identity != null && identity.spawnData != null && identity.spawnData.entityPrefab != null) {
            GameObject prefabKey = identity.spawnData.entityPrefab;
            if (currentEntityCounts.TryGetValue(prefabKey, out int count)) {
                currentEntityCounts[prefabKey] = Mathf.Max(0, count - 1); // Decrement, ensure non-negative
            }   
        }  
        // ^^^ Up here is for runtime spawning, not using atm...
        // Unsubscribe to prevent memory leaks - VERY IMPORTANT
        var c = nob.GetComponent<DestroyEntityCallback>();
        c.OnServerStopped -= HandleEntityDespawned;
        if (c.IsDestroyedPermanently) {
            ulong persistentId = FindIdForInstance(nob);
            ServerRemovePersistentEntity(persistentId);
        }
    }
    // --- Deactivate (Despawn NetworkObject) ---
    private void DeactivateEntity(ulong persistentId) {
        if (!IsServerInitialized) return;

        if (persistentEntityDatabase.TryGetValue(persistentId, out PersistentEntityData data)) {
            if (data.activeInstance != null && data.activeInstance.IsSpawned) {
                // --- IMPORTANT: Update persistent data BEFORE despawning ---
                // Capture final state (position, health, etc.) from the instance
                UpdateDataFromInstance(data.activeInstance, data);

                // Remove the unexpected despawn listener FIRST to avoid issues
                data.activeInstance.GetComponent<DestroyEntityCallback>().OnServerStopped -= HandleEntityDespawned;
                InstanceFinder.ServerManager.Despawn(data.activeInstance); // Despawn it
            } else {
                // Instance was already null or despawned, ensure data ref is null
                data.activeInstance = null;
            }
            data.activeInstance = null; // Clear the link
            Debug.Log($"Deactivated entity {persistentId}");
        } else {
            Debug.LogWarning($"Cannot deactivate entity {persistentId}: Data not found");
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
    private void UpdateDataFromInstance(NetworkObject nob, PersistentEntityData data) {
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
    // --- Updating Entity Chunk Association (If Entities Move) ---
    public void ServerUpdateEntityChunkLocation(ulong persistentId, Vector3 oldPosition, Vector3 newPosition) {
        if (!IsServerInitialized) return;
        Vector2Int oldChunk = chunkManager.WorldToChunkCoord(oldPosition);
        Vector2Int newChunk = chunkManager.WorldToChunkCoord(newPosition);

        if (oldChunk != newChunk) {
            // Remove from old chunk's list
            if (entityIdsByByChunkCoord.TryGetValue(oldChunk, out List<ulong> oldList)) {
                oldList.Remove(persistentId);
            }
            // Add to new chunk's list
            if (!entityIdsByByChunkCoord.ContainsKey(newChunk)) {
                entityIdsByByChunkCoord[newChunk] = new List<ulong>();
            }
            entityIdsByByChunkCoord[newChunk].Add(persistentId);
            // Debug.Log($"Entity {persistentId} moved from chunk {oldChunk} to {newChunk}");
        }
    }

    // Helper to check if an entity is still needed by another loaded chunk
    private bool IsEntityStillNeededByOtherChunks(ulong entityId, Vector2Int chunkBeingUnloaded) {
        foreach (var kvp in cachedEntityIdsByChunk) {
            Vector2Int checkCoord = kvp.Key;
            List<ulong> idsInChunk = kvp.Value;

            // Skip the chunk being unloaded, only check other *active* chunks
            if (checkCoord != chunkBeingUnloaded && chunkManager.IsChunkActive(checkCoord)) {
                if (idsInChunk != null && idsInChunk.Contains(entityId)) {
                    return true; // Found in another active chunk
                }
            }
        }
        return false; // Not found in any other active chunk
    }

    // Helper needed for HandleUnexpectedDespawn
    private ulong FindIdForInstance(NetworkObject nob) {
        foreach (var kvp in persistentEntityDatabase) {
            if (kvp.Value.activeInstance == nob) return kvp.Key;
        }
        return 0;
    }
    // ==================================================
    // == HELPER FUNCTIONS for Entities (Called by Entity Components SERVER-SIDE) ==
    // ==================================================

    #region Entity helpers

    // --- Player Queries (Server-Side) ---
    [Server]
    public List<NetworkObject> GetPlayersNear(Vector3 position, float radius) {
        List<NetworkObject> nearbyPlayers = new List<NetworkObject>();
        if (InstanceFinder.ServerManager == null || !InstanceFinder.ServerManager.Started) return nearbyPlayers;

        float sqrRadius = radius * radius;
        foreach (NetworkConnection conn in InstanceFinder.ServerManager.Clients.Values) {
            if (conn.FirstObject != null) // FirstObject is usually the player's root Nob
           {
                // Use world generator's method which likely includes range check already
                // Or calculate here:
                if (Vector3.SqrMagnitude(conn.FirstObject.transform.position - position) <= sqrRadius) {
                    nearbyPlayers.Add(conn.FirstObject);
                }
            }
        }
        return nearbyPlayers;
    }

    // --- Entity Queries (Server-Side) ---
    [Server]
    public List<NetworkObject> GetActiveEntitiesNear(Vector3 position, float radius, ulong selfIdToExclude = 0) {
        List<NetworkObject> nearbyEntities = new List<NetworkObject>();
        float sqrRadius = radius * radius;
        // This still iterates the whole database, but only checks ACTIVE entities.
        // Optimization: Could use WorldGenerator's chunk map + proximity check if DB is huge.
        foreach (var kvp in persistentEntityDatabase) {
            PersistentEntityData data = kvp.Value;
            if (data.persistentId != selfIdToExclude && data.activeInstance != null && data.activeInstance.IsSpawned) {
                if (Vector3.SqrMagnitude(data.cellPos - position) <= sqrRadius) // Check against persistent position (more stable) or activeInstance.transform.position?
               {
                    nearbyEntities.Add(data.activeInstance);
                }
            }
        }
        return nearbyEntities;
    }
    // ==================================================
    // == NOTIFICATION FUNCTIONS (Called by other Managers SERVER-SIDE) ==
    // ==================================================

    [Server]
    public void NotifyTileChanged(Vector3Int changedCellPosition, Vector2Int chunkCoord, int newTileID) {
        List<ulong> candidateIds = GetEntityIDsByChunkCoord(chunkCoord);
        if (candidateIds == null) return; // No entities registered in this chunk
        candidateIds = candidateIds.ToList();
        float sqrNotifyRadius = 5 * 5; //default of 5 tiles now
        Vector3 changeWorldPos = worldManager.GetCellCenterWorld(changedCellPosition);

        foreach (ulong entityId in candidateIds) {
            PersistentEntityData entityData = persistentEntityDatabase[entityId];
            // Check if entity is ACTIVE and within notification radius
            if (entityData != null && entityData.activeInstance != null && entityData.activeInstance.IsSpawned) {
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

    // Generic helper to find and call methods on components implementing an interface
    [Server]
    private void NotifyEntityComponents<T>(NetworkObject targetNobo, System.Action<T> action) where T : class // Interface constraint
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

    #endregion
}
// --- Helper Component for Entities ---
// Attach this to your entity PREFABS
public class EntityIdentity : MonoBehaviour {
    public EntityBaseSO spawnData; // Assign the corresponding Spawn Data SO in the prefab inspector
}
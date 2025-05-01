using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using FishNet.Object; // For NetworkBehaviour and ServerManager access
using FishNet;       // For InstanceFinder

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
    private Dictionary<int, GameObject> idToPrefab = new Dictionary<int, GameObject>();

    // Key: Persistent Entity ID, Value: How many active chunks require it
    private Dictionary<ulong, int> entityActivationRefCount = new Dictionary<ulong, int>();
    private Dictionary<Vector2Int, List<ulong>> entityIdsByChunkCoord = new Dictionary<Vector2Int, List<ulong>>();
    private Dictionary<ulong, PersistentEntityData> persistentEntityDatabase = new Dictionary<ulong, PersistentEntityData>();
    private ulong nextPersistentEntityId = 1; // Counter for assigning unique IDs
    private ulong GetNextPersistentEntityId() { return nextPersistentEntityId++; }
    public List<ulong> GetEntityIDsByChunkCoord(Vector2Int chunkCoord) { 
        if(entityIdsByChunkCoord.TryGetValue(chunkCoord, out var Idlist)){
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
            GameObject instance = Instantiate(prefabToSpawn, spawnPos, Quaternion.identity);
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
    // Instead of spawning directly, it now adds to the persistent database
    public void ServerSpawnGeneratedEntities(Vector2Int chunkCoord, List<EntitySpawnInfo> entityList) {
        if (!IsServerInitialized || entityList == null || entityList.Count == 0) return;
        if (!entityIdsByChunkCoord.ContainsKey(chunkCoord)) {
            entityIdsByChunkCoord[chunkCoord] = new List<ulong>();
        }

        foreach (EntitySpawnInfo info in entityList) {
            if (!idToPrefab.ContainsKey(info.entityID)) {
                // new entry
                idToPrefab.Add(info.entityID, info.prefab);
            }
            // Add to the main persistent database
            PersistentEntityData newEntityData = ServerAddNewPersistentEntity(info.entityID, info.position, info.rotation, info.scale);
            if (newEntityData != null) {
                // Add the ID to this chunk's list
                entityIdsByChunkCoord[chunkCoord].Add(newEntityData.persistentId);
            }
        }
    }
    public PersistentEntityData ServerAddNewPersistentEntity(int id, Vector3 pos, Quaternion rot, Vector3 scale) {
        ulong unqiueID = GetNextPersistentEntityId();
        PersistentEntityData newEntityData = new PersistentEntityData(unqiueID, id, pos, rot, scale);
        persistentEntityDatabase.Add(unqiueID, newEntityData);
        Debug.Log($"Added new persistent entity ID:{unqiueID} at {pos}");
        return newEntityData; // Return the created data
    }
    // --- Called by WorldGenerator when a chunk activating NEEDS this entity ---
    [Server] // Decorator reinforces server-only execution
    public void IncrementActivationRef(ulong persistentId) {
        if (!persistentEntityDatabase.ContainsKey(persistentId)) {
            // This might happen if entity was removed just before activation notification
            Debug.LogWarning($"EntityManager: Tried to increment ref for unknown entity ID {persistentId}");
            return;
        }

        entityActivationRefCount.TryGetValue(persistentId, out int currentRefCount);
        currentRefCount++;
        entityActivationRefCount[persistentId] = currentRefCount;

        // If count was 0, it's now 1 -> Activate the entity instance
        if (currentRefCount == 1) {
            ActivateEntity(persistentId);
        }
    }

    // --- Called by WorldGenerator when a chunk deactivating NO LONGER NEEDS this entity ---
    [Server]
    public void DecrementActivationRef(ulong persistentId) {
        if (entityActivationRefCount.TryGetValue(persistentId, out int currentRefCount)) {
            currentRefCount--;
            if (currentRefCount < 0) {
                Debug.LogError($"Entity {persistentId} ref count dropped below zero!");
                currentRefCount = 0; // Prevent negative counts
            }
            entityActivationRefCount[persistentId] = currentRefCount;

            // If count is now 0 -> Deactivate the entity instance
            if (currentRefCount == 0) {
                DeactivateEntity(persistentId);
                // Optionally remove from dictionary to save memory if count is 0
                entityActivationRefCount.Remove(persistentId);
            }
        } else {
            // This could happen if Decrement is called before Increment, or after forced removal
            // Debug.LogWarning($"EntityManager: Tried to decrement ref for entity ID {persistentId} which had no active refs.");
        }
    }

    // --- Force Deactivation (e.g., when entity is permanently removed) ---
    [Server]
    public void ForceDeactivation(ulong persistentId) {
        // Deactivate instance if present
        if (entityActivationRefCount.ContainsKey(persistentId)) {
            DeactivateEntity(persistentId);
        }
        // Ensure ref count is removed
        entityActivationRefCount.Remove(persistentId);
    }
    // Called when a NetworkObject tracked by this spawner is despawned/destroyed on the server
    private void HandleEntityDespawned(NetworkObject nob) {
        if (!IsServer) return; // Should only be invoked on server

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

        // Unsubscribe to prevent memory leaks - VERY IMPORTANT
        //nob.OnStopServer -= HandleEntityDespawned; // Use -= operator

        // Debug.Log($"Handled despawn for {nob.name}");
    }  // --- Activate (Spawn NetworkObject) ---
    private void ActivateEntity(ulong persistentId) {
        if (!IsServerInitialized) return;

        if (persistentEntityDatabase.TryGetValue(persistentId, out PersistentEntityData data)) {
            if (data.activeInstance != null) {
                // Already active? Log warning or ignore.
                Debug.LogWarning($"Attempted to activate entity {persistentId} which is already active.");
                return;
            }

            // Get prefab based on type ID
            GameObject prefab = idToPrefab[data.entityID];
            if (prefab == null) {
                Debug.LogError($"Cannot activate entity {persistentId}: Prefab not found for type ID {data.entityID}");
                return;
            }

            // Instantiate and spawn
            GameObject instance = Instantiate(prefab, data.position, data.rotation);
            instance.transform.localScale = data.scale;
            NetworkObject nob = instance.GetComponent<NetworkObject>();

            if (nob != null) {
                // --- IMPORTANT: Sync State BEFORE spawning ---
                ApplyDataToInstance(instance, data); // Apply health, growth etc.

                InstanceFinder.ServerManager.Spawn(nob); // Spawn it

                data.activeInstance = nob; // Link data to instance
                // Add despawn listener specific to activation/deactivation cycle
                // We NEED to know if it despawned for reasons OTHER than our DeactivateEntity call
                //nob.OnStopServer += HandleUnexpectedDespawn; // todo add this later if it becomes a problem

                Debug.Log($"Activated entity {persistentId} ({prefab.name})");
            } else {
                Debug.LogError($"Entity prefab {prefab.name} (TypeID {data.entityID}) is missing NetworkObject component!");
                Destroy(instance);
            }
        } else {
            Debug.LogWarning($"Cannot activate entity {persistentId}: Data not found in database.");
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
                //data.activeInstance.OnStopServer -= HandleUnexpectedDespawn;
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
        data.position = nob.transform.position;
        data.rotation = nob.transform.rotation;
        data.scale = nob.transform.localScale;
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
            if (entityIdsByChunkCoord.TryGetValue(oldChunk, out List<ulong> oldList)) {
                oldList.Remove(persistentId);
            }
            // Add to new chunk's list
            if (!entityIdsByChunkCoord.ContainsKey(newChunk)) {
                entityIdsByChunkCoord[newChunk] = new List<ulong>();
            }
            entityIdsByChunkCoord[newChunk].Add(persistentId);
            // Debug.Log($"Entity {persistentId} moved from chunk {oldChunk} to {newChunk}");
        }
    }
    // --- Modify entity removal to also clear from chunk map ---
    public void ServerRemovePersistentEntity(ulong persistentId) {
        if (!IsServerInitialized) return;
        if (persistentEntityDatabase.TryGetValue(persistentId, out PersistentEntityData data)) {
            // ... (Despawn active instance if needed) ...

            // --- Remove from chunk map ---
            Vector2Int chunkCoord = chunkManager.WorldToChunkCoord(data.position);
            if (entityIdsByChunkCoord.TryGetValue(chunkCoord, out List<ulong> list)) {
                list.Remove(persistentId);
            }
            // ---------------------------

            persistentEntityDatabase.Remove(persistentId);
            // Notify Entity Manager in case it needs to clean up ref count
            ForceDeactivation(persistentId);
        }
    }
}

// --- Helper Component for Entities ---
// Attach this to your entity PREFABS
public class EntityIdentity : MonoBehaviour {
    public EntityBaseSO spawnData; // Assign the corresponding Spawn Data SO in the prefab inspector
}
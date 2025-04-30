using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using FishNet.Object; // For NetworkBehaviour and ServerManager access
using FishNet;       // For InstanceFinder

public class EntitySpawner : NetworkBehaviour // Needs to be NetworkBehaviour to use ServerManager etc.
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
    [SerializeField] private int maxTotalEntities = 100; // Global cap for all spawned entities managed by this spawner
    [SerializeField] private float playerDespawnRange = 120f; // Despawn entities further than this from *any* player

    // Server-side tracking
    private Dictionary<GameObject, int> currentEntityCounts = new Dictionary<GameObject, int>(); // Key: Prefab, Value: Count
    private List<NetworkObject> spawnedEntities = new List<NetworkObject>(); // Track instances for despawning

    public override void OnStartServer() {
        base.OnStartServer();
        if (chunkManager == null) chunkManager = FindFirstObjectByType<ChunkManager>();
        if (biomeManager == null) biomeManager = worldManager.BiomeManager;
        if (chunkManager == null || biomeManager == null) {
            Debug.LogError("EntitySpawner cannot find WorldGenerator or BiomeManager! Disabling.");
            enabled = false; // Disable the spawner component
            return;
        }
        StartCoroutine(SpawnCheckLoop());
        StartCoroutine(DespawnCheckLoop());
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
    // NEW public method for spawning specific, pre-defined entities
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

}

// --- Helper Component for Entities ---
// Attach this to your entity PREFABS
public class EntityIdentity : MonoBehaviour {
    public EntityBaseSO spawnData; // Assign the corresponding Spawn Data SO in the prefab inspector
}
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.Collections;
using FishNet.Object;
using System;

// Data structure to hold calculated biome info for a chunk
public class BiomeChunkInfo {
    // Store counts first, calculate rates/dominant later if needed
    public Dictionary<BiomeType, int> biomeCounts = new Dictionary<BiomeType, int>();
    public BiomeType dominantBiome = BiomeType.None;
    public int totalTilesCounted = 0; // For calculating percentages

    public void Clear() {
        biomeCounts.Clear();
        dominantBiome = BiomeType.None;
        totalTilesCounted = 0;
    }

    public void RecalculateDominantBiome() {
        if (totalTilesCounted == 0) {
            dominantBiome = BiomeType.None;
            return;
        }

        BiomeType currentDominant = BiomeType.None;
        int maxCount = -1;

        foreach (var kvp in biomeCounts) {
            if (kvp.Value > maxCount) {
                maxCount = kvp.Value;
                currentDominant = kvp.Key;
            }
        }
        dominantBiome = currentDominant;
    }

    public float GetBiomeRate(BiomeType type) {
        if (totalTilesCounted == 0) return 0f;
        if (biomeCounts.TryGetValue(type, out int count)) {
            return (float)count / totalTilesCounted;
        }
        return 0f;
    }
}

public class BiomeManager : NetworkBehaviour 
{
    private WorldManager _worldManager;
    private ChunkManager _chunkManager;
    private int chunkSize;
    public event Action<BiomeType, BiomeType> OnNewClientBiome;
    // Server-side cache of calculated biome data per chunk
    private Dictionary<Vector2Int, BiomeChunkInfo> serverBiomeData = new Dictionary<Vector2Int, BiomeChunkInfo>();
    private BiomeType _currentClientBiome;
    public BiomeType GetCurrentClientBiome() => _currentClientBiome;
    // DIRECTIONS to look for neighbours (4-way). 
    private static readonly Vector2Int[] NeighbourDirs = {
        new Vector2Int( 1,  0),
        new Vector2Int(-1,  0),
        new Vector2Int( 0,  1),
        new Vector2Int( 0, -1),
    };
    public void SetWorldManager(WorldManager parent) {
        _worldManager = parent;
        _chunkManager = parent.ChunkManager;
        chunkSize = _worldManager.GetChunkSize();
    }
    public override void OnStartClient() {
        base.OnStartClient();
        if(!IsOwner) {
            enabled = false; 
            return;
        }
            StartCoroutine(ClientMovingRoutine());
    }

    private IEnumerator ClientMovingRoutine() {
        var checkInterval = 0.2f;
        Vector2Int clientCurrentChunkCoord = new Vector2Int(int.MinValue, int.MinValue);
        // Wait until the player object owned by this client is spawned and available
        yield return new WaitUntil(() => NetworkedPlayer.LocalInstance != null);
        Transform localPlayerTransform = NetworkedPlayer.LocalInstance.transform;
        while (true) {
            if (localPlayerTransform == null) { // Safety check if player despawns
                yield return new WaitForSeconds(checkInterval);
                continue;
            }
            // Change light depending on player biome
            Vector2Int newClientChunkCoord = _chunkManager.WorldToChunkCoord(localPlayerTransform.position);
            if (newClientChunkCoord != clientCurrentChunkCoord) {
                //Debug.Log($"New client chunkcoord, it was: {clientCurrentChunkCoord} now it is: {newClientChunkCoord}");
                clientCurrentChunkCoord = newClientChunkCoord;
                var newBiome = _worldManager.BiomeManager.GetBiomeInfo(clientCurrentChunkCoord);
                if (newBiome == null) {
                    yield return new WaitForSeconds(checkInterval);
                    continue;
                }
                if (_currentClientBiome != newBiome.dominantBiome) {
                    // Only set if we are in a biome that we know of
                    if (newBiome.dominantBiome != BiomeType.None) {
                        OnNewClientBiome?.Invoke(_currentClientBiome, newBiome.dominantBiome);
                        _currentClientBiome = newBiome.dominantBiome;
                    }
                }
            }
            yield return new WaitForSeconds(checkInterval);
        }
    }
    // Method called by WorldGenerator when a chunk is generated/loaded/modified on server
    public void CalculateBiomeForChunk(Vector2Int chunkCoord, ChunkData chunkData) {
        if (chunkData == null || chunkData.tiles == null) {
            Debug.LogWarning($"BiomeManager: Cannot calculate biome for {chunkCoord}, chunk data is null.");
            return;
        }

        if (!serverBiomeData.TryGetValue(chunkCoord, out BiomeChunkInfo biomeInfo)) {
            biomeInfo = new BiomeChunkInfo();
            serverBiomeData[chunkCoord] = biomeInfo;
        }
        biomeInfo.Clear();

        for (int x = 0; x < chunkSize; x++) {
            for (int y = 0; y < chunkSize; y++) {
                ushort tileID = chunkData.tiles[x, y];
                // TODO this should now depend on the Biome ID in ChunkData

                /*if (tileBase is TileSO customTile && customTile.associatedBiome != BiomeType.None) {
                    BiomeType biome = customTile.associatedBiome;
                    if (!biomeInfo.biomeCounts.ContainsKey(biome)) {
                        biomeInfo.biomeCounts[biome] = 0;
                    }
                    biomeInfo.biomeCounts[biome]++;
                    biomeInfo.totalTilesCounted++;
                }*/
                // Optionally count 'None' biome tiles if needed, or just count contributing tiles
            }
        }

        // Optional: Pre-calculate the dominant biome after counting
        biomeInfo.RecalculateDominantBiome();
        PropagateNeighbourOverrides(chunkCoord);
        // new: look at neighbours and apply any override rules
       // BiomeType final = ApplyNeighbourRules(chunkCoord, biomeInfo.dominantBiome);

       // if (final != biomeInfo.dominantBiome) {
        //    Debug.Log($"Biome at {chunkCoord} overridden from {biomeInfo.dominantBiome} to {final}");
        //    biomeInfo.dominantBiome = final;
            // if you need counts to reflect that override:
            // biomeInfo.biomeCounts[final] = biomeInfo.totalTilesCounted;
       // }
       // Debug.Log($"Biome calculated for chunk {chunkCoord}. Dominant: {biomeInfo.dominantBiome}");
    }

    /// <summary>
    /// Starting from the given chunk, repeatedly re-evaluate override rules
    /// on it and any neighbour whose biome flips, until no more changes occur.
    /// </summary>
    private void PropagateNeighbourOverrides(Vector2Int start) {
        var visited = new HashSet<Vector2Int>();
        var toProcess = new Queue<Vector2Int>();
        toProcess.Enqueue(start);
        visited.Add(start);

        while (toProcess.Count > 0) {
            var coord = toProcess.Dequeue();
            var info = GetBiomeInfo(coord);
            if (info == null)
                continue;               // no data -> skip

            var original = info.dominantBiome;
            var corrected = ApplyNeighbourRules(coord, original);

            if (corrected != original) {
                // commit override
                info.dominantBiome = corrected;
                Debug.Log($"[{coord}] overridden {original} -> {corrected}");

                // now any neighbour might also flip, so enqueue them
                foreach (var d in NeighbourDirs) {
                    var nc = coord + d;
                    if (!visited.Contains(nc) && GetBiomeInfo(nc) != null) {
                        visited.Add(nc);
                        toProcess.Enqueue(nc);
                    }
                }
            }
        }
    }
    /// <summary>
    /// Checks each neighbour’s dominant biome and applies any override rules.
    /// </summary>
    private BiomeType ApplyNeighbourRules(Vector2Int chunkCoord, BiomeType original) {

        // get all neighbour biomes
        foreach (var d in NeighbourDirs) {
            var neighbourCoord = chunkCoord + d;
            var info = GetBiomeInfo(neighbourCoord);
            if (info == null)
                continue;

            if (original == BiomeType.Cave && info.dominantBiome == BiomeType.Trench) {
                return BiomeType.Trench;
            }
        }

        // no rule fired, stay the same
        return original;
    }
    // Method for the Entity Spawner (or other server systems) to query biome data
    public BiomeChunkInfo GetBiomeInfo(Vector2Int chunkCoord) {
        // --- SERVER ONLY ---
        // Add server check if this is a NetworkBehaviour: if (!IsServer) return null;

        if (serverBiomeData.TryGetValue(chunkCoord, out BiomeChunkInfo biomeInfo)) {
            return biomeInfo;
        }

        // Optionally: If data doesn't exist, try to calculate it on demand?
        // Requires access to WorldGenerator's GetChunkData method.
        // ChunkData data = worldGenerator.GetChunkData(chunkCoord); // Assume this exists
        // if(data != null && data.hasBeenGenerated) {
        //      CalculateBiomeForChunk(chunkCoord, data);
        //      serverBiomeData.TryGetValue(chunkCoord, out biomeInfo); // Try getting again
        //      return biomeInfo;
        // }

        //Debug.LogWarning($"BiomeManager: No biome data found for chunk {chunkCoord}.");
        return null; // No data available
    }

    // Clear data if world is reset/unloaded
    public void ClearAllBiomeData() {
        serverBiomeData.Clear();
    }
    public Dictionary<Vector2Int, BiomeChunkInfo> GetAllBiomeData() {
        return new Dictionary<Vector2Int, BiomeChunkInfo>(serverBiomeData);
    }

    internal void AddNewData(Vector2Int chunkCoord, ChunkData data) {
        // Create a fresh info object
        //Debug.Log("added new data to biomemanager");
        var info = new BiomeChunkInfo();

        byte[,] ids = data.biomeID;
        int width = ids.GetLength(0);
        int height = ids.GetLength(1);

        // 1) Count every tile’s biome
        for (int x = 0; x < width; x++) {
            for (int y = 0; y < height; y++) {
                // Cast your byte ID into the BiomeType enum
                BiomeType biome = (BiomeType)ids[x, y];

                // Increment total tiles counter
                info.totalTilesCounted++;

                // Add or update the count for this biome
                if (info.biomeCounts.ContainsKey(biome))
                    info.biomeCounts[biome]++;
                else
                    info.biomeCounts[biome] = 1;
            }
        }

        // 2) Figure out which biome is dominant
        if (info.totalTilesCounted > 0) {
            // Order by count descending and pick the first key
            info.dominantBiome = info.biomeCounts
                .OrderByDescending(kvp => kvp.Value)
                .First()
                .Key;
        } else {
            info.dominantBiome = BiomeType.None;
        }

        // 3) Store it in chunk lookup 
        serverBiomeData[chunkCoord] = info;
    }
}
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Tilemaps; // Required for NetworkBehaviour if attached to one

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

public class BiomeManager : MonoBehaviour 
{
    private WorldManager worldManager; 
    private int chunkSize; 

    // Server-side cache of calculated biome data per chunk
    private Dictionary<Vector2Int, BiomeChunkInfo> serverBiomeData = new Dictionary<Vector2Int, BiomeChunkInfo>();
    
    // DIRECTIONS to look for neighbours (4-way). 
    private static readonly Vector2Int[] NeighbourDirs = {
        new Vector2Int( 1,  0),
        new Vector2Int(-1,  0),
        new Vector2Int( 0,  1),
        new Vector2Int( 0, -1),
    };
    public void SetWorldManager(WorldManager parent) {
        worldManager = parent;
        chunkSize = worldManager.GetChunkSize();
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
                TileBase tileBase = chunkData.tiles[x, y];
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
}
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

public class BiomeManager : StaticInstance<BiomeManager>
{
    private WorldManager _worldManager;
    private ChunkManager _chunkManager;
    private int chunkSize;
    public event Action<BiomeType, BiomeType> OnNewClientBiome;
    // Server-side cache of calculated biome data per chunk
    private Dictionary<Vector2Int, BiomeChunkInfo> serverBiomeData = new Dictionary<Vector2Int, BiomeChunkInfo>();
    private BiomeType _currentClientBiome;
    public BiomeType GetCurrentClientBiome() => _currentClientBiome;

    public void Init(WorldManager parent) {
        _worldManager = parent;
        _chunkManager = parent.ChunkManager;
        chunkSize = _worldManager.GetChunkSize();
    }
    // Not having multiplayer atm 
    //public override void OnStartClient() {
    //    base.OnStartClient();
    //    // Ownership is checked in coroutine
    //    StartCoroutine(ClientMovingRoutine());
    //}
    //protected override void Awake() {
    //    base.Awake();
    //    StartCoroutine(ClientMovingRoutine());
    //    StartCoroutine(Routine());
    //}

    // For some fucking reason if we have this in awake it never fucking finds NEtworkedPlayer.LocalInstance!?!?
    private void Start() {
        StartCoroutine(ClientMovingRoutine());
    }

    private IEnumerator ClientMovingRoutine() {
        var checkInterval = 0.2f;
        Vector2Int clientCurrentChunkCoord = new Vector2Int(int.MinValue, int.MinValue);
        // Wait until the player object owned by this client is spawned and available
        //Debug.Log("starting biome moving routine...");
        //yield return new WaitUntil(() => base.Owner != null && NetworkedPlayer.LocalInstance != null); 
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

                // TODO the client wont have access to server data, so this wont work on non host clients!!!!!
                var newBiome = GetBiomeInfo(clientCurrentChunkCoord);
                Debug.Log("checking biome...");
                if (newBiome == null) {
                    yield return new WaitForSeconds(checkInterval);
                    continue;
                }
                if (_currentClientBiome != newBiome.dominantBiome) {
                    // Only set if we are in a biome that we know of
                    if (newBiome.dominantBiome != BiomeType.None) {
                        Debug.Log("Entered new biome!: " + newBiome.dominantBiome.ToString());
                        OnNewClientBiome?.Invoke(_currentClientBiome, newBiome.dominantBiome);
                        _currentClientBiome = newBiome.dominantBiome;
                    }
                }
            }
            yield return new WaitForSeconds(checkInterval);
        }
    }
  

    // Method for the Entity Spawner (or other server systems) to query biome data
    public BiomeChunkInfo GetBiomeInfo(Vector2Int chunkCoord) {
        // --- SERVER ONLY ---
        // Add server check if this is a NetworkBehaviour: if (!IsServer) return null;

        if (serverBiomeData.TryGetValue(chunkCoord, out BiomeChunkInfo biomeInfo)) {
            Debug.Log("Fetched new biome info, its: " +  biomeInfo.dominantBiome.ToString());
            return biomeInfo;
        }

        Debug.LogWarning($"BiomeManager: No biome data found for chunk {chunkCoord}.");
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
        if (serverBiomeData.ContainsKey(chunkCoord)) {
            //Debug.Log("changed existing data in biomemanager");
            serverBiomeData[chunkCoord] = info;
        } else {
            //Debug.Log("added new data to biomemanager");
            serverBiomeData.Add(chunkCoord, info);

        }
    }
}
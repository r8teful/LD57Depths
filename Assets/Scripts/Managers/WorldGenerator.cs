using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections;
using System.Collections.Generic;
using FishNet.Object;
using FishNet.Connection;
using Newtonsoft.Json;
using System.IO;
using Sirenix.OdinInspector;
using Unity.Mathematics;


[System.Serializable]
public struct BiomeLayer {
    public string name;
    public int startY; // Y-coordinate where this biome begins (inclusive)
    public int endY;   // Y-coordinate where this biome ends (exclusive)
    public TileBase defaultGroundTile;
    [Range(0f, 1f)] public float biomeBlendSharpness; // How quickly it blends (optional, see noise blending)

    // Add biome-specific parameters if needed (e.g., specific ore likelihoods)
}

[System.Serializable]
public struct OreType {
    public string name;
    public TileBase tile;
    public List<string> allowedBiomeNames; // Names of biomes where this ore can spawn
    public float frequency;     // Noise frequency for this ore
    [Range(0f, 1f)] public float threshold; // Noise value above which ore spawns (higher = rarer)
    public float clusterFrequency; // Lower frequency noise for controlling large clusters
    [Range(0f, 1f)] public float clusterThreshold; // Threshold for cluster noise
    public bool requireCluster; // Must the cluster noise also be above threshold?
}

[System.Serializable]
public struct DeterministicStructure {
    public string name;
    public int yLevel; // Exact Y level where this structure element appears
    public int minX;   // Min X range relative to trench center (0)
    public int maxX;   // Max X range relative to trench center (0)
    public TileBase structureTile;
    // Optional: Add pattern information if it's not just a single line
}
// Represents the runtime data for a single chunk (tile references)
public class ChunkData {
    public TileBase[,] tiles; // Store references to TileBase assets
    public bool isModified = false; // Flag to track if chunk has changed since load/generation
    public bool hasBeenGenerated = false; // Flag to prevent regenerating loaded chunks

    public ChunkData(int chunkSizeX, int chunkSizeY) {
        tiles = new TileBase[chunkSizeX, chunkSizeY];
    }
}
[System.Serializable]
public class ChunkSaveData {
    public List<int> tileIds; // Flattened list of Tile IDs

    public ChunkSaveData() { tileIds = new List<int>(); }
    public ChunkSaveData(int capacity) { tileIds = new List<int>(capacity); }
}

// Top-level container for the entire world save data
[System.Serializable]
public class WorldSaveData {
    // Use string keys for JSON compatibility across different serializers more easily
    public Dictionary<string, ChunkSaveData> savedChunks;
    public Vector3 playerPosition; // Save player position too

    public WorldSaveData() {
        savedChunks = new Dictionary<string, ChunkSaveData>();
    }
}

public class WorldGenerator : NetworkBehaviour {
    [SerializeField] private bool useSave = false;
    [FoldoutGroup("World Settings")]
    [SerializeField] private int worldWidth = 2000; // Total world size in tiles
    [FoldoutGroup("World Settings")]
    [SerializeField] private int worldHeight = 500;
    [FoldoutGroup("World Settings")]
    [SerializeField] private int chunkSize = 16; // Size of chunks (16x16 tiles) - Power of 2 often good
                                                 // Note: World dimensions don't strictly need to be multiples of chunk size, but edge handling is easier if they are.
    [FoldoutGroup("World Gen")]
    public int seed = 12345;

    [FoldoutGroup("World Gen")]
    public float trenchBaseWidth = 5f;
    [FoldoutGroup("World Gen")]
    public float trenchWidenFactor = 0.1f; // How much wider per unit Y increase
    [FoldoutGroup("World Gen")]
    public float trenchEdgeNoiseFrequency = 0.05f;
    [FoldoutGroup("World Gen")]
    public float trenchEdgeNoiseAmplitude = 2f;

    [FoldoutGroup("World Gen")]
    public List<BiomeLayer> biomeLayers = new List<BiomeLayer>();

    [FoldoutGroup("World Gen")]
    public bool generateCaves = true;
    [FoldoutGroup("World Gen")]
    public float caveFrequency = 0.08f;
    [FoldoutGroup("World Gen")]
    [Range(0f, 1f)] public float caveThreshold = 0.6f; // Noise values above this become caves

    [FoldoutGroup("World Gen")]
    public List<OreType> oreTypes = new List<OreType>();

    [FoldoutGroup("World Gen")]
    public List<DeterministicStructure> structures = new List<DeterministicStructure>();
    // Noise instance (using Unity.Mathematics)
    private Unity.Mathematics.Random noiseRandomGen;
    private float seedOffsetX;
    private float seedOffsetY;
    [FoldoutGroup("Tilemap & Tiles")]
    [SerializeField] private List<TileBase> tileAssets; // Assign ALL your TileBase assets here in order
    [FoldoutGroup("Tilemap & Tiles")]
    [SerializeField] private Tilemap mainTilemap; // Assign your ground Tilemap GameObject here
    [FoldoutGroup("Tilemap & Tiles")]
     
    // Add more TileBase fields for other tile types

    [SerializeField] private int surfaceLevel = 400; // Y level for the surface
    [SerializeField] private float noiseScale = 0.05f; // For Perlin noise terrain generation
    [SerializeField] private int stoneDepth = 50;    // How far below the surface stone starts appearing

    [Header("Player & Loading")]
    [SerializeField] private Transform playerTransform; // Assign the player's transform
    [SerializeField] private int loadDistance = 3; // How many chunks away from the player to load (e.g., 3 means a 7x7 area around the player's chunk)
    [SerializeField] private float checkInterval = 0.5f; // How often (seconds) to check for loading/unloading chunks
    [SerializeField] private string saveFileName = "world.json"; // Name of the save file
    // --- Runtime Data ---
    private Dictionary<Vector2Int, ChunkData> worldChunks = new Dictionary<Vector2Int, ChunkData>();
    private HashSet<Vector2Int> activeChunks = new HashSet<Vector2Int>();
    private Vector2Int currentPlayerChunkCoord = new Vector2Int(int.MinValue, int.MinValue);

    // --- Tile ID Mapping ---
    private Dictionary<TileBase, int> tileAssetToIdMap = new Dictionary<TileBase, int>();
    private Dictionary<int, TileBase> idToTileAssetMap = new Dictionary<int, TileBase>();


    [FoldoutGroup("World Gen")]
    [Button("NewWorld")]
    private void DEBUGNewGen() {
        worldChunks.Clear();
        activeChunks.Clear();
        currentPlayerChunkCoord = default;
        DebugForceChunkLoad();
        InitializeNoise();
    }
    private void DebugForceChunkLoad() {
        Vector2Int newClientChunkCoord = WorldToChunkCoord(PlayerController.LocalInstance.transform.position);
        for (int xOffset = -loadDistance; xOffset <= loadDistance; xOffset++) {
            for (int yOffset = -loadDistance; yOffset <= loadDistance; yOffset++) {
                Vector2Int chunkCoord = new Vector2Int(newClientChunkCoord.x + xOffset, newClientChunkCoord.y + yOffset);
                ServerRequestChunkData(chunkCoord); // Send RPC request
                
            }
        }
    }
    public override void OnStartServer() {
        base.OnStartServer();
        // Server-only initialization
        InitializeTileMapping();
        InitializeNoise();

        biomeLayers.Sort((a, b) => a.startY.CompareTo(b.startY));
        if (useSave) LoadWorld(); // Load happens only on server

        //StartCoroutine(ServerChunkManagementRoutine()); // Not using atm
    }
    public override void OnStartClient() {
        base.OnStartClient();
        InitializeTileMapping(); // Clients also need the ID maps to interpret RPCs
        Debug.Log("Start client");
        mainTilemap.ClearAllTiles(); // Start with a clear visual map
         // Clients need to know their own position to request chunks
        StartCoroutine(ClientChunkLoadingRoutine());
        
    }

    // --- Initialization ---
    void InitializeTileMapping() {
        tileAssetToIdMap.Clear();
        idToTileAssetMap.Clear();
        // Assign IDs based on the order in the tileAssets list
        for (int i = 0; i < tileAssets.Count; i++) {
            if (tileAssets[i] == null) continue; // Skip null entries in the list itself
;
            if (!tileAssetToIdMap.ContainsKey(tileAssets[i])) {
                tileAssetToIdMap.Add(tileAssets[i], i);
                idToTileAssetMap.Add(i, tileAssets[i]);
                Debug.Log($"Mapped Tile: {tileAssets[i].name} to ID: {i}");
            } else {
                Debug.LogWarning($"Duplicate TileBase '{tileAssets[i].name}' detected in tileAssets list. Only the first instance will be used for ID mapping.");
            }
        }
    }
    IEnumerator ServerChunkManagementRoutine() {
        // Ensure this only runs on the server
        if (!IsServerInitialized) yield break;

        // Need to track *all* player positions on the server
        while (true) {
            // We aren't doing anything here right now because when a player goes into a zone with no chunk it will request it to the server
            // This is more efficient as it saves us checking for player positions all the time
            yield return new WaitForSeconds(checkInterval * 2); // Can check less often server-side maybe
            // Potentially pre-generate chunks around players proactively here if needed
        }
    }
    // --- Client-Side Chunk VISUAL Loading ---
    // This routine runs on each client (including host) to manage visuals
    IEnumerator ClientChunkLoadingRoutine() {
        Debug.Log("Chunk loading");
        HashSet<Vector2Int> clientActiveVisualChunks = new HashSet<Vector2Int>();
        Vector2Int clientCurrentChunkCoord = new Vector2Int(int.MinValue, int.MinValue);

        // Wait until the player object owned by this client is spawned and available
        // This assumes your player spawn logic is handled correctly by FishNet
        //yield return new WaitUntil(() => base.Owner != null && base.Owner.IsActive && base.Owner.IsLocalClient && PlayerController.LocalInstance != null); // Assumes a static LocalInstance on your PlayerControll
        yield return new WaitUntil(() => base.Owner != null && PlayerController.LocalInstance != null); // Assumes a static LocalInstance on your PlayerController

        Transform localPlayerTransform = PlayerController.LocalInstance.transform; // Get the locally controlled player

        while (true) {
            if (localPlayerTransform == null) { // Safety check if player despawns
                yield return new WaitForSeconds(checkInterval);
                continue;
            }

            Vector2Int newClientChunkCoord = WorldToChunkCoord(localPlayerTransform.position);

            if (newClientChunkCoord != clientCurrentChunkCoord) {
                clientCurrentChunkCoord = newClientChunkCoord;

                HashSet<Vector2Int> previouslyActive = new HashSet<Vector2Int>(clientActiveVisualChunks);
                HashSet<Vector2Int> requiredVisuals = new HashSet<Vector2Int>();

                for (int xOffset = -loadDistance; xOffset <= loadDistance; xOffset++) {
                    for (int yOffset = -loadDistance; yOffset <= loadDistance; yOffset++) {
                        Vector2Int chunkCoord = new Vector2Int(clientCurrentChunkCoord.x + xOffset, clientCurrentChunkCoord.y + yOffset);
                        requiredVisuals.Add(chunkCoord);

                        if (!clientActiveVisualChunks.Contains(chunkCoord)) {
                            // NEW: Request chunk data from server if we don't have it visually
                            ServerRequestChunkData(chunkCoord); // Send RPC request
                            clientActiveVisualChunks.Add(chunkCoord); // Assume we *will* get data
                        }
                    }
                }

                // Visually deactivate chunks we no longer need
                previouslyActive.ExceptWith(requiredVisuals);
                foreach (Vector2Int coord in previouslyActive) {
                    // No need to tell the server, just clear local visuals
                    ClientDeactivateVisualChunk(coord);
                    clientActiveVisualChunks.Remove(coord);
                }
            }
            yield return new WaitForSeconds(checkInterval);
        }
    }
    // --- Server RPC for Chunk Request ---
    [ServerRpc(RequireOwnership = false)] // Allow any client to request
    private void ServerRequestChunkData(Vector2Int chunkCoord, NetworkConnection requester = null) // FishNet automatically provides requester
    {
        // 1. Check if data exists on server
        if (!worldChunks.TryGetValue(chunkCoord, out ChunkData chunkData)) {
            // 2. If not, generate it (server only)
            chunkData = ServerGenerateChunkData(chunkCoord);
            if (chunkData == null) {
                Debug.LogError($"Server failed to generate requested chunk {chunkCoord}");
                // Optionally send an error back to client?
                return;
            }
        }

        // 3. Serialize chunk data into Tile IDs
        if (chunkData.tiles != null) {
            List<int> tileIds = new List<int>(chunkSize * chunkSize);
            for (int y = 0; y < chunkSize; y++) {
                for (int x = 0; x < chunkSize; x++) {
                    tileIds.Add(GetTileId(chunkData.tiles[x, y]));
                }
            }

            // 4. Send data back to the SPECIFIC client who requested it
            TargetReceiveChunkData(requester, chunkCoord, tileIds);
        }
    }
    // --- Target RPC to send chunk data to a specific client ---
    [TargetRpc]
    private void TargetReceiveChunkData(NetworkConnection conn, Vector2Int chunkCoord, List<int> tileIds) {
        // Executed ONLY on the client specified by 'conn'
        if (tileIds == null || tileIds.Count != chunkSize * chunkSize) {
            Debug.LogWarning($"Received invalid tile data for chunk {chunkCoord} from server.");
            return;
        }

        // Apply the received tiles visually
        Vector3Int chunkOriginCell = ChunkCoordToCellOrigin(chunkCoord);
        BoundsInt chunkBounds = new BoundsInt(chunkOriginCell.x, chunkOriginCell.y, 0, chunkSize, chunkSize, 1);
        TileBase[] tilesToSet = new TileBase[chunkSize * chunkSize];
        for (int i = 0; i < tileIds.Count; i++) {
            tilesToSet[i] = GetTileAsset(tileIds[i]);
        }

        mainTilemap.SetTilesBlock(chunkBounds, tilesToSet);
        // Debug.Log($"Client received and visually loaded chunk {chunkCoord}");
    }

    // --- Helper to generate chunk data ON SERVER ONLY ---
    private ChunkData ServerGenerateChunkData(Vector2Int chunkCoord) {
        // Ensure called only on server
        if (!IsServerInitialized) return null;

        // Check if it already exists (maybe generated by another player's request)
        if (worldChunks.ContainsKey(chunkCoord)) return worldChunks[chunkCoord];

        Debug.Log("Server generating new chunk");
        // --- Generation logic (same as before, but ensure server context) ---
        ChunkData newChunk = new ChunkData(chunkSize, chunkSize);
        Vector3Int chunkOriginCell = ChunkCoordToCellOrigin(chunkCoord);
        for (int localY = 0; localY < chunkSize; localY++) {
            for (int localX = 0; localX < chunkSize; localX++) {
                int worldX = chunkOriginCell.x + localX;
                int worldY = chunkOriginCell.y + localY;
                TileBase determinedTile = DetermineTileType(worldX, worldY); // Your generation func
                newChunk.tiles[localX, localY] = determinedTile;
            }
        }
        newChunk.hasBeenGenerated = true;
        worldChunks.Add(chunkCoord, newChunk); // Add to server's dictionary
        return newChunk;
    }
    // --- Visually Deactivate Chunk (Client Side) ---
    private void ClientDeactivateVisualChunk(Vector2Int chunkCoord) {
        Vector3Int chunkOriginCell = ChunkCoordToCellOrigin(chunkCoord);
        BoundsInt chunkBounds = new BoundsInt(chunkOriginCell.x, chunkOriginCell.y, 0, chunkSize, chunkSize, 1);
        TileBase[] clearTiles = new TileBase[chunkSize * chunkSize]; // Array of nulls
        mainTilemap.SetTilesBlock(chunkBounds, clearTiles);
        // Debug.Log($"Client visually deactivated chunk {chunkCoord}");
    }
    // --- Tile Modification ---
    // This is the entry point called by the PlayerController's ServerRpc
    public void ServerRequestModifyTile(Vector3Int cellPos, int newTileId) {
        // Must run on server
        if (!IsServerInitialized) return;

        TileBase tileToSet = GetTileAsset(newTileId);
        Vector2Int chunkCoord = CellToChunkCoord(cellPos);

        // Get the chunk data on the server
        if (!worldChunks.TryGetValue(chunkCoord, out ChunkData chunk)) {
            // Important: If player modifies an area server hasn't generated yet, GENERATE IT FIRST!
            chunk = ServerGenerateChunkData(chunkCoord);
            if (chunk == null) {
                Debug.LogError($"Server failed to generate chunk {chunkCoord} for modification request at {cellPos}.");
                return;
            }
        }

        // Calculate local coordinates
        int localX = cellPos.x - chunkCoord.x * chunkSize;
        int localY = cellPos.y - chunkCoord.y * chunkSize;

        if (localX >= 0 && localX < chunkSize && localY >= 0 && localY < chunkSize) {
            // Check if the tile is actually changing
            if (chunk.tiles[localX, localY] != tileToSet) {
                // --- Update SERVER data FIRST ---
                chunk.tiles[localX, localY] = tileToSet;
                chunk.isModified = true; // Mark chunk as modified for saving

                // --- Update Server's OWN visuals (optional but good for host) ---
                // groundTilemap.SetTile(cellPos, tileToSet);

                // --- BROADCAST change to ALL clients ---
                ObserversUpdateTile(cellPos, newTileId);
            }
        } else {
            Debug.LogWarning($"Server: Invalid local coordinates for modification at {cellPos}");
        }
    }

    // --- RPC to tell all clients about a tile change ---
    [ObserversRpc(BufferLast = false)] // Don't buffer, could spam late joiners. Consider buffering important static tiles.
    private void ObserversUpdateTile(Vector3Int cellPos, int newTileId) {
        // This runs on ALL clients (including the host)
        TileBase tileToSet = GetTileAsset(newTileId);
        mainTilemap.SetTile(cellPos, tileToSet); // Update local visuals

        // Optional: Update client-side data cache if you implement one.
        // Optional: Trigger particle effects, sound, etc. on the client here.
    }

    private TileBase GetTileAsset(int id) {
        if (id == 0) return null; // ID 0 is null
        if (idToTileAssetMap.TryGetValue(id, out TileBase tile)) {
            return tile;
        }
        Debug.LogWarning($"Tile ID '{id}' not found in mapping. Returning null.");
        return null; // Fallback to air/null
    }
    // --- Tile ID Helpers (Ensure these exist and are correct) ---
    private int GetTileId(TileBase tile) {
        if (tile == null) return 0; // Null is ID 0
        if (tileAssetToIdMap.TryGetValue(tile, out int id)) {
            return id;
        }
        Debug.LogWarning($"Tile '{tile.name}' not found in mapping. Returning 0.");
        return 0; // Fallback to air/null ID
    }

    void UpdateChunks() { // Old for singleplayer
        if (playerTransform == null) return;

        Vector2Int newPlayerChunkCoord = WorldToChunkCoord(playerTransform.position);

        // Only update if the player has moved to a new chunk
        if (newPlayerChunkCoord != currentPlayerChunkCoord) {
            currentPlayerChunkCoord = newPlayerChunkCoord;

            HashSet<Vector2Int> previouslyActiveChunks = new HashSet<Vector2Int>(activeChunks);
            HashSet<Vector2Int> requiredChunks = new HashSet<Vector2Int>();

            // Determine which chunks should be active
            for (int xOffset = -loadDistance; xOffset <= loadDistance; xOffset++) {
                for (int yOffset = -loadDistance; yOffset <= loadDistance; yOffset++) {
                    Vector2Int chunkCoord = new Vector2Int(currentPlayerChunkCoord.x + xOffset, currentPlayerChunkCoord.y + yOffset);
                    requiredChunks.Add(chunkCoord);

                    // --- Load or Generate Chunk ---
                    if (!activeChunks.Contains(chunkCoord)) {
                        // Check if chunk data exists but isn't active
                        if (worldChunks.ContainsKey(chunkCoord)) {
                            ActivateChunk(chunkCoord, worldChunks[chunkCoord]);
                        } else // Generate new chunk data
                          {
                            GenerateAndActivateChunk(chunkCoord);
                        }
                        activeChunks.Add(chunkCoord);
                    }
                }
            }

            // --- Unload Chunks ---
            previouslyActiveChunks.ExceptWith(requiredChunks); // Find chunks that are no longer required
            foreach (Vector2Int chunkCoord in previouslyActiveChunks) {
                DeactivateChunk(chunkCoord);
                activeChunks.Remove(chunkCoord);
                // Optional: Remove from worldChunks dictionary entirely to save memory,
                // but requires regeneration if player returns. For modifiable worlds, keep the data.
                // worldChunks.Remove(chunkCoord);
            }
        }
    }

    // Generates the data for a chunk and immediately activates it (sets tiles on tilemap)
    void GenerateAndActivateChunk(Vector2Int chunkCoord) {
        // Safety check: Ensure we aren't trying to regenerate an already existing chunk
        if (worldChunks.ContainsKey(chunkCoord)) {
            Debug.LogWarning($"Attempted to Generate chunk {chunkCoord} which already exists in dictionary. Activating instead.");
            ActivateChunk(chunkCoord, worldChunks[chunkCoord]);
            return;
        }

        ChunkData newChunk = new ChunkData(chunkSize, chunkSize);
        Vector3Int chunkOriginCell = ChunkCoordToCellOrigin(chunkCoord);
        TileBase[] tilesToSet = new TileBase[chunkSize * chunkSize];
        BoundsInt chunkBounds = new BoundsInt(chunkOriginCell.x, chunkOriginCell.y, 0, chunkSize, chunkSize, 1);

        int tileIndex = 0;
        for (int localY = 0; localY < chunkSize; localY++) {
            for (int localX = 0; localX < chunkSize; localX++) {
                int worldX = chunkOriginCell.x + localX;
                int worldY = chunkOriginCell.y + localY;
                TileBase determinedTile = DetermineTileType(worldX, worldY); // Procedural generation
                newChunk.tiles[localX, localY] = determinedTile;
                tilesToSet[tileIndex++] = determinedTile;
            }
        }

        newChunk.hasBeenGenerated = true; // Mark as generated
        worldChunks.Add(chunkCoord, newChunk);
        mainTilemap.SetTilesBlock(chunkBounds, tilesToSet);
    }
    // Activates a chunk (makes it visible), pulling data from ChunkData
    void ActivateChunk(Vector2Int chunkCoord, ChunkData chunkData) {
        // If chunk was loaded but never visually activated yet, ensure tiles are set
        if (!activeChunks.Contains(chunkCoord)) {
            Vector3Int chunkOriginCell = ChunkCoordToCellOrigin(chunkCoord);
            BoundsInt chunkBounds = new BoundsInt(chunkOriginCell.x, chunkOriginCell.y, 0, chunkSize, chunkSize, 1);
            TileBase[] tilesToSet = new TileBase[chunkSize * chunkSize];
            int tileIndex = 0;
            for (int localY = 0; localY < chunkSize; localY++) {
                for (int localX = 0; localX < chunkSize; localX++) {
                    tilesToSet[tileIndex++] = chunkData.tiles[localX, localY];
                }
            }
            mainTilemap.SetTilesBlock(chunkBounds, tilesToSet);
        }
        // (No else needed, if already active, do nothing visually)
    }


    // Removes a chunk's tiles from the tilemap (clears visually)
    void DeactivateChunk(Vector2Int chunkCoord) {
        Vector3Int chunkOriginCell = ChunkCoordToCellOrigin(chunkCoord);
        BoundsInt chunkBounds = new BoundsInt(chunkOriginCell.x, chunkOriginCell.y, 0, chunkSize, chunkSize, 1);

        // Create an array full of nulls to clear the area
        TileBase[] clearTiles = new TileBase[chunkSize * chunkSize];
        // No need to fill it explicitly, default is null

        mainTilemap.SetTilesBlock(chunkBounds, clearTiles);
        // Add similar SetTilesBlock calls for other tilemap layers if needed
    }


    // Call this if you change the seed at runtime
    public void InitializeNoise() {
        // Use the seed to initialize the random generator for noise offsets
        noiseRandomGen = new Unity.Mathematics.Random((uint)seed);
        // Generate large offsets based on the seed to shift noise patterns
        seedOffsetX = noiseRandomGen.NextFloat(-10000f, 10000f);
        seedOffsetY = noiseRandomGen.NextFloat(-10000f, 10000f);
        // Note: Unity.Mathematics.noise doesn't *directly* use this Random object for per-call randomness,
        // but we use it here to get deterministic offsets for the noise input coordinates.
    }

    // Get noise value (using Simplex noise from Unity.Mathematics for better results than Perlin)
    private float GetNoise(float x, float y, float frequency) {
        // Apply seed offsets and frequency
        float sampleX = (x + seedOffsetX) * frequency;
        float sampleY = (y + seedOffsetY) * frequency;
        // noise.snoise returns value in range [-1, 1], remap to [0, 1]
        return (noise.snoise(new float2(sampleX, sampleY)) + 1f) * 0.5f;
    }

    // 0 Air, 1 Stone, 
    public TileBase DetermineTileType(int worldX, int worldY) {
        // --- 1. Trench Definition ---
        float halfTrenchWidth = (trenchBaseWidth + Mathf.Abs(worldY) * trenchWidenFactor) / 2f;
        float edgeNoise = (GetNoise(worldX, worldY, trenchEdgeNoiseFrequency) - 0.5f) * 2f; // Remap noise to [-1, 1]
        float noisyHalfWidth = halfTrenchWidth + edgeNoise * trenchEdgeNoiseAmplitude;

        // Clamp width to be non-negative
        noisyHalfWidth = Mathf.Max(0.5f, noisyHalfWidth); // Ensure trench is at least 1 unit wide minimum

        if (Mathf.Abs(worldX) < noisyHalfWidth) {
            return idToTileAssetMap[0]; // Inside the main trench
        }

        // --- 2. Deterministic Structures ---
        // Check *before* caves/ores so structures overwrite base rock
        foreach (var structure in structures) {
            if (worldY == structure.yLevel && worldX >= structure.minX && worldX <= structure.maxX) {
                // Add more complex checks here if needed (e.g., using noise/hash for patterns)
                return structure.structureTile;
            }
        }

        // --- 3. Caves ---
        if (generateCaves) {
            float caveNoise = GetNoise(worldX, worldY, caveFrequency);
            if (caveNoise > caveThreshold) {
                return idToTileAssetMap[0]; // Inside a water-filled cave pocket
            }
        }

        // If we reach here, the tile is some kind of solid ground/rock

        // --- 4. Biome Determination ---
        BiomeLayer currentBiome = default(BiomeLayer); // Use default struct if no match
        bool biomeFound = false;
        // Iterate backwards for efficiency if higher Y biomes are more common near surface
        for (int i = biomeLayers.Count - 1; i >= 0; i--) {
            if (worldY >= biomeLayers[i].startY && worldY < biomeLayers[i].endY) {
                currentBiome = biomeLayers[i];
                biomeFound = true;
                break;
            }
        }

        // If below the lowest defined biome, maybe use a default deep rock
        if (!biomeFound && worldY < (biomeLayers.Count > 0 ? biomeLayers[0].startY : 0)) {
            // Optionally handle layers below the first defined one specifically
            return idToTileAssetMap[1]; // Or a specific "DeepRockTile"
        }
        // If above the highest biome, use its default ground or a surface/fallback tile
        if (!biomeFound && worldY >= (biomeLayers.Count > 0 ? biomeLayers[biomeLayers.Count - 1].endY : 0)) {
            return idToTileAssetMap[0];
        }

        // --- 5. Ore Spawning ---
        TileBase baseTile = biomeFound ? currentBiome.defaultGroundTile : idToTileAssetMap[1];
        TileBase finalTile = baseTile; // Start with the biome's default ground

        // Process ores (consider processing rarer ores last to let them overwrite common ones)
        foreach (var ore in oreTypes) {
            // Check if ore is allowed in this biome (if a biome was found)
            if (!biomeFound || !ore.allowedBiomeNames.Contains(currentBiome.name)) {
                continue; // Skip this ore if not in the right biome
            }

            // Check ore noise
            float oreNoise = GetNoise(worldX, worldY, ore.frequency);
            bool spawnOre = oreNoise > ore.threshold;

            // Check cluster noise if required
            if (spawnOre && ore.requireCluster) {
                float clusterNoise = GetNoise(worldX, worldY, ore.clusterFrequency);
                if (clusterNoise <= ore.clusterThreshold) {
                    spawnOre = false; // Do not spawn if cluster requirement not met
                }
            }
            // If multiple ores could spawn here, the last one checked in the list wins.
            // Re-order oreTypes list in inspector for precedence, or add specific priority values.
            if (spawnOre) {
                finalTile = ore.tile;
                // Optimization: If you found the "most important" ore, you could potentially 'break' here.
            }
        }

        // --- 6. Return Final Tile ---
        return finalTile;
    }

    // =============================================
    // === Coordinate Conversion Helper Methods ===
    // =============================================

    public Vector2Int WorldToChunkCoord(Vector3 worldPosition) {
        Vector3Int cellPos = mainTilemap.WorldToCell(worldPosition);
        // Integer division automatically floors, which is what we want.
        // Be careful if chunkSize is not a factor of world origin/coordinates start negative.
        // Using Mathf.FloorToInt ensures correct behavior with negative coordinates.
        int chunkX = Mathf.FloorToInt((float)cellPos.x / chunkSize);
        int chunkY = Mathf.FloorToInt((float)cellPos.y / chunkSize);
        return new Vector2Int(chunkX, chunkY);
    }

    public Vector3Int WorldToCell(Vector3 worldPosition) {
        return mainTilemap.WorldToCell(worldPosition);
    }

    public Vector3 CellToWorld(Vector3Int cellPosition) {
        return mainTilemap.GetCellCenterWorld(cellPosition); // Get center for placing objects
        // return groundTilemap.CellToWorld(cellPosition); // Get bottom-left corner
    }

    public Vector2Int CellToChunkCoord(Vector3Int cellPosition) {
        int chunkX = Mathf.FloorToInt((float)cellPosition.x / chunkSize);
        int chunkY = Mathf.FloorToInt((float)cellPosition.y / chunkSize);
        return new Vector2Int(chunkX, chunkY);
    }

    // Gets the cell coordinate of the bottom-left tile OF a chunk
    public Vector3Int ChunkCoordToCellOrigin(Vector2Int chunkCoord) {
        return new Vector3Int(chunkCoord.x * chunkSize, chunkCoord.y * chunkSize, 0);
    }

    // Gets the world coordinate of the center of a specific cell
    public Vector3 GetCellCenterWorld(Vector3Int cellPosition) {
        return mainTilemap.GetCellCenterWorld(cellPosition);
    }


    // =============================================
    // === World Interaction Helper Methods ===
    // =============================================

    // Gets the TileBase asset at a given world position (checks the ground layer)
    public TileBase GetTileAtWorldPos(Vector3 worldPos) {
        Vector3Int cellPos = WorldToCell(worldPos);
        return mainTilemap.GetTile(cellPos);
        // To check other layers, call GetTile on their respective Tilemaps
    }

    // Sets a tile at a given world position (modifies the ground layer)
    // IMPORTANT: Also updates the underlying ChunkData!
    public void SetTileAtWorldPos(Vector3 worldPos, TileBase tileToSet) {
        Vector3Int cellPos = WorldToCell(worldPos);
        Vector2Int chunkCoord = CellToChunkCoord(cellPos);

        // Get the chunk data (might need to generate if modifying an area not loaded yet)
        if (!worldChunks.TryGetValue(chunkCoord, out ChunkData chunk)) {
            // If player tries modifying a chunk that doesn't even exist in data yet
            // (e.g., far away, or just outside loaded range before next update)
            // Option 1: Disallow modification (simplest)
            Debug.LogWarning($"Attempted to modify tile in unloaded/non-existent chunk {chunkCoord}. Modification ignored.");
            return;

            // Option 2: Generate the chunk on the spot (can cause hiccups)
            // GenerateAndActivateChunk(chunkCoord); // Generate it first
            // if (!worldChunks.TryGetValue(chunkCoord, out chunk)) { // Try getting it again
            //     Debug.LogError($"Failed to generate and retrieve chunk {chunkCoord} for modification.");
            //     return; // Exit if failed
            // }
        }
        // Calculate local coordinates
        int localX = cellPos.x - chunkCoord.x * chunkSize;
        int localY = cellPos.y - chunkCoord.y * chunkSize;

        // Boundary check within the chunk
        if (localX >= 0 && localX < chunkSize && localY >= 0 && localY < chunkSize) {
            // --- Only proceed if the tile is actually changing ---
            if (chunk.tiles[localX, localY] != tileToSet) {
                // --- Update the Data First! ---
                chunk.tiles[localX, localY] = tileToSet;
                chunk.isModified = true; // Mark chunk as modified!

                // --- Then update the Tilemap visually ---
                // Only update visually if the chunk is currently active/rendered
                if (activeChunks.Contains(chunkCoord)) {
                    mainTilemap.SetTile(cellPos, tileToSet);
                    // Optional: Force collider update if needed immediately
                    // TilemapCollider2D collider = groundTilemap.GetComponent<TilemapCollider2D>();
                    // if (collider) collider.ProcessTilemapChanges();
                }
            }
        } else {
            Debug.LogWarning($"Calculated local tile coordinates ({localX},{localY}) outside chunk bounds for cell {cellPos}. Chunk coord: {chunkCoord}. Tile not set.");
        }
    }
    // --- Saving and Loading ---

    private string GetSaveFilePath() {
        return Path.Combine(Application.persistentDataPath, saveFileName);
        // persistentDataPath is a good place for save files on most platforms
    }
    [Button("SaveWorld")]
    public void SaveWorld() {
        WorldSaveData saveData = new WorldSaveData();

        // --- Save Chunks ---
        foreach (KeyValuePair<Vector2Int, ChunkData> chunkPair in worldChunks) {
            // Only save chunks that have been generated/loaded AND potentially modified
            if (chunkPair.Value.hasBeenGenerated) // Or save all in worldChunks if memory isn't a concern
            {
                Vector2Int chunkCoord = chunkPair.Key;
                ChunkData chunkData = chunkPair.Value;

                ChunkSaveData chunkSave = new ChunkSaveData(chunkSize * chunkSize);
                for (int y = 0; y < chunkSize; y++) {
                    for (int x = 0; x < chunkSize; x++) {
                        TileBase tile = chunkData.tiles[x, y];
                        if (tileAssetToIdMap.TryGetValue(tile, out int tileId)) {
                            chunkSave.tileIds.Add(tileId);
                        } else {
                            Debug.LogWarning($"Tile '{tile?.name ?? "NULL"}' at [{x},{y}] in chunk {chunkCoord} has no ID mapping! Saving as air (ID 0).");
                            chunkSave.tileIds.Add(0); // Save as air/null ID
                        }
                    }
                }
                // Use a string key for better compatibility e.g. "x,y"
                string chunkKey = $"{chunkCoord.x},{chunkCoord.y}";
                saveData.savedChunks.Add(chunkKey, chunkSave);
            }
        }

        // --- Save Player Position ---
        if (playerTransform != null) {
            saveData.playerPosition = playerTransform.position;
        }


        // --- Serialize and Write to File ---
        try {
            string filePath = GetSaveFilePath();
            string json = JsonConvert.SerializeObject(saveData, Formatting.Indented, new JsonSerializerSettings() {
                ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore
            }); // Use Newtonsoft
            //string json = JsonUtility.ToJson(saveData, true); // Use Unity's (needs Dictionary workaround)
            File.WriteAllText(filePath, json);
            Debug.Log($"World saved to {filePath}");
        } catch (System.Exception e) {
            Debug.LogError($"Failed to save world: {e.Message}\n{e.StackTrace}");
        }
    }


    public void LoadWorld() {
        string filePath = GetSaveFilePath();

        if (File.Exists(filePath)) {
            try {
                string json = File.ReadAllText(filePath);
                WorldSaveData loadData = JsonConvert.DeserializeObject<WorldSaveData>(json); // Use Newtonsoft
                // WorldSaveData loadData = JsonUtility.FromJson<WorldSaveData>(json); // Use Unity's (needs Dictionary workaround)

                if (loadData != null && loadData.savedChunks != null) {
                    worldChunks.Clear(); // Clear existing runtime chunk data
                    activeChunks.Clear(); // Clear active chunks before loading
                    mainTilemap.ClearAllTiles(); // Clear the visual tilemap

                    foreach (KeyValuePair<string, ChunkSaveData> savedChunkPair in loadData.savedChunks) {
                        // Parse the string key back to Vector2Int
                        string[] keyParts = savedChunkPair.Key.Split(',');
                        if (keyParts.Length == 2 && int.TryParse(keyParts[0], out int x) && int.TryParse(keyParts[1], out int y)) {
                            Vector2Int chunkCoord = new Vector2Int(x, y);
                            ChunkSaveData chunkSave = savedChunkPair.Value;

                            if (chunkSave.tileIds.Count == chunkSize * chunkSize) {
                                ChunkData newChunk = new ChunkData(chunkSize, chunkSize);
                                int tileIndex = 0;
                                for (int localY = 0; localY < chunkSize; localY++) {
                                    for (int localX = 0; localX < chunkSize; localX++) {
                                        int tileId = chunkSave.tileIds[tileIndex++];
                                        if (idToTileAssetMap.TryGetValue(tileId, out TileBase tileAsset)) {
                                            newChunk.tiles[localX, localY] = tileAsset;
                                        } else {
                                            Debug.LogWarning($"Unknown Tile ID {tileId} found in chunk {chunkCoord} during load. Setting to null/air.");
                                            newChunk.tiles[localX, localY] = null; // Or airTile if not null
                                        }
                                    }
                                }
                                newChunk.hasBeenGenerated = true; // Mark as loaded/existing
                                newChunk.isModified = false; // Reset modified flag on load
                                worldChunks.Add(chunkCoord, newChunk);
                            } else {
                                Debug.LogWarning($"Chunk {chunkCoord} has incorrect tile count ({chunkSave.tileIds.Count}) in save file. Skipping load for this chunk.");
                            }
                        } else {
                            Debug.LogWarning($"Invalid chunk key format '{savedChunkPair.Key}' in save file. Skipping.");
                        }

                    }

                    // --- Load Player Position ---
                    if (playerTransform != null) {
                        // Ensure player doesn't fall through floor on load - may need adjustments
                        playerTransform.position = loadData.playerPosition;
                        // Force physics update or short disable/enable of Rigidbody could be needed
                        // e.g. playerTransform.GetComponent<Rigidbody2D>()?.Sleep();
                        // e.g. playerTransform.GetComponent<Rigidbody2D>()?.WakeUp();

                    }
                    // Update current chunk coord AFTER potentially moving player
                    currentPlayerChunkCoord = WorldToChunkCoord(playerTransform != null ? playerTransform.position : Vector3.zero);


                    Debug.Log($"World loaded successfully from {filePath}");
                    // Note: Initial chunk activation around player happens in Start() -> ChunkLoadingRoutine -> UpdateChunks
                } else {
                    Debug.LogError("Failed to deserialize world data or data was empty.");
                    // Optionally trigger initial generation if load fails catastrophically
                }
            } catch (System.Exception e) {
                Debug.LogError($"Failed to load world: {e.Message}\n{e.StackTrace}");
                // Optionally trigger initial generation if load fails
            }
        } else {
            Debug.Log("No save file found. Starting new world.");
            // No need to do anything else, world will generate as player moves
        }
    }
}
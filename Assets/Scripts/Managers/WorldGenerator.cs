using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections;
using System.Collections.Generic;
using FishNet.Object;
using FishNet.Connection;

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
    [Header("World Settings")]
    [SerializeField] private int worldWidth = 2000; // Total world size in tiles
    [SerializeField] private int worldHeight = 500;
    [SerializeField] private int chunkSize = 16; // Size of chunks (16x16 tiles) - Power of 2 often good
    // Note: World dimensions don't strictly need to be multiples of chunk size, but edge handling is easier if they are.

    [Header("Tilemap & Tiles")]
    [SerializeField] private List<TileBase> tileAssets; // Assign ALL your TileBase assets here in order
    [SerializeField] private Tilemap groundTilemap; // Assign your ground Tilemap GameObject here
    [SerializeField] private TileBase water; // Assign your default ground tile (e.g., Dirt)
    [SerializeField] private TileBase stoneTile;         // Assign your Stone tile asset
    [SerializeField] private TileBase airTile;       
    [SerializeField] private TileBase diamondTile;     
    [SerializeField] private TileBase rubyTile;     
    [SerializeField] private TileBase goldTile;
    [SerializeField] private TileBase silverTile;     
    [SerializeField] private TileBase boundaryTile;     
    // Add more TileBase fields for other tile types

    [Header("Generation Parameters")]
    [SerializeField] private int surfaceLevel = 400; // Y level for the surface
    [SerializeField] private float noiseScale = 0.05f; // For Perlin noise terrain generation
    [SerializeField] private int stoneDepth = 50;    // How far below the surface stone starts appearing

    [Header("Player & Loading")]
    [SerializeField] private Transform playerTransform; // Assign the player's transform
    [SerializeField] private int loadDistance = 3; // How many chunks away from the player to load (e.g., 3 means a 7x7 area around the player's chunk)
    [SerializeField] private float checkInterval = 0.5f; // How often (seconds) to check for loading/unloading chunks

    // --- Runtime Data ---
    private Dictionary<Vector2Int, ChunkData> worldChunks = new Dictionary<Vector2Int, ChunkData>();
    private HashSet<Vector2Int> activeChunks = new HashSet<Vector2Int>();
    private Vector2Int currentPlayerChunkCoord = new Vector2Int(int.MinValue, int.MinValue);

    // --- Tile ID Mapping ---
    private Dictionary<TileBase, int> tileAssetToIdMap = new Dictionary<TileBase, int>();
    private Dictionary<int, TileBase> idToTileAssetMap = new Dictionary<int, TileBase>();

    [Header("Trench Settings")]
    public int trenchWidthTop = 7;      // Width of trench at the surface (top)
    public int trenchWidthBottom = 3;   // Width of trench at the bottom
    public int trenchDepthTrapezoid = 30; // Depth over which trench narrows to bottom width
    public float trenchEdgeNoiseScale = 3f; // Scale for noise on trench edges
    public float trenchEdgeNoiseIntensity = 2f; // Intensity of noise offset for edges
    [Header("Trench Padding")]
    public int trenchPaddingTop = 0;     // Stone layers above the trench entrance
    public int trenchPaddingBottom = 5;  // Stone layers at the bottom of the trench and grid
    public int trenchPaddingSides = 2;   // Stone layers on each side of the trench

    [Header("Noise Settings - General")]
    public float silverNoiseScale = 10f; // Adjust for noise detail
    public float goldNoiseScale = 7f; // Adjust for noise detail
    public float rubyNoiseScale = 2f; // Adjust for noise detail
    public float diamondNoiseScale = 1f; // Adjust for noise detail
    public float noiseOffset_X = 0f; // For different noise patterns each run
    public float noiseOffset_Y = 0f;

    [Header("Ore Settings - Silver")]
    [Range(0f, 0.4f)] public float silverOreFrequencySurface = 0.02f;
    [Range(0f, 0.4f)] public float silverOreFrequencyDeep = 0.08f;
    [Range(0f, 1f)] public float silverDepthStart;
    [Range(0f, 1f)] public float silverDepthEnd;

    [Header("Ore Settings - Gold")]
    [Range(0f, 0.4f)] public float GoldFrequencySurface = 0.1f; // Frequency near surface
    [Range(0f, 0.4f)] public float GoldFrequencyDeep = 0.05f;   // Frequency deeper down
    [Range(0f, 1f)] public float GoldDepthStart;
    [Range(0f, 1f)] public float GoldDepthEnd;

    [Header("Ore Settings - Ruby")]
    [Range(0f, 0.4f)] public float RubyFrequencySurface = 0.1f; // Frequency near surface
    [Range(0f, 0.4f)] public float RubyFrequencyDeep = 0.05f;   // Frequency deeper down
    [Range(0f, 1f)] public float RubyDepthStart;
    [Range(0f, 1f)] public float RubyDepthEnd;

    [Header("Ore Settings - Diamond")]
    [Range(0f, 0.4f)] public float DiamondFrequencySurface = 0.1f; // Frequency near surface
    [Range(0f, 0.4f)] public float DiamondFrequencyDeep = 0.05f;   // Frequency deeper down
    [Range(0f, 1f)] public float DiamondDepthStart;
    [Range(0f, 1f)] public float DiamondDepthEnd;

    public override void OnStartServer() {
        base.OnStartServer();
        // Server-only initialization
        InitializeTileMapping();
        //LoadWorld(); // Load happens only on server
        // Start the chunk loading check *only* on the server for managing data
        StartCoroutine(ServerChunkManagementRoutine());
    }
    public override void OnStartClient() {
        base.OnStartClient();
        // Client-specific initialization
        InitializeTileMapping(); // Clients also need the ID maps to interpret RPCs

        if (!IsServerInitialized) // Avoid double-init on host
        {
            groundTilemap.ClearAllTiles(); // Start with a clear visual map
                                           // Clients need to know their own position to request chunks
            StartCoroutine(ClientChunkLoadingRoutine());
        } else // Host (Server + Client) specific client-side init
          {
            // Host needs its visual loading routine too
            StartCoroutine(ClientChunkLoadingRoutine());
        }
    }
    //void Start() {
    //    if (groundTilemap == null) {
    //        Debug.LogError("Ground Tilemap is not assigned!");
    //        return;
    //    }
    //    if (playerTransform == null) {
    //        Debug.LogError("Player Transform is not assigned!");
    //        // Optionally try to find the player by tag:
    //        // GameObject player = GameObject.FindGameObjectWithTag("Player");
    //        // if (player != null) playerTransform = player.transform;
    //        // else return; // Still couldn't find it
    //    }
    //
    //    StartCoroutine(ChunkLoadingRoutine());
    //}

    // --- Initialization ---
    void InitializeTileMapping() {
        tileAssetToIdMap.Clear();
        idToTileAssetMap.Clear();

        // Assign ID 0 to null (representing Air or empty space in save data)
        // Ensure airTile variable actually holds null if you want null tiles cleared
        tileAssetToIdMap.Add(airTile, 0); 
        idToTileAssetMap.Add(0, airTile); 

        // Assign IDs based on the order in the tileAssets list
        for (int i = 0; i < tileAssets.Count; i++) {
            if (tileAssets[i] == null) continue; // Skip null entries in the list itself

            // Start IDs from 1 to avoid conflict with the explicit null ID 0
            int tileId = i + 1;

            if (!tileAssetToIdMap.ContainsKey(tileAssets[i])) {
                tileAssetToIdMap.Add(tileAssets[i], tileId);
                idToTileAssetMap.Add(tileId, tileAssets[i]);
                Debug.Log($"Mapped Tile: {tileAssets[i].name} to ID: {tileId}");
            } else {
                Debug.LogWarning($"Duplicate TileBase '{tileAssets[i].name}' detected in tileAssets list. Only the first instance will be used for ID mapping.");
            }
        }

        // Crucial Check: Ensure specific tiles used in generation are mapped
       // if (groundTilemap && !tileAssetToIdMap.ContainsKey(groundTilemap)) Debug.LogError("defaultGroundTile is not in the tileAssets list!");
        if (stoneTile && !tileAssetToIdMap.ContainsKey(stoneTile)) Debug.LogError("stoneTile is not in the tileAssets list!");
        // Check airTile only if it's NOT meant to be null
        if (airTile != null && !tileAssetToIdMap.ContainsKey(airTile)) Debug.LogError("airTile is not in the tileAssets list!");
    }
    IEnumerator ServerChunkManagementRoutine() {
        // Ensure this only runs on the server
        if (!IsServerInitialized) yield break;

        // Need to track *all* player positions on the server
        while (true) {
            // In a real game, you'd iterate through all connected NetworkObjects tagged as players
            // For simplicity now, we might just check if ANY player requires a chunk?
            // Or more efficiently, track required chunks for all players collectively.
            // Let's keep it simple: focus on ensuring data exists when requested.
            // The main generation trigger will be client requests (see below).

            yield return new WaitForSeconds(checkInterval * 2); // Can check less often server-side maybe
                                                                // Potentially pre-generate chunks around players proactively here if needed
        }
    }
    // --- Client-Side Chunk VISUAL Loading ---
    // This routine runs on each client (including host) to manage visuals
    IEnumerator ClientChunkLoadingRoutine() {
        HashSet<Vector2Int> clientActiveVisualChunks = new HashSet<Vector2Int>();
        Vector2Int clientCurrentChunkCoord = new Vector2Int(int.MinValue, int.MinValue);

        // Wait until the player object owned by this client is spawned and available
        // This assumes your player spawn logic is handled correctly by FishNet
        yield return new WaitUntil(() => base.Owner != null && base.Owner.IsActive && base.Owner.IsLocalClient && PlayerController.LocalInstance != null); // Assumes a static LocalInstance on your PlayerController

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
                            Debug.Log("Requesting chunk data");
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

        groundTilemap.SetTilesBlock(chunkBounds, tilesToSet);
        // Debug.Log($"Client received and visually loaded chunk {chunkCoord}");
    }

    // --- Helper to generate chunk data ON SERVER ONLY ---
    private ChunkData ServerGenerateChunkData(Vector2Int chunkCoord) {
        // Ensure called only on server
        if (!IsServerInitialized) return null;

        // Check if it already exists (maybe generated by another player's request)
        if (worldChunks.ContainsKey(chunkCoord)) return worldChunks[chunkCoord];

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
        groundTilemap.SetTilesBlock(chunkBounds, clearTiles);
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
        groundTilemap.SetTile(cellPos, tileToSet); // Update local visuals

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

    // Coroutine to periodically check player position and load/unload chunks
    IEnumerator ChunkLoadingRoutine() {
        while (true) {
            UpdateChunks();
            yield return new WaitForSeconds(checkInterval);
        }
    }

    void UpdateChunks() {
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
        ChunkData newChunk = new ChunkData(chunkSize, chunkSize);
        Vector3Int chunkOriginCell = ChunkCoordToCellOrigin(chunkCoord); // Bottom-left cell of the chunk

        TileBase[] tilesToSet = new TileBase[chunkSize * chunkSize];
        BoundsInt chunkBounds = new BoundsInt(chunkOriginCell.x, chunkOriginCell.y, 0, chunkSize, chunkSize, 1);

        int tileIndex = 0;
        for (int localY = 0; localY < chunkSize; localY++) {
            for (int localX = 0; localX < chunkSize; localX++) {
                int worldX = chunkOriginCell.x + localX;
                int worldY = chunkOriginCell.y + localY;

                // --- Your Tile Generation Logic Here ---
                TileBase determinedTile = DetermineTileType(worldX, worldY);
                // --- End Generation Logic ---

                newChunk.tiles[localX, localY] = determinedTile;
                tilesToSet[tileIndex++] = determinedTile; // Flatten array for SetTilesBlock
            }
        }

        worldChunks.Add(chunkCoord, newChunk); // Store the data
        groundTilemap.SetTilesBlock(chunkBounds, tilesToSet); // Efficiently set tiles visually
        // Add similar SetTilesBlock calls for other tilemap layers if needed
    }

    // Makes an existing chunk's tiles visible on the tilemap
    void ActivateChunk(Vector2Int chunkCoord, ChunkData chunkData) {
        Vector3Int chunkOriginCell = ChunkCoordToCellOrigin(chunkCoord);
        BoundsInt chunkBounds = new BoundsInt(chunkOriginCell.x, chunkOriginCell.y, 0, chunkSize, chunkSize, 1);
        TileBase[] tilesToSet = new TileBase[chunkSize * chunkSize];

        int tileIndex = 0;
        for (int localY = 0; localY < chunkSize; localY++) {
            for (int localX = 0; localX < chunkSize; localX++) {
                // Handle cases where chunk data might be partially null (if saving/loading allows)
                tilesToSet[tileIndex++] = chunkData.tiles[localX, localY];
            }
        }
        groundTilemap.SetTilesBlock(chunkBounds, tilesToSet);
        // Add similar SetTilesBlock calls for other tilemap layers if needed
    }

    // Removes a chunk's tiles from the tilemap (clears visually)
    void DeactivateChunk(Vector2Int chunkCoord) {
        Vector3Int chunkOriginCell = ChunkCoordToCellOrigin(chunkCoord);
        BoundsInt chunkBounds = new BoundsInt(chunkOriginCell.x, chunkOriginCell.y, 0, chunkSize, chunkSize, 1);

        // Create an array full of nulls to clear the area
        TileBase[] clearTiles = new TileBase[chunkSize * chunkSize];
        // No need to fill it explicitly, default is null

        groundTilemap.SetTilesBlock(chunkBounds, clearTiles);
        // Add similar SetTilesBlock calls for other tilemap layers if needed
    }


    // --- Core Tile Generation Logic ---
    // Replace this with your actual procedural generation algorithms (Noise, Cellular Automata, etc.)
    private TileBase DetermineTileType2(int worldX, int worldY) {
        // Basic Example: Perlin noise for terrain height + stone below surface
        if (worldX < 0 || worldX >= worldWidth || worldY < 0 || worldY >= worldHeight) {
            // Consider outside world bounds as air or a solid border
            return airTile; // Or some border tile
        }

        // Use Perlin noise to determine surface height variation
        float noiseValue = Mathf.PerlinNoise((worldX + 0.1f) * noiseScale, (worldY + 0.1f) * noiseScale * 2); // Added slight offset and different scale for y
        int currentSurfaceLevel = surfaceLevel + Mathf.RoundToInt(noiseValue * 10f); // Example variation of +/- 5 tiles


        if (worldY > currentSurfaceLevel) {
            return airTile; // Above surface
        } else if (worldY < currentSurfaceLevel - stoneDepth) {
            return stoneTile; // Deep underground
        } else {
            // Simple check, could be more complex (e.g., blend stone near the stone depth)
            if (worldY < currentSurfaceLevel - 1) // Make stone appear a bit below surface
            {
                // Add probability for stone even in the dirt layer for variety
                if (Random.Range(0, 100) < (currentSurfaceLevel - worldY) / 2) // More stone deeper
                    return stoneTile;
            }
            return airTile; // Near surface
        }
    }
    TileBase DetermineTileType(int x, int y) {
        // Check for Boundary Tiles
        if (x < 2 || x >= worldWidth - 2 // Sides 
            || y >= worldHeight - 2) { // Bottom
            return boundaryTile; // Create boundary tile
        }
        // Apply Padding Layers (Stone)
        // Top Padding
        if (y < trenchPaddingTop) {
            return stoneTile;
        }

        // Bottom Padding
        if (y >= worldHeight - trenchPaddingBottom) {
            return stoneTile;
        }

        // Side Padding
        int paddedGridWidth = worldWidth - (trenchPaddingSides * 2); // Effective grid width after side padding
        int sidePaddingStartX = trenchPaddingSides;                // Starting X for non-padded area
        if (x < sidePaddingStartX || x >= sidePaddingStartX + paddedGridWidth) {
            return stoneTile;
        }


        // Calculate Trapezoidal Trench Shape (within the non-padded area)

        float normalizedDepth = Mathf.Clamp01((float)(y - trenchPaddingTop) / trenchDepthTrapezoid); // Depth adjusted for top padding
        float currentTrenchWidth = Mathf.Lerp(trenchWidthTop, trenchWidthBottom, normalizedDepth);
        float trenchCenterX = worldWidth * 0.5f;

        float leftEdgeNoise = Mathf.PerlinNoise((y * trenchEdgeNoiseScale) + noiseOffset_Y, noiseOffset_X) - 0.5f;
        float rightEdgeNoise = Mathf.PerlinNoise((y * trenchEdgeNoiseScale) + noiseOffset_Y + 100f, noiseOffset_X) - 0.5f;

        float noisyTrenchStartX = trenchCenterX - (currentTrenchWidth * 0.5f) + (leftEdgeNoise * trenchEdgeNoiseIntensity);
        float noisyTrenchEndX = trenchCenterX + (currentTrenchWidth * 0.5f) + (rightEdgeNoise * trenchEdgeNoiseIntensity);

        int trenchStartX = Mathf.Clamp(Mathf.RoundToInt(noisyTrenchStartX), sidePaddingStartX, sidePaddingStartX + paddedGridWidth); // Clamped to padded width
        int trenchEndX = Mathf.Clamp(Mathf.RoundToInt(noisyTrenchEndX), sidePaddingStartX, sidePaddingStartX + paddedGridWidth); // Clamped to padded width


        // Determine Tile Type based on Trench and Ore Noise (within the trench area)

        bool isInTrench = (x >= trenchStartX && x < trenchEndX);

        if (isInTrench) {
            return airTile; // Trench area is still empty
        } else {
            // Generate separate noise value for each ore
            // Generate separate noise value for each ore, using its specific noiseScale
            float diamondNoiseValue = Mathf.PerlinNoise((x + noiseOffset_X) / diamondNoiseScale, (y + noiseOffset_Y) / diamondNoiseScale);
            float rubyNoiseValue = Mathf.PerlinNoise((x + noiseOffset_X + 100) / rubyNoiseScale, (y + noiseOffset_Y + 100) / rubyNoiseScale);
            float goldNoiseValue = Mathf.PerlinNoise((x + noiseOffset_X + 200) / goldNoiseScale, (y + noiseOffset_Y + 200) / goldNoiseScale);
            float silverNoiseValue = Mathf.PerlinNoise((x + noiseOffset_X + 300) / silverNoiseScale, (y + noiseOffset_Y) / silverNoiseScale);


            float diamondFrequency = CalculateOreFrequency(x, y, DiamondFrequencySurface, DiamondFrequencyDeep, DiamondDepthStart, DiamondDepthEnd);
            float rubyFrequency = CalculateOreFrequency(x, y, RubyFrequencySurface, RubyFrequencyDeep, RubyDepthStart, RubyDepthEnd);
            float goldFrequency = CalculateOreFrequency(x, y, GoldFrequencySurface, GoldFrequencyDeep, GoldDepthStart, GoldDepthEnd);
            float silverFrequency = CalculateOreFrequency(x, y, silverOreFrequencySurface, silverOreFrequencyDeep, silverDepthStart, silverDepthEnd);


            // Check for ores in order of rarity (most rare to least rare)
            if (diamondNoiseValue < diamondFrequency) {
                return diamondTile;
            }
            if (rubyNoiseValue < rubyFrequency) {
                return rubyTile;
            }
            if (goldNoiseValue < goldFrequency) {
                return goldTile;
            }
            if (silverNoiseValue < silverFrequency) {
                return silverTile;
            }

            return stoneTile; // Default to Stone
        }
    }
    float CalculateOreFrequency(int x, int y, float surfaceFrequency, float deepFrequency,
                           float depthStartPercent, float depthEndPercent) {
        // ... (CalculateOreFrequency function remains the same) ...
        // ... (The logic for depth and x-multiplier is unchanged and still good) ...
        // ... (You don't need to modify this function for this change) ...
        float depthStart = worldHeight * depthStartPercent;
        float depthEnd = worldHeight* depthEndPercent;

        float baseFrequency;
        if (y < depthStart) {
            baseFrequency = surfaceFrequency;
        } else if (y >= depthEnd) {
            baseFrequency = deepFrequency;
        } else {
            float t = Mathf.InverseLerp(depthStart, depthEnd, y);
            baseFrequency = Mathf.Lerp(surfaceFrequency, deepFrequency, t);
        }

        float centerX = worldWidth/ 2.0f;
        float maxDistance = worldWidth / 2.0f;
        float xDistance = Mathf.Abs(x - centerX);
        float xNormalizedDistance = xDistance / maxDistance;
        float xMultiplier = Mathf.Lerp(1.0f, 1.0f + (xNormalizedDistance * xNormalizedDistance), 1f);


        return baseFrequency * xMultiplier;
    }

    // =============================================
    // === Coordinate Conversion Helper Methods ===
    // =============================================

    public Vector2Int WorldToChunkCoord(Vector3 worldPosition) {
        Vector3Int cellPos = groundTilemap.WorldToCell(worldPosition);
        // Integer division automatically floors, which is what we want.
        // Be careful if chunkSize is not a factor of world origin/coordinates start negative.
        // Using Mathf.FloorToInt ensures correct behavior with negative coordinates.
        int chunkX = Mathf.FloorToInt((float)cellPos.x / chunkSize);
        int chunkY = Mathf.FloorToInt((float)cellPos.y / chunkSize);
        return new Vector2Int(chunkX, chunkY);
    }

    public Vector3Int WorldToCell(Vector3 worldPosition) {
        return groundTilemap.WorldToCell(worldPosition);
    }

    public Vector3 CellToWorld(Vector3Int cellPosition) {
        return groundTilemap.GetCellCenterWorld(cellPosition); // Get center for placing objects
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
        return groundTilemap.GetCellCenterWorld(cellPosition);
    }


    // =============================================
    // === World Interaction Helper Methods ===
    // =============================================

    // Gets the TileBase asset at a given world position (checks the ground layer)
    public TileBase GetTileAtWorldPos(Vector3 worldPos) {
        Vector3Int cellPos = WorldToCell(worldPos);
        return groundTilemap.GetTile(cellPos);
        // To check other layers, call GetTile on their respective Tilemaps
    }

    // Sets a tile at a given world position (modifies the ground layer)
    // IMPORTANT: Also updates the underlying ChunkData!
    public void SetTileAtWorldPos(Vector3 worldPos, TileBase tileToSet) {
        Vector3Int cellPos = WorldToCell(worldPos);
        Vector2Int chunkCoord = CellToChunkCoord(cellPos);

        // Check if the chunk data exists (it should if it's near the player, but good practice)
        if (worldChunks.TryGetValue(chunkCoord, out ChunkData chunk)) {
            // Calculate local coordinates within the chunk
            int localX = cellPos.x - chunkCoord.x * chunkSize;
            int localY = cellPos.y - chunkCoord.y * chunkSize;

            // Ensure local coordinates are within bounds (safety check)
            if (localX >= 0 && localX < chunkSize && localY >= 0 && localY < chunkSize) {
                // --- Update the Data First! ---
                chunk.tiles[localX, localY] = tileToSet;

                // --- Then update the Tilemap visually ---
                groundTilemap.SetTile(cellPos, tileToSet);

                // Optimization: Could potentially trigger a Tilemap Collider 2D update if needed,
                // but it often updates automatically or sufficiently frequently.
                // TilemapCollider2D collider = groundTilemap.GetComponent<TilemapCollider2D>();
                // if (collider) collider.ProcessTilemapChanges();

                // Add similar logic for other Tilemap layers if the action affects them
            } else {
                Debug.LogWarning($"Calculated local tile coordinates ({localX},{localY}) outside chunk bounds for cell {cellPos}. Chunk coord: {chunkCoord}");
            }
        } else {
            Debug.LogWarning($"Attempted to set tile at {worldPos} (cell {cellPos}) but chunk {chunkCoord} data doesn't exist. Tile not set.");
            // Optionally: Could trigger generation of this chunk if modification is allowed anywhere anytime.
        }
    }

    // Example: How to use the interaction methods from another script (e.g., PlayerController)
    /*
    public WorldGenerator worldGenerator; // Assign in Inspector

    void Update()
    {
        if (Input.GetMouseButtonDown(0)) // Left Click: Break Tile
        {
            Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            mouseWorldPos.z = 0; // Ensure Z is 0 for 2D Tilemap interaction
            worldGenerator.SetTileAtWorldPos(mouseWorldPos, null); // Set to null to remove
        }

        if (Input.GetMouseButtonDown(1)) // Right Click: Place Tile
        {
             Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
             mouseWorldPos.z = 0;
             TileBase currentTile = worldGenerator.GetTileAtWorldPos(mouseWorldPos);
             if (currentTile == null || currentTile == worldGenerator.airTile) // Only place in empty space
             {
                // Assuming player has 'stoneTile' selected to place
                worldGenerator.SetTileAtWorldPos(mouseWorldPos, worldGenerator.stoneTile);
             }
        }
    }
    */
}
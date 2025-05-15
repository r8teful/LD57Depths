using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections;
using System.Collections.Generic;
using FishNet.Object;
using FishNet.Connection;
using Sirenix.OdinInspector;
using UnityEditor;
using System.Linq;
// Represents the runtime data for a single chunk (tile references)
public class ChunkData {
    public ushort[,] tiles; // The ground layer 
    public ushort[,] oreID;      // Second "ore" layer
    public short[,] tileDurability; // Third "dmg" layer
    [TableMatrix(DrawElementMethod = "DrawElement")]
    public byte[,] biomeID;
    public bool isModified = false; // Flag to track if chunk has changed since load/generation
    public bool hasBeenGenerated = false; // Flag to prevent regenerating loaded chunks

    public ChunkData() {
        tiles = new ushort[ChunkManager.CHUNK_SIZE, ChunkManager.CHUNK_SIZE];
        tileDurability = new short[ChunkManager.CHUNK_SIZE, ChunkManager.CHUNK_SIZE];
        oreID = new ushort[ChunkManager.CHUNK_SIZE, ChunkManager.CHUNK_SIZE];
        biomeID = new byte[ChunkManager.CHUNK_SIZE, ChunkManager.CHUNK_SIZE];
        // Initialize defaults
        for (int y = 0; y < ChunkManager.CHUNK_SIZE; ++y)
        for (int x = 0; x < ChunkManager.CHUNK_SIZE; ++x) {
            tileDurability[x, y] = -1;
            oreID[x, y] = ResourceSystem.InvalidID;
            biomeID[x, y] = 0;
        }
    }

    public ChunkData(int chunkSizeX, int chunkSizeY) {
        tiles = new ushort[chunkSizeX, chunkSizeY];
        tileDurability = new short[chunkSizeX, chunkSizeY];
        for (int y = 0; y < chunkSizeY; ++y) {
            for (int x = 0; x < chunkSizeX; ++x) {
                tileDurability[x, y] = -1; // Default state
            }
        }
        oreID = new ushort[chunkSizeX, chunkSizeY];
        for (int y = 0; y < chunkSizeY; ++y) {
            for (int x = 0; x < chunkSizeX; ++x) {
                oreID[x, y] = ResourceSystem.InvalidID; // Default state
            }
        }
        biomeID = new byte[chunkSizeX, chunkSizeY];
        for (int y = 0; y < chunkSizeY; ++y) {
            for (int x = 0; x < chunkSizeX; ++x) {
                biomeID[x, y] = 0; // Default state
            }
        }
        //entitiesToSpawn = new List<PersistentEntityData>(); // Initialize the list
    }

    static byte DrawElement(Rect rect, byte value) {
        // Draw an int field in the given rect, initializing with the current byte value
        int intVal = EditorGUI.IntField(rect, value);
        // Clamp the int to the valid byte range 0–255
        intVal = Mathf.Clamp(intVal, byte.MinValue, byte.MaxValue);
        // Cast back to byte and return as the new cell value
        return (byte)intVal;
    }
}
public struct ChunkPayload {
    public Vector2Int ChunkCoord;
    public List<ushort> TileIds;
    public List<ushort> OreIds;
    public List<short> Durabilities;
    public List<ulong> EntityIds;

    public ChunkPayload(Vector2Int chunkCoord, List<ushort> tileIds, List<ushort> oreIds, List<short> durabilities, List<ulong> entityIds) {
        ChunkCoord = chunkCoord;
        TileIds = tileIds;
        OreIds = oreIds;
        Durabilities = durabilities;
        EntityIds = entityIds;
    }
}

public class ChunkManager : NetworkBehaviour {
    [SerializeField] private bool useSave = false;

    // Add more TileBase fields for other tile types

    [SerializeField] private int surfaceLevel = 400; // Y level for the surface
    [SerializeField] private float noiseScale = 0.05f; // For Perlin noise terrain generation
    [SerializeField] private int stoneDepth = 50;    // How far below the surface stone starts appearing

    [Header("Player & Loading")]
    [SerializeField] private Transform playerTransform; // Assign the player's transform
    [SerializeField] private int loadDistance = 3; // How many chunks away from the player to load (e.g., 3 means a 7x7 area around the player's chunk)
    [SerializeField] private float checkInterval = 0.5f; // How often (seconds) to check for loading/unloading chunks

    public const int CHUNK_SIZE = 16; // Size of chunks (16x16 tiles) - Power of 2 often good
    public int GetChunkSize() => CHUNK_SIZE;
    public bool IsChunkActive(Vector2Int c) => activeChunks.Contains(c);
    // --- Chunk Data ---
    [ShowInInspector]
    private Dictionary<Vector2Int, ChunkData> worldChunks = new Dictionary<Vector2Int, ChunkData>(); // Main world data
    [ShowInInspector]
    private HashSet<Vector2Int> activeChunks = new HashSet<Vector2Int>();
    private Vector2Int currentPlayerChunkCoord = new Vector2Int(int.MinValue, int.MinValue);
    private Dictionary<Vector2Int, short[,]> clientDurabilityCache = new Dictionary<Vector2Int, short[,]>();

    private WorldManager _worldManager;
    private EntityManager _entitySpawner;
    private WorldLightingManager _lightManager;

    public void DEBUGNewGen() {
        worldChunks.Clear();
        activeChunks.Clear();
        currentPlayerChunkCoord = default;
        DebugForceChunkLoad();
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

    public override void OnStartClient() {
        base.OnStartClient();
        _worldManager = FindFirstObjectByType<WorldManager>();
        if(_worldManager != null) {
            _worldManager.SetChunkManager(this);
        } else {
            Debug.LogError("ChunkManager needs a reference to world manager!");
        }
        _entitySpawner = FindFirstObjectByType<EntityManager>();
        _lightManager = FindFirstObjectByType<WorldLightingManager>();
        StartCoroutine(ClientChunkLoadingRoutine());
    }
    IEnumerator ServerChunkManagementRoutine() {
        // Ensure this only runs on the server
        if (!_worldManager.IsServerInitialized) yield break;

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
    private IEnumerator ClientChunkLoadingRoutine() {
        //Debug.Log("Chunk loading");
        HashSet<Vector2Int> clientActiveVisualChunks = new HashSet<Vector2Int>();
        Vector2Int clientCurrentChunkCoord = new Vector2Int(int.MinValue, int.MinValue);

        // Wait until the player object owned by this client is spawned and available
        // This assumes your player spawn logic is handled correctly by FishNet
        //yield return new WaitUntil(() => base.Owner != null && base.Owner.IsActive && base.Owner.IsLocalClient && PlayerController.LocalInstance != null); // Assumes a static LocalInstance on your PlayerControll
        yield return new WaitUntil(() => base.Owner != null && PlayerController.LocalInstance != null); // Assumes a static LocalInstance on your PlayerController

        Transform localPlayerTransform = PlayerController.LocalInstance.transform; // Get the locally controlled player
        // Temporary list for batching requests
        List<Vector2Int> chunksToRequestBatch = new List<Vector2Int>();
        while (true) {
            if (localPlayerTransform == null) { // Safety check if player despawns
                yield return new WaitForSeconds(checkInterval);
                continue;
            }

            Vector2Int newClientChunkCoord = WorldToChunkCoord(localPlayerTransform.position);

            if (newClientChunkCoord != clientCurrentChunkCoord) {
                clientCurrentChunkCoord = newClientChunkCoord;
                chunksToRequestBatch.Clear(); // Clear batch for new calculation
                HashSet<Vector2Int> previouslyActive = new HashSet<Vector2Int>(clientActiveVisualChunks);
                HashSet<Vector2Int> requiredVisuals = new HashSet<Vector2Int>();

                for (int xOffset = -loadDistance; xOffset <= loadDistance; xOffset++) {
                    for (int yOffset = -loadDistance; yOffset <= loadDistance; yOffset++) {
                        Vector2Int chunkCoord = new Vector2Int(clientCurrentChunkCoord.x + xOffset, clientCurrentChunkCoord.y + yOffset);
                        requiredVisuals.Add(chunkCoord);

                        if (!clientActiveVisualChunks.Contains(chunkCoord)) {
                            // Add to batch instead of immediate request
                            chunksToRequestBatch.Add(chunkCoord);
                            // Optimistically add to active chunks to prevent re-requesting
                            // before server responds. If server fails to send, it won't have visuals.
                            clientActiveVisualChunks.Add(chunkCoord);
                        }
                    }
                }
                if(chunksToRequestBatch.Count > 0) {
                    ServerRequestChunkDataBatch(chunksToRequestBatch,newClientChunkCoord);
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
        if (!IsServerInitialized) return;
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
            List<ushort> tileIds = new List<ushort>(CHUNK_SIZE * CHUNK_SIZE);
            List<ushort> oreIDs = new List<ushort>(CHUNK_SIZE * CHUNK_SIZE);
            List<short> durabilities = new List<short>(CHUNK_SIZE * CHUNK_SIZE);
            for (int y = 0; y < CHUNK_SIZE; y++) {
                for (int x = 0; x < CHUNK_SIZE; x++) {
                    tileIds.Add(chunkData.tiles[x, y]);
                    durabilities.Add(chunkData.tileDurability[x, y]);
                    oreIDs.Add(chunkData.oreID[x, y]); 
                }
            }
            // This entitySpawner gets the ids for the chunk through ServerGenerateChunkData
            var entityIds = _entitySpawner.GetEntityIDsByChunkCoord(chunkCoord);
            // 4. Send data back to the SPECIFIC client who requested it
            TargetReceiveChunkData(requester, chunkCoord, tileIds, oreIDs, durabilities, entityIds);
        }
    }
    [ServerRpc(RequireOwnership = false)]
    private void ServerRequestChunkDataBatch(List<Vector2Int> chunksToRequestBatch, Vector2Int newClientChunkCoor, NetworkConnection requester = null) {
        if (!IsServerInitialized)
            return;
        // 1. Check if data exists on server
        List<Vector2Int> chunksNotAvailable = new List<Vector2Int>();
        List<ChunkData> chunksExist = new List<ChunkData>();
        foreach (var chunkCoord in chunksToRequestBatch) {
            if (worldChunks.TryGetValue(chunkCoord, out ChunkData chunkData)) {
                chunksExist.Add(chunkData);
            } else {
                // Keep track of it and send a batch request when loop is done
                chunksNotAvailable.Add(chunkCoord);
            }
        }
        // Data not instant so request and wait for it, when its done send it to client
        StartCoroutine(_worldManager.WorldGen.GenerateChunkAsync(chunksNotAvailable, newClientChunkCoor, requester, OnChunkGenerationComplete));

        // Serialize existing data second
        foreach (var chunkData in chunksExist) {
            if (chunkData.tiles != null) {
                List<ushort> tileIds = new List<ushort>(CHUNK_SIZE * CHUNK_SIZE);
                List<ushort> oreIDs = new List<ushort>(CHUNK_SIZE * CHUNK_SIZE);
                List<short> durabilities = new List<short>(CHUNK_SIZE * CHUNK_SIZE);
                for (int y = 0; y < CHUNK_SIZE; y++) {
                    for (int x = 0; x < CHUNK_SIZE; x++) {
                        tileIds.Add(chunkData.tiles[x, y]);
                        durabilities.Add(chunkData.tileDurability[x, y]);
                        oreIDs.Add(chunkData.oreID[x, y]);
                    }
                }
                // This entitySpawner gets the ids for the chunk through ServerGenerateChunkData
                // NO CLUE HOW TO DO THIS NOW BUT I DONT CARE JUST WANT THE SAMPLING TO WORK

                //- ----------------- !!TODODODO!! ------------------//
                //var entityIds = _entitySpawner.GetEntityIDsByChunkCoord(chunkCoord);
                
                // 4. Send data back to the SPECIFIC client who requested it
                
                //TargetReceiveChunkData(requester, chunkCoord, tileIds, oreIDs, durabilities, entityIds);
            }
        }
    }

    private void OnChunkGenerationComplete(Dictionary<Vector2Int, ChunkData> list, NetworkConnection requester) {
        // We MUST de-serialize into lists so we can send it over the network, fishnet cant just send chunkdata like that 
        List<ChunkPayload> payloadData = new List<ChunkPayload>(); 
        foreach (var chunk in list) {
            List<ushort> tileIds = new List<ushort>(CHUNK_SIZE * CHUNK_SIZE);
            List<ushort> oreIDs = new List<ushort>(CHUNK_SIZE * CHUNK_SIZE);
            List<short> durabilities = new List<short>(CHUNK_SIZE * CHUNK_SIZE);
            if (list.TryGetValue(chunk.Key, out var chunkData)) {
                for (int y = 0; y < CHUNK_SIZE; y++) {
                    for (int x = 0; x < CHUNK_SIZE; x++) {
                        tileIds.Add(chunkData.tiles[x, y]);
                        durabilities.Add(chunkData.tileDurability[x, y]);
                        oreIDs.Add(chunkData.oreID[x, y]);
                    }
                }
                payloadData.Add(new ChunkPayload(chunk.Key,tileIds,oreIDs,durabilities,null));
            } else {
                Debug.LogError($"No chunk data found for chunk {chunk.Key}");
            }
        }
        TargetReceiveChunkDataMultiple(requester, payloadData); // send it to requesting client

    }
    // --- Target RPC to send chunk data to a specific client ---
    [TargetRpc]
    public void TargetReceiveChunkData(NetworkConnection conn, Vector2Int chunkCoord, List<ushort> tileIds, List<ushort> OreIDs, List<short> durabilities, List<ulong> entityIds) {
        // Executed ONLY on the client specified by 'conn'
        if (tileIds == null || tileIds.Count != CHUNK_SIZE * CHUNK_SIZE) {
            Debug.LogWarning($"Received invalid tile data for chunk {chunkCoord} from server.");
            //return;
        }
        // Store durability locally for effects 
        if(durabilities !=null)
            ClientCacheChunkDurability(chunkCoord, durabilities);
        // New active local chunk
        activeChunks.Add(chunkCoord);
        // Apply the received tiles visually
        Vector3Int chunkOriginCell = ChunkCoordToCellOrigin(chunkCoord);
        BoundsInt chunkBounds = new BoundsInt(chunkOriginCell.x, chunkOriginCell.y, 0, CHUNK_SIZE, CHUNK_SIZE, 1);
        TileBase[] tilesToSet = new TileBase[CHUNK_SIZE * CHUNK_SIZE];
        TileBase[] oresToSet = new TileBase[CHUNK_SIZE * CHUNK_SIZE];
        for (int i = 0; i < tileIds.Count; i++) {
            tilesToSet[i] = App.ResourceSystem.GetTileByID(tileIds[i]);
        }
        _worldManager.SetTiles(chunkBounds, tilesToSet);
        for (int i = 0; i < OreIDs.Count; i++) {
            // Don't add if there is no ore, TODO, we could just shorten the array by filtering out invalidID before we get here
            if (OreIDs[i] == ResourceSystem.InvalidID)
                continue; // skip
            oresToSet[i] = App.ResourceSystem.GetTileByID(OreIDs[i]);
        }
        _worldManager.SetOres(chunkBounds, oresToSet);

        // Update lighting
        _lightManager.RequestLightUpdate();

        // Spawn enemies client only
        if (entityIds != null) {
            _entitySpawner.ProcessReceivedEntityIds(chunkCoord, entityIds);
        }
        // Debug.Log($"Client received and visually loaded chunk {chunkCoord}");
    }
    [TargetRpc]
    public void TargetReceiveChunkDataMultiple(NetworkConnection conn, List<ChunkPayload> chunks) {
        // Executed ONLY on the client specified by 'conn'
        // --- Attempt to use SetTilesBlock ---
        if (TrySetTilesBlockOptimized(chunks)) {
            //_groundTilemap.RefreshAllTiles(); // Or selective refresh if possible
            return; // Successfully used SetTilesBlock
        }

        // --- Fallback to SetTiles per chunk if optimization wasn't possible ---
        Debug.Log("Falling back to SetTiles per chunk.");
        foreach (var chunkPayload in chunks) {
            ApplySingleChunkPayload(chunkPayload);
        }
            // for (int i = 0; i < chunk.TileIds.Count; i++) {
            //     tilesToSet[i] = App.ResourceSystem.GetTileByID(chunk.TileIds[i]);
            // }
            // _worldManager.SetTiles(chunkBounds, tilesToSet);
            // for (int i = 0; i < chunk.OreIds.Count; i++) {
            //     // Don't add if there is no ore, TODO, we could just shorten the array by filtering out invalidID before we get here
            //     if (chunk.OreIds[i] == ResourceSystem.InvalidID)
            //         continue; // skip
            //     oresToSet[i] = App.ResourceSystem.GetTileByID(chunk.OreIds[i]);
            // }
            // _worldManager.SetOres(chunkBounds, oresToSet);
            // // Update lighting
            // _lightManager.RequestLightUpdate();

            // Spawn enemies client only
            
            // TODO 
            //if (entityIds != null) {
            //    _entitySpawner.ProcessReceivedEntityIds(chunkCoord, entityIds);
            //}
        
        // Debug.Log($"Client received and visually loaded chunk {chunkCoord}");
    }

    private void ApplySingleChunkPayload(ChunkPayload chunkPayload) {
        ClientCacheChunkDurability(chunkPayload.ChunkCoord, chunkPayload.Durabilities);
        // New active local chunk
        activeChunks.Add(chunkPayload.ChunkCoord);
        // Apply the received tiles visually
        Vector3Int[] tilePositions = new Vector3Int[CHUNK_SIZE * CHUNK_SIZE];
        TileBase[] tilesToSet = new TileBase[CHUNK_SIZE * CHUNK_SIZE];
        TileBase[] oresToSet = new TileBase[CHUNK_SIZE * CHUNK_SIZE];
        int tileIndex = 0;
        for (int y = 0; y < CHUNK_SIZE; y++) {
            for (int x = 0; x < CHUNK_SIZE; x++) {
                ushort tileID = chunkPayload.TileIds[tileIndex];
                TileBase tile = App.ResourceSystem.GetTileByID(tileID); // Use your resource system

                // Calculate world position for the tile
                // Tilemap uses cell coordinates. ChunkCoord is chunk-level.
                int worldTileX = chunkPayload.ChunkCoord.x * CHUNK_SIZE + x;
                int worldTileY = chunkPayload.ChunkCoord.y * CHUNK_SIZE + y;

                tilePositions[tileIndex] = new Vector3Int(worldTileX, worldTileY, 0); // Assuming Z=0 for 2D tilemap
                tilesToSet[tileIndex] = tile; // Can be null to clear a tile

                tileIndex++;
            }
        }
        Vector3Int chunkOriginCell = ChunkCoordToCellOrigin(chunkPayload.ChunkCoord);
        BoundsInt chunkBounds = new BoundsInt(chunkOriginCell.x, chunkOriginCell.y, 0, CHUNK_SIZE, CHUNK_SIZE, 1);
        _worldManager.SetTiles(chunkBounds, tilesToSet);
    }
    private bool TrySetTilesBlockOptimized(List<ChunkPayload> chunkPayloads) {
        if (chunkPayloads.Count == 0)
            return false;

        // 1. Calculate overall bounds and check for contiguity.
        int minChunkX = chunkPayloads[0].ChunkCoord.x;
        int maxChunkX = chunkPayloads[0].ChunkCoord.x;
        int minChunkY = chunkPayloads[0].ChunkCoord.y;
        int maxChunkY = chunkPayloads[0].ChunkCoord.y;

        HashSet<Vector2Int> receivedChunkCoords = new HashSet<Vector2Int>();
        foreach (var payload in chunkPayloads) {
            minChunkX = Mathf.Min(minChunkX, payload.ChunkCoord.x);
            maxChunkX = Mathf.Max(maxChunkX, payload.ChunkCoord.x);
            minChunkY = Mathf.Min(minChunkY, payload.ChunkCoord.y);
            maxChunkY = Mathf.Max(maxChunkY, payload.ChunkCoord.y);
            receivedChunkCoords.Add(payload.ChunkCoord);
        }
        int expectedNumChunksX = (maxChunkX - minChunkX) + 1;
        int expectedNumChunksY = (maxChunkY - minChunkY) + 1;
        int expectedTotalChunks = expectedNumChunksX * expectedNumChunksY;
        if (chunkPayloads.Count != expectedTotalChunks) {
            // Not a dense rectangle of chunks (or duplicate chunk coords were sent)
            Debug.Log($"SetTilesBlock optimization: Payload count ({chunkPayloads.Count}) doesn't match expected count for bounds ({expectedTotalChunks}).");
            return false;
        }
        // Verify all chunks within the bounds are present
        for (int cy = 0; cy < expectedNumChunksY; cy++) {
            for (int cx = 0; cx < expectedNumChunksX; cx++) {
                if (!receivedChunkCoords.Contains(new Vector2Int(minChunkX + cx, minChunkY + cy))) {
                    // A chunk is missing within the calculated rectangle
                    Debug.Log($"SetTilesBlock optimization: Missing chunk {new Vector2Int(minChunkX + cx, minChunkY + cy)} within bounds.");
                    return false;
                }
            }
        }
        // If we reach here, chunks form a contiguous rectangle.
        Debug.Log($"SetTilesBlock optimization: Payloads form a contiguous rectangle from ({minChunkX},{minChunkY}) to ({maxChunkX},{maxChunkY}).");

        // 2. Prepare data for SetTilesBlock.
        int blockWidthInTiles = expectedNumChunksX * CHUNK_SIZE;
        int blockHeightInTiles = expectedNumChunksY * CHUNK_SIZE;
        TileBase[] blockTileBases = new TileBase[blockWidthInTiles * blockHeightInTiles];

        // Sort payloads to ensure correct order when populating blockTileBases.
        // We need to iterate through them as if we're filling the larger tile block row by row (of chunks),
        // and within each chunk, row by row (of tiles).
        var sortedPayloads = chunkPayloads.OrderBy(p => p.ChunkCoord.y).ThenBy(p => p.ChunkCoord.x).ToList();

        // Fill blockTileBases
        // The blockTileBases array is filled tile-row by tile-row for the entire block.
        for (int chunkRowY = 0; chunkRowY < expectedNumChunksY; chunkRowY++) // Iterate through rows of chunks
        {
            for (int tileRowInChunk = 0; tileRowInChunk < CHUNK_SIZE; tileRowInChunk++) // Iterate through tile rows within a chunk row
            {
                for (int chunkColX = 0; chunkColX < expectedNumChunksX; chunkColX++) // Iterate through chunks in the current chunk row
                {
                    // Find the correct payload for (minChunkX + chunkColX, minChunkY + chunkRowY)
                    // This assumes sortedPayloads helps, but direct access might be better.
                    // Let's make a dictionary for faster lookup.
                    Dictionary<Vector2Int, ChunkPayload> payloadMap = sortedPayloads.ToDictionary(p => p.ChunkCoord);
                    ChunkPayload currentChunkPayload = payloadMap[new Vector2Int(minChunkX + chunkColX, minChunkY + chunkRowY)];

                    if (currentChunkPayload.TileIds == null || currentChunkPayload.TileIds.Count != CHUNK_SIZE * CHUNK_SIZE) {
                        Debug.LogError($"Critical error during SetTilesBlock: Invalid tile data for chunk {currentChunkPayload.ChunkCoord}. This should have been caught earlier.");
                        return false; // Or handle more gracefully
                    }

                    for (int tileColInChunk = 0; tileColInChunk < CHUNK_SIZE; tileColInChunk++) // Iterate through tiles in current chunk's current tile row
                    {
                        int payloadTileIndex = tileRowInChunk * CHUNK_SIZE + tileColInChunk; // Index within the payload's 1D list
                        ushort tileID = currentChunkPayload.TileIds[payloadTileIndex];
                        TileBase tileBase = App.ResourceSystem.GetTileByID(tileID);

                        // Calculate index in the final blockTileBases array
                        // Overall Y tile index: (chunkRowY * CHUNK_TILE_DIMENSION + tileRowInChunk)
                        // Overall X tile index: (chunkColX * CHUNK_TILE_DIMENSION + tileColInChunk)
                        int blockTileY = chunkRowY * CHUNK_SIZE + tileRowInChunk;
                        int blockTileX = chunkColX * CHUNK_SIZE + tileColInChunk;
                        int blockArrayIndex = blockTileY * blockWidthInTiles + blockTileX;

                        if (blockArrayIndex < 0 || blockArrayIndex >= blockTileBases.Length) {
                            Debug.LogError($"SetTilesBlock index out of bounds: ({blockTileX}, {blockTileY}) -> index {blockArrayIndex}. blockWidth: {blockWidthInTiles}, Length: {blockTileBases.Length}");
                            return false; // Should not happen if logic is correct
                        }
                        blockTileBases[blockArrayIndex] = tileBase;
                    }
                }
            }
        }

        // 3. Define the bounds for SetTilesBlock.
        // The position for BoundsInt is the bottom-left corner of the block in tile coordinates.
        Vector3Int blockOriginTileCoord = new Vector3Int(
            minChunkX * CHUNK_SIZE,
            minChunkY * CHUNK_SIZE,
            0 // Assuming Z=0
        );

        BoundsInt blockBounds = new BoundsInt(blockOriginTileCoord, new Vector3Int(blockWidthInTiles, blockHeightInTiles, 1));

        // 4. Call SetTilesBlock.
        _worldManager.SetTiles(blockBounds, blockTileBases);
        Debug.Log($"Applied {blockWidthInTiles}x{blockHeightInTiles} tiles using SetTilesBlock at {blockOriginTileCoord}.");
        return true;
    }
    private void ClientCacheChunkDurability(Vector2Int chunkCoord, List<short> durabilityList) {
        if (!clientDurabilityCache.ContainsKey(chunkCoord)) {
            clientDurabilityCache[chunkCoord] = new short[CHUNK_SIZE, CHUNK_SIZE];
        }

        short[,] chunkDurability = clientDurabilityCache[chunkCoord];
        int index = 0;
        for (int y = 0; y < CHUNK_SIZE; y++) {
            for (int x = 0; x < CHUNK_SIZE; x++) {
                chunkDurability[x, y] = durabilityList[index++];
            }
        }
        // We might want to remove entries from this cache when chunks are visually unloaded
        // in ClientChunkLoadingRoutine to save client memory.
    }

    public int GetClientCachedDurability(Vector3Int cellPos) {
        Vector2Int chunkCoord = CellToChunkCoord(cellPos);
        if (clientDurabilityCache.TryGetValue(chunkCoord, out short[,] chunkDurability)) {
            int localX = cellPos.x - chunkCoord.x * CHUNK_SIZE;
            int localY = cellPos.y - chunkCoord.y * CHUNK_SIZE;
            if (localX >= 0 && localX < CHUNK_SIZE && localY >= 0 && localY < CHUNK_SIZE) {
                return chunkDurability[localX, localY];
            }
        }
        return -1; // Default or error state
    }
    // --- Helper to generate chunk data ON SERVER ONLY ---
    private ChunkData ServerGenerateChunkData(Vector2Int chunkCoord) {
        // Ensure called only on server
        if (!IsServerInitialized) return null;

        // Check if it already exists (maybe generated by another player's request)
        if (worldChunks.ContainsKey(chunkCoord)) return worldChunks[chunkCoord];

        // --- Prep ---
        Vector3Int chunkOriginCell = ChunkCoordToCellOrigin(chunkCoord);

        //_worldManager.RequestGenerateChunk(chunkCoord);

        ChunkData chunkData = null; // placeholder
        
        //ChunkData chunkData = WorldGen.GenerateChunk(chunkSize, chunkOriginCell, this, out var enemyList);

        //_worldManager.BiomeManager.CalculateBiomeForChunk(chunkCoord, chunkData);
        // --- Add Generated Entities to Persistent Store AND Chunk Map ---
        //if (enemyList != null && enemyList.Count > 0) {
        //    _entitySpawner.AddGeneratedEntityData(chunkCoord, enemyList);
        //}
        
        // --- Finalization ---
        chunkData.hasBeenGenerated = true;
        worldChunks.Add(chunkCoord, chunkData);
        return chunkData;

    }
    // --- Visually Deactivate Chunk (Client Side) ---
    private void ClientDeactivateVisualChunk(Vector2Int chunkCoord) {
        Vector3Int chunkOriginCell = ChunkCoordToCellOrigin(chunkCoord);
        BoundsInt chunkBounds = new BoundsInt(chunkOriginCell.x, chunkOriginCell.y, 0, CHUNK_SIZE, CHUNK_SIZE, 1);
        TileBase[] clearTiles = new TileBase[CHUNK_SIZE * CHUNK_SIZE]; // Array of nulls
        _worldManager.SetTiles(chunkBounds, clearTiles);
        _worldManager.SetOres(chunkBounds, clearTiles);
        //Debug.Log($"Client visually deactivated chunk {chunkCoord}");
        activeChunks.Remove(chunkCoord);
        // Entities, note this will not work in multiplayer now, 
        _entitySpawner.RemoveEntitieAtChunk(chunkCoord);
        
    }
    // --- Tile Modification ---
    // This is the entry point called by the PlayerController's ServerRpc, this is usually after all checks have been done already
    public void ServerRequestModifyTile(Vector3Int cellPos, ushort newTileId) {
        // Must run on server
        if (!IsServerInitialized) return;
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
        int localX = cellPos.x - chunkCoord.x * CHUNK_SIZE;
        int localY = cellPos.y - chunkCoord.y * CHUNK_SIZE;

        if (localX >= 0 && localX < CHUNK_SIZE && localY >= 0 && localY < CHUNK_SIZE) {
            // Check if the tile is actually changing
            if (chunk.tiles[localX, localY] != newTileId) {
                // --- Update SERVER data FIRST ---
                chunk.tiles[localX, localY] = newTileId;
                chunk.isModified = true; // Mark chunk as modified for saving
                chunk.tileDurability[localX, localY] = -1; // Reset to default state
                // --- Update Server's OWN visuals (optional but good for host) ---
                _worldManager.SetTile(cellPos, newTileId);
                // --- BROADCAST change to ALL clients ---
                ObserversUpdateTileDurability(cellPos, -1);
                _worldManager.ObserversUpdateTile(cellPos, newTileId); // TODO check if this works it might break because we call it in the parent

                // Entity behaviour might change state, notify entitymanager
                _entitySpawner.NotifyTileChanged(cellPos, chunkCoord, newTileId);
            }
        } else {
            Debug.LogWarning($"Server: Invalid local coordinates for modification at {cellPos}");
        }
    }

    // --- RPC to inform clients of durability change ---
    [ObserversRpc]
    private void ObserversUpdateTileDurability(Vector3Int cellPos, short newDurability) {
        // Runs on all clients
        if(newDurability == -1)
            _lightManager.RequestLightUpdate(); // Tile broke so update lights
        // Update local cache if you have one
        Vector2Int chunkCoord = CellToChunkCoord(cellPos);
        if (clientDurabilityCache.TryGetValue(chunkCoord, out short[,] chunkDurability)) {
            int localX = cellPos.x - chunkCoord.x * CHUNK_SIZE;
            int localY = cellPos.y - chunkCoord.y * CHUNK_SIZE;
            if (localX >= 0 && localX < CHUNK_SIZE && localY >= 0 && localY < CHUNK_SIZE) {
                chunkDurability[localX, localY] = newDurability;
                UpdateTileVisuals(cellPos, newDurability);
            }
        }
    }
    // New method on server to handle receiving damage requests
    public void ServerProcessDamageTile(Vector3Int cellPos, short damageAmount, NetworkConnection sourceConnection = null) {
        if (!IsServerInitialized) return;
        Vector2Int chunkCoord = CellToChunkCoord(cellPos);
        // Ensure chunk data exists (generate if necessary)
        if (!worldChunks.TryGetValue(chunkCoord, out ChunkData chunk)) {
            chunk = ServerGenerateChunkData(chunkCoord);
            if (chunk == null) { Debug.LogError($"Server failed to get/generate chunk {chunkCoord} for damage."); return; }
        }

        // Calculate local coordinates
        int localX = cellPos.x - chunkCoord.x * CHUNK_SIZE;
        int localY = cellPos.y - chunkCoord.y * CHUNK_SIZE;

        if (localX < 0 || localX >= CHUNK_SIZE || localY < 0 || localY >= CHUNK_SIZE) { Debug.LogWarning($"Server: Invalid local coordinates {localX},{localY} for damage at {cellPos}"); return; }


        // --- Get Tile Type & Properties ---
        TileSO targetTile = App.ResourceSystem.GetTileByID(chunk.tiles[localX, localY]);
        var ore = App.ResourceSystem.GetTileByID(chunk.oreID[localX, localY]);
        // if ore use Ore tilebase, not stone
        if (targetTile != null) {
            if (ore != null) targetTile = ore;
            Debug.Log("Processing damage!!: ");
            if (targetTile.maxDurability <= 0) return; // Indestructible tile
            Debug.Log("Reutneutnet!!: ");

            // --- Apply Damage ---
            short currentDurability = chunk.tileDurability[localX, localY];
            if (currentDurability < 0) // Was at full health (-1 sentinel)
            {
                currentDurability = targetTile.maxDurability;
            }

            short newDurability = (short)(currentDurability - damageAmount);

            // Mark as modified ONLY if durability actually changed
            if (newDurability != chunk.tileDurability[localX, localY]) {
                chunk.isModified = true;
            }
            chunk.tileDurability[localX, localY] = newDurability;


            // --- Check for Destruction ---
            if (newDurability <= 0) {
                // Destroy Tile: Set to Air (will broadcast visual change via existing RPC)
                // Setting durability back to -1 for the (now air) tile in the data is good practice
                chunk.tileDurability[localX, localY] = -1;
                // TODO air tile type should be of the dominant biome of the chunk
                ServerRequestModifyTile(cellPos, 0); // Tile ID 0 = Air/Null

                // Spawn Drops (Server-side)
                SpawnDrops(targetTile, _worldManager.CellToWorld(cellPos) + new Vector3(0.5f, 0.5f, 0)); // Drop at cell center

                // Spawn Break Effect (Broadcast to clients)
                if (targetTile.breakEffectPrefab != null)
                    ObserversSpawnEffect(targetTile.breakEffectPrefab, _worldManager.CellToWorld(cellPos) + new Vector3(0.5f, 0.5f, 0)); // Pass prefab path or ID if effects aren't NetworkObjects
            } else {
                // Tile Damaged, Not Destroyed: Broadcast new durability
                ObserversUpdateTileDurability(cellPos, newDurability);

                // Spawn Hit Effect (Broadcast to clients)
                if (targetTile.hitEffectPrefab != null)
                    ObserversSpawnEffect(targetTile.hitEffectPrefab, _worldManager.CellToWorld(cellPos) + new Vector3(0.5f, 0.5f, 0));
            }
        } else {
            Debug.LogError("ResourceSystem returned NULL tile");
        }
    }
    // --- Server-side method to handle spawning drops ---
    // We might want to move this to WorldManager but eh
    private void SpawnDrops(TileSO sourceTile, Vector3 position) {
        if (!IsServerInitialized) return;
        if (sourceTile.dropTable == null) return;

        foreach (ItemDropInfo dropInfo in sourceTile.dropTable.drops) {
            if (dropInfo.ItemData.droppedPrefab != null && Random.value <= dropInfo.dropChance) {
                int amountToDrop = Random.Range(dropInfo.minAmount, dropInfo.maxAmount + 1);
                for (int i = 0; i < amountToDrop; i++) {
                    // Slightly randomize drop position
                    Vector3 spawnPos = position + (Vector3)Random.insideUnitCircle * 0.3f;

                    // Instantiate the PREFAB, then spawn the INSTANCE
                    GameObject dropInstance = Instantiate(dropInfo.ItemData.droppedPrefab, spawnPos, Quaternion.identity);
                    base.ServerManager.Spawn(dropInstance); // FishNet spawn call
                    
                    var worldItem = dropInstance.GetComponent<DroppedEntity>();
                    if (worldItem != null) {
                        var id = App.ResourceSystem.GetIDByItem(dropInfo.ItemData);
                        worldItem.ServerInitialize(id, 1); // we either drop 1, or just specify amountToDrop and don't loop
                        Debug.Log($"[Server] Player {base.Owner.ClientId} dropped {1} of {worldItem.name}.");
                        // No need to send TargetRpc for success IF Server_RemoveItem sends update
                    } else {
                        Debug.LogError($"[Server] Dropped prefab {dropInfo.ItemData.droppedPrefab.name} is missing a WorldItem component!");
                        ServerManager.Despawn(dropInstance); // Despawn broken itemk
                    }
                }
            }
        }
    }

    // --- RPC to spawn visual effects (non-networked objects usually) ---
    [ObserversRpc(RunLocally = true)] // RunLocally = true ensures host sees it too without delay
    private void ObserversSpawnEffect(GameObject effectPrefab, Vector3 position) {
        // Runs on all clients (and host if RunLocally = true)
        if (effectPrefab != null) {
            // Consider using an object pool for effects
            Instantiate(effectPrefab, position, Quaternion.identity);
            // Set a self-destruct timer on the effect particle system
        }
    }

    // Placeholder for client-side visual updates (e.g., crack overlays)
    private void UpdateTileVisuals(Vector3Int cellPos, short currentDurability) {

        TileBase crackTile = GetCrackTileForDurability(cellPos, currentDurability); // Find appropriate crack sprite

        _worldManager.SetOverlayTile(cellPos, crackTile);
    }
    // Activates a chunk (makes it visible), pulling data from ChunkData, this might be usefull later if we want to see the chunk
    // Without spawning or doing any of the loading, (maybe for maps?)
    void ActivateChunk(Vector2Int chunkCoord, ChunkData chunkData) {
        // If chunk was loaded but never visually activated yet, ensure tiles are set
        if (!activeChunks.Contains(chunkCoord)) {
            Vector3Int chunkOriginCell = ChunkCoordToCellOrigin(chunkCoord);
            BoundsInt chunkBounds = new BoundsInt(chunkOriginCell.x, chunkOriginCell.y, 0, CHUNK_SIZE, CHUNK_SIZE, 1);
            TileSO[] tilesToSet = new TileSO[CHUNK_SIZE * CHUNK_SIZE];
            int tileIndex = 0;
            for (int localY = 0; localY < CHUNK_SIZE; localY++) {
                for (int localX = 0; localX < CHUNK_SIZE; localX++) {
                    var tile = App.ResourceSystem.GetTileByID(chunkData.tiles[localX, localY]);
                    if(tile != null) {
                        tilesToSet[tileIndex++] = tile;
                    } else {
                        Debug.LogError("Resource system returned NULL tile, defaulting to air");
                        tilesToSet[tileIndex++] = App.ResourceSystem.GetTileByID(0);
                    }
                }
            }
            _worldManager.SetTiles(chunkBounds, tilesToSet);
        }
        // (No else needed, if already active, do nothing visually)
    }

    private TileBase GetCrackTileForDurability(Vector3Int cellPos, int currentDurability) {
        var t = _worldManager.GetFirstTileAtCellPos(cellPos);
        //var ore = _worldManager.GetOreFromID(chunk.oreID[localX, localY]);
        //var ore = _worldManager.GetOreAtCellPos(cellPos);
        if (t is TileSO tile) {
            var r = tile.GetDurabilityRatio(currentDurability);
            //Debug.Log($"Durability ratio is: {r} current dur is: {currentDurability} max is {tile.maxDurability}");
            if (r > 0.75) {
                return null; // No cracks for high durability.
            } else if (r > 0.50) {
                return tile.breakVersions[0];
            } else if (r > 0.25) {
                return tile.breakVersions[1];
            } else if (r > 0) {
                return tile.breakVersions[2];
            }
        }
        return null;

    }
    // Removes a chunk's tiles from the tilemap (clears visually)
    void DeactivateChunk(Vector2Int chunkCoord) {
        Vector3Int chunkOriginCell = ChunkCoordToCellOrigin(chunkCoord);
        BoundsInt chunkBounds = new BoundsInt(chunkOriginCell.x, chunkOriginCell.y, 0, CHUNK_SIZE, CHUNK_SIZE, 1);

        // Create an array full of nulls to clear the area
        TileBase[] clearTiles = new TileBase[CHUNK_SIZE * CHUNK_SIZE];
        // No need to fill it explicitly, default is null

        _worldManager.SetTiles(chunkBounds, clearTiles);
        // Add similar SetTilesBlock calls for other tilemap layers if needed
    }

    public void AddChunkData(Vector2Int chunkCoord, ChunkData data) {
        worldChunks.Add(chunkCoord, data);
    }

    // =============================================
    // === Coordinate Conversion Helper Methods ===
    // =============================================

    public Vector2Int WorldToChunkCoord(Vector3 worldPosition) {
        Vector3Int cellPos = _worldManager.WorldToCell(worldPosition);
        // Integer division automatically floors, which is what we want.
        // Be careful if chunkSize is not a factor of world origin/coordinates start negative.
        // Using Mathf.FloorToInt ensures correct behavior with negative coordinates.
        int chunkX = Mathf.FloorToInt((float)cellPos.x / CHUNK_SIZE);
        int chunkY = Mathf.FloorToInt((float)cellPos.y / CHUNK_SIZE);
        return new Vector2Int(chunkX, chunkY);
    }

    public Vector2Int CellToChunkCoord(Vector3Int cellPosition) {
        int chunkX = Mathf.FloorToInt((float)cellPosition.x / CHUNK_SIZE);
        int chunkY = Mathf.FloorToInt((float)cellPosition.y / CHUNK_SIZE);
        return new Vector2Int(chunkX, chunkY);
    }

    // Gets the cell coordinate of the bottom-left tile OF a chunk
    public Vector3Int ChunkCoordToCellOrigin(Vector2Int chunkCoord) {
        return new Vector3Int(chunkCoord.x * CHUNK_SIZE, chunkCoord.y * CHUNK_SIZE, 0);
    }

    internal void ClearWorldChunks() {
        worldChunks.Clear(); // Clear existing runtime chunk data
        activeChunks.Clear(); // Clear active chunks before loading
    }

    internal bool CanWriteData(Vector2Int chunkCoord) {
        return worldChunks.ContainsKey(chunkCoord);
    }
    public ushort GetTileAtWorldPos(int x, int y) {
        var chunkCoord = WorldToChunkCoord(new(x, y));
        if(worldChunks.TryGetValue(chunkCoord, out var chunk)) {
            // Calculate local tile indices within the chunk
            int localX = x - chunkCoord.x * CHUNK_SIZE;
            int localY = y - chunkCoord.y * CHUNK_SIZE;

            // Ensure indices are within the chunk bounds
            if (localX < 0 || localX >= CHUNK_SIZE || localY < 0 || localY >= CHUNK_SIZE)
                return ResourceSystem.InvalidID;

            // Return the tile from the chunk's tile array
            return chunk.tiles[localX, localY];
        } else {
            return ResourceSystem.InvalidID; // Chunk not generated yet
        }
    }
    // Tile should be as unique global position,
    internal (HashSet<Vector2Int>, Dictionary<Vector2Int, BiomeType>) GetAllNonSolidTilesInLoadedChunks() {
        var validTiles = new HashSet<Vector2Int>();
        var tilesByBiome = new Dictionary<Vector2Int, BiomeType>();
        foreach (var chunkCoord in activeChunks) {
            if (!worldChunks.TryGetValue(chunkCoord, out ChunkData chunkData))
                continue;


            // nested for-loop for best performance
            for (int localX = 0; localX < CHUNK_SIZE; localX++) {
                for (int localY = 0; localY < CHUNK_SIZE; localY++) {
                    TileSO tile = App.ResourceSystem.GetTileByID(chunkData.tiles[localX, localY]);
                    if (tile.IsSolid || tile == null)
                        continue;
                    // biome
                    byte biomeID = chunkData.biomeID[localX, localY];

                    // compute global cell position
                    int globalX = chunkCoord.x * CHUNK_SIZE + localX;
                    int globalY = chunkCoord.y * CHUNK_SIZE + localY;
                    var tilePos = new Vector2Int(globalX, globalY);
                    validTiles.Add(tilePos);

                    tilesByBiome.Add(tilePos, (BiomeType)biomeID);
                }
            }
        }

        return (validTiles,tilesByBiome);
    }
}
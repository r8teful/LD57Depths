using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections;
using System.Collections.Generic;
using FishNet.Object;
using FishNet.Connection;
using Sirenix.OdinInspector;
using UnityEditor;
// Represents the runtime data for a single chunk (tile references)
public class ChunkData {
    public ushort[,] tiles; // The ground layer 
    public ushort[,] oreID;      // Second "ore" layer
    public float[,] tileDurability; // Third "dmg" layer
#if UNITY_EDITOR
    [TableMatrix(DrawElementMethod = "DrawElement")]
#endif
    public byte[,] biomeID;
    public bool isModified = false; // Flag to track if chunk has changed since load/generation
    public bool hasBeenGenerated = false; // Flag to prevent regenerating loaded chunks

    public ChunkData() {
        tiles = new ushort[ChunkManager.CHUNK_SIZE, ChunkManager.CHUNK_SIZE];
        tileDurability = new float[ChunkManager.CHUNK_SIZE, ChunkManager.CHUNK_SIZE];
        oreID = new ushort[ChunkManager.CHUNK_SIZE, ChunkManager.CHUNK_SIZE];
        biomeID = new byte[ChunkManager.CHUNK_SIZE, ChunkManager.CHUNK_SIZE];
        // Initialize defaults
        for (int y = 0; y < ChunkManager.CHUNK_SIZE; ++y)
        for (int x = 0; x < ChunkManager.CHUNK_SIZE; ++x) {
            tileDurability[x, y] = -1;
            oreID[x, y] = 0;
            biomeID[x, y] = 0;
        }
    }

    public ChunkData(int chunkSizeX, int chunkSizeY) {
        tiles = new ushort[chunkSizeX, chunkSizeY];
        tileDurability = new float[chunkSizeX, chunkSizeY];
        for (int y = 0; y < chunkSizeY; ++y) {
            for (int x = 0; x < chunkSizeX; ++x) {
                tileDurability[x, y] = -1; // Default state
            }
        }
        oreID = new ushort[chunkSizeX, chunkSizeY];
        for (int y = 0; y < chunkSizeY; ++y) {
            for (int x = 0; x < chunkSizeX; ++x) {
                oreID[x, y] = 0; // Default state
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
#if UNITY_EDITOR
    static byte DrawElement(Rect rect, byte value) {
        // Draw an int field in the given rect, initializing with the current byte value
        int intVal = EditorGUI.IntField(rect, value);
        // Clamp the int to the valid byte range 0–255
        intVal = Mathf.Clamp(intVal, byte.MinValue, byte.MaxValue);
        // Cast back to byte and return as the new cell value
        return (byte)intVal;
    }
#endif
}
// Used by CLIENT
public struct ChunkPayload {
    public Vector2Int ChunkCoord;
    public List<ushort> TileIds;
    public List<ushort> OreIds;
    public List<float> Durabilities;
    public List<ulong> EntityPersistantIds;

    public ChunkPayload(Vector2Int chunkCoord, List<ushort> tileIds, List<ushort> oreIds, List<float> durabilities, List<ulong> entityIds) {
        ChunkCoord = chunkCoord;
        TileIds = tileIds;
        OreIds = oreIds;
        Durabilities = durabilities;
        EntityPersistantIds = entityIds;
    }
    public ChunkPayload(ChunkPayload fromJobs, List<ulong> entityIds) {
        ChunkCoord = fromJobs.ChunkCoord;
        TileIds = fromJobs.TileIds;
        OreIds = fromJobs.OreIds;
        Durabilities = fromJobs.Durabilities;
        EntityPersistantIds = entityIds;
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
    public bool DidOnStart() => _didOnStart;
    private bool _didOnStart;
    public Dictionary<Vector2Int, ChunkData> GetWorldChunks() => worldChunks; // Used by save manager
    public bool IsChunkActive(Vector2Int c) => activeChunks.Contains(c);
    // --- Chunk Data ---
    [ShowInInspector]
    private Dictionary<Vector2Int, ChunkData> worldChunks = new Dictionary<Vector2Int, ChunkData>(); // Main world data
    [ShowInInspector]
    private HashSet<Vector2Int> activeChunks = new HashSet<Vector2Int>();
    private Vector2Int currentPlayerChunkCoord = new Vector2Int(int.MinValue, int.MinValue);
    private Dictionary<Vector2Int, float[,]> clientDurabilityCache = new Dictionary<Vector2Int, float[,]>();

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
        Vector2Int newClientChunkCoord = WorldToChunkCoord(NetworkedPlayer.LocalInstance.transform.position);
        for (int xOffset = -loadDistance; xOffset <= loadDistance; xOffset++) {
            for (int yOffset = -loadDistance; yOffset <= loadDistance; yOffset++) {
                Vector2Int chunkCoord = new Vector2Int(newClientChunkCoord.x + xOffset, newClientChunkCoord.y + yOffset);
                // ServerRequestChunkData(chunkCoord); // TODO doesn't exist anymore
                
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
        _entitySpawner = EntityManager.Instance;
        _lightManager = FindFirstObjectByType<WorldLightingManager>();
        _didOnStart = true;
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
        yield return new WaitUntil(() => base.Owner != null && NetworkedPlayer.LocalInstance != null); // Assumes a static LocalInstance on your PlayerController

        Transform localPlayerTransform = NetworkedPlayer.LocalInstance.transform; // Get the locally controlled player
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
  
    [ServerRpc(RequireOwnership = false)]
    private void ServerRequestChunkDataBatch(List<Vector2Int> chunksToRequestBatch, Vector2Int newClientChunkCoor, NetworkConnection requester = null) {
        if (!IsServerInitialized)
            return;
        // 1. Check if data exists on server
        List<Vector2Int> chunksNotAvailable = new List<Vector2Int>();
        Dictionary<Vector2Int,ChunkData> chunksExist = new Dictionary<Vector2Int, ChunkData>();
        foreach (var chunkCoord in chunksToRequestBatch) {
            if (worldChunks.TryGetValue(chunkCoord, out ChunkData chunkData)) {
                chunksExist.Add(chunkCoord,chunkData);
            } else {
                // Keep track of it and send a batch request when loop is done
                chunksNotAvailable.Add(chunkCoord);
            }
        }
        // Data not instant so request and wait for it, when its done send it to client
        StartCoroutine(_worldManager.WorldGen.GenerateChunkAsync(chunksNotAvailable, newClientChunkCoor, requester, OnChunkGenerationComplete));

        // Serialize existing data second
        List<ChunkPayload> dataToSend = new List<ChunkPayload>();
        foreach (var kvp in chunksExist) {
            var chunkData = kvp.Value;
            List<ushort> tileIds = new List<ushort>(CHUNK_SIZE * CHUNK_SIZE);
            List<ushort> oreIDs = new List<ushort>(CHUNK_SIZE * CHUNK_SIZE);
            List<float> durabilities = new List<float>(CHUNK_SIZE * CHUNK_SIZE);
            for (int y = 0; y < CHUNK_SIZE; y++) {
                for (int x = 0; x < CHUNK_SIZE; x++) {
                    tileIds.Add(chunkData.tiles[x, y]);
                    durabilities.Add(chunkData.tileDurability[x, y]);
                    oreIDs.Add(chunkData.oreID[x, y]);
                }
            }
            // This entitySpawner gets the ids for the chunk through ServerGenerateChunkData
            var entityIds = _entitySpawner.GetEntityIDsByChunkCoord(kvp.Key);

            dataToSend.Add(new ChunkPayload(kvp.Key, tileIds, oreIDs, durabilities, entityIds));
        }
        if(dataToSend.Count > 0) {
            // Only actually send if we added existing data to the list
            TargetReceiveChunkDataMultiple(requester, dataToSend);
        }
    }

    private void OnChunkGenerationComplete(List<ChunkPayload> payloadData, Dictionary<Vector2Int, ChunkData> severData, Dictionary<Vector2Int,List<EntitySpawnInfo>> entities, NetworkConnection requester) {
        // Store data on server
        foreach (var data in severData) {
            data.Value.hasBeenGenerated = true;
            worldChunks.Add(data.Key, data.Value);
        }
        // Biome data also is stored on the server
        foreach (var data in severData) {
            data.Value.hasBeenGenerated = true;
            _worldManager.BiomeManager.AddNewData(data.Key, data.Value);
        }
        // Dont need to do this because its already in ChunkPayLoad
        //foreach(var data in entities) {
        //    _entitySpawner.AddGeneratedEntityData(data.Key, data.Value);
        //}
        // FINALLY, we add persistant to the chunkPayLoad and send final result to client

        TargetReceiveChunkDataMultiple(requester, payloadData); // send it to requesting client
    }
    // --- Target RPC to send chunk data to a specific client ---
    [TargetRpc]
    public void TargetReceiveChunkData(NetworkConnection conn, Vector2Int chunkCoord, List<ushort> tileIds, List<ushort> OreIDs, List<float> durabilities, List<ulong> entityIds) {
        // Executed ONLY on the client specified by 'conn'
        if (tileIds == null || tileIds.Count != CHUNK_SIZE * CHUNK_SIZE) {
            Debug.LogWarning($"Received invalid tile data for chunk {chunkCoord} from server.");
            //return;
        }
        // Store durability locally for effects 
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

        // --- Fallback to SetTiles per chunk if optimization wasn't possible ---
        Dictionary<BoundsInt, TileBase[]> tiles = new Dictionary<BoundsInt, TileBase[]>();
        Dictionary<BoundsInt, TileBase[]> ores = new Dictionary<BoundsInt, TileBase[]>();
        Dictionary<BoundsInt, TileBase[]> tilesShading = new Dictionary<BoundsInt, TileBase[]>();
        foreach (var chunkPayload in chunks) {
            TileBase[] tilesToSet = new TileBase[CHUNK_SIZE * CHUNK_SIZE];
            TileBase[] oresToSet = new TileBase[CHUNK_SIZE * CHUNK_SIZE];
            TileBase[] tilesShadingToSet = new TileBase[CHUNK_SIZE * CHUNK_SIZE];
            //List<short> durabilities = new List<short>(CHUNK_SIZE * CHUNK_SIZE);
            int tileIndex = 0;
            for (int y = 0; y < CHUNK_SIZE; y++) {
                for (int x = 0; x < CHUNK_SIZE; x++) {
                    ushort tileID = chunkPayload.TileIds[tileIndex];
                    ushort oreID = chunkPayload.OreIds[tileIndex];
                    //durabilities.Add(chunkPayload.Durabilities[tileIndex]);
                    tilesToSet[tileIndex] = App.ResourceSystem.GetTileByID(tileID);
                    oresToSet[tileIndex] = App.ResourceSystem.GetTileByID(oreID);
                    tilesShadingToSet[tileIndex] = tileID != 0 ? App.ResourceSystem.GetTileByID(9999) : null;
                    tileIndex++;
                }
            }
            Vector3Int chunkOriginCell = ChunkCoordToCellOrigin(chunkPayload.ChunkCoord);
            BoundsInt chunkBounds = new BoundsInt(chunkOriginCell.x, chunkOriginCell.y, 0, CHUNK_SIZE, CHUNK_SIZE, 1);
            //ApplySingleChunkPayload(chunkPayload);
            tiles.Add(chunkBounds, tilesToSet);
            ores.Add(chunkBounds, oresToSet);
            tilesShading.Add(chunkBounds, tilesShadingToSet);
            ClientCacheChunkDurability(chunkPayload.ChunkCoord, chunkPayload.Durabilities);
            // Entities!!
            if (chunkPayload.EntityPersistantIds != null) {
                _entitySpawner.ProcessReceivedEntityIds(chunkPayload.ChunkCoord, chunkPayload.EntityPersistantIds);
            }
        }
        _worldManager.SetTileIEnumerator(tiles, tilesShading);
        _worldManager.SetOreIEnumerator(ores);
        _lightManager.RequestLightUpdate();
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
        //_worldManager.SetOres(chunkBounds, oresToSet);

        // Spawn enemies client only

        // TODO 

        // Debug.Log($"Client received and visually loaded chunk {chunkCoord}");
    }
    private void ApplySingleChunkPayload(ChunkPayload chunkPayload) {
        //ClientCacheChunkDurability(chunkPayload.ChunkCoord, chunkPayload.Durabilities);
        // New active local chunk
        activeChunks.Add(chunkPayload.ChunkCoord);
        // Apply the received tiles visually
        TileBase[] tilesToSet = new TileBase[CHUNK_SIZE * CHUNK_SIZE];
        TileBase[] oresToSet = new TileBase[CHUNK_SIZE * CHUNK_SIZE];
        int tileIndex = 0;
        for (int y = 0; y < CHUNK_SIZE; y++) {
            for (int x = 0; x < CHUNK_SIZE; x++) {
                ushort tileID = chunkPayload.TileIds[tileIndex];
                ushort oreID = chunkPayload.OreIds[tileIndex];
                tilesToSet[tileIndex] = App.ResourceSystem.GetTileByID(tileID);
                oresToSet[tileIndex] = App.ResourceSystem.GetTileByID(oreID);

                tileIndex++;
            }
        }
        Vector3Int chunkOriginCell = ChunkCoordToCellOrigin(chunkPayload.ChunkCoord);
        BoundsInt chunkBounds = new BoundsInt(chunkOriginCell.x, chunkOriginCell.y, 0, CHUNK_SIZE, CHUNK_SIZE, 1);

        _worldManager.SetOres(chunkBounds, oresToSet);
        _worldManager.SetTiles(chunkBounds, tilesToSet);
    }
 
    private void ClientCacheChunkDurability(Vector2Int chunkCoord, List<float> durabilityList) {
        if (!clientDurabilityCache.ContainsKey(chunkCoord)) {
            clientDurabilityCache[chunkCoord] = new float[CHUNK_SIZE, CHUNK_SIZE];
        }

        float[,] chunkDurability = clientDurabilityCache[chunkCoord];
        int index = 0;
        for (int y = 0; y < CHUNK_SIZE; y++) {
            for (int x = 0; x < CHUNK_SIZE; x++) {
                chunkDurability[x, y] = durabilityList[index++];
            }
        }
        // We might want to remove entries from this cache when chunks are visually unloaded
        // in ClientChunkLoadingRoutine to save client memory.
    }

    public float GetClientCachedDurability(Vector3Int cellPos) {
        Vector2Int chunkCoord = CellToChunkCoord(cellPos);
        if (clientDurabilityCache.TryGetValue(chunkCoord, out float[,] chunkDurability)) {
            int localX = cellPos.x - chunkCoord.x * CHUNK_SIZE;
            int localY = cellPos.y - chunkCoord.y * CHUNK_SIZE;
            if (localX >= 0 && localX < CHUNK_SIZE && localY >= 0 && localY < CHUNK_SIZE) {
                return chunkDurability[localX, localY];
            }
        }
        return -1; // Default or error state
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
            // chunk = ServerGenerateChunkData(chunkCoord);
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
                if (newTileId == 0) // Check if destroyed, then we get rid of the ore
                    chunk.oreID[localX, localY] = 0;
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
    private void ObserversUpdateTileDurability(Vector3Int cellPos, float newDurability) {
        // Runs on all clients
        if(newDurability == -1)
            _lightManager.RequestLightUpdate(); // Tile broke so update lights
                                                // Update local cache if you have one
        Vector2Int chunkCoord = CellToChunkCoord(cellPos);
        if (clientDurabilityCache.TryGetValue(chunkCoord, out float[,] chunkDurability)) {
            int localX = cellPos.x - chunkCoord.x * CHUNK_SIZE;
            int localY = cellPos.y - chunkCoord.y * CHUNK_SIZE;
            if (localX >= 0 && localX < CHUNK_SIZE && localY >= 0 && localY < CHUNK_SIZE) {
                chunkDurability[localX, localY] = newDurability;
                UpdateTileVisuals(cellPos, newDurability);
            }
        } else {
            Debug.Log("BRO WHAT THE FUCK");
        }
    }
    // New method on server to handle receiving damage requests
    public void ServerProcessDamageTile(Vector3Int cellPos, float damageAmount, NetworkConnection sourceConnection = null) {
        if (!IsServerInitialized) return;
        Vector2Int chunkCoord = CellToChunkCoord(cellPos);
        // Ensure chunk data exists (generate if necessary)
        if (!worldChunks.TryGetValue(chunkCoord, out ChunkData chunk)) {
            // chunk = ServerGenerateChunkData(chunkCoord);
            // This should never happen but if it does just return lol
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
            if (targetTile.maxDurability <= 0) return; // Indestructible tile

            // --- Apply Damage ---
            float currentDurability = chunk.tileDurability[localX, localY];
            if (currentDurability < 0) // Was at full health (-1 sentinel)
            {
                currentDurability = targetTile.maxDurability;
            }

            float newDurability = currentDurability - damageAmount;

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
                        worldItem.ServerInitialize(id, 1,true); // we either drop 1, or just specify amountToDrop and don't loop
                        //Debug.Log($"[Server] Player {base.Owner.ClientId} dropped {1} of {worldItem.name}.");
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
    private void UpdateTileVisuals(Vector3Int cellPos, float currentDurability) {

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

    private TileBase GetCrackTileForDurability(Vector3Int cellPos, float currentDurability) {
        var t = _worldManager.GetFirstTileAtCellPos(cellPos);
        //var ore = _worldManager.GetOreFromID(chunk.oreID[localX, localY]);
        //var ore = _worldManager.GetOreAtCellPos(cellPos);
        if (t is TileSO tile) {
            return tile.GetCrackTileForDurability(currentDurability);
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
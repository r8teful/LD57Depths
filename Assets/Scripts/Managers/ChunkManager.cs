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
    public TileBase[,] tiles; // The ground layer 
    public int[,] oreID;      // Second "ore" layer
    public int[,] tileDurability; // Third "dmg" layer
                                  // Odin doesn't know how to draw a table matrix for this particular type. Make a custom DrawElementMethod via the TableMatrix attribute like so:

    [TableMatrix(DrawElementMethod = "DrawElement")]
    public byte[,] biomeID;
    public bool isModified = false; // Flag to track if chunk has changed since load/generation
    public bool hasBeenGenerated = false; // Flag to prevent regenerating loaded chunks
    public List<PersistentEntityData> entitiesToSpawn; 
    public ChunkData(int chunkSizeX, int chunkSizeY) {
        tiles = new TileBase[chunkSizeX, chunkSizeY];
        tileDurability = new int[chunkSizeX, chunkSizeY];
        for (int y = 0; y < chunkSizeY; ++y) {
            for (int x = 0; x < chunkSizeX; ++x) {
                tileDurability[x, y] = -1; // Default state
            }
        }
        oreID = new int[chunkSizeX, chunkSizeY];
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
        entitiesToSpawn = new List<PersistentEntityData>(); // Initialize the list
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

    [SerializeField] private int chunkSize = 16; // Size of chunks (16x16 tiles) - Power of 2 often good
    public int GetChunkSize() => chunkSize;
    public bool IsChunkActive(Vector2Int c) => activeChunks.Contains(c);
    // --- Chunk Data ---
    [ShowInInspector]
    private Dictionary<Vector2Int, ChunkData> worldChunks = new Dictionary<Vector2Int, ChunkData>(); // Main world data
    [ShowInInspector]
    private HashSet<Vector2Int> activeChunks = new HashSet<Vector2Int>();
    private Vector2Int currentPlayerChunkCoord = new Vector2Int(int.MinValue, int.MinValue);
    private Dictionary<Vector2Int, int[,]> clientDurabilityCache = new Dictionary<Vector2Int, int[,]>();

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
            List<int> tileIds = new List<int>(chunkSize * chunkSize);
            List<int> durabilities = new List<int>(chunkSize * chunkSize);
            List<int> oreIDs = new List<int>(chunkSize * chunkSize);
            for (int y = 0; y < chunkSize; y++) {
                for (int x = 0; x < chunkSize; x++) {
                    tileIds.Add(_worldManager.GetIDFromTile(chunkData.tiles[x, y]));
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

    // --- Target RPC to send chunk data to a specific client ---
    [TargetRpc]
    private void TargetReceiveChunkData(NetworkConnection conn, Vector2Int chunkCoord, List<int> tileIds, List<int> OreIDs, List<int> durabilities, List<ulong> entityIds) {
        // Executed ONLY on the client specified by 'conn'
        if (tileIds == null || tileIds.Count != chunkSize * chunkSize) {
            Debug.LogWarning($"Received invalid tile data for chunk {chunkCoord} from server.");
            return;
        }
        // Store durability locally for effects 
        ClientCacheChunkDurability(chunkCoord, durabilities);
        // New active local chunk
        activeChunks.Add(chunkCoord);
        // Apply the received tiles visually
        Vector3Int chunkOriginCell = ChunkCoordToCellOrigin(chunkCoord);
        BoundsInt chunkBounds = new BoundsInt(chunkOriginCell.x, chunkOriginCell.y, 0, chunkSize, chunkSize, 1);
        TileBase[] tilesToSet = new TileBase[chunkSize * chunkSize];
        TileBase[] oresToSet = new TileBase[chunkSize * chunkSize];
        for (int i = 0; i < tileIds.Count; i++) {
            tilesToSet[i] = _worldManager.GetTileFromID(tileIds[i]);
        }
        _worldManager.SetTiles(chunkBounds, tilesToSet);
        for (int i = 0; i < OreIDs.Count; i++) {
            oresToSet[i] = _worldManager.GetOreFromID(OreIDs[i]);
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
    private void ClientCacheChunkDurability(Vector2Int chunkCoord, List<int> durabilityList) {
        if (!clientDurabilityCache.ContainsKey(chunkCoord)) {
            clientDurabilityCache[chunkCoord] = new int[chunkSize, chunkSize];
        }

        int[,] chunkDurability = clientDurabilityCache[chunkCoord];
        int index = 0;
        for (int y = 0; y < chunkSize; y++) {
            for (int x = 0; x < chunkSize; x++) {
                chunkDurability[x, y] = durabilityList[index++];
            }
        }
        // We might want to remove entries from this cache when chunks are visually unloaded
        // in ClientChunkLoadingRoutine to save client memory.
    }

    public int GetClientCachedDurability(Vector3Int cellPos) {
        Vector2Int chunkCoord = CellToChunkCoord(cellPos);
        if (clientDurabilityCache.TryGetValue(chunkCoord, out int[,] chunkDurability)) {
            int localX = cellPos.x - chunkCoord.x * chunkSize;
            int localY = cellPos.y - chunkCoord.y * chunkSize;
            if (localX >= 0 && localX < chunkSize && localY >= 0 && localY < chunkSize) {
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

        ChunkData chunkData = WorldGen.GenerateChunk(chunkSize, chunkOriginCell, this, out var enemyList);
        _worldManager.BiomeManager.CalculateBiomeForChunk(chunkCoord, chunkData);
        // --- Add Generated Entities to Persistent Store AND Chunk Map ---
        if (enemyList != null && enemyList.Count > 0) {
            _entitySpawner.AddGeneratedEntityData(chunkCoord, enemyList);
        }
        
        // --- Finalization ---
        chunkData.hasBeenGenerated = true;
        worldChunks.Add(chunkCoord, chunkData);
        return chunkData;

    }
    // --- Visually Deactivate Chunk (Client Side) ---
    private void ClientDeactivateVisualChunk(Vector2Int chunkCoord) {
        Vector3Int chunkOriginCell = ChunkCoordToCellOrigin(chunkCoord);
        BoundsInt chunkBounds = new BoundsInt(chunkOriginCell.x, chunkOriginCell.y, 0, chunkSize, chunkSize, 1);
        TileBase[] clearTiles = new TileBase[chunkSize * chunkSize]; // Array of nulls
        _worldManager.SetTiles(chunkBounds, clearTiles);
        _worldManager.SetOres(chunkBounds, clearTiles);
        //Debug.Log($"Client visually deactivated chunk {chunkCoord}");
        activeChunks.Remove(chunkCoord);
        // Entities, note this will not work in multiplayer now, 
        _entitySpawner.RemoveEntitieAtChunk(chunkCoord);
        
    }
    // --- Tile Modification ---
    // This is the entry point called by the PlayerController's ServerRpc, this is usually after all checks have been done already
    public void ServerRequestModifyTile(Vector3Int cellPos, int newTileId) {
        // Must run on server
        if (!IsServerInitialized) return;
        TileBase tileToSet = _worldManager.GetTileFromID(newTileId);
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
                chunk.tileDurability[localX, localY] = -1; // Reset to default state
                // --- Update Server's OWN visuals (optional but good for host) ---
                _worldManager.SetTile(cellPos, tileToSet);
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
    private void ObserversUpdateTileDurability(Vector3Int cellPos, int newDurability) {
        // Runs on all clients
        _lightManager.RequestLightUpdate(); // Do this here because fuck it
        // Update local cache if you have one
        Vector2Int chunkCoord = CellToChunkCoord(cellPos);
        if (clientDurabilityCache.TryGetValue(chunkCoord, out int[,] chunkDurability)) {
            int localX = cellPos.x - chunkCoord.x * chunkSize;
            int localY = cellPos.y - chunkCoord.y * chunkSize;
            if (localX >= 0 && localX < chunkSize && localY >= 0 && localY < chunkSize) {
                chunkDurability[localX, localY] = newDurability;
                UpdateTileVisuals(cellPos, newDurability);
            }
        }
    }
    // New method on server to handle receiving damage requests
    public void ServerProcessDamageTile(Vector3Int cellPos, int damageAmount, NetworkConnection sourceConnection = null) {
        if (!IsServerInitialized) return;
        Vector2Int chunkCoord = CellToChunkCoord(cellPos);

        // Ensure chunk data exists (generate if necessary)
        if (!worldChunks.TryGetValue(chunkCoord, out ChunkData chunk)) {
            chunk = ServerGenerateChunkData(chunkCoord);
            if (chunk == null) { Debug.LogError($"Server failed to get/generate chunk {chunkCoord} for damage."); return; }
        }

        // Calculate local coordinates
        int localX = cellPos.x - chunkCoord.x * chunkSize;
        int localY = cellPos.y - chunkCoord.y * chunkSize;

        if (localX < 0 || localX >= chunkSize || localY < 0 || localY >= chunkSize) { Debug.LogWarning($"Server: Invalid local coordinates {localX},{localY} for damage at {cellPos}"); return; }


        // --- Get Tile Type & Properties ---
        TileBase tileBase = chunk.tiles[localX, localY];
        var ore = _worldManager.GetOreFromID(chunk.oreID[localX, localY]);
        // if ore use Ore tilebase, not stone
        if (tileBase is TileSO targetTile) {
            if (ore != null) targetTile = ore;
            if (targetTile.maxDurability <= 0) return; // Indestructible tile

            // --- Apply Damage ---
            int currentDurability = chunk.tileDurability[localX, localY];
            if (currentDurability < 0) // Was at full health (-1 sentinel)
            {
                currentDurability = targetTile.maxDurability;
            }

            int newDurability = currentDurability - damageAmount;

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
            // Tile is not a 'CustomTile' - maybe it's indestructible base Tile?
            // Decide how to handle - ignore damage, or maybe break immediately if base tile?
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
    private void UpdateTileVisuals(Vector3Int cellPos, int currentDurability) {

        TileBase crackTile = GetCrackTileForDurability(cellPos, currentDurability); // Find appropriate crack sprite

        _worldManager.SetOverlayTile(cellPos, crackTile);
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
        BoundsInt chunkBounds = new BoundsInt(chunkOriginCell.x, chunkOriginCell.y, 0, chunkSize, chunkSize, 1);

        // Create an array full of nulls to clear the area
        TileBase[] clearTiles = new TileBase[chunkSize * chunkSize];
        // No need to fill it explicitly, default is null

        _worldManager.SetTiles(chunkBounds, clearTiles);
        // Add similar SetTilesBlock calls for other tilemap layers if needed
    }

    public void AddChunkData(Vector2Int chunkCoord, ChunkData data) {
        worldChunks.Add(chunkCoord, data);
    }
    private bool IsTileAir(int newTileId) {
        // Check if the tile ID corresponds to an air tile
        // Assuming 0 is the ID for air tiles
        return newTileId == 0;
    }


    // =============================================
    // === Coordinate Conversion Helper Methods ===
    // =============================================

    public Vector2Int WorldToChunkCoord(Vector3 worldPosition) {
        Vector3Int cellPos = _worldManager.WorldToCell(worldPosition);
        // Integer division automatically floors, which is what we want.
        // Be careful if chunkSize is not a factor of world origin/coordinates start negative.
        // Using Mathf.FloorToInt ensures correct behavior with negative coordinates.
        int chunkX = Mathf.FloorToInt((float)cellPos.x / chunkSize);
        int chunkY = Mathf.FloorToInt((float)cellPos.y / chunkSize);
        return new Vector2Int(chunkX, chunkY);
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

    internal void ClearWorldChunks() {
        worldChunks.Clear(); // Clear existing runtime chunk data
        activeChunks.Clear(); // Clear active chunks before loading
    }

    internal bool CanWriteData(Vector2Int chunkCoord) {
        return worldChunks.ContainsKey(chunkCoord);
    }
    public TileSO GetTileAtWorldPos(int x, int y) {
        var chunkCoord = WorldToChunkCoord(new(x, y));
        if(worldChunks.TryGetValue(chunkCoord, out var chunk)) {
            // Calculate local tile indices within the chunk
            int localX = x - chunkCoord.x * chunkSize;
            int localY = y - chunkCoord.y * chunkSize;

            // Ensure indices are within the chunk bounds
            if (localX < 0 || localX >= chunkSize || localY < 0 || localY >= chunkSize)
                return null;

            // Return the tile from the chunk's tile array
            return chunk.tiles[localX, localY] as TileSO;
        } else {
            return null; // Chunk not generated yet
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
            for (int localX = 0; localX < chunkSize; localX++) {
                for (int localY = 0; localY < chunkSize; localY++) {
                    TileSO tile = chunkData.tiles[localX, localY] as TileSO;
                    if (tile.IsSolid)
                        continue;
                    // biome
                    byte biomeID = chunkData.biomeID[localX, localY];

                    // compute global cell position
                    int globalX = chunkCoord.x * chunkSize + localX;
                    int globalY = chunkCoord.y * chunkSize + localY;
                    var tilePos = new Vector2Int(globalX, globalY);
                    validTiles.Add(tilePos);

                    tilesByBiome.Add(tilePos, (BiomeType)biomeID);
                }
            }
        }

        return (validTiles,tilesByBiome);
    }
}
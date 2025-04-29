using FishNet.Object;
using System.Collections.Generic;
using UnityEngine.Tilemaps;
using UnityEngine;
using FishNet.Connection;
using Sirenix.OdinInspector;
using System;
using FishNet;

public class WorldManager : NetworkBehaviour {
    // --- Managers ---
    public WorldDataManager WorldDataManager;
    [SerializeField] private ChunkManager _chunkManagerPrefab;
    [HideInInspector] public ChunkManager ChunkManager;
    [InlineEditor]
    public WorldGenSettingSO WorldGenSettings;
    // --- Tile ID Mapping ---
    private Dictionary<TileBase, int> tileAssetToIdMap = new Dictionary<TileBase, int>();
    private Dictionary<int, TileBase> idToTileAssetMap = new Dictionary<int, TileBase>();
    public Dictionary<TileBase, int> GetTileToID() => tileAssetToIdMap;
    public Dictionary<int, TileBase> GetIDToTile() => idToTileAssetMap;
    
    [FoldoutGroup("Tilemap & Tiles")]
    [SerializeField] private List<TileBase> tileAssets; // Assign ALL your TileBase assets here in order
    [FoldoutGroup("Tilemap & Tiles")]
    [SerializeField] private Tilemap mainTilemap; // Assign your ground Tilemap GameObject here
    public bool useSave;

     [Button("NewWorld")]
    private void DEBUGNEWGEN() {
        ChunkManager.DEBUGNewGen();
        WorldGen.InitializeNoise();
    }
    public override void OnStartServer() {
        base.OnStartServer();
        // Server-only initialization
        InitializeTileMapping();
        WorldGen.Init(WorldGenSettings, idToTileAssetMap);
        ChunkManager = Instantiate(_chunkManagerPrefab);
        InstanceFinder.ServerManager.Spawn(ChunkManager.gameObject, Owner);
        //ChunkManager.Spawn(ChunkManager.gameObject, Owner);
        ChunkManager.SetWorldManager(this);
        if (useSave) WorldDataManager.LoadWorld(); // Load happens only on server

        //StartCoroutine(ServerChunkManagementRoutine()); // Not using atm
    }
    public override void OnStartClient() {
        base.OnStartClient();
        InitializeTileMapping(); // Clients also need the ID maps to interpret RPCs
        Debug.Log("Start client");
        mainTilemap.ClearAllTiles(); // Start with a clear visual map
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
    // --- RPC to tell all clients about a tile change ---
    [ObserversRpc(BufferLast = false)] // Don't buffer, could spam late joiners. Consider buffering important static tiles.
    public void ObserversUpdateTile(Vector3Int cellPos, int newTileId) {
        // This runs on ALL clients (including the host)
        TileBase tileToSet = GetTileFromID(newTileId);
        mainTilemap.SetTile(cellPos, tileToSet); // Update local visuals

        // Optional: Update client-side data cache if you implement one.
        // Optional: Trigger particle effects, sound, etc. on the client here.
    }

    public TileBase GetTileFromID(int id) {
        if (idToTileAssetMap.TryGetValue(id, out TileBase tile)) {
            return tile;
        }
        Debug.LogWarning($"Tile ID '{id}' not found in mapping. Returning null.");
        return null; // Fallback to air/null
    }
    // --- Tile ID Helpers (Ensure these exist and are correct) ---
    public int GetTileId(TileBase tile) {
        if (tileAssetToIdMap.TryGetValue(tile, out int id)) {
            return id;
        }
        Debug.LogWarning($"Tile '{tile.name}' not found in mapping. Returning 0.");
        return 0; // Fallback to air/null ID
    }

    // Modify the world (visually)
    internal void SetTiles(BoundsInt chunkBounds, TileBase[] tilesToSet) {
        mainTilemap.SetTilesBlock(chunkBounds, tilesToSet);
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

    /*
    // Sets a tile at a given world position (modifies the ground layer)
    // IMPORTANT: Also updates the underlying ChunkData!

    public void SetTileAtWorldPos(Vector3 worldPos, TileBase tileToSet) {
        Vector3Int cellPos = WorldToCell(worldPos);
        Vector2Int chunkCoord = ChunkManager.CellToChunkCoord(cellPos);

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
    */
    // Gets the world coordinate of the center of a specific cell
    public Vector3 GetCellCenterWorld(Vector3Int cellPosition) {
        return mainTilemap.GetCellCenterWorld(cellPosition); // Get center for placing objects
    }
    public Vector3 CellToWorld(Vector3Int cellPosition) {
        return mainTilemap.CellToWorld(cellPosition); // Get bottom-left corner
    }
    public Vector3Int WorldToCell(Vector3 worldPosition) {
        return mainTilemap.WorldToCell(worldPosition);
    }

    internal void ClearAllData() {
        ChunkManager.ClearWorldChunks();
        mainTilemap.ClearAllTiles(); // Clear the visual tilemap
    }
}
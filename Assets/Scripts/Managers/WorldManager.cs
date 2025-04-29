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
    private ChunkManager _chunkManager;
    [InlineEditor]
    public WorldGenSettingSO WorldGenSettings;
    // --- Tile ID Mapping ---
    private Dictionary<TileBase, int> tileAssetToIdMap = new Dictionary<TileBase, int>();
    private Dictionary<int, TileBase> idToTileAssetMap = new Dictionary<int, TileBase>();
    public Dictionary<TileBase, int> GetTileToID() => tileAssetToIdMap;
    public Dictionary<int, TileBase> GetIDToTile() => idToTileAssetMap;
    
    [SerializeField] private List<TileBase> tileAssets; // Assign ALL your TileBase assets here in order
    [SerializeField] private Tilemap mainTilemap; // Main visual grid component for the game
    [SerializeField] private Tilemap overlayTilemap; // for damaged tiles 
    public float GetVisualTilemapGridSize() => mainTilemap.transform.parent.GetComponent<Grid>().cellSize.x; // Cell size SHOULD be square
    public bool useSave; 
    [SerializeField] Transform playerSpawn;

     [Button("NewWorld")]
    private void DEBUGNEWGEN() {
        _chunkManager.DEBUGNewGen();
        WorldGen.InitializeNoise();
    }
    public override void OnStartServer() {
        base.OnStartServer();
        // Server-only initialization
        InitializeTileMapping();
        WorldGen.Init(WorldGenSettings, idToTileAssetMap);
        //InstanceFinder.ServerManager.Spawn(ChunkManager.gameObject, Owner);
        //ChunkManager.Spawn(ChunkManager.gameObject, Owner);
        if (useSave) WorldDataManager.LoadWorld(); // Load happens only on server
        playerSpawn.transform.position = new Vector3(0,-WorldGen.GetDepth()* GetVisualTilemapGridSize()); // Depths is in blocks, so times it with grid size to get world space pos
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
    public int GetIDFromTile(TileBase tile) {
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
    internal void SetTile(Vector3Int cellPos, TileBase tileToSet) {
        mainTilemap.SetTile(cellPos, tileToSet);
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
        // Let chunk manager handle it
        _chunkManager.ServerRequestModifyTile(cellPos, tileAssetToIdMap[tileToSet]);
    }   
    
    // Gets the world coordinate of the center of a specific cell
    public Vector3 GetCellCenterWorld(Vector3Int cellPosition) {
        return mainTilemap.GetCellCenterWorld(cellPosition); // Get center for placing objects
    }
    public Vector3 CellToWorld(Vector3Int cellPosition) {
        return mainTilemap.CellToWorld(cellPosition); // Get bottom-left corner
    }
    public TileBase GetTileAtCellPos(Vector3Int cellPosition) {
        var world = mainTilemap.CellToWorld(cellPosition);
        return GetTileAtWorldPos(world);

    }
    public Vector3Int WorldToCell(Vector3 worldPosition) {
        return mainTilemap.WorldToCell(worldPosition);
    }

    internal void ClearAllData() {
        _chunkManager.ClearWorldChunks();
        mainTilemap.ClearAllTiles(); // Clear the visual tilemap
    }

    internal void SetChunkManager(ChunkManager chunkManager) {
        _chunkManager = chunkManager;
    }

    internal void SetOverlayTile(Vector3Int cellPos, TileBase crackTile) {
        overlayTilemap.SetTile(cellPos, crackTile); // Set tile on overlay layer
    }
}
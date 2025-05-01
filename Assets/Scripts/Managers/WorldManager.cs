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
    public ChunkManager ChunkManager;
    public BiomeManager BiomeManager;
    [InlineEditor]
    [OnValueChanged("DEBUGNEWGEN")]
    public WorldGenSettingSO WorldGenSettings;
    // --- Tile ID Mapping ---
    private Dictionary<TileSO, int> tileAssetToIdMap = new Dictionary<TileSO, int>();
    private Dictionary<int, TileSO> idToTileAssetMap = new Dictionary<int, TileSO>();
    public Dictionary<TileSO, int> GetTileToID() => tileAssetToIdMap;
    public Dictionary<int, TileSO> GetIDToTile() => idToTileAssetMap;
    public int GetChunkSize() => ChunkManager.GetChunkSize();
    [InlineEditor]
    [SerializeField] private List<TileSO> tileAssets;
    [SerializeField] private Tilemap mainTilemap; // Main visual grid component for the game
    [SerializeField] private Tilemap overlayTilemapOre; // for damaged tiles 
    [SerializeField] private Tilemap overlayTilemap; // for damaged tiles 

    public float GetVisualTilemapGridSize() => mainTilemap.transform.parent.GetComponent<Grid>().cellSize.x; // Cell size SHOULD be square
    public bool useSave; 
    [SerializeField] Transform playerSpawn;

     [Button("NewWorld")]
    private void DEBUGNEWGEN() {
        ChunkManager.DEBUGNewGen();
        WorldGen.InitializeNoise();
    }
    public override void OnStartServer() {
        base.OnStartServer();
        // Server-only initialization
        InitializeTileMapping();
        WorldGen.Init(WorldGenSettings, this);
        //InstanceFinder.ServerManager.Spawn(ChunkManager.gameObject, Owner);
        //ChunkManager.Spawn(ChunkManager.gameObject, Owner);
        if (useSave) WorldDataManager.LoadWorld(); // Load happens only on server
        BiomeManager = gameObject.GetComponent<BiomeManager>(); // No clue if we have to set the owner
        BiomeManager.SetWorldManager(this);
        var offset = GetVisualTilemapGridSize() * 6;
        playerSpawn.transform.position = new Vector3(0,-WorldGen.GetDepth()* GetVisualTilemapGridSize() + offset); // Depths is in blocks, so times it with grid size to get world space pos
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
            if (!tileAssetToIdMap.ContainsKey(tileAssets[i])) {
                tileAssetToIdMap.Add(tileAssets[i], tileAssets[i].tileID);
                idToTileAssetMap.Add(tileAssets[i].tileID, tileAssets[i]);
                //Debug.Log($"Mapped Tile: {tileAssets[i].name} to ID: {i}");
            } else {
                Debug.LogWarning($"Duplicate TileBase '{tileAssets[i].name}' detected in tileAssets list. Only the first instance will be used for ID mapping.");
            }
        }
    }
    // --- RPC to tell all clients about a tile change ---
    [ObserversRpc(BufferLast = false)] // Don't buffer, could spam late joiners. Consider buffering important static tiles.
    public void ObserversUpdateTile(Vector3Int cellPos, int newTileId) {
        // This runs on ALL clients (including the host)
        TileSO tileToSet = GetTileFromID(newTileId);
        mainTilemap.SetTile(cellPos, tileToSet); // Update local visuals

        // Optional: Update client-side data cache if you implement one.
        // Optional: Trigger particle effects, sound, etc. on the client here.
    }

    public TileSO GetTileFromID(int id) {
        if (idToTileAssetMap.TryGetValue(id, out TileSO tile)) {
            return tile;
        }
        Debug.LogWarning($"Tile ID '{id}' not found in mapping. Returning null.");
        return null; // Fallback to air/null
    }
    // --- Tile ID Helpers (Ensure these exist and are correct) ---
    public int GetIDFromTile(TileSO tile) {
        if (tileAssetToIdMap.TryGetValue(tile, out int id)) {
            return id;
        }
        Debug.LogWarning($"Tile '{tile.name}' not found in mapping. Returning 0.");
        return 0; // Fallback to air/null ID
    }

    // Modify the world (visually)
    internal void SetTiles(BoundsInt chunkBounds, TileSO[] tilesToSet) {
        mainTilemap.SetTilesBlock(chunkBounds, tilesToSet);
    }
    internal void SetTile(Vector3Int cellPos, TileSO tileToSet) {
        mainTilemap.SetTile(cellPos, tileToSet);
    }

    // =============================================
    // === World Interaction Helper Methods ===
    // =============================================

    // Gets the TileBase asset at a given world position (checks the ground layer)
    public TileSO GetTileAtWorldPos(Vector3 worldPos) {
        Vector3Int cellPos = WorldToCell(worldPos);
        return mainTilemap.GetTile(cellPos) as TileSO;
        // To check other layers, call GetTile on their respective Tilemaps
    }

    
    // Sets a tile at a given world position (modifies the ground layer)
    // IMPORTANT: Also updates the underlying ChunkData!

    public void SetTileAtWorldPos(Vector3 worldPos, TileSO tileToSet) {
        Vector3Int cellPos = WorldToCell(worldPos);
        // Let chunk manager handle it
        ChunkManager.ServerRequestModifyTile(cellPos, tileAssetToIdMap[tileToSet]);
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
        ChunkManager.ClearWorldChunks();
        mainTilemap.ClearAllTiles(); // Clear the visual tilemap
    }

    internal void SetChunkManager(ChunkManager chunkManager) {
        ChunkManager = chunkManager;
    }

    internal void SetOverlayTile(Vector3Int cellPos, TileBase crackTile) {
        overlayTilemap.SetTile(cellPos, crackTile); // Set tile on overlay layer
    }
}
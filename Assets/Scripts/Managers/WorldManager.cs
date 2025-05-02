using FishNet.Object;
using System.Collections.Generic;
using UnityEngine.Tilemaps;
using UnityEngine;
using Sirenix.OdinInspector;

public class WorldManager : NetworkBehaviour {
    // --- Managers ---
    public WorldDataManager WorldDataManager;
    public ChunkManager ChunkManager;
    public BiomeManager BiomeManager;
    [InlineEditor]
    public WorldGenSettingSO WorldGenSettings;
    // --- Tile ID Mapping ---
    private Dictionary<TileBase, int> tileAssetToIdMap = new Dictionary<TileBase, int>();
    private Dictionary<int, TileBase> idToTileAssetMap = new Dictionary<int, TileBase>();
    // --- Ore to ID Mapping
    private Dictionary<int, TileSO> idToOreAssetMap = new Dictionary<int, TileSO>();
    private Dictionary<TileSO, int> oreToidAssetMap = new Dictionary<TileSO, int>();
    public Dictionary<TileBase, int> GetTileToID() => tileAssetToIdMap;
    public Dictionary<int, TileBase> GetIDToTile() => idToTileAssetMap;
    public int GetChunkSize() => ChunkManager.GetChunkSize();
    
    [SerializeField] private List<TileSO> tileAssets; // Assign ALL your TileBase assets here in order
    [SerializeField] private List<TileSO> oreAssets; // Assign ALL your TileBase assets here in order
    [SerializeField] private Tilemap mainTilemap; // Main visual grid component for the game
    [SerializeField] private Tilemap overlayTilemapOre; // Main visual grid component for the game
    [SerializeField] private Tilemap overlayTilemapDamage; // for damaged tiles 

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
        oreToidAssetMap.Clear();
        idToOreAssetMap.Clear();
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
        // Ores
        for (int i = 0; i < oreAssets.Count; i++) {
            if (oreAssets[i] == null) continue; // Skip null entries in the list itself
            if (!oreToidAssetMap.ContainsKey(oreAssets[i])) {
                oreToidAssetMap.Add(oreAssets[i], oreAssets[i].tileID);
                idToOreAssetMap.Add(oreAssets[i].tileID, oreAssets[i]);
                //Debug.Log($"Mapped Tile: {tileAssets[i].name} to ID: {i}");
            } else {
                Debug.LogWarning($"Duplicate Ore '{oreAssets[i].name}' detected in oreAssets list. Only the first instance will be used for ID mapping.");
            }
        }
    }
    // --- RPC to tell all clients about a tile change ---
    [ObserversRpc(BufferLast = false)] // Don't buffer, could spam late joiners. Consider buffering important static tiles.
    public void ObserversUpdateTile(Vector3Int cellPos, int newTileId) {
        // This runs on ALL clients (including the host)
        TileBase tileToSet = GetTileFromID(newTileId);
        mainTilemap.SetTile(cellPos, tileToSet); // Update local visuals
        if (newTileId == 0)
            overlayTilemapOre.SetTile(cellPos, tileToSet);
        // Optional: Update client-side data cache if you implement one.
        // Optional: Trigger particle effects, sound, etc. on the client here.
    }
    public TileSO GetOreFromID(int id) {
        if (idToOreAssetMap.TryGetValue(id, out TileSO tile)) {
            return tile;
        }
        return null; // Fallback to air/null
    }
    public int GetIDFromOre(TileSO ore) {
        if (oreToidAssetMap.TryGetValue(ore, out int id)) {
            return id;
        }
        return 0;
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
    public void ToggleWorldTilemap(bool enableWorld) {
        mainTilemap.GetComponent<TilemapRenderer>().enabled = enableWorld;
        overlayTilemapOre.GetComponent<TilemapRenderer>().enabled = enableWorld;
        overlayTilemapDamage.GetComponent<TilemapRenderer>().enabled = enableWorld;
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

    // Ores get returned first, then ground layer
    public TileSO GetFirstTileAtCellPos (Vector3Int cellPos) {
        //Vector3Int cellPos = WorldToCell(worldPos);

        // 1st choice: ore overlay
        TileSO ore = overlayTilemapOre.GetTile(cellPos) as TileSO;
        if (ore != null)
            return ore;

        // fallback: main map
        return mainTilemap.GetTile(cellPos) as TileSO;
    }

    public void SetTileAtWorldPos(Vector3 worldPos, TileBase tileToSet) {
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
        overlayTilemapDamage.SetTile(cellPos, crackTile); // Set tile on overlay layer
    }

    internal void SetOres(BoundsInt chunkBounds, TileBase[] oresToSet) {
        //Debug.Log("Setting ores for chunk " + chunkBounds);
        overlayTilemapOre.SetTilesBlock(chunkBounds, oresToSet); // Set tile on overlay layer
    }
}
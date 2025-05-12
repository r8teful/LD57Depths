using FishNet.Object;
using System.Collections.Generic;
using UnityEngine.Tilemaps;
using UnityEngine;
using Sirenix.OdinInspector;

public class WorldManager : NetworkBehaviour {
    public static WorldManager Instance { get; private set; }
    // --- Managers ---
    public WorldDataManager WorldDataManager;
    public ChunkManager ChunkManager;
    public BiomeManager BiomeManager;
    public BackgroundManager Backgroundmanager;
    [SerializeField] private Transform _sub;
    [SerializeField] private Transform _worldRoot; // All world entities have this as their parent, used for hiding when entering sub or other interiors
    [InlineEditor]
    public WorldGenSettingSO WorldGenSettings;
    public int GetChunkSize() => ChunkManager.GetChunkSize();
    public Transform GetWorldRoot() => _worldRoot;
    public GameObject GetMainTileMap() => mainTilemap.gameObject;
    
    [SerializeField] private Tilemap mainTilemap; // Main visual grid component for the game
    [SerializeField] private Tilemap overlayTilemapOre; // Main visual grid component for the game
    [SerializeField] private Tilemap overlayTilemapDamage; // for damaged tiles 

    public float GetVisualTilemapGridSize() => mainTilemap.transform.parent.GetComponent<Grid>().cellSize.x; // Cell size SHOULD be square
    public bool useSave; 
    [SerializeField] Transform playerSpawn;

    [Button("NewWorld")]
    private void DEBUGNEWGEN() {
        ChunkManager.DEBUGNewGen();
        WorldGen.Init(WorldGenSettings, this);
        WorldGen.InitializeNoise(); 
    }
    [SerializeField] private bool DEBUGConstantNewGen;
    private void Update() {
        if (DEBUGConstantNewGen && Time.frameCount % Mathf.RoundToInt(1f / (Time.deltaTime * 2)) == 0)
            DEBUGNEWGEN();
    }
    private void Awake() {
        if (Instance != null && Instance != this) Destroy(gameObject);
        else Instance = this;
    }
    public override void OnStartServer() {
        base.OnStartServer();
        // Server-only initialization
        WorldGen.Init(WorldGenSettings, this);
        //InstanceFinder.ServerManager.Spawn(ChunkManager.gameObject, Owner);
        //ChunkManager.Spawn(ChunkManager.gameObject, Owner);
        if (useSave) WorldDataManager.LoadWorld(); // Load happens only on server
        BiomeManager = gameObject.GetComponent<BiomeManager>(); // No clue if we have to set the owner
        BiomeManager.SetWorldManager(this);
        var offset = GetVisualTilemapGridSize() * 6;
        playerSpawn.transform.position = new Vector3(0,-WorldGen.GetDepth()* GetVisualTilemapGridSize() + offset); // Depths is in blocks, so times it with grid size to get world space pos
        _sub.transform.position = new Vector3(0, -WorldGen.GetDepth() * GetVisualTilemapGridSize() + offset/4);
        //InstanceFinder.ServerManager.Spawn(sub);
        //StartCoroutine(ServerChunkManagementRoutine()); // Not using atm
        Backgroundmanager.Init(WorldGenSettings);
    }
    public override void OnStartClient() {
        base.OnStartClient();
        Debug.Log("Start client");
        mainTilemap.ClearAllTiles(); // Start with a clear visual map
    }

    // --- RPC to tell all clients about a tile change ---
    [ObserversRpc(BufferLast = false)] // Don't buffer, could spam late joiners. Consider buffering important static tiles.
    public void ObserversUpdateTile(Vector3Int cellPos, ushort newTileId) {
        // This runs on ALL clients (including the host)
        TileSO tileToSet = App.ResourceSystem.GetTileByID(newTileId);
        mainTilemap.SetTile(cellPos, tileToSet); // Update local visuals
        if (newTileId == 0)
            overlayTilemapOre.SetTile(cellPos, tileToSet);
        // Optional: Update client-side data cache if you implement one.
        // Optional: Trigger particle effects, sound, etc. on the client here.
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
    internal void SetTile(Vector3Int cellPos, ushort tileToSet) {
        mainTilemap.SetTile(cellPos, App.ResourceSystem.GetTileByID(tileToSet));
    }

    // =============================================
    // === World Interaction Helper Methods ===
    // =============================================

    // Ores get returned first, then ground layer
    public TileSO GetFirstTileAtCellPos (Vector3Int cellPos) {
        //Vector3Int cellPos = WorldToCell(worldPos);

        // 1st choice: ore overlay
        TileSO ore = overlayTilemapOre.GetTile(cellPos) as TileSO;
        if (ore != null) {
            Debug.Log("Returning ore");
            return ore;
        }

        // fallback: main map
        return mainTilemap.GetTile(cellPos) as TileSO;
    }

    public void SetTileAtWorldPos(Vector3 worldPos, TileSO tileToSet) {
        Vector3Int cellPos = WorldToCell(worldPos);
        // Let chunk manager handle it
        ChunkManager.ServerRequestModifyTile(cellPos, App.ResourceSystem.GetIDByTile(tileToSet));
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
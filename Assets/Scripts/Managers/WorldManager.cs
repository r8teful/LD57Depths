using FishNet.Object;
using UnityEngine.Tilemaps;
using UnityEngine;
using Sirenix.OdinInspector;
using System.Collections.Generic;
using System.Collections;

public class WorldManager : NetworkBehaviour {
    public static WorldManager Instance { get; private set; }
    // --- Managers ---
    public WorldDataManager WorldDataManager;
    public WorldGen WorldGen;
    public ChunkManager ChunkManager;
    public BiomeManager BiomeManager;
    [SerializeField] private RenderTexture worldRenderTexture;
    [SerializeField] private Transform _sub;
    [SerializeField] private Transform _worldRoot; // All world entities have this as their parent, used for hiding when entering sub or other interiors
    [SerializeField] private Camera _worldGenCamera;
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
        WorldGen.Init(worldRenderTexture, WorldGenSettings, this, ChunkManager,_worldGenCamera);
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
        WorldGen.Init(worldRenderTexture, WorldGenSettings, this, ChunkManager, _worldGenCamera);
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
    }
    public override void OnStartClient() {
        base.OnStartClient();
        Debug.Log("Start client");
        mainTilemap.ClearAllTiles(); // Start with a clear visual map
    }
    public void MoveCamToChunkCoord(Vector2Int chunkCoord) {
        var size = ChunkManager.GetChunkSize() / 2;
        var cell = ChunkManager.ChunkCoordToCellOrigin(chunkCoord);
        _worldGenCamera.transform.position = GetCellCenterWorld(new(cell.x + size, cell.y + size));
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
        //StartCoroutine(SetWorldTiles(chunkBounds, tilesToSet));
        //mainTilemap.lock(pos, TileFlags.LockAll);
        mainTilemap.SetTilesBlock(chunkBounds, tilesToSet);
    }
    public void SetTileIEnumerator(Dictionary<BoundsInt, TileBase[]> tilesToSet) {
        StartCoroutine(SetWorldTiles(tilesToSet));
    }
    internal void SetOreIEnumerator(Dictionary<BoundsInt, TileBase[]> ores) {
        StartCoroutine(SetWorldOres(ores));    
    }
    private IEnumerator SetWorldTiles(Dictionary<BoundsInt, TileBase[]> tilesToSet) {
        foreach (var kvp in tilesToSet) {
            mainTilemap.SetTilesBlock(kvp.Key, kvp.Value);
            yield return null; // pause one frame
        }
    }
    private IEnumerator SetWorldOres(Dictionary<BoundsInt, TileBase[]> oresToSet) {
        foreach (var kvp in oresToSet) {
            overlayTilemapOre.SetTilesBlock(kvp.Key, kvp.Value);
            yield return null; // pause one frame
        }
    }
    //  You should use BoundsInt instead its faster
    internal void SetTiles(Vector3Int[] tiles, TileBase[] tilesToSet) {
        mainTilemap.SetTiles(tiles, tilesToSet);
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
    public void RequestDamageTile(Vector3 worldPosition,short dmg) {
        var cell = WorldToCell(worldPosition);
        //Debug.Log($"Requesting processdamage of:{cell} with {dmg}");
        ChunkManager.ServerProcessDamageTile(cell, dmg);
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
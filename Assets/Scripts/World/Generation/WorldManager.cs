using Sirenix.OdinInspector;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

// Handles visual display of the world, also acts as a central part for references 
public class WorldManager : StaticInstance<WorldManager> {
    // --- Managers ---
    public WorldDataManager WorldDataManager;
    public WorldGen WorldGen;
    public ChunkManager ChunkManager;
    public BiomeManager BiomeManager;
    public StructureManager StructureManager;
    [SerializeField] private RenderTexture worldRenderTexture;
    [SerializeField] private Transform _worldRoot; // All world entities have this as their parent, used for hiding when entering sub or other interiors
    [SerializeField] private Camera _worldGenCamera;
    public int GetChunkSize() => ChunkManager.GetChunkSize();
    public Transform GetWorldRoot() => _worldRoot;
    public GameObject GetMainTileMapObject() => mainTilemap.gameObject;
    public Tilemap MainTileMap => mainTilemap;
    
    [SerializeField] private Tilemap mainTilemap; // Main visual grid component for the game
    [SerializeField] private Tilemap overlayTilemapOre; // Ores are overlayed ontop of the main tilemap, when we break a tile, we first check if there is an ore there and use that as drop
    [SerializeField] private Tilemap overlayTilemapShading; 
    [SerializeField] private Tilemap overlayTilemapDamage; // for damaged tiles
    [SerializeField] private Tilemap overlayDEBUG; 
    [SerializeField] private TileSO _shadeTile; // We can set this with the resource system but just doing this now
    public float GetVisualTilemapGridSize() => mainTilemap.transform.parent.GetComponent<Grid>().cellSize.x; // Cell size SHOULD be square
    public bool useSave; 
    public Vector3 PlayerSpawn { get; private set; }

    [Button("NewWorld")]
    private void DEBUGNEWGEN() {
        //ChunkManager.DEBUGNewGen();
        //WorldGen.Init(worldRenderTexture, WorldGenSettings, this, ChunkManager,_worldGenCamera);
        //WorldGen.InitializeNoise();
    }
    [SerializeField] private bool DEBUGConstantNewGen;
    private GameSetupManager _gameSetupManager;

    private void Update() {
        if (DEBUGConstantNewGen && Time.frameCount % Mathf.RoundToInt(1f / (Time.deltaTime * 2)) == 0)
            DEBUGNEWGEN();
    }
    public void Init(GameSetupManager setupManager) {
        _gameSetupManager = setupManager;
        WorldGen.Init(worldRenderTexture, setupManager.WorldGenSettings, this, ChunkManager, _worldGenCamera);
        BiomeManager.Init(this);
        SetSubAndPlayerSpawn();
        mainTilemap.ClearAllTiles(); // Start with a clear visual map
        StructureManager = gameObject.AddComponent<StructureManager>();
        //if (useSave) WorldDataManager.LoadWorld(); // Load happens only on server
        SpawnStructures();
        PlayerLayerController.OnPlayerVisibilityChanged += PlayerLayerChange;
    }

    private void PlayerLayerChange(VisibilityLayerType type) {
        if(type == VisibilityLayerType.Exterior) {
            // enable tilemap collider this seems hacky but whatever
            mainTilemap.GetComponent<TilemapCollider2D>().enabled = true;
        } else {
            // dissable tilemap collider 
            mainTilemap.GetComponent<TilemapCollider2D>().enabled = false;

        }
    }

    private void SpawnStructures() {
        var settings = GameSetupManager.Instance.WorldGenSettings;
        foreach(var biome in settings.biomes) {
            StructureManager.GenerateArtifact(biome);
        }
        StructureManager.GenerateExplorationEntities(settings);
    }

    private void SetSubAndPlayerSpawn() {
        var offset = GetVisualTilemapGridSize() * 6;
        var maxDepth = _gameSetupManager.WorldGenSettings.MaxDepth;
        // Depths is in blocks, so times it with grid size to get world space pos
        PlayerSpawn = new Vector3(0, maxDepth + offset);
    }

    public void MoveCamToChunkCoord(Vector2Int chunkCoord) {
        var size = ChunkManager.GetChunkSize() / 2;
        var cell = ChunkManager.ChunkCoordToCellOrigin(chunkCoord);
        _worldGenCamera.transform.position = GetCellCenterWorld(new(cell.x + size, cell.y + size));
    }
    public void UpdateTile(Vector3Int cellPos, ushort newTileId) {
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
    public void SetTileIEnumerator(Dictionary<BoundsInt, TileBase[]> tilesToSet, Dictionary<BoundsInt, TileBase[]> tilesShading) {
        StartCoroutine(SetWorldTiles(tilesToSet, tilesShading));
    }
    internal void SetOreIEnumerator(Dictionary<BoundsInt, TileBase[]> ores) {
        StartCoroutine(SetWorldOres(ores));    
    }
    private IEnumerator SetWorldTiles(Dictionary<BoundsInt, TileBase[]> tilesToSet, Dictionary<BoundsInt, TileBase[]> tilesShading) {
        foreach (var kvp in tilesToSet) {
            mainTilemap.SetTilesBlock(kvp.Key, kvp.Value);
            yield return null; // pause one frame
        }
        foreach (var kvp in tilesShading) {
            overlayTilemapShading.SetTilesBlock(kvp.Key, kvp.Value);
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
        if (tileToSet == 0) {
            // Remove shading
            overlayTilemapShading.SetTile(cellPos, null);
            overlayTilemapDamage.SetTile(cellPos, null);
        }
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
    public void RequestDamageTile(Vector3 worldPosition,float dmg) {
        var cell = WorldToCell(worldPosition);
        //Debug.Log($"Requesting processdamage of:{cell} with {dmg}");
        RequestDamageTile(cell, dmg);
    }
    public void RequestDamageTile(Vector3Int cellPos, float dmg) {
        ChunkManager.ProcessDamageTile(cellPos, dmg);
    }
    public void RequestDamageNearestSolidTile(Vector3 worldPosition, float dmg, int searchRadius = 3) {
        // Use your tilemap conversion
        Vector3Int originCell = WorldToCell(worldPosition);

        Vector3Int bestCell = originCell;
        float bestDistSqr = float.MaxValue;
        bool found = false;

        // Search square region around originCell
        for (int dx = -searchRadius; dx <= searchRadius; dx++) {
            for (int dy = -searchRadius; dy <= searchRadius; dy++) {
                int tx = originCell.x + dx;
                int ty = originCell.y + dy;

                // Directly use your integer-solid check
                if (ChunkManager.IsSolidTileAtWorldPos(tx, ty)) {
                    // Compute distance using the *center of the cell* in world space
                    // to ensure closest-tile behavior is consistent.
                    Vector3 candidateWorld = CellToWorld(new Vector3Int(tx, ty, originCell.z));
                    float distSqr = (candidateWorld - worldPosition).sqrMagnitude;

                    if (distSqr < bestDistSqr) {
                        bestDistSqr = distSqr;
                        bestCell = new Vector3Int(tx, ty, originCell.z);
                        found = true;
                    }
                }
            }
        }

        if (found) {
            ChunkManager.ProcessDamageTile(bestCell, dmg);
        } else {
            Debug.Log($"No solid tile found near {worldPosition} (radius {searchRadius})");
        }
    }

    internal void ClearAllData() {
        ChunkManager.ClearWorldChunks();
        mainTilemap.ClearAllTiles(); // Clear the visual tilemap
    }

    internal void SetChunkManager(ChunkManager chunkManager) {
        ChunkManager = chunkManager;
    }

    internal void SetOverlayTile(Vector3Int cellPos, TileBase crackTile) {
        //Debug.Log($"SETTING OVERLAY TILE {crackTile}");
        overlayTilemapDamage.SetTile(cellPos, crackTile); // Set tile on overlay layer
    }

    internal void SetOres(BoundsInt chunkBounds, TileBase[] oresToSet) {
        //Debug.Log("Setting ores for chunk " + chunkBounds);
        overlayTilemapOre.SetTilesBlock(chunkBounds, oresToSet); // Set tile on overlay layer
    }

    // Somehow got to get the worldGen to pick one spot in the biome for the artifact to be, then this has to be called 
    // Most likely from the chunk manager, and generated. 
    public void Place3x3Artifact(Transform t, Vector3Int centerCell, List<TileBase> tiles) {
        // set tiles in tilemap
        for (int dx = -1; dx <= 1; dx++) {
            for (int dy = -1; dy <= 1; dy++) {
                Vector3Int pos = centerCell + new Vector3Int(dx, dy, 0);
                var i = IndexFromOffset(dx, dy);
                TileBase tileToPlace = tiles[i];
                overlayTilemapOre.SetTile(pos, tileToPlace); // Ore generation will overwrite this (I think)
            }
        }
        // instantiate background prefab at the cell center in world coords
        Vector3 worldPos = mainTilemap.CellToWorld(centerCell) + mainTilemap.cellSize * 0.5f;
        t.position = worldPos;
    }
    // Index helper: (dx, dy) in [-1..1]
    private int IndexFromOffset(int dx, int dy) {
        // top-to-bottom, left-to-right mapping
        return (1 - dy) * 3 + (dx + 1);
    }

    internal void SetBiomeDebug(Dictionary<BoundsInt, TileBase[]> tiles) {
        foreach (var kvp in tiles) {
            overlayDEBUG.SetTilesBlock(kvp.Key, kvp.Value);
        }
    }
}
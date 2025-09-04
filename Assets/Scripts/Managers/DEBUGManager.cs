using Sirenix.OdinInspector;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

#if UNITY_EDITOR
public class DEBUGManager : StaticInstance<DEBUGManager> {
    [Header("References")]
    public BiomeManager biomeManager;
    public ChunkManager chunkManager;
    public Tilemap tilemap;
    public MachineControlPanel machineControlPanel;

    [System.Serializable]
    public struct BiomeTileMapping {
        public BiomeType biome;
        public TileBase tile;
    }

    [Header("Biome Tiles")]
    public List<BiomeTileMapping> biomeTileMappings;

    private Dictionary<BiomeType, TileBase> tileLookup;

    [SerializeField] PlayerMovement player;
    [OnValueChanged("PlayerSpeed")]
    public float playerSpeed;
    private NetworkedPlayer _player;

    private void PlayerSpeed() {
        player.accelerationForce = playerSpeed;
        player.swimSpeed = playerSpeed;
    }

    public void RegisterOwningPlayer(NetworkedPlayer player) {
        _player = player;
        _player.InventoryN.DEBUGGIVE(0, 9000);
        _player.InventoryN.DEBUGGIVE(1, 9000);
    }

    protected override void Awake() {
        base.Awake();
        tileLookup = new Dictionary<BiomeType, TileBase>();
        foreach (var mapping in biomeTileMappings) {
            tileLookup[mapping.biome] = mapping.tile;
        }
    }

    [ContextMenu("Visualize Biomes")]
    public void VisualizeBiomes() {

        if (biomeManager == null || tilemap == null || chunkManager == null) {
            biomeManager = FindFirstObjectByType<BiomeManager>();
            chunkManager = FindFirstObjectByType<ChunkManager>();
        }
            if (biomeManager == null || tilemap == null) {
            Debug.LogWarning("Missing references on BiomeDebugVisualizer.");
            return;
        }

        tilemap.ClearAllTiles();

        var allData = biomeManager.GetAllBiomeData();
        foreach (var kvp in allData) {
            Vector2Int chunkCoord = kvp.Key;
            BiomeChunkInfo info = kvp.Value;
            if (!tileLookup.TryGetValue(info.dominantBiome, out TileBase tile)) {
                continue;
            }
            var c = chunkManager.ChunkCoordToCellOrigin(chunkCoord);
            Vector3Int tilePos = new Vector3Int(c.x, c.y, 0);
            tilemap.SetTile(tilePos, tile);
        }
    }
    [ConsoleCommand("give", value: "itemID, amount")]
    private void debugGive(int i, int j) {
        _player.InventoryN.DEBUGGIVE(i,j);
    }


    [ConsoleCommand("showupgrade")]
    private void debugShowUpgradeScreen() {
        _player.UiManager.UpgradeScreen.DEBUGShowScreen();
    }
    [ConsoleCommand("showcontrol")]
    private void debugShowSubControlScreen() {
        machineControlPanel.DEBUGToggle();
    }
    [ConsoleCommand("setTerraform")]
    private void debugShowUpgradeScreen(float v) {
        TerraformingManager.Instance.DEBUGSetValue(v);
    }
    [ConsoleCommand("setSubIndex")]
    private void debugSetSubIndex(int v) {
        SubMovementManager.Instance.MoveSub(v);
    }
}
#endif
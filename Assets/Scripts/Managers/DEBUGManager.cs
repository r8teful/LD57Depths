using Sirenix.OdinInspector;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

#if UNITY_EDITOR
public class DEBUGManager : MonoBehaviour {
    [Header("References")]
    public BiomeManager biomeManager;
    public ChunkManager chunkManager;
    public Tilemap tilemap;

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
    private void PlayerSpeed() {
        player.accelerationForce = playerSpeed;
        player.swimSpeed = playerSpeed;
    }
       
    void Awake() {
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
        FindFirstObjectByType<NetworkedPlayerInventory>().DEBUGGIVE(i,j);
    }


    [ConsoleCommand("showupgrade")]
    private void debugShowUpgradeScreen() {
        FindFirstObjectByType<UIUpgradeScreen>().DEBUGShowScreen();
    }
}
#endif
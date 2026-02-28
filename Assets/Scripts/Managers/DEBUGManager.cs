using Sirenix.OdinInspector;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
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
    private PlayerManager _player;


    public void RegisterOwningPlayer(PlayerManager player) {
        _player = player;
    }
    private void GiveAll() {
        SubmarineManager.Instance.SubInventory.AddItem(0, 90000);
        SubmarineManager.Instance.SubInventory.AddItem(1, 90000);
        SubmarineManager.Instance.SubInventory.AddItem(2, 90000);
        SubmarineManager.Instance.SubInventory.AddItem(3, 90000);
        SubmarineManager.Instance.SubInventory.AddItem(4, 90000);
        SubmarineManager.Instance.SubInventory.AddItem(5, 90000);
        SubmarineManager.Instance.SubInventory.AddItem(6, 90000);
        SubmarineManager.Instance.SubInventory.AddItem(7, 90000);
        SubmarineManager.Instance.SubInventory.AddItem(8, 90000);
        SubmarineManager.Instance.SubInventory.AddItem(9, 90000);
        SubmarineManager.Instance.SubInventory.AddItem(10,90000);
        /*
        _player.InventoryN.DEBUGGIVE(0, Random.Range(300,600));
        _player.InventoryN.DEBUGGIVE(1, Random.Range(300,500));
        _player.InventoryN.DEBUGGIVE(2, Random.Range(200,400));
        _player.InventoryN.DEBUGGIVE(3, Random.Range(100,300));
        _player.InventoryN.DEBUGGIVE(4, Random.Range(100,300));
        _player.InventoryN.DEBUGGIVE(5, Random.Range(100,300));
        _player.InventoryN.DEBUGGIVE(6, Random.Range(100,300));
        _player.InventoryN.DEBUGGIVE(7, Random.Range(100,300));
        _player.InventoryN.DEBUGGIVE(8, Random.Range(50, 100));
        _player.InventoryN.DEBUGGIVE(9, Random.Range(50, 100));
        _player.InventoryN.DEBUGGIVE(10,Random.Range(50, 100));
    */
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
    [ConsoleCommand("giveXP")]
    private void debugXP(int i) {
        RewardEvents.TriggerGainXP(i);
    }

    [ConsoleCommand("giveShrine")]
    private void debugShrineReward() {
        GameSequenceManager.Instance.AddEvent(shouldPause: true,
         onStart: () => {
             _player.PlayerReward.GenerateRewardsShrine();
             RewardEvents.TriggerOpenShrine();
         }
     );
    }
    [ConsoleCommand("giveChest")]
    private void debugChestReward() {
        GameSequenceManager.Instance.AddEvent(shouldPause: true,
         onStart: () => {
             _player.PlayerReward.GenerateRewardsChest();
             RewardEvents.TriggerOpenChest();
         }
     );
    }
    [ConsoleCommand("giveCave")]
    private void debugCaveReward() {
        // todo
    }

    [ConsoleCommand("showupgrade")]
    private void debugShowUpgradeScreen() {
        _player.UiManager.UpgradeScreen.PanelToggle();
    }

    [ConsoleCommand("giveAll")]
    private void debugGiveAll() {
        GiveAll();
    }

    [ConsoleCommand("clearAll")]
    private void debugClearInventory() {
        _player.InventoryN.RemoveAll();
        SubmarineManager.Instance.SubInventory.RemoveAllItems();
    }
    [ConsoleCommand("clearAllUpgrade")]
    private void debugClearUpgrades() {
        _player.UpgradeManager.RemoveAllUpgrades();
        SubmarineManager.Instance.RemoveAllUpgrades();
    }
    [ConsoleCommand("showcontrol")]
    private void debugShowSubControlScreen() {
        machineControlPanel.DEBUGToggle();
    }
    [ConsoleCommand("setSubIndex")]
    private void debugSetSubIndex(int v) {
        SubmarineManager.Instance.MoveSub(v);
    }
    [ConsoleCommand("giveMineDamage")]
    private void debugAddDamage(float v) {
        PlayerManager.LocalInstance.PlayerStats.DEBUGADDSTAT(StatType.MiningDamage,v);
    }
    [ConsoleCommand("giveMineRange")]
    private void debugSetRange(float v) {
        PlayerManager.LocalInstance.PlayerStats.DEBUGADDSTAT(StatType.MiningRange, v);
    }
    [ConsoleCommand("toggleHitbox")]
    private void debugToggleHitbox() {
        _player.PlayerMovement.DEBUGToggleHitbox();
    }

    [ConsoleCommand("toggleGod")]
    private void debugToggleGOD() {
        _player.PlayerMovement.DEBUGToggleGodMove();
        _player.OxygenManager.DEBUGinfOx();
    }
    [ConsoleCommand("giveInfOxygen")]
    private void debugOxygen() {
        _player.OxygenManager.DEBUGinfOx();
    }
    //[ConsoleCommand("setSpeed")]
    //private void debugSetSpeed(float speed) {
    //    _player.PlayerMovement.DEBUGSetSpeed(speed);
    //}
    [ConsoleCommand("setZoom")]
    private void debugSetZoom(float size) {
        Camera.main.orthographicSize = size;   
    }
    [ConsoleCommand("toggleUI")]
    private void debugToggleUI() {
        _player.UiManager.DEBUGToggleALLUI();
    }
    [ConsoleCommand("gotoBiome",value: "bio,fungal,forest,desert")]
    private void debugGotoBiome(string biome) {
        var settings = GameSetupManager.Instance.CurrentGameSettings;
        if(settings == null) {
            Debug.LogError("Couldnt get host settings!");
            return;
        }
        int index = 0;
        if(biome == "bio") {
            index = 0;
        }
        if (biome == "fungal") {
            index = 1;
        }
        if (biome == "forest") {
            index = 2;
        }
        if (biome == "desert") {
            index = 3;
        }
        var worldBiome = App.ResourceSystem.GetWorldGenByID(settings.WorldGenID).biomes[index];
        _player.gameObject.transform.position = 
            new(worldBiome.XOffset, worldBiome.YStart + worldBiome.YHeight * 0.5f);
    }
}
#endif
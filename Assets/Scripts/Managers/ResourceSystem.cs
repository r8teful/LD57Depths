using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;

// Based on https://youtu.be/tE1qH8OxO2Y
// Basically just runs once at the start of the program and stores a dictionary of the different
// scriptable data which can then be easaly accessed within different scripts
public class ResourceSystem {
    public List<GameObject> Prefabs { get; private set; }
    public List<Sprite> Sprites { get; private set; }
    public List<Material> Materials { get; private set; }
    public List<BackgroundObjectSO> BackgroundObjects { get; private set; }
    public List<UpgradeTreeDataSO> UpgradeTreeData { get; private set; }
    public static int[] GetRarityWeight => new[] { 70, 15, 6, 2 };

    public static string ScenePlayName = "PlayScene";
    public static string SceneMenuName = "MainMenu";

    private Dictionary<string, GameObject> _prefabDict;
    private Dictionary<string, Sprite> _spriteDict;
    private Dictionary<string, Material> _materialDict;

    private Dictionary<ushort, TileSO> _tileLookupByID;
    private Dictionary<TileSO, ushort> _idLookupByTile;

    private Dictionary<ushort, ItemData> _itemLookupByID;
    private Dictionary<ItemData, ushort> _idLookupByItem;

    private Dictionary<ushort, EntityBaseSO> _entityLookupByID;

    private Dictionary<ushort, WorldGenSettingSO> _worldGenLookupByID;

    private Dictionary<ushort, UpgradeNodeSO> _upgradeNodeByID;

    private Dictionary<ushort, BuffSO> _buffLookupByID;
    
    private Dictionary<ushort, AbilitySO> _abilityLookupByID;

    private Dictionary<ushort, EventCaveSO> _eventCaveLookupByID;

    private Dictionary<ushort, BiomeDataSO> _biomeLookupByID;

    private Dictionary<ushort, StructureSO> _structureLookupByID;

    private Dictionary<ushort, MetaUnlockSO> _unlockLookupByID;


    public const ushort InvalidID = ushort.MaxValue; // Reserve MaxValue for invalid/empty
    public const ushort AirID = 0; // Air is ALWAYS 0 
    public const ushort StoneID = 1; // stone is ALWAYS 1 
    public const ushort StoneItemID = 0;
    public const ushort StoneToughID = 2; 
    public const ushort StoneVeryToughID = 3; 
    public const ushort LadderID = 501; // Ladder is always 501, used in SubInterior.cs
    public const ushort ControlPanellRecipeID = 101; // FixRecipe.cs
    public const ushort BiomeEssenceID = 11; 
    public const ushort StructureArtifactID = 1; 
    public const ushort StructureChestID = 2; 
    public const ushort StructureShrineID = 3; 
    public const ushort StructureEventCaveID = 4; 

    // World stuff?
    public static ushort WORLD_MAP_ID = 1;

    public const ushort BrimstoneBuffID = 1; // There must be a better way
    public const ushort BiomeBuffID = 100; // There must be a better way
    public const ushort PlayerDashID = 10; 
    public const ushort LazerEffectID = 0; // There must be a better way
    public const ushort BlockOxygenID = 200; 
    public const ushort CompassID = 210; 
    public const ushort CompassPlusID = 211; 
    public const ushort GraveStoneID = 220; // There must be a better way
    public const ushort LazerChainID = 240; 
    public const ushort Adrenaline = 260; 
    public const ushort CactusAbilityID = 500; 
    public const ushort ShockwaveID = 530; 
    public const ushort BoomerangID = 540; 
    public const ushort BlackholeID = 550; 
    public const ushort BouncingBallID = 560; 
    public const ushort FishShooterID = 570; 
  
    public const ushort SubUpgradePanel = 0;   
    public const ushort SubUpgradeControlPanel = 1000; 
    public const ushort SubUpgradeCables = 1001; 
   public static bool IsGrowEntity(ushort id) {
        if (id == 900) // Tree farm
            return true;
        return false;
    }
    public void AssembleResources() {

        Prefabs = Resources.LoadAll<GameObject>("Prefabs").ToList();
        _prefabDict = Prefabs.ToDictionary(r => r.name, r => r);

        Sprites = Resources.LoadAll<Sprite>("Sprites").ToList();
        _spriteDict = Sprites.ToDictionary(r => r.name, r => r);

        Materials = Resources.LoadAll<Material>("Materials").ToList();
        _materialDict = Materials.ToDictionary(r => r.name, r => r);


        BackgroundObjects = Resources.LoadAll<BackgroundObjectSO>("BackgroundObjectData").ToList();

        UpgradeTreeData = Resources.LoadAll<UpgradeTreeDataSO>("UpgradeTreeData").ToList();

        InitializeLookup("ItemData", out _itemLookupByID, out _idLookupByItem);
        InitializeLookup("TileData", out _tileLookupByID, out _idLookupByTile);
        InitializeLookup("EntityData", out _entityLookupByID, out _);
        InitializeLookup("UpgradeNodeData", out _upgradeNodeByID, out _);
        InitializeLookup("WorldGenData", out _worldGenLookupByID, out _);
        InitializeLookup("BuffData", out _buffLookupByID, out _);
        InitializeLookup("AbilityData", out _abilityLookupByID, out _);
        InitializeLookup("EventCaveData", out _eventCaveLookupByID, out _);
        InitializeLookup("StructureData", out _structureLookupByID, out _);
        InitializeLookup("UnlockData", out _unlockLookupByID, out _);
        InitializeLookup("BiomeData", out _biomeLookupByID, out _);
    }

    public void InitializeWorldEntities(int worldSeed,Vector2 worldOffset) {
        foreach (var entity in _entityLookupByID) {
            if (entity.Value is WorldSpawnEntitySO wse) {
                wse.Init(worldSeed, worldOffset);
            }
        }
    }

    private void InitializeLookup<T>(string resourcePath, out Dictionary<ushort, T> lookupByID,
        out Dictionary<T, ushort> idByLookup) where T : Object, IIdentifiable { lookupByID = new Dictionary<ushort, T>(); idByLookup = new Dictionary<T, ushort>();

        var allAssets = Resources.LoadAll<T>(resourcePath).ToList();
        for (int i = 0; i < allAssets.Count; i++) {
            T asset = allAssets[i];
            if (asset == null) {
                Debug.LogWarning($"Found a NULL {typeof(T).Name} entry at index {i}. Skipping.");
                continue;
            }
            ushort id = asset.ID;
            if (lookupByID.ContainsKey(id)) {
                Debug.LogError($"{typeof(T).Name}Database conflict: ID {id}, asset {asset.name} (index {i}) is already assigned to '{lookupByID[id].name}'. Duplicate or internal error?");
                continue;
            }
            if (idByLookup.ContainsKey(asset)) {
                Debug.LogError($"{typeof(T).Name}Database conflict: '{asset.name}' is already in the database with ID {idByLookup[asset]}. Ensure assets are unique.");
                continue;
            }
            lookupByID.Add(id, asset);
            idByLookup.Add(asset, id);
        }
        Debug.Log($"{typeof(T).Name}Database Initialized with {lookupByID.Count} items.");
    }
    public ItemData GetItemByID(ushort id) {
        if (id == InvalidID || !_itemLookupByID.TryGetValue(id, out ItemData item)) {
            Debug.LogWarning($"Item ID {id} not found in ItemDatabase.");
            return null;
        }
        return item;
    }

    public ushort GetIDByItem(ItemData item) {
        if (item == null || !_idLookupByItem.TryGetValue(item, out ushort id)) {
            Debug.LogWarning($"ItemData '{item.name}' not found in ItemDatabase. Make sure it's added to the database asset.");
            return InvalidID;
        }
        return id;
    }

    public TileSO GetTileByID(ushort id) {
        if (id == InvalidID || !_tileLookupByID.TryGetValue(id, out TileSO tile)) {
            if (id != InvalidID) Debug.LogWarning($"Item ID {id} not found in ItemDatabase.");
            return null;
        }
        return tile;
    }
    public ushort GetIDByTile(TileSO tile) {
        if (tile == null || !_idLookupByTile.TryGetValue(tile, out ushort id)) {
            Debug.LogWarning($"TileData '{tile.name}' not found in ItemDatabase. Make sure it's added to the database asset.");
            return InvalidID;
        }
        return id;
    }
    public EntityBaseSO GetEntityByID(ushort id) {
        if (id == InvalidID || !_entityLookupByID.TryGetValue(id, out EntityBaseSO entity)) {
            Debug.LogWarning($"Entity ID {id} not found in database.");
            return null;
        }
        return entity;
    }
    public UpgradeNodeSO GetUpgradeNodeByID(ushort id) {
        if (id == InvalidID || !_upgradeNodeByID.TryGetValue(id, out UpgradeNodeSO node)) {
            Debug.LogWarning($"UpgradeNodeSO ID {id} not found in database.");
            return null;
        }
        return node; 
    }
    public WorldGenSettingSO GetWorldGenByID(ushort id) {
        if (id == InvalidID || !_worldGenLookupByID.TryGetValue(id, out WorldGenSettingSO worldGen)) {
            Debug.LogWarning($"worldGen ID {id} not found in database.");
            return null;
        }
        return worldGen;
    }
    public BuffSO GetBuffByID(ushort id) {
        if (id == InvalidID || !_buffLookupByID.TryGetValue(id, out BuffSO buff)) {
            Debug.LogWarning($"buff ID {id} not found in database.");
            return null;
        }
        return buff;
    }
    public BiomeDataSO GetBiomeData(BiomeType b) {
        return GetBiomeData((ushort)b);
    }

    public BiomeDataSO GetBiomeData(ushort id) {
        if (id == InvalidID || !_biomeLookupByID.TryGetValue(id, out BiomeDataSO biome)) {
            Debug.LogWarning($"biome ID {id} not found in database.");
            return null;
        }
        return biome;
    }
    public AbilitySO GetAbilityByID(ushort id) {
        if (id == InvalidID || !_abilityLookupByID.TryGetValue(id, out AbilitySO ability)) {
            Debug.LogWarning($"ability ID {id} not found in database.");
            return null;
        }
        return ability;
    }
    internal StructureSO GetStructureByID(ushort id) {
        if (id == InvalidID || !_structureLookupByID.TryGetValue(id, out StructureSO structure)) {
            Debug.LogWarning($"structure ID {id} not found in database.");
            return null;
        }
        return structure;
    }
    internal MetaUnlockSO GetUnlockByID(ushort id) {
        if (id == InvalidID || !_unlockLookupByID.TryGetValue(id, out MetaUnlockSO unlock)) {
            Debug.LogWarning($"unlock ID {id} not found in database.");
            return null;
        }
        return unlock;
    }
    public AbilitySO GetRandomAvailableAbility(HashSet<ushort> exluded) {
        var rnd = new System.Random();
        var available = GameManager.Instance.CurrentGameSettings.AvailableAbilityIDs;
        var validEntries = available
            .Where(a => !exluded.Contains(a))
            .ToArray();
        if (validEntries.Length == 0) return null;
        var abilityID = validEntries[rnd.Next(validEntries.Length)];
        return _abilityLookupByID[abilityID];
    }
    public EventCaveSO GetRandomAvailableCave(HashSet<ushort> exluded) {
        var rnd = new System.Random();
        var available = GameManager.Instance.CurrentGameSettings.AvailableEventCaveIDs;
        var validEntries = available
            .Where(a => !exluded.Contains(a))
            .ToArray();
        if (validEntries.Length == 0) return null;
        var abilityID = validEntries[rnd.Next(validEntries.Length)];
        return _eventCaveLookupByID[abilityID];
    }
    public ZoneSO GetZoneByIndex(int zoneIndex) {
        var list = Resources.LoadAll<ZoneSO>("TrenchZones").ToList();
        var index = list.FindIndex(i => i.ZoneIndex == zoneIndex);
        if (index == -1) {
            Debug.LogWarning($"Zoneindex {zoneIndex} not found in database.");
            return null;
        }
        return list[index];
    }
    public UpgradeTreeDataSO GetTreeByName(string name) {
        return UpgradeTreeData.FirstOrDefault(s => s.treeName == name);
    }

    public GameObject GetPrefab(string s) {
        if (_prefabDict.TryGetValue(s, out var g)) {
            return g;
        } else {
            throw new KeyNotFoundException($"No prefab found in _prefabDict with key '{s}'");
        }
    }
    public bool TryGetPrefab(string s, out GameObject gameObject) {
        if (_prefabDict.TryGetValue(s, out var g)) {
            gameObject = g;
            return true;
        } else {
            gameObject = null;
            return false;
        }
    }
    public T GetPrefab<T>(string key) where T : Component {
        if (!_prefabDict.TryGetValue(key, out GameObject prefab))
            throw new KeyNotFoundException($"No prefab found in _prefabDict with key '{key}'");

        T component = prefab.GetComponent<T>();
        if (component == null)
            throw new System.InvalidOperationException(
                $"Prefab '{key}' does not have a component of type {typeof(T).Name}"
            );

        return component;
    }
    public Sprite GetSprite(string s) {
        if (s == "" || !_spriteDict.TryGetValue(s, out Sprite sprite)) {
            Debug.LogWarning($"Recipe string {s} not found in database.");
            return null;
        }
        return sprite;
    }
    public Material GetMaterial(string s) => _materialDict[s];

    internal List<ItemData> GetAllItems() {
        return _itemLookupByID.Values.OfType<ItemData>().OrderBy(item => item.ID).ToList();
    }
    public List<WorldGenOreSO> GetAllOreData() {
        return Resources.LoadAll<WorldGenOreSO>("Ores").ToList();
    }
    public List<GameObject> GetAllTools() {
        List<GameObject> list = new() {
            GetPrefab("MiningLazer"),
            GetPrefab("MiningDrill"),
            GetPrefab("MiningRPG")
        };
        return list;
    }
    public List<WorldSpawnEntitySO> GetAllWorldSpawnEntities() {
        return _entityLookupByID.Values.OfType<WorldSpawnEntitySO>().ToList();
    }
    public List<MetaUnlockSO> GetAllUnlocks() {
        return _unlockLookupByID.Values.ToList();
    }

    internal Dictionary<ushort,int> GetMaxItemPool() {
        var items = GetAllItems();
        var d = new Dictionary<ushort, int>();
        foreach (var item in items)
        {
            d.Add(item.ID, 999);
        }
        return d;
    }

    internal List<WorldSpawnEntitySO> GetDebugEntity() {
        return new() {
            _entityLookupByID[9999] as WorldSpawnEntitySO
        };
    }


    public static bool IsLowerBad(StatType stat) {
        return stat switch {
            StatType.Knockback => false,
            StatType.MiningFalloff => false,
            _ => true,
        };
    }
    public static bool IsLowerBad(ValueKey key) {
        return key switch {
            _ => false,
        };
    }
    public static string GetStatString(StatType stat) {
        return stat switch {
            StatType.MiningRange => "Range",
            StatType.MiningDamage => "Damage",
            StatType.MiningRotationSpeed => "Movement Speed",
            StatType.PlayerSpeedMax => "Maximum Speed",
            StatType.PlayerAcceleration => "Acceleration",
            StatType.PlayerOxygenMax => "Oxygen Capacity",
            StatType.Knockback => "Knockback Force",
            StatType.MiningFalloff => "Falloff",
            StatType.MiningCombo => "Damage Falloff",
            StatType.PlayerDrag => "Player Drag",
            StatType.Cooldown => "Cooldown",
            StatType.Duration => "Duration",
            StatType.Size => "Size",
            StatType.ProjectileCount => "Projectile Count",
            StatType.ProjectileSpeed => "Projectile Speed",
            StatType.ProjectileBounces => "Projectile Bounces",
            StatType.Luck => "Luck",
            StatType.MiningCritDamage => "Crit DMG",
            StatType.MiningCritChance => "Crit Chance",
            _ => "NULL",
        };
    }
 
    public static float GetIncreaseByRarity(RarityType t) {
        return t switch {
            RarityType.Common => 1f,
            RarityType.Uncommon => 1.4f,
            RarityType.Rare => 1.6f,
            RarityType.Legendary => 2f,
            _ => 0,
        };
    }
    public static WorldGenSettingSO GetMapByID(ushort id) {
        var allAssets = Resources.LoadAll<WorldGenSettingSO>("WorldGenData").ToList();
        //var guids = AssetDatabase.FindAssets("t:WorldGenSettingSO");
        foreach (var world in allAssets) {
            //var path = AssetDatabase.GUIDToAssetPath(guid);
            //var so = AssetDatabase.LoadAssetAtPath<WorldGenSettingSO>(path);
            if (world != null && world.ID == id)
                return world;
        }
        return null;
    }
    public static WorldGenSettingSO GetMainMap() {
        return GetMapByID(WORLD_MAP_ID);
    }

    // So we have a texture array that the shaders use. This simply takes the biomeEnum and turns it 
    // into the texture index that is used by that biome
    internal static int GetTextureIndexFromBiome(BiomeType b) {
        return b switch {
            BiomeType.Trench => 0,
            BiomeType.Bioluminescent => 1,
            BiomeType.Fungal => 2,
            BiomeType.Forest => 3,
            BiomeType.Deadzone => 4,
            BiomeType.Surface => InvalidID,
            BiomeType.AncientCaves => InvalidID,
            BiomeType.Algea => InvalidID,
            BiomeType.Reef => InvalidID,
            BiomeType.Ocean => InvalidID,
            BiomeType.LostCity => InvalidID,
            BiomeType.None => AirID,
            BiomeType.Snow => InvalidID,
            BiomeType.Gems => InvalidID,
            BiomeType.ShipGraveyard => InvalidID,
            BiomeType.Volcanic => InvalidID,
            BiomeType.Trench1 => 7,
            BiomeType.Trench2 => 9,
            BiomeType.Trench3 => 10,
            _ => 0,
        };
    }

    internal static List<StatModifier> GetStatRewards() {
        return new List<StatModifier>() {
            //new(0.2f, StatType.Size, StatModifyType.Add,null),
            //new(0.2f, StatType.Cooldown, StatModifyType.Add,null),
            new(0.4f, StatType.MiningDamage, StatModifyType.PercentAdd,null),
            new(0.4f, StatType.MiningRange, StatModifyType.PercentAdd,null),
            new(0.4f, StatType.PlayerSpeedMax, StatModifyType.PercentAdd,null),
            new(0.5f, StatType.MiningCritDamage, StatModifyType.PercentAdd,null),
            new(0.01f, StatType.MiningCritChance, StatModifyType.Add,null),
            new(10f, StatType.PlayerOxygenMax, StatModifyType.Add,null),
            //new(0.2f, StatType.Luck, StatModifyType.Add,null),
            //new(0.2f, StatType.ProjectileCount, StatModifyType.Add,null)
        };
    }

    internal TileBase GetDebugBiomeTile(BiomeType biomeID) {
        switch (biomeID) {
            case BiomeType.Bioluminescent:
                return GetTileByID(65532);
            case BiomeType.Deadzone:
                return GetTileByID(65533);
            case BiomeType.Forest:
                return GetTileByID(65534);
            case BiomeType.Fungal:
                return GetTileByID(65535);
            case BiomeType.None:
                return null;
            default:
                return null;
        }
    }

    internal static string BiomeToString(BiomeType to) {
        return to.ToString(); // idk needs localization 
    }

    internal static DisplayType GetDisplayType(ValueKey valueType) {
        return valueType switch {
            ValueKey.GravestoneHoldProcent  => DisplayType.Absolute,
            ValueKey.MagnetismPickup        => DisplayType.Absolute,
            ValueKey.MagnetismStrength      => DisplayType.Procent,
            ValueKey.ItemTransferRate       => DisplayType.Procent,
            ValueKey.LazerChainLength       => DisplayType.Procent,
            ValueKey.LazerChainDamage       => DisplayType.Procent,
            ValueKey.LazerChainChance       => DisplayType.Absolute,
            ValueKey.ExplosiveCritChance    => DisplayType.Procent,
            ValueKey.ExplosiveCritDamage    => DisplayType.Procent,
            ValueKey.ExplosiveCritRange     => DisplayType.Absolute,
            ValueKey.BlockOxygenAmount      => DisplayType.Absolute,
            ValueKey.BlockOxygenChance      => DisplayType.Absolute,
            _ => DisplayType.Procent,
        };
    }
    internal static DisplayType GetDisplayType(StatType stat) {
        return stat switch {
            StatType.MiningCritChance => DisplayType.Absolute,
            StatType.MiningCritDamage => DisplayType.Absolute,
            _ => DisplayType.Procent,
        };
    }
}

[System.Serializable]
public class StatDefault {
    public StatType Stat;
    public float BaseValue;
}
public enum StatType {
    // MINING
    MiningRange = 0,
    MiningDamage = 1,
    MiningRotationSpeed = 2,
    Knockback = 3,
    MiningFalloff = 4,
    MiningCombo = 5,
    MiningCritDamage = 6,
    MiningCritChance = 7,

    // PLAYER 
    PlayerSpeedMax = 20,
    PlayerAcceleration = 21,
    PlayerDrag = 22,
    PlayerOxygenMax = 24,

    // General
    Cooldown = 1000,
    Duration = 1001,
    Size = 1002,
    ProjectileCount = 1003,
    ProjectileSpeed = 1004,
    ProjectileBounces = 1005,
    Luck = 1006
}
public enum RarityType { 
    Common,
    Uncommon,
    Rare,
    Legendary
}
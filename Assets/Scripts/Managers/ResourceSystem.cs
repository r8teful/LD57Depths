using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

// Based on https://youtu.be/tE1qH8OxO2Y
// Basically just runs once at the start of the program and stores a dictionary of the different
// scriptable data which can then be easaly accessed within different scripts
public class ResourceSystem {
    public List<GameObject> Prefabs { get; private set; }
    public List<Sprite> Sprites { get; private set; }
    public List<Material> Materials { get; private set; }
    public List<BackgroundObjectSO> BackgroundObjects { get; private set; }
    public List<UpgradeTreeDataSO> UpgradeTreeData { get; private set; }

    private Dictionary<string, GameObject> _prefabDict;
    private Dictionary<string, Sprite> _spriteDict;
    private Dictionary<string, Material> _materialDict;

    private Dictionary<ushort, TileSO> _tileLookupByID;
    private Dictionary<TileSO, ushort> _idLookupByTile;

    private Dictionary<ushort, ItemData> _itemLookupByID;
    private Dictionary<ItemData, ushort> _idLookupByItem;

    private Dictionary<ushort, EntityBaseSO> _entityLookupByID;

    private Dictionary<ushort, RecipeBaseSO> _recipeLookupByID;

    private Dictionary<ushort, WorldGenSettingSO> _worldGenLookupByID;

    private Dictionary<ushort, UpgradeRecipeSO> _recipeUpgradeLookupByID;

    private Dictionary<ushort, BuffSO> _buffLookupByID;
    
    private Dictionary<ushort, AbilitySO> _abilityLookupByID;
    private Dictionary<ushort, BiomeDataSO> _biomeLookupByID;


    public const ushort InvalidID = ushort.MaxValue; // Reserve MaxValue for invalid/empty
    public const ushort AirID = 0; // Air is ALWAYS 0 
    public const ushort LadderID = 501; // Ladder is always 501, used in SubInterior.cs
    public const ushort ControlPanellRecipeID = 101; // FixRecipe.cs

    public const ushort BrimstoneBuffID = 1; // There must be a better way
    public const ushort BiomeBuffID = 99; // There must be a better way
    public const ushort LazerEffectID = 0; // There must be a better way

    public const ushort UpgradeFlippersID = 102; // Max speed 3 
    public const ushort UpgradeOxygenID = 122;  // Oxygen tier 3
    public const ushort UpgradeJetpackID = 132; // Special handling 
    public const ushort FIRST_SHIP_RECIPE_ID = 200;
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
        
        UpgradeTreeData = Resources.LoadAll<UpgradeTreeDataSO>("UpgradeTreeData").ToList();

        InitializeLookup("ItemData", out _itemLookupByID, out _idLookupByItem);
        InitializeLookup("TileData", out _tileLookupByID, out _idLookupByTile);
        InitializeLookup("EntityData", out _entityLookupByID, out _);
        InitializeLookup("RecipeData", out _recipeLookupByID, out _);
        InitializeLookup("WorldGenData", out _worldGenLookupByID, out _);
        InitializeLookup("BuffData", out _buffLookupByID, out _);
        InitializeLookup("AbilityData", out _abilityLookupByID, out _);
        InitializeLookup("BiomeData", out _biomeLookupByID, out _);

        InitializeLookup<UpgradeRecipeSO>("", out _recipeUpgradeLookupByID, out _);
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
                Debug.LogError($"{typeof(T).Name}Database conflict: ID {id} (index {i}) is already assigned to '{lookupByID[id].name}'. Duplicate or internal error?");
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
    public RecipeBaseSO GetRecipeByID(ushort id) {
        if (id == InvalidID || !_recipeLookupByID.TryGetValue(id, out RecipeBaseSO recipe)) {
            Debug.LogWarning($"Recipe ID {id} not found in database.");
            return null;
        }
        return recipe;
    }
    public UpgradeRecipeSO GetRecipeUpgradeByID(ushort id) {
        if (id == InvalidID || !_recipeUpgradeLookupByID.TryGetValue(id, out UpgradeRecipeSO recipe)) {
            Debug.LogWarning($"Recipe ID {id} not found in database.");
            return null;
        }
        return recipe;
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

    public List<CraftingRecipeSO> GetAllCraftingRecipes() {
        return _recipeLookupByID.Values.OfType<CraftingRecipeSO>().ToList();
    }
    internal List<SubRecipeSO> GetAllSubRecipes() {
        return _recipeLookupByID.Values.OfType<SubRecipeSO>().ToList();
    }
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
            StatType.MiningRange => true,
            StatType.MiningDamage => true,
            StatType.MiningRotationSpeed => true,
            StatType.PlayerSpeedMax => true,
            StatType.PlayerAcceleration => true,
            StatType.PlayerOxygenMax => true,
            StatType.Knockback => false,
            StatType.MiningFalloff => false,
            _ => true,
        };
    }
    public static string GetStatString(StatType stat) {
        return stat switch {
            StatType.MiningRange => "Range",
            StatType.MiningDamage => "Damage",
            StatType.MiningRotationSpeed => "Movement Speed",
            StatType.PlayerSpeedMax => "Maximum Speed",
            StatType.PlayerAcceleration => "Acceleration",
            StatType.PlayerOxygenMax => "Capacity (seconds)",
            StatType.Knockback => "Knockback Force",
            StatType.MiningFalloff => "Falloff",
            StatType.MiningCombo => "Damage Falloff",
            StatType.PlayerDrag => "Player Drag",
            StatType.PlayerMagnetism => "Player Drag",
            _ => "NULL",
        };
    }
    public static WorldGenSettingSO GetMapByID(ushort id) {
#if UNITY_EDITOR
        var allAssets = Resources.LoadAll<WorldGenSettingSO>("WorldGenData").ToList();
        //var guids = AssetDatabase.FindAssets("t:WorldGenSettingSO");
        foreach (var world in allAssets) {
            //var path = AssetDatabase.GUIDToAssetPath(guid);
            //var so = AssetDatabase.LoadAssetAtPath<WorldGenSettingSO>(path);
            if (world != null && world.ID == id)
                return world;
        }
#endif
        return null;
    }
    public static WorldGenSettingSO GetMainMap() {
        return GetMapByID(WorldManager.WORLD_MAP_ID);
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

    // PLAYER 
    PlayerSpeedMax = 20,
    PlayerAcceleration = 21,
    PlayerDrag = 22,
    PlayerMagnetism = 23,
    PlayerOxygenMax = 24,

    // General
    Cooldown = 1000
}
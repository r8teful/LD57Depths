using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

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
    private Dictionary<EntityBaseSO, ushort> _idLookupByEntity;

    private Dictionary<ushort, RecipeBaseSO> _recipeLookupByID;
    private Dictionary<RecipeBaseSO, ushort> _idLookupByRecipe;
    
    public const ushort InvalidID = ushort.MaxValue; // Reserve MaxValue for invalid/empty
    public const ushort AirID = 0; // Air is ALWAYS 0 
    public const ushort LadderID = 501; // Ladder is always 501, used in SubInterior.cs
    public const ushort ControlPanellRecipeID = 101; // FixRecipe.cs

    public const ushort UpgradeMiningRange = 101;
    public const ushort UpgradeMiningDamage = 101;
    public const ushort UpgradeSpeedMax = 101;
    public const ushort UpgradeSpeedAcceleration = 101;
    public const ushort UpgradeOxygenMax = 101;
    public const ushort UpgradeDashUnlock = 101;
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

        InitializeLookup("ItemData", out _itemLookupByID, out _idLookupByItem);
        InitializeLookup("TileData", out _tileLookupByID, out _idLookupByTile);
        InitializeLookup("EntityData", out _entityLookupByID, out _idLookupByEntity);
        InitializeLookup("RecipeData", out _recipeLookupByID, out _idLookupByRecipe);

        InitializeWorldEntityOffsets();
    }

    private void InitializeWorldEntityOffsets() {
        foreach (var entity in _entityLookupByID) {
            if(entity.Value is WorldSpawnEntitySO wse) {
                wse.Init();
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
            if(id!=InvalidID)Debug.LogWarning($"Item ID {id} not found in ItemDatabase.");
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
    public ZoneSO GetZoneByIndex(int zoneIndex) {
        var list = Resources.LoadAll<ZoneSO>("TrenchZones").ToList();
        var index = list.FindIndex(i =>  i.ZoneIndex == zoneIndex);
        if(index == -1) {
            Debug.LogWarning($"Zoneindex {zoneIndex} not found in database.");
            return null;
        }
        return list[index];
    }
    public UpgradeTreeDataSO GetTreeByName(string name) {
        return UpgradeTreeData.FirstOrDefault(s => s.treeName == name);
    }

    public GameObject GetPrefab(string s) => _prefabDict[s];
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
        return _itemLookupByID.Values.OfType<ItemData>().ToList();
    }
    public List<WorldGenOreSO> GetAllOreData() {
        return Resources.LoadAll<WorldGenOreSO>("Ores").ToList();

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
}
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

    private Dictionary<string, GameObject> _prefabDict;
    private Dictionary<string, Sprite> _spriteDict; 
    private Dictionary<string, Material> _materialDict;
    private Dictionary<ushort, TileSO> _tileLookupByID;
    private Dictionary<TileSO, ushort> _idLookupByTile;
    private Dictionary<ushort, ItemData> _itemLookupByID;
    private Dictionary<ItemData, ushort> _idLookupByItem;

    private Dictionary<ushort, EntityBaseSO> _entityLookupByID;
    private Dictionary<EntityBaseSO, ushort> _idLookupByEntity;
    public const ushort InvalidID = ushort.MaxValue; // Reserve MaxValue for invalid/empty
    public const ushort AirID = 0; // Air is ALWAYS 0 
    public void AssembleResources() {
   
        Prefabs = Resources.LoadAll<GameObject>("Prefabs").ToList();
        _prefabDict = Prefabs.ToDictionary(r => r.name, r => r);

        Sprites = Resources.LoadAll<Sprite>("Sprites").ToList();
        _spriteDict = Sprites.ToDictionary(r => r.name, r => r); 
        
        Materials = Resources.LoadAll<Material>("Materials").ToList(); 
        _materialDict = Materials.ToDictionary(r => r.name, r => r);

        InitializeLookup("ItemData", out _itemLookupByID, out _idLookupByItem);
        InitializeLookup("TileData", out _tileLookupByID, out _idLookupByTile);
        InitializeLookup("EntityData", out _entityLookupByID, out _idLookupByEntity);
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
            Debug.LogWarning($"Item ID {id} not found in ItemDatabase.");
            return null;
        }
        return entity;
    }
    public GameObject GetPrefab(string s) => _prefabDict[s];
    public Sprite GetSprite(string s) => _spriteDict[s];
    public Material GetMaterial(string s) => _materialDict[s]; 
}
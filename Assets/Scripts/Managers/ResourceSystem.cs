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
    private Dictionary<ushort, ItemData> _itemLookupByID;
    private Dictionary<ItemData, ushort> _idLookupByItem;
    public const ushort InvalidID = ushort.MaxValue; // Reserve MaxValue for invalid/empty
    public void AssembleResources() {
   
        Prefabs = Resources.LoadAll<GameObject>("Prefabs").ToList();
        _prefabDict = Prefabs.ToDictionary(r => r.name, r => r);

        Sprites = Resources.LoadAll<Sprite>("Sprites").ToList();
        _spriteDict = Sprites.ToDictionary(r => r.name, r => r); 
        
        Materials = Resources.LoadAll<Material>("Materials").ToList(); 
        _materialDict = Materials.ToDictionary(r => r.name, r => r);
        
        InitializeItemLookups();
    }
    private void InitializeItemLookups() {
        _itemLookupByID = new Dictionary<ushort, ItemData>();
        _idLookupByItem = new Dictionary<ItemData, ushort>();

        var allItems = Resources.LoadAll<ItemData>("ItemData").ToList();
        for (int i = 0; i < allItems.Count; i++) {
            ItemData item = allItems[i];
            if (item == null) {
                Debug.LogWarning($"Found a NULL ItemData entry at index {i}. Skipping.");
                continue;
            }
            ushort id = (ushort)i;
            if (_itemLookupByID.ContainsKey(id)) {
                Debug.LogError($"ItemDatabase conflict: ID {id} (index {i}) is already assigned to '{_itemLookupByID[id].name}'. Duplicate ItemData '{item.name}' or internal error?");
                continue; // Skip duplicate ID assignment
            }
            if (_idLookupByItem.ContainsKey(item)) {
                Debug.LogError($"ItemDatabase conflict: ItemData '{item.name}' is already in the database with ID {_idLookupByItem[item]}. Ensure ItemDatas are unique in the list.");
                continue; // Skip duplicate item assignment
            }
            _itemLookupByID.Add(id, item);
            _idLookupByItem.Add(item, id);
        }
        Debug.Log($"ItemDatabase Initialized with {_itemLookupByID.Count} items.");
    }

    public ItemData GetItemByID(ushort id) {
        if (id == InvalidID || !_itemLookupByID.TryGetValue(id, out ItemData item)) {
            if (id != InvalidID) Debug.LogWarning($"Item ID {id} not found in ItemDatabase.");
            return null;
        }
        return item;
    }

    public ushort GetIDByItem(ItemData item) {
        if (item == null || !_idLookupByItem.TryGetValue(item, out ushort id)) {
            if (item != null) Debug.LogWarning($"ItemData '{item.name}' not found in ItemDatabase. Make sure it's added to the database asset.");
            return InvalidID;
        }
        return id;
    }
    public GameObject GetPrefab(string s) => _prefabDict[s];
    public Sprite GetSprite(string s) => _spriteDict[s];
    public Material GetMaterial(string s) => _materialDict[s]; 
}
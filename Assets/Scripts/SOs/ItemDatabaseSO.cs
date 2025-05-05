using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector; // Required for FirstOrDefault and Select

[CreateAssetMenu(fileName = "ItemDatabase", menuName = "Inventory/Item Database")]
public class ItemDatabase : ScriptableObject {
    // --- Singleton Access ---
    private static ItemDatabase _instance;
    public static ItemDatabase Instance {
        get {
            if (_instance == null) {
                // Load from Resources (ensure it's in a Resources folder)
                _instance = Resources.Load<ItemDatabase>("ItemDatabase");
                if (_instance == null) {
                    Debug.LogError("ItemDatabase not found in Resources folder! Create one via Create > Inventory > Item Database and place it in Resources.");
                } else {
                    // Initialize lookup dictionaries on first access after load
                    _instance.InitializeLookups();
                }
            }
            return _instance;
        }
    }

    // --- Constants ---
    public const ushort InvalidID = ushort.MaxValue; // Reserve MaxValue for invalid/empty


    [Header("Database")]
    // Assign all your ItemData SOs here in the Inspector
    [SerializeField] private List<ItemData> allItems = new List<ItemData>();

    // --- Runtime Lookups (for performance) ---
    private Dictionary<ushort, ItemData> _itemLookupByID;
    private Dictionary<ItemData, ushort> _idLookupByItem;
    private bool _isInitialized = false;
/*    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetSingleton() {
        _instance = null;  // clear any leftover from previous Play
    }*/

    [Button("rebuildLookups")]
    public void RebuildLookups() {
        _isInitialized = false;               // clear the guard
        InitializeLookups();                  // rebuild dictionaries
    }
    // Called automatically by Instance getter
    private void InitializeLookups() {
        if (_isInitialized) return;


        _itemLookupByID = new Dictionary<ushort, ItemData>();
        _idLookupByItem = new Dictionary<ItemData, ushort>();


        for (int i = 0; i < allItems.Count; i++) {
            ItemData item = allItems[i];
            if (item == null) {
                Debug.LogWarning($"ItemDatabase found a NULL ItemData entry at index {i}. Skipping.");
                continue;
            }


            // Use index as ID (ushort)
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
        _isInitialized = true;
        Debug.Log($"ItemDatabase Initialized with {_itemLookupByID.Count} items.");
    }


    /// <summary>
    /// Gets the ItemData ScriptableObject corresponding to the given ID.
    /// </summary>
    /// <param name="id">The ID of the item.</param>
    /// <returns>The ItemData object, or null if the ID is invalid or not found.</returns>
    public ItemData GetItemByID(ushort id) {
        if (!_isInitialized) InitializeLookups(); // Ensure initialized

        if (id == InvalidID || !_itemLookupByID.TryGetValue(id, out ItemData item)) {
            // Don't log error for InvalidID, it means empty. Log if ID is otherwise unknown.
            if (id != InvalidID) Debug.LogWarning($"Item ID {id} not found in ItemDatabase.");
            return null;
        }
        return item;
    }

    /// <summary>
    /// Gets the unique ID for the given ItemData ScriptableObject.
    /// </summary>
    /// <param name="item">The ItemData object.</param>
    /// <returns>The ushort ID, or InvalidID if the item is null or not found in the database.</returns>
    public ushort GetIDByItem(ItemData item) {
        if (!_isInitialized) InitializeLookups(); // Ensure initialized

        if (item == null || !_idLookupByItem.TryGetValue(item, out ushort id)) {
            if (item != null) Debug.LogWarning($"ItemData '{item.name}' not found in ItemDatabase. Make sure it's added to the database asset.");
            return InvalidID;
        }
        return id;
    }


#if UNITY_EDITOR
    // Helper to automatically find items (Optional, but useful)
    [ContextMenu("Find All Items in Project")]
    private void FindAllItems() {
        allItems.Clear();
        string[] guids = UnityEditor.AssetDatabase.FindAssets("t:ItemData"); // Find assets of type ItemData
        foreach (string guid in guids) {
            string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
            ItemData item = UnityEditor.AssetDatabase.LoadAssetAtPath<ItemData>(path);
            if (item != null) {
                allItems.Add(item);
            }
        }
        UnityEditor.EditorUtility.SetDirty(this); // Mark asset as changed
        Debug.Log($"Found and added {allItems.Count} items to the database. Save the project.");
        _isInitialized = false; // Force reinitialization of lookups on next access
    }
#endif
}
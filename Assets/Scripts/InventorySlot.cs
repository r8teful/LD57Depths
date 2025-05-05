using UnityEngine;

[System.Serializable]
public class InventorySlot {
    // Store the Item ID, which IS serializable by FishNet easily.
    public ushort itemID; // Default is 0, we'll use InvalidID constant for empty checks.

    // Store quantity.
    public int quantity;

    // --- Property to get the actual ItemData (performs lookup) ---
    // Non-serialized: This is derived data, not sent over network.
    [System.NonSerialized]
    private ItemData _cachedItemData = null;
    public ItemData ItemData {
        get {
            // Return cached version if available and ID matches
            if (_cachedItemData != null && ItemDatabase.Instance.GetIDByItem(_cachedItemData) == this.itemID) {
                return _cachedItemData;
            }
            // Otherwise, lookup based on current ID
            _cachedItemData = ItemDatabase.Instance.GetItemByID(this.itemID);
            return _cachedItemData;
        }
    }

    public InventorySlot() : this(ItemDatabase.InvalidID, 0) { }

    // Constructor - takes ID
    public InventorySlot(ushort id = ItemDatabase.InvalidID, int amount = 0) {
        itemID = id;
        quantity = amount;
        // Clear cache initially, will be populated by ItemData property getter
        _cachedItemData = null;
    }

    // Check against InvalidID
    public bool IsEmpty() => itemID == ItemDatabase.InvalidID || quantity <= 0;


    // Needs to lookup ItemData to get max stack size
    public bool CanAddToStack(int amount = 1) {
        if (IsEmpty()) return false; // Cannot add to empty slot stack

        ItemData data = this.ItemData; // Use property getter (performs lookup)
        if (data == null) {
            Debug.LogError($"Cannot check stack size for invalid ItemID: {itemID}");
            return false;
        }
        return quantity + amount <= data.maxStackSize;
    }


    public void Clear() {
        itemID = ItemDatabase.InvalidID;
        quantity = 0;
        _cachedItemData = null; // Clear cache
    }


    public void AddQuantity(int amount) {
        quantity += amount;
        if (quantity <= 0) {
            Clear(); // Clear completely if quantity drops to 0 or less
        }
    }

    public void RemoveQuantity(int amount) {
        AddQuantity(-amount);
    }

    // Optional: Direct setter for server updates - ensure ID and quantity are valid
    public void SetSlot(ushort id, int quant) {
        itemID = id;
        quantity = quant;
        _cachedItemData = null; // Clear cache, let property re-lookup

        if (itemID == ItemDatabase.InvalidID || quantity <= 0) {
            // Ensure consistency if set to invalid state
            Clear();
        }
    }
}
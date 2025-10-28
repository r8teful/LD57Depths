[System.Serializable]
public class InventorySlot {
    // Store the Item ID, which IS serializable by FishNet easily.
    public ushort itemID; // Default is 0, we'll use InvalidID constant for empty checks.

    // Store quantity.
    public int quantity;

    // Has the player gotten this item before?
    public bool discovered;

    // --- Property to get the actual ItemData (performs lookup) ---
    // Non-serialized: This is derived data, not sent over network.
    [System.NonSerialized]
    private ItemData _cachedItemData = null;
    public ItemData ItemData {
        get {
            // Return cached version if available and ID matches
            if (_cachedItemData != null && App.ResourceSystem.GetIDByItem(_cachedItemData) == this.itemID) {
                return _cachedItemData;
            }
            // Otherwise, lookup based on current ID
            _cachedItemData = App.ResourceSystem.GetItemByID(this.itemID);
            return _cachedItemData;
        }
    }

    public InventorySlot() : this(ResourceSystem.InvalidID, 0) { }

    // Constructor - takes ID
    public InventorySlot(ushort id = ResourceSystem.InvalidID, int amount = 0, bool discovered = false) {
        itemID = id;
        quantity = amount;
        // Clear cache initially, will be populated by ItemData property getter
        _cachedItemData = null;
        this.discovered = discovered;
    }

    // Check against InvalidID
    public bool IsEmpty() => itemID == ResourceSystem.InvalidID || quantity <= 0;

    public void Clear() {
        itemID = ResourceSystem.InvalidID;
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
}
[System.Serializable]
public class InventorySlot {
    public ushort ItemID => _cachedItemData != null ? _cachedItemData.ID : ResourceSystem.InvalidID;

    // Store quantity.
    public int quantity;

    // Has the player gotten this item before?
    public bool discovered;

    public ItemData ItemData => _cachedItemData;    
   
    private ItemData _cachedItemData;

    public InventorySlot(ushort id, int amount = 0, bool discovered = false) {
        quantity = amount;
        _cachedItemData = App.ResourceSystem.GetItemByID(id);
        this.discovered = discovered;
    }
    public bool IsEmpty() => _cachedItemData == null || ItemID == ResourceSystem.InvalidID || quantity <= 0;
    public void AddQuantity(int amount) {
        quantity += amount;
    }

    public void RemoveQuantity(int amount) {
        AddQuantity(-amount);
    }
}
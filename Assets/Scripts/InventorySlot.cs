[System.Serializable]
public class InventorySlot {
    public ushort ItemID => _cachedItemData != null ? _cachedItemData.ID : ResourceSystem.InvalidID;

    // Store quantity.
    public int quantity;

    public ItemData ItemData => _cachedItemData;    
   
    private ItemData _cachedItemData;

    public InventorySlot(ushort id, int amount = 0) {
        quantity = amount;
        _cachedItemData = App.ResourceSystem.GetItemByID(id);
    }
    public bool IsEmpty() => _cachedItemData == null || ItemID == ResourceSystem.InvalidID || quantity <= 0;
    public void AddQuantity(int amount) {
        quantity += amount;
    }

    public void RemoveQuantity(int amount) {
        AddQuantity(-amount);
    }
}
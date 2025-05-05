[System.Serializable]
public class InventorySlot {
    public ItemData itemData; 
    public int quantity;      

    // Constructor
    public InventorySlot(ItemData data = null, int amount = 0) {
        itemData = data;
        quantity = amount;
    }

    public bool IsEmpty() => itemData == null || quantity <= 0;

    public bool CanAddToStack(int amount = 1) {
        return !IsEmpty() && quantity + amount <= itemData.maxStackSize;
    }

    public void Clear() {
        itemData = null;
        quantity = 0;
    }

    // Optional: Helper to directly modify quantity, ensures it doesn't go below 0
    public void AddQuantity(int amount) {
        quantity += amount;
        if (quantity < 0) quantity = 0; // Safety check
        if (quantity == 0) itemData = null; // If quantity reaches zero, clear the item data too
    }

    // Optional: Helper to remove quantity
    public void RemoveQuantity(int amount) {
        AddQuantity(-amount);
    }
}
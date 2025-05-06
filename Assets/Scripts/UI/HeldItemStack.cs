// HeldItemStack.cs
[System.Serializable] // If you ever wanted to serialize this (not needed for current plan)
public class HeldItemStack {
    public ushort itemID = ResourceSystem.InvalidID;
    public int quantity = 0;
    public int originalSourceSlotIndex = -1; // Where it came from (player inventory)
    public bool isFromContainer = false;      // Was it picked from the container?
    public int originalContainerSlotIndex = -1; // If from container, its original slot

    public bool IsEmpty() => itemID == ResourceSystem.InvalidID || quantity <= 0;

    public void SetItem(ushort id, int quant, int sourcePlayerSlot = -1, bool fromContainer = false, int sourceContainerSlot = -1) {
        itemID = id;
        quantity = quant;
        originalSourceSlotIndex = sourcePlayerSlot;
        isFromContainer = fromContainer;
        originalContainerSlotIndex = sourceContainerSlot;
    }

    public void Clear() {
        itemID = ResourceSystem.InvalidID;
        quantity = 0;
        originalSourceSlotIndex = -1;
        isFromContainer = false;
        originalContainerSlotIndex = -1;
    }

    public ItemData GetItemData() {
        if (IsEmpty()) return null;
        return App.ResourceSystem.GetItemByID(itemID);
    }
}
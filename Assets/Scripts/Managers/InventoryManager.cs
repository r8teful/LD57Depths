// InventoryManager.cs
using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using UnityEngine;

public class InventoryManager {
    // List to hold the actual slot data
    [ShowInInspector]
    public List<InventorySlot> Slots { get; private set; }
    // --- Events ---
    // Event invoked when any slot in the inventory changes
    public event Action<int> OnSlotChanged; // Sends the index of the changed slot

    public InventoryManager(List<InventorySlot> initialSlots) {
        Slots = initialSlots ?? new List<InventorySlot>();
    }

    public InventoryManager(List<ItemData> itemDatas) {
        Slots = new List<InventorySlot>();
        foreach (ItemData itemData in itemDatas) {
            Slots.Add(new(itemData.ID, 0));
        }
    }


    /// <summary>
    /// Attempts to add an item to the inventory. Handles stacking.
    /// If a specific slot is provided, attempts to add *only* to that slot.
    /// </summary>
    /// <param name="itemIDToAdd">The ItemData of the item to add.</param>
    /// <param name="quantityToAdd">How many to add.</param>
    /// <param name="slot">The specific slot index to add to. If -1, finds the first available/stackable slot.</param>
    /// <returns>True if the entire quantity was added successfully, false otherwise (e.g., inventory full or specific slot unsuitable).</returns>
    public bool AddItem(ushort itemIDToAdd, int quantityToAdd = 1) {
        if (itemIDToAdd == ResourceSystem.InvalidID || quantityToAdd <= 0) {
            Debug.LogWarning("Attempted to add invalid item or quantity.");
            return false;
        }
        ItemData itemDataToAdd = App.ResourceSystem.GetItemByID(itemIDToAdd); // Renamed for clarity
        if (itemIDToAdd == ResourceSystem.InvalidID || itemDataToAdd == null) // Ensure item exists in system
        {
            Debug.LogWarning($"Attempted to add item with ID {itemIDToAdd} which was not found in ResourceSystem.");
            return false;
        }

        for (int i = 0; i < Slots.Count; i++) {
            if (Slots[i].itemID == itemIDToAdd) // Same item?
            {
                Slots[i].AddQuantity(quantityToAdd);
                OnSlotChanged?.Invoke(i);
            }
     
        }
        return true;
    }



    /// <summary>
    /// Removes a specific quantity of an item from a given slot index.
    /// </summary>
    /// <param name="slotIndex">The index of the slot to remove from.</param>
    /// <param name="quantityToRemove">How many to remove.</param>
    public void RemoveItem(int slotIndex, int quantityToRemove = 1, bool sendTargetRpcUpdate = true) {
        if (!IsValidIndex(slotIndex) || Slots[slotIndex].IsEmpty() || quantityToRemove <= 0) {
            return; // Invalid operation
        }
        if (sendTargetRpcUpdate) {
            //PlayerInventorySyncer.CmdUpdateSlotAfterLocalRemove(...); // Not how it works currently
        }
        Slots[slotIndex].RemoveQuantity(quantityToRemove); // Let InventorySlot handle clamping and clearing
        Debug.Log($"Removed: {quantityToRemove} from slot {slotIndex}");
        OnSlotChanged?.Invoke(slotIndex); // Notify UI
    }
    public bool RemoveItem(ushort itemID, int quantityToRemove) {
        if (itemID == ResourceSystem.InvalidID || quantityToRemove <= 0)
            return false;
        int S = Slots.Count - 1;
        for (int i = S; i >= 0; --i) { //Iterate backwards to make removing easier during loop
            if (Slots[i].itemID == itemID) {
                if (Slots[i].quantity > quantityToRemove) {
                    Slots[i].quantity -= quantityToRemove;
                    OnSlotChanged?.Invoke(i); // Notify UI
                    return true;
                } else {
                    quantityToRemove -= Slots[i].quantity;
                    Slots[i].itemID = ResourceSystem.InvalidID;
                    Slots[i].quantity = 0;
                    if (quantityToRemove == 0) {
                        OnSlotChanged?.Invoke(i); // Notify UI
                        return true;
                    }
                }
            }
        }
        return quantityToRemove == 0; // True if all requested items were removed
    }
    public bool ConsumeItems(List<ItemQuantity> itemsToConsume) {
        // First, check if all items can be consumed
        foreach (var req in itemsToConsume) {
            if (!HasItemCount(req.item.ID, req.quantity)) {
                return false; // Not enough of one item
            }
        }
        // All checks passed, now consume them
        foreach (var req in itemsToConsume) {
            RemoveItem(req.item.ID, req.quantity); // We already know this will succeed
        }
        return true;
    }
    /// <summary>
    /// Swaps the contents of two slots.
    /// </summary>
    public void SwapSlots(int indexA, int indexB) {
        if (!IsValidIndex(indexA) || !IsValidIndex(indexB) || indexA == indexB) {
            return; // Invalid swap
        }

        // Simple swap
        InventorySlot temp = Slots[indexA];
        Slots[indexA] = Slots[indexB];
        Slots[indexB] = temp;

        // Notify that both slots changed
        OnSlotChanged?.Invoke(indexA);
        OnSlotChanged?.Invoke(indexB);
    }


    /// <summary>
    /// Gets the InventorySlot data at a specific index. Use carefully - modifying directly bypasses events.
    /// </summary>
    public InventorySlot GetSlot(int index) {
        if (!IsValidIndex(index)) {
            Debug.LogError($"Invalid slot index requested: {index}");
            return null;
        }
        return Slots[index];
    }

    /// <summary>
    /// Checks if a slot index is within the valid range.
    /// </summary>
    public bool IsValidIndex(int index) {
        return index >= 0 && index < Slots.Count;
    }

    public void TriggerOnSlotChanged(int slotIndex) { OnSlotChanged?.Invoke(slotIndex); }

    private List<int> FindSlotsContaining(ushort itemID) {
        List<int> indices = new List<int>();
        for (int i = 0; i < Slots.Count; ++i) {
            if (!Slots[i].IsEmpty() && Slots[i].itemID == itemID) {
                indices.Add(i);
            }
        }
        return indices;
    }
    public int GetItemCount(ushort itemID) {
        var slotsToCheck = FindSlotsContaining(itemID);
        int totalAmount = 0;
        foreach (var slot in slotsToCheck) {
            totalAmount += Slots[slot].quantity;
        }
        return totalAmount;
    }
    public bool HasItemCount(ushort itemID, int quantity) {
        return GetItemCount(itemID) >= quantity;
    }
    // --- Helper for Debugging ---
    [Button("Add Test Item")]
    void AddTestItemDebug() {
        // Find an ItemData asset in your project (replace "YourTestItemName"!)
        //ItemData testItem = Resources.Load<ItemData>("testitem"); // Assumes item is in a Resources folder
            Debug.Log("Adding item");
            AddItem(0, 1);
        
    }
    [Button("Add Test Item 2")]
    void AddTestItemDebug2() {
  
        Debug.Log("Adding item");
        AddItem(1, 1);
        
    }
}
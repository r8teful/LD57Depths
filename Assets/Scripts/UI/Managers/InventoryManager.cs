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
            if (Slots[i].ItemID == itemIDToAdd) // Same item?
            {
                Slots[i].AddQuantity(quantityToAdd);
                OnSlotChanged?.Invoke(i);
            }
     
        }
        return true;
    }

    public bool RemoveItem(ushort itemID, int quantityToRemove) {
        if (itemID == ResourceSystem.InvalidID || quantityToRemove <= 0)
            return false;
        int S = Slots.Count - 1;
        for (int i = S; i >= 0; --i) { //Iterate backwards to make removing easier during loop
            if (Slots[i].ItemID == itemID) {
                if (Slots[i].quantity >= quantityToRemove) {
                    Slots[i].quantity -= quantityToRemove;
                    OnSlotChanged?.Invoke(i); // Notify UI
                    return true;
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
 
    private List<int> FindSlotsContaining(ushort itemID) {
        List<int> indices = new List<int>();
        for (int i = 0; i < Slots.Count; ++i) {
            if (!Slots[i].IsEmpty() && Slots[i].ItemID == itemID) {
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

    internal void RemoveAllItems() {
        foreach (var item in Slots) {
            item.quantity = 0;
        }
    }
}
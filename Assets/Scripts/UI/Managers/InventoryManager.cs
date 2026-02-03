using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class InventoryManager {
    // List to hold the actual slot data
    [ShowInInspector]
    public Dictionary<ushort, InventorySlot> Slots { get; private set; }

    public event Action<ushort,int> OnSlotChanged; // Sends item ID
    public event Action<ushort, int> OnSlotNew;
    public event Action<ushort> OnSlotRemoved;

    public InventoryManager() {
        Slots = new Dictionary<ushort, InventorySlot>();
    }

    public bool AddItem(ushort itemIDToAdd, int quantityToAdd = 1) {
        if (itemIDToAdd == ResourceSystem.InvalidID || quantityToAdd <= 0) {
            Debug.LogWarning("Attempted to add invalid item or quantity.");
            return false;
        }

        ItemData itemDataToAdd = App.ResourceSystem.GetItemByID(itemIDToAdd);
        if (itemDataToAdd == null) {
            Debug.LogWarning($"Attempted to add item with ID {itemIDToAdd} which was not found in ResourceSystem.");
            return false;
        }

        if (Slots.TryGetValue(itemIDToAdd, out var slot)) {
            slot.AddQuantity(quantityToAdd);
            OnSlotChanged?.Invoke(itemIDToAdd,quantityToAdd);
        } else {
            Slots.Add(itemIDToAdd, new InventorySlot(itemIDToAdd, quantityToAdd));
            OnSlotNew?.Invoke(itemIDToAdd, quantityToAdd);
        }
        return true;
    }

    public bool RemoveItem(ushort itemID, int quantityToRemove) {
        if (itemID == ResourceSystem.InvalidID || quantityToRemove <= 0)
            return false;

        if (Slots.TryGetValue(itemID, out var slot)) {
            slot.RemoveQuantity(quantityToRemove);

            if (slot.IsEmpty()) {
                RemoveSlot(itemID);
            } else {
                OnSlotChanged?.Invoke(itemID, -quantityToRemove);
            }
            return true;
        }

        // Item not found in inventory
        return false;
    }

    private void RemoveSlot(ushort itemID) {
        if (Slots.Remove(itemID)) {
            OnSlotRemoved?.Invoke(itemID);
        }
    }

    public int GetItemCount(ushort itemID) {
        if (!HasItem(itemID)) return 0;
        return Slots[itemID].quantity;
    }

    public bool HasItemCount(ushort itemID, int quantity) {
        return GetItemCount(itemID) >= quantity;
    }

    internal void RemoveAllItems() {
        var itemsToRemove = Slots.Keys.ToList();

        foreach (var itemID in itemsToRemove) {
            RemoveSlot(itemID);
        }
    }

    internal bool HasItem(ushort itemID) {
        return Slots.ContainsKey(itemID);
    }

    internal ItemData GetItem(ushort item) {
        if (Slots.TryGetValue(item, out var slot)) {
            return slot.ItemData;
        }
        return null;
    }

    public bool RemoveItems(List<ItemQuantity> itemsToConsume) {
        // First, check if all items can be consumed
        foreach (var req in itemsToConsume) {
            if (!HasItemCount(req.item.ID, req.quantity)) {
                return false; // Not enough of one item
            }
        }
        // All checks passed, now consume them
        foreach (var req in itemsToConsume) {
            RemoveItem(req.item.ID, req.quantity);
        }
        return true;
    }
}
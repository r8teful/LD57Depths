// InventoryManager.cs
using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using UnityEngine;

public class InventoryManager : Singleton<InventoryManager> {
    protected override void Awake() {
        base.Awake();
        InitializeInventory();
    }
    // --- End Singleton ---

    [Header("Inventory Settings")]
    [SerializeField] private int inventorySize = 20; // How many slots
    // --- Runtime Data ---
    // List to hold the actual slot data
    private List<InventorySlot> slots = new List<InventorySlot>();

    // --- Events ---
    // Event invoked when any slot in the inventory changes
    public event Action<int> OnSlotChanged; // Sends the index of the changed slot
    
    // --- Properties ---
    public int InventorySize => inventorySize;

    private void InitializeInventory() {
        slots = new List<InventorySlot>(inventorySize);
        for (int i = 0; i < inventorySize; i++) {
            slots.Add(new InventorySlot()); // Add empty slots
        }
        Debug.Log($"Inventory Initialized with {inventorySize} slots.");
    }

    /// <summary>
    /// Attempts to add an item to the inventory. Handles stacking.
    /// </summary>
    /// <param name="itemToAdd">The ItemData of the item to add.</param>
    /// <param name="quantityToAdd">How many to add.</param>
    /// <returns>True if the entire quantity was added successfully, false otherwise (e.g., inventory full).</returns>
    public bool AddItem(ItemData itemToAdd, int quantityToAdd = 1) {
        if (itemToAdd == null || quantityToAdd <= 0) {
            Debug.LogWarning("Attempted to add invalid item or quantity.");
            return false; // Indicate failure (nothing added)
        }

        int remainingQuantity = quantityToAdd;

        // 1. Try to stack with existing items
        if (itemToAdd.maxStackSize > 1) // Only stack if stackable
        {
            for (int i = 0; i < slots.Count; i++) {
                if (!slots[i].IsEmpty() && slots[i].itemData == itemToAdd) // Same item?
                {
                    int canAdd = itemToAdd.maxStackSize - slots[i].quantity;
                    if (canAdd > 0) {
                        int amountToAdd = Mathf.Min(remainingQuantity, canAdd);
                        slots[i].AddQuantity(amountToAdd);
                        remainingQuantity -= amountToAdd;
                        OnSlotChanged?.Invoke(i); // Notify UI

                        if (remainingQuantity <= 0) return true; // All added
                    }
                }
            }
        }

        // 2. If items remain, try to place in empty slots
        for (int i = 0; i < slots.Count; i++) {
            if (slots[i].IsEmpty()) {
                int amountToAdd = Mathf.Min(remainingQuantity, itemToAdd.maxStackSize);
                slots[i].itemData = itemToAdd;
                slots[i].quantity = amountToAdd; // Use direct assignment here
                remainingQuantity -= amountToAdd;
                OnSlotChanged?.Invoke(i); // Notify UI

                if (remainingQuantity <= 0) return true; // All added
            }
        }

        // If we reach here, inventory is full for the remaining quantity
        if (remainingQuantity > 0) {
            Debug.LogWarning($"Inventory full. Could not add {remainingQuantity} of {itemToAdd.itemName}");
        }
        return remainingQuantity == 0; // True if all was added, false if some remained
    }


    /// <summary>
    /// Removes a specific quantity of an item from a given slot index.
    /// </summary>
    /// <param name="slotIndex">The index of the slot to remove from.</param>
    /// <param name="quantityToRemove">How many to remove.</param>
    public void RemoveItem(int slotIndex, int quantityToRemove = 1) {
        if (!IsValidIndex(slotIndex) || slots[slotIndex].IsEmpty() || quantityToRemove <= 0) {
            return; // Invalid operation
        }

        slots[slotIndex].RemoveQuantity(quantityToRemove); // Let InventorySlot handle clamping and clearing

        OnSlotChanged?.Invoke(slotIndex); // Notify UI
    }

    /// <summary>
    /// Swaps the contents of two slots.
    /// </summary>
    public void SwapSlots(int indexA, int indexB) {
        if (!IsValidIndex(indexA) || !IsValidIndex(indexB) || indexA == indexB) {
            return; // Invalid swap
        }

        // Simple swap
        InventorySlot temp = slots[indexA];
        slots[indexA] = slots[indexB];
        slots[indexB] = temp;

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
        return slots[index];
    }

    /// <summary>
    /// Checks if a slot index is within the valid range.
    /// </summary>
    public bool IsValidIndex(int index) {
        return index >= 0 && index < slots.Count;
    }

    // --- Helper for Debugging ---
    [Button("Add Test Item")]
    void AddTestItemDebug() {
        // Find an ItemData asset in your project (replace "YourTestItemName"!)
        ItemData testItem = Resources.Load<ItemData>("testitem"); // Assumes item is in a Resources folder
        if (testItem != null) {
            Debug.Log("Adding item");
            AddItem(testItem, 1);
        } else {
            Debug.LogError("Test item not found. Create an ItemData named 'YourTestItemName' in a Resources folder.");
        }
    }
    [Button("Add Test Item 2")]
    void AddTestItemDebug2() {
        // Find an ItemData asset in your project (replace "YourTestItemName"!)
        ItemData testItem = Resources.Load<ItemData>("testitem2"); // Assumes item is in a Resources folder
        if (testItem != null) {
            Debug.Log("Adding item");
            AddItem(testItem, 1);
        } else {
            Debug.LogError("Test item not found. Create an ItemData named 'YourTestItemName' in a Resources folder.");
        }
    }
}
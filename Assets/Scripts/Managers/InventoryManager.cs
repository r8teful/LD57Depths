// InventoryManager.cs
using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

public class InventoryManager : MonoBehaviour {
    public void Awake() {
        InitializeInventory();
    }
    [Header("Inventory Settings")]
    [SerializeField] private int inventorySize = 20; // How many slots
    // --- Runtime Data ---
    // List to hold the actual slot data
    [ShowInInspector]
    private List<InventorySlot> slots = new List<InventorySlot>();
    // --- Events ---
    // Event invoked when any slot in the inventory changes
    public event Action<int> OnSlotChanged; // Sends the index of the changed slot

    // --- Properties ---
    public int InventorySize => inventorySize;

    private void InitializeInventory() {
        slots.Clear();
        slots = new List<InventorySlot>(inventorySize);
        for (int i = 0; i < inventorySize; i++) {
            slots.Add(new InventorySlot()); // Add empty slots
        }
        Debug.Log($"Inventory Initialized with {inventorySize} slots.");
    }

    /// <summary>
    /// Attempts to add an item to the inventory. Handles stacking.
    /// If a specific slot is provided, attempts to add *only* to that slot.
    /// </summary>
    /// <param name="itemIDToAdd">The ItemData of the item to add.</param>
    /// <param name="quantityToAdd">How many to add.</param>
    /// <param name="slot">The specific slot index to add to. If -1, finds the first available/stackable slot.</param>
    /// <returns>True if the entire quantity was added successfully, false otherwise (e.g., inventory full or specific slot unsuitable).</returns>
    public bool AddItem(ushort itemIDToAdd, int quantityToAdd = 1, int slot = -1) {
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

        int remainingQuantity = quantityToAdd;

        // --- Specific Slot Logic ---
        if (slot != -1) {
            if (!IsValidIndex(slot)) {
                Debug.LogWarning($"AddItem: Invalid target slot index {slot}.");
                return false;
            }

            InventorySlot targetSlot = slots[slot];

            // Case 1: Target slot is empty
            if (targetSlot.IsEmpty()) {
                if (quantityToAdd <= itemDataToAdd.maxStackSize) {
                    targetSlot.itemID = itemIDToAdd;
                    targetSlot.quantity = quantityToAdd; // Direct assignment
                    OnSlotChanged?.Invoke(slot);
                    return true; // All added to the specified empty slot
                } else {
                    // Cannot fit the entire quantity even in an empty slot (e.g. trying to add 2 of a non-stackable item)
                    // Or trying to add more than maxStackSize to a single slot.
                    // For strict slot addition, this is a failure.
                    Debug.LogWarning($"AddItem: Cannot add {quantityToAdd} of {itemDataToAdd.itemName} (max stack: {itemDataToAdd.maxStackSize}) to empty slot {slot}. Quantity exceeds max stack size for a single placement.");
                    return false;
                }
            }
            // Case 2: Target slot has the same item and it's stackable
            else if (targetSlot.itemID == itemIDToAdd && itemDataToAdd.maxStackSize > 1) {
                int canAdd = itemDataToAdd.maxStackSize - targetSlot.quantity;
                if (canAdd >= quantityToAdd) {
                    targetSlot.AddQuantity(quantityToAdd);
                    OnSlotChanged?.Invoke(slot);
                    return true; // All added by stacking in the specified slot
                } else {
                    // Cannot fit the entire requested quantity by stacking in this specific slot.
                    Debug.LogWarning($"AddItem: Slot {slot} can only take {canAdd} more of {itemDataToAdd.itemName}, but {quantityToAdd} were requested.");
                    return false;
                }
            }
            // Case 3: Target slot is occupied by a different item, or same item but not stackable/full
            else {
                Debug.LogWarning($"AddItem: Cannot add item to specified slot {slot}. It's occupied by a different item, or the item is not stackable and slot is occupied, or it's already full.");
                return false; // Specified slot is not suitable
            }
        }
        // --- General Slot Logic (slot == -1) ---
        else {
            // 1. Try to stack with existing items
            if (itemDataToAdd.maxStackSize > 1) // Only stack if stackable
            {
                for (int i = 0; i < slots.Count; i++) {
                    if (!slots[i].IsEmpty() && slots[i].itemID == itemIDToAdd) // Same item?
                    {
                        int canAdd = itemDataToAdd.maxStackSize - slots[i].quantity;
                        if (canAdd > 0) {
                            int amountToAddThisIteration = Mathf.Min(remainingQuantity, canAdd);
                            slots[i].AddQuantity(amountToAddThisIteration);
                            remainingQuantity -= amountToAddThisIteration;
                            OnSlotChanged?.Invoke(i);

                            if (remainingQuantity <= 0)
                                return true; // All added
                        }
                    }
                }
            }

            // 2. If items remain, try to place in empty slots
            if (remainingQuantity > 0) // Check if there's anything left to add
            {
                for (int i = 0; i < slots.Count; i++) {
                    if (slots[i].IsEmpty()) {
                        int amountToAddThisIteration = Mathf.Min(remainingQuantity, itemDataToAdd.maxStackSize);
                        slots[i].itemID = itemIDToAdd;
                        slots[i].quantity = amountToAddThisIteration; // Use direct assignment here
                        remainingQuantity -= amountToAddThisIteration;
                        OnSlotChanged?.Invoke(i);

                        if (remainingQuantity <= 0)
                            return true; // All added
                    }
                }
            }

            // If we reach here, inventory is full for the remaining quantity
            if (remainingQuantity > 0) {
                Debug.LogWarning($"Inventory full. Could not add {remainingQuantity} of {itemDataToAdd.itemName}");
            }
            return remainingQuantity == 0; // True if all was added, false if some remained
        }
    }


    /// <summary>
    /// Removes a specific quantity of an item from a given slot index.
    /// </summary>
    /// <param name="slotIndex">The index of the slot to remove from.</param>
    /// <param name="quantityToRemove">How many to remove.</param>
    public void RemoveItem(int slotIndex, int quantityToRemove = 1, bool sendTargetRpcUpdate = true) {
        if (!IsValidIndex(slotIndex) || slots[slotIndex].IsEmpty() || quantityToRemove <= 0) {
            return; // Invalid operation
        }
        if (sendTargetRpcUpdate) {
            //PlayerInventorySyncer.CmdUpdateSlotAfterLocalRemove(...); // Not how it works currently
        }
        slots[slotIndex].RemoveQuantity(quantityToRemove); // Let InventorySlot handle clamping and clearing
        Debug.Log($"Server removed: {quantityToRemove} from slot {slotIndex}");
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

    public void TriggerOnSlotChanged(int slotIndex) { OnSlotChanged?.Invoke(slotIndex); }

    public int CalculateHowMuchCanBeAdded(ushort idToAdd, int quantityToAdd) {
        if (idToAdd == ResourceSystem.InvalidID || quantityToAdd <= 0) return 0;
        int canAddTotal = 0;
        int remainingQuantity = quantityToAdd;

        var itemToAdd = App.ResourceSystem.GetItemByID(idToAdd);
        // 1. Check existing stacks
        if (itemToAdd.maxStackSize > 1) {
            for (int i = 0; i < slots.Count; i++) {
                if (!slots[i].IsEmpty() && slots[i].itemID == idToAdd) {
                    int stackSpace = itemToAdd.maxStackSize - slots[i].quantity;
                    int amountForThisStack = Mathf.Min(remainingQuantity, stackSpace);
                    if (amountForThisStack > 0) {
                        canAddTotal += amountForThisStack;
                        remainingQuantity -= amountForThisStack;
                        if (remainingQuantity <= 0) return quantityToAdd; // Can fit all
                    }
                }
            }
        }

        // 2. Check empty slots
        for (int i = 0; i < slots.Count; i++) {
            if (slots[i].IsEmpty()) {
                int amountForThisSlot = Mathf.Min(remainingQuantity, itemToAdd.maxStackSize);
                canAddTotal += amountForThisSlot;
                remainingQuantity -= amountForThisSlot;
                if (remainingQuantity <= 0) return quantityToAdd; // Can fit all
            }
        }
        // Return how much could actually fit
        return quantityToAdd - remainingQuantity; // = canAddTotal
    }

    public List<int> FindSlotsContaining(ushort itemID) {
        List<int> indices = new List<int>();
        for (int i = 0; i < slots.Count; ++i) {
            if (!slots[i].IsEmpty() && slots[i].itemID == itemID) {
                indices.Add(i);
            }
        }
        return indices;
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

    public List<int> FindSlotsContainingID(ushort itemID) {
        List<int> indices = new List<int>();
        for (int i = 0; i < slots.Count; ++i) {
            if (slots[i].itemID == itemID && !slots[i].IsEmpty()) { // Check ID and ensure not accidentally cleared
                indices.Add(i);
            }
        }
        return indices;
    }
}
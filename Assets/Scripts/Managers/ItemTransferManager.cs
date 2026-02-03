using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Transfers the items from player inventory to sub inventory
public class ItemTransferManager : MonoBehaviour, IPlayerModule {

    private InventoryManager inventorySub;
    private InventoryManager inventoryPlayer;
    [SerializeField] private float timePerCategory = 1.0f;
    [SerializeField] private int transferSteps = 10;
    [SerializeField] private float delayBetweenCategories = 0.5f;

    private Coroutine currentTransferRoutine;

    public int InitializationOrder => 24; // no clue 

    public void InitializeOnOwner(PlayerManager playerParent) {
        inventoryPlayer = playerParent.GetInventory();
        inventorySub = SubmarineManager.Instance.SubInventory;
        PlayerLayerController.OnPlayerVisibilityChanged += PlayerLayerChange;
    }

    private void PlayerLayerChange(VisibilityLayerType layer) {
        if(layer == VisibilityLayerType.Interior) {
            TriggerTransferSequence();
        } else {
            TriggerInstantTransfer();
        }
    }

    public void TriggerTransferSequence() {
        if (currentTransferRoutine != null) return;
        currentTransferRoutine = StartCoroutine(TransferRoutine());
    }

    public void TriggerInstantTransfer() {
        if (currentTransferRoutine != null) {
            StopCoroutine(currentTransferRoutine);
            currentTransferRoutine = null;
        }
        List<ushort> itemsToMove = new List<ushort>(inventoryPlayer.Slots.Keys);

        foreach (ushort itemID in itemsToMove) {
            // Verify the item still exists in the dictionary
            if (inventoryPlayer.Slots.TryGetValue(itemID, out InventorySlot slot)) {
                int quantity = slot.quantity;
                if (quantity > 0) {
                    // Move the total remaining amount instantly
                    if (inventoryPlayer.RemoveItem(itemID, quantity)) {
                        inventorySub.AddItem(itemID, quantity);
                    }
                }
            }
        }

        Debug.Log("Instant transfer complete.");
    }

    private IEnumerator TransferRoutine() {
        // Snapshot the list of ItemIDs to transfer so we have a queue
        List<ushort> itemsToTransfer = new List<ushort>(inventoryPlayer.Slots.Keys);

        foreach (ushort itemID in itemsToTransfer) {
            if (!inventoryPlayer.Slots.ContainsKey(itemID)) continue;

            InventorySlot slot = inventoryPlayer.Slots[itemID];
            int totalQuantityToMove = slot.quantity;

            // If empty, skip
            if (totalQuantityToMove <= 0) continue;

            // --- Calculation Logic ---
            // Calculate delay per step (e.g., 1.0s / 10 steps = 0.1s per step)
            float stepDelay = timePerCategory / (float)transferSteps;

            // Calculate base amount per step (e.g., 53 / 10 = 5)
            int amountPerStep = totalQuantityToMove / transferSteps;

            // If the stack is smaller than the step count (e.g. 4 items, 10 steps),
            // we ensure we move at least 1 item per step.
            if (amountPerStep < 1) amountPerStep = 1;

            // Perform the transfer in steps
            for (int i = 0; i < transferSteps; i++) {
                if (!inventoryPlayer.Slots.ContainsKey(itemID)) break;
                int currentRemaining = inventoryPlayer.Slots[itemID].quantity;

                if (currentRemaining <= 0) break;

                int amountToTransferThisStep = amountPerStep;

                if (i == transferSteps - 1 || amountToTransferThisStep > currentRemaining) {
                    amountToTransferThisStep = currentRemaining;
                }

                // Execute the move
                if (inventoryPlayer.RemoveItem(itemID, amountToTransferThisStep)) {
                    inventorySub.AddItem(itemID, amountToTransferThisStep);
                }

                // Wait for the visual tick
                yield return new WaitForSeconds(stepDelay);
            }

            // --- Category Finished ---
            // Wait the desired amount of time before starting the next item category (e.g. Copper after Stone)
            yield return new WaitForSeconds(delayBetweenCategories);
        }

        currentTransferRoutine = null;
        Debug.Log("Sequential transfer complete.");
    }
}
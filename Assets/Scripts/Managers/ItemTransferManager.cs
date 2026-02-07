using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Transfers the items from player inventory to sub inventory
public class ItemTransferManager : MonoBehaviour, IPlayerModule {

    private InventoryManager inventorySub;
    private InventoryManager inventoryPlayer;
    private PlayerManager _player;
    [SerializeField] private float timePerCategory = 1.0f;
    [SerializeField] private int transferSteps = 10;
    [SerializeField] private float delayBetweenCategories = 0.5f;

    private Coroutine currentTransferRoutine;
    public static event Action OnTransferComplete;
    public int InitializationOrder => 1001; // after ui 

    public void InitializeOnOwner(PlayerManager playerParent) {
        inventoryPlayer = playerParent.GetInventory();
        _player = playerParent;
        inventorySub = SubmarineManager.Instance.SubInventory;
        PlayerLayerController.OnPlayerVisibilityChanged += PlayerLayerChange;
        _player.UiManager.UpgradeScreen.OnPanelChanged += UpgradeScreenToggle;
    }
    private void OnDestroy() {
        PlayerLayerController.OnPlayerVisibilityChanged -= PlayerLayerChange;
        _player.UiManager.UpgradeScreen.OnPanelChanged -= UpgradeScreenToggle;
        
    }
    private void UpgradeScreenToggle(bool isActive) {
        if (isActive) {
            // transfer all
            TriggerInstantTransfer();
        }
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
        OnTransferComplete?.Invoke();
        Debug.Log("Instant transfer complete.");
    }

    private IEnumerator TransferRoutine() {
        // 1. Snapshot items
        List<ushort> itemsToTransfer = new List<ushort>(inventoryPlayer.Slots.Keys);

        // 2. Count TOTAL items first
        int totalItems = 0;
        foreach (var id in itemsToTransfer) {
            if (inventoryPlayer.Slots.ContainsKey(id))
                totalItems += inventoryPlayer.Slots[id].quantity;
        }

        // 3. Calculate Speed
        float targetDuration = 1.0f; // Max time allowed
        float calculatedRate = totalItems / targetDuration;

        // "Shorter is fine": 
        // If we only have 5 items, 'calculatedRate' would be 5 per second (slow).
        // We clamp it to a minimum (e.g., 50/sec) so small transfers feel snappy.
        float minRate = 50f;
        float itemsPerSecond = Mathf.Max(calculatedRate, minRate);

        // 4. The Accumulator
        float moveCredit = 0f;

        foreach (ushort itemID in itemsToTransfer) {
            // Keep looping as long as the player still has this item
            while (inventoryPlayer.Slots.ContainsKey(itemID) &&
                   inventoryPlayer.Slots[itemID].quantity > 0) {

                // Wait until we have enough "Credit" (Time) to move an item
                // If FPS is high, we wait here.
                // If FPS is low (lag), this loop is skipped, processing multiple items instantly.
                while (moveCredit < 1.0f) {
                    yield return null;
                    moveCredit += Time.deltaTime * itemsPerSecond;
                }

                // Move the item
                if (inventoryPlayer.RemoveItem(itemID, 1)) {
                    inventorySub.AddItem(itemID, 1);
                    moveCredit -= 1.0f; // "Spend" the time credit
                } else {
                    break; // Failsafe
                }
            }
            yield return new WaitForSeconds(delayBetweenCategories);
        }

        currentTransferRoutine = null;
        OnTransferComplete?.Invoke();
        Debug.Log("Transfer Complete");
    }
}
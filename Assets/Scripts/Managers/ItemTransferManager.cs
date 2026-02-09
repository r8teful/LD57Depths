using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Transfers the items from player inventory to sub inventory
public class ItemTransferManager : MonoBehaviour, IPlayerModule, IValueModifiable {

    private InventoryManager inventorySub;
    private InventoryManager inventoryPlayer;
    private PlayerManager _player;
    [SerializeField] private float delayBetweenCategories = 0.5f;
    private float itemsPerSecond;
    private const float itemPerSecondBase = 20f;

    private Coroutine transferCoroutine;
    public static event Action OnTransferCompleteAll;
    public static event Action<ushort> OnTransferCompleteItem;
    public int InitializationOrder => 900; // before ui

    public void InitializeOnOwner(PlayerManager playerParent) {
        _player = playerParent;
        inventorySub = SubmarineManager.Instance.SubInventory;
        itemsPerSecond = itemPerSecondBase;
        Register();
        PlayerLayerController.OnPlayerVisibilityChanged += PlayerLayerChange;

    }
    // after ui
    public void InitLate(PlayerManager playerParent) {
        inventoryPlayer = playerParent.GetInventory();
    }
    private void OnDestroy() {
        PlayerLayerController.OnPlayerVisibilityChanged -= PlayerLayerChange;

    }
    private void PlayerLayerChange(VisibilityLayerType layer) {
        if (layer == VisibilityLayerType.Interior) {
            TriggerTransferSequence();
        } else if (layer == VisibilityLayerType.Exterior) {
            if (transferCoroutine != null) {
                // Stop transfering
                StopCoroutine(transferCoroutine);
                transferCoroutine = null; 
            }
        }
    }
    public void TriggerTransferSequence() {
        if (transferCoroutine != null) return;
        transferCoroutine = StartCoroutine(TransferRoutine());
    }

    public void TriggerInstantTransfer() {
        if (transferCoroutine != null) {
            StopCoroutine(transferCoroutine);
            transferCoroutine = null;
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
        OnTransferCompleteAll?.Invoke();
        Debug.Log("Instant transfer complete.");
    }
    // Uses a currency system, where the currency is the deltaTime and we pay based on our transfer rate
    private IEnumerator TransferRoutine() {
        List<ushort> itemTypesToTransfer = new List<ushort>(inventoryPlayer.Slots.Keys);
        float timeAccumulator = 0f;
        foreach (ushort itemID in itemTypesToTransfer) {
            // Verify item still exists in dictionary
            while (inventoryPlayer.Slots.ContainsKey(itemID)) {
                InventorySlot slot = inventoryPlayer.Slots[itemID];
                if (slot.quantity <= 0) break; // None left of this type, move to next

                float secondsPerItem = 1.0f / itemsPerSecond;
                timeAccumulator += Time.deltaTime;
                while (timeAccumulator >= secondsPerItem) {
                    // Check availability inside the micro-loop
                    if (!inventoryPlayer.Slots.ContainsKey(itemID) ||
                        inventoryPlayer.Slots[itemID].quantity <= 0) {
                        break;
                    }
                    bool removed = inventoryPlayer.RemoveItem(itemID, 1);
                    if (!removed) continue;
                    bool added = inventorySub.AddItem(itemID, 1); 
                    timeAccumulator -= secondsPerItem;
                }

                // Wait for the next frame to accumulate more time
                yield return null;
            }
            OnTransferCompleteItem?.Invoke(itemID);
            yield return new WaitForSeconds(delayBetweenCategories);
        }
        transferCoroutine = null;
        OnTransferCompleteAll?.Invoke();
        Debug.Log("Transfer Complete.");
    }
    public void StopTransfer() {
        if (transferCoroutine != null) {
            StopCoroutine(transferCoroutine);
            transferCoroutine = null;
        }
    }

    public void ModifyValue(ValueModifier modifier) {
        itemsPerSecond = UpgradeCalculator.CalculateUpgradeChange(itemsPerSecond, modifier);
    }

    public void Register() {
        UpgradeManagerPlayer.Instance.RegisterValueModifierScript(ValueKey.ItemTransferRate, this);
    }

    public float GetValueNow(ValueKey key) 
        => key == ValueKey.ItemTransferRate? itemsPerSecond : 0;

    public float GetValueBase(ValueKey key) 
        => key == ValueKey.ItemTransferRate ? itemPerSecondBase : 0;
    
}
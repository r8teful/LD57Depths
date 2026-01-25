using FishNet.Object;
using Sirenix.OdinInspector;
using System;
using UnityEngine;

public class NetworkedPlayerInventory : NetworkBehaviour {
    // Local event that gets called when we pickup an item
    public static event Action<ushort,int> OnItemPickup;       
    [ShowInInspector]
    private InventoryManager inventoryManager;

    public InventoryManager GetInventoryManager() => inventoryManager;
    public void Initialize() {

        InitializeInventory();
    }
    public override void OnStartClient() {
        base.OnStartClient();
        if (!base.IsOwner) {
            base.enabled = false;
        }
    }

    private void InitializeInventory() {
        inventoryManager = new InventoryManager(App.ResourceSystem.GetAllItems()); 
    }
 
    public void AwardItem(ushort itemID) {
        bool added = inventoryManager.AddItem(itemID, 1); 
        if (added) {
            OnItemPickup?.Invoke(itemID, 1);
            XPEvents.TriggerGainXP(XPCalculation.
                CalculateXP(App.ResourceSystem.GetItemByID(itemID),1));
            DiscoveryManager.Instance.ServerDiscoverResource(itemID);
        } else {
            Debug.LogWarning($"Client: Could not add item {itemID} to inventory (full?).");
        }
    }
    
    internal void AddItem(ushort itemId, int quantityTransferred) {
        inventoryManager.AddItem(itemId, quantityTransferred);
    }

    internal void RemoveItem(ushort itemId, int quantityTransferred) {
        inventoryManager.RemoveItem(itemId, quantityTransferred);
    }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    public void DEBUGGIVE(int ID, int amount) {
        inventoryManager.AddItem((ushort)ID, amount);
    }
#endif
}
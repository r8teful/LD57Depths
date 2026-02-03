using Sirenix.OdinInspector;
using System;
using UnityEngine;

public class PlayerInventory : MonoBehaviour {

    public static event Action<ushort,int> OnItemPickup;       
    [ShowInInspector]
    private InventoryManager inventoryManager;

    public InventoryManager GetInventoryManager() => inventoryManager;
    public void Initialize() {
        InitializeInventory();
    }

    private void InitializeInventory() {
        // Fetch inventory data from 
        inventoryManager = new InventoryManager(); 
    }
 
    public void AwardItem(ushort itemID, int amount = 1) {
        bool added = inventoryManager.AddItem(itemID, amount);
        if (!added) return; 
        OnItemPickup?.Invoke(itemID, amount);
        RewardEvents.TriggerGainXP(XPCalculation.
            CalculateXP(App.ResourceSystem.GetItemByID(itemID),amount));
        DiscoveryManager.Instance.ServerDiscoverResource(itemID);
        
    }
    
    internal void AddItem(ushort itemId, int quantityTransferred) {
        inventoryManager.AddItem(itemId, quantityTransferred);
    }

    internal void RemoveItem(ushort itemId, int quantityTransferred) {
        inventoryManager.RemoveItem(itemId, quantityTransferred);
    }
    internal void RemoveAll() {
        inventoryManager.RemoveAllItems();
    }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    public void DEBUGGIVE(int ID, int amount) {
        inventoryManager.AddItem((ushort)ID, amount);
    }

#endif
}
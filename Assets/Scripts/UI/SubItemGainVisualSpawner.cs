using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// Spawns a nice lil popup when the submarine inventory gains items
public class SubItemGainVisualSpawner : MonoBehaviour {

    [SerializeField] Transform _popupContainer;
    [SerializeField] Transform _transferVisualWorldDestination;
    [SerializeField] UIInventoryGainPopup _popup; 
    [SerializeField] SubItemTransferVisual _transferVisualPrefab; 

    private Dictionary<ushort, UIInventoryGainPopup> activePopups = new Dictionary<ushort, UIInventoryGainPopup>();
    private Dictionary<ushort, SubItemTransferVisual> activeTransferVisuals= new Dictionary<ushort, SubItemTransferVisual>();
    private InventoryManager _inv;

    public void Init(InventoryManager subInventory) {
        subInventory.OnSlotNew += SlotNew;
        subInventory.OnSlotChanged += SlotChanged;
        //ItemTransferManager.OnItemTransferStart += ItemStart;
        //ItemTransferManager.OnItemTransferStop += ItemStop;
        _inv = subInventory;
    }
    private void OnDestroy() {
        _inv.OnSlotNew -= SlotNew;
        _inv.OnSlotChanged -= SlotChanged;
        //ItemTransferManager.OnItemTransferStart -= ItemStart;
        //ItemTransferManager.OnItemTransferStop -= ItemStop;
    }
    private void ItemStart(ushort itemID, int quantity) {
        // quantity determines visual speed...
        if (activeTransferVisuals.ContainsKey(itemID)) return;
        var icon = App.ResourceSystem.GetItemByID(itemID).icon;
        
        var playerTrans = PlayerManager.Instance.transform;
        var visual = Instantiate(_transferVisualPrefab, transform);
        visual.StartVisual(playerTrans, _transferVisualWorldDestination, icon, quantity);
        activeTransferVisuals.Add(itemID, visual);
    }

    private void ItemStop(ushort itemID) {
        Debug.Log("item stop!");
        if (!activeTransferVisuals.TryGetValue(itemID, out var visual)) {
            Debug.LogError("STOPPED BUT COULDN'T FIND ID");
            return;
        }
        
        visual.StopVisual();
        activeTransferVisuals.Remove(itemID);
    }

    private void SlotNew(ushort itemId,int newAmount) {
        if (PlayerManager.Instance == null) return;
        if (!PlayerManager.Instance.PlayerLayerController.IsInSub) return;
        if (newAmount <= 0) return;
        Sprite icon = App.ResourceSystem.GetItemByID(itemId).icon;
        var popup = Instantiate(_popup, _popupContainer);
        popup.Init(icon, newAmount, itemId);
        popup.OnDespawned += HandlePopupDespawn;
        activePopups[itemId] = popup;
     }
    private void SlotChanged(ushort itemId,int changeAmount) {
        if (activePopups.TryGetValue(itemId, out UIInventoryGainPopup popup) && popup != null) {
            popup.IncreaseAmount(changeAmount);
        } else {
            SlotNew(itemId,changeAmount);
        }
    }

    private void HandlePopupDespawn(UIInventoryGainPopup popup) {
        popup.OnDespawned -= HandlePopupDespawn;

        var entry = activePopups.FirstOrDefault(kvp => kvp.Value == popup);
        if (entry.Value != null) {
            activePopups.Remove(entry.Key);
        }
    }
}
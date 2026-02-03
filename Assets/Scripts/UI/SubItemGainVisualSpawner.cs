using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// Spawns a nice lil popup when the submarine inventory gains items
public class SubItemGainVisualSpawner : MonoBehaviour {

    [SerializeField] Transform _popupContainer;
    [SerializeField] UIInventoryGainPopup _popup; 
    private Dictionary<ushort, UIInventoryGainPopup> activePopups = new Dictionary<ushort, UIInventoryGainPopup>();
    private InventoryManager _inv;

    public void Init(InventoryManager subInventory) {
        subInventory.OnSlotNew += SlotNew;
        subInventory.OnSlotChanged += SlotChanged;
        _inv = subInventory;
    }

    private void SlotNew(ushort itemId,int newAmount) {
        Sprite icon = App.ResourceSystem.GetItemByID(itemId).icon;
        var popup = Instantiate(_popup, _popupContainer);
        popup.Init(icon, newAmount);
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
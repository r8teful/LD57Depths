using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// Old script we don't use anymore
public class PickupPopupSpawner : MonoBehaviour {
    [SerializeField] private Transform popupParent;

    private Dictionary<ushort, UIInventoryGainPopup> activePopups = new Dictionary<ushort, UIInventoryGainPopup>();

    private void Awake() {
        // If playerInventory is not assigned in the inspector, attempt to find it

    }

    private void OnEnable() {
        PlayerInventory.OnItemPickup += HandleItemPickup;
    }

    private void OnDisable() {
        PlayerInventory.OnItemPickup -= HandleItemPickup;
    }

    private void HandleItemPickup(ushort itemId, int amount) {
        if (amount <= 0) return;

        Sprite icon = App.ResourceSystem.GetItemByID(itemId).icon;
        if (icon == null) {
            Debug.LogWarning($"No icon found for item ID: {itemId}");
            return;
        }

        if (activePopups.TryGetValue(itemId, out UIInventoryGainPopup popup) && popup != null) {
            popup.IncreaseAmount(amount);
        } else {
            popup = Instantiate(App.ResourceSystem.GetPrefab<UIInventoryGainPopup>("UIInventoryGainPopup"), popupParent);
            popup.Init(icon, amount);
            popup.OnDespawned += HandlePopupDespawn;
            activePopups[itemId] = popup;
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
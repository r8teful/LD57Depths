using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// Basically UIManagerInventory but for the upgrade screen
public class UISubInventory : MonoBehaviour {
    [SerializeField] private Transform _itemContainer;
    private InventoryManager _localInventoryManager;
    private Dictionary<ushort, UIInventoryItem> slotInventoryUIs = new Dictionary<ushort, UIInventoryItem>();

    public void Init() {
        _localInventoryManager = SubmarineManager.Instance.SubInventory; // this should exist
        SubscribeToEvents();
    }
    private void OnDestroy() {
        UnsubscribeToEvents();
    }
    private void SubscribeToEvents() {
        if (_localInventoryManager != null) {
            _localInventoryManager.OnSlotChanged += UpdateSlot;
            _localInventoryManager.OnSlotNew += CreateUISlot;
        }
    }

    private void UnsubscribeToEvents() {
        if (_localInventoryManager != null) {
            _localInventoryManager.OnSlotChanged -= UpdateSlot;
            _localInventoryManager.OnSlotNew -= CreateUISlot;
        }
    }

    public void CreateUISlot(ushort itemID, int newAmount = 1) {
        if (slotInventoryUIs.ContainsKey(itemID)) {
            UpdateSlot(itemID);
            return;
        }
        UIInventoryItem slotUI = Instantiate(App.ResourceSystem.GetPrefab<UIInventoryItem>("InventoryItem"), _itemContainer);
        slotInventoryUIs.Add(itemID, slotUI);
        // Fetch item data from inv
        slotUI.Init(_localInventoryManager.GetItem(itemID));
        UpdateSlot(itemID);
        EnsureRightSlotOrder();
    }

    void UpdateSlot(ushort itemID, int changeAmount = 0) {
        if (slotInventoryUIs.TryGetValue(itemID, out var uiItem)) {
            int amount = _localInventoryManager.GetItemCount(itemID);
            uiItem.UpdateSlot(amount);
        }
    }
    private void EnsureRightSlotOrder() {
        // Sort dictionary entries by key (ascending) and get the values in that order.
        var orderedItems = slotInventoryUIs.OrderBy(kv => kv.Key).Select(kv => kv.Value).ToList();

        for (int i = 0; i < orderedItems.Count; i++) {
            var item = orderedItems[i];
            if (item == null) continue;

            // Ensure the transform exists, then set sibling index to i
            var t = item.transform;
            if (t != null)
                t.SetSiblingIndex(i);
        }
    }
}
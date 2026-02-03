// InventoryUIManager.cs
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

public class UIManagerInventory : Singleton<UIManagerInventory> {
    [SerializeField] private Transform slotInvContainer; // Parent transform where UI slots will be instantiated
    private InventoryManager _localInventoryManager;
    
    private GameObject _playerGameObject; // player that own this UI
    private Dictionary<ushort,UIInventoryItem> slotInventoryUIs = new Dictionary<ushort, UIInventoryItem>();
    private bool _isOpen = false;

    // --- Properties ---
    public bool IsOpen => _isOpen;
    public event Action<bool> OnInventoryToggle;
    public void Init(GameObject owningPlayer, PlayerManager client) {
        _localInventoryManager = client.InventoryN.GetInventoryManager();
        _playerGameObject = owningPlayer; // Important for knowing who to pass to item usage
        if (_localInventoryManager == null || _playerGameObject == null) {
            Debug.LogError("InventoryUIManager received null references during Initialize! UI may not function.", gameObject);
            enabled = false;
            return;
        }
        CreateExistingSlots();
        SubscribeToEvents();
        Debug.Log("InventoryUIManager Initialized for player: " + _playerGameObject.name);
    }

    private void CreateExistingSlots() {
        // Make sure old shit is gone
        foreach (Transform child in slotInvContainer) {
            Destroy(child.gameObject);
        }
        slotInventoryUIs.Clear();
        foreach (var slot in _localInventoryManager.Slots) {
            CreateUISlot(slot.Key);
        }
    }

    private void OnDestroy() {
        UnsubscribeToEvents();
    }
    private void SubscribeToEvents() {
        if (_localInventoryManager != null) {

            _localInventoryManager.OnSlotChanged += UpdateSlot;
            _localInventoryManager.OnSlotNew += CreateUISlot;
            _localInventoryManager.OnSlotRemoved += RemoveUISlot;
        }
        // If player syncer is on the same player object, find it for container events
        PlayerInventory playerSyncer = _playerGameObject.GetComponent<PlayerInventory>();
      
    }

    private void UnsubscribeToEvents() {
        if (_localInventoryManager != null) {
            _localInventoryManager.OnSlotChanged -= UpdateSlot;
            _localInventoryManager.OnSlotNew -= CreateUISlot;
            _localInventoryManager.OnSlotRemoved -= RemoveUISlot;
        }
    }

    private void RemoveUISlot(ushort itemID) {
        if (slotInventoryUIs.TryGetValue(itemID, out var uiItem)) {
            if (uiItem != null) {
                uiItem.Remove();
            }
            slotInventoryUIs.Remove(itemID);
        }
    }

    public void CreateUISlot(ushort itemID, int newAmount = 1) {
        if (slotInventoryUIs.ContainsKey(itemID)) {
            UpdateSlot(itemID);
            return;
        }
        UIInventoryItem slotUI = Instantiate(App.ResourceSystem.GetPrefab<UIInventoryItem>("InventoryItem"), slotInvContainer);
        slotInventoryUIs.Add(itemID, slotUI);
        // Fetch item data from inv
        slotUI.Init(_localInventoryManager.GetItem(itemID));
        UpdateSlot(itemID);
        EnsureRightSlotOrder();
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

    void UpdateSlot(ushort itemID, int changeAmount = 0) {
        if (slotInventoryUIs.TryGetValue(itemID, out var uiItem)) {
            int amount = _localInventoryManager.GetItemCount(itemID);
            uiItem.UpdateSlot(amount);
        }
    }
}
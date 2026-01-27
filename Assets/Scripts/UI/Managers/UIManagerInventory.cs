// InventoryUIManager.cs
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI; // Required for UI elements like Panel
using UnityEngine.InputSystem;
using TMPro;
using UnityEngine.EventSystems;
using System;

public class UIManagerInventory : Singleton<UIManagerInventory> {
    [Header("UI Elements")]
    [SerializeField] private GameObject playerUIPanel; // The main window panel
    [SerializeField] private GameObject inventoryPanel; // The actual inventory grid, we move this around into the inventory, or at the bottom for containers
    [SerializeField] private Image inventoryPanelBackground;
    [SerializeField] private RectTransform inventoryUIBounds;
    [Header("Shared container UI")]
    [SerializeField] private GameObject containerPanel; // Separate panel/area for container slots
    [SerializeField] private Transform slotInvContainer; // Parent transform where UI slots will be instantiated
    [SerializeField] private Transform containerSlotContainer; // Parent for container slot prefabs
    [SerializeField] private TextMeshProUGUI containerTitleText; // Optional: To show container name/type
    // Use the SAME slotPrefab
    private InventoryManager _localInventoryManager;
    // --- Runtime ---
    private GameObject _playerGameObject; // player that own this UI
    private NetworkedPlayerInventory _playerInventory; // player that own this UI
    private List<UIInventoryItem> slotInventoryUIs = new List<UIInventoryItem>();
    private Image draggingIconImage;
    private bool isDragging = false;
    private bool _isOpen = false;
    private int dragSourceIndex = -1;
    private int currentTabIndex = 0;

    // --- Properties ---
    public bool IsOpen => playerUIPanel != null && _isOpen;
    public event Action<bool> OnInventoryToggle;
    public void Init(GameObject owningPlayer, PlayerManager client) {
        _localInventoryManager = client.InventoryN.GetInventoryManager();
        _playerGameObject = owningPlayer; // Important for knowing who to pass to item usage
        _playerInventory = _playerGameObject.GetComponent<NetworkedPlayerInventory>();
        //GetComponent<PopupManager>().Init(_localInventoryManager);

        if (_localInventoryManager == null || _playerGameObject == null) {
            Debug.LogError("InventoryUIManager received null references during Initialize! UI may not function.", gameObject);
            enabled = false;
            return;
        }

        // Validate essential UI components assigned in prefab
        if (!playerUIPanel || !containerPanel || !containerSlotContainer) {
            Debug.LogError("One or more UI elements missing in InventoryUIPrefab!", gameObject);
            //enabled = false; return;
        }

        containerPanel.SetActive(false);

        SubscribeToEvents();
        CreateSlotUIs();
        // OR itemSelectionManager.Init(manager.gameObject, manager);
        playerUIPanel.SetActive(false); // ensure inventory is closed
        Debug.Log("InventoryUIManager Initialized for player: " + _playerGameObject.name);
    }

    private void OnInvSlotChanged(int obj) {
        throw new NotImplementedException();
    }

    private void OnDestroy() {
        UnsubscribeToEvents();
    }
    private void SubscribeToEvents() {

        if (_localInventoryManager != null)
            _localInventoryManager.OnSlotChanged += UpdateSlotUI;
        // If player syncer is on the same player object, find it for container events
        NetworkedPlayerInventory playerSyncer = _playerGameObject.GetComponent<NetworkedPlayerInventory>();
      
    }
    private void UnsubscribeToEvents() {
        if (_localInventoryManager != null)
            _localInventoryManager.OnSlotChanged -= UpdateSlotUI;
    }
  
  
  

  

 
    private void OpenInventory() {
        playerUIPanel.SetActive(true);
        OnInventoryToggle?.Invoke(true);
    }
    private void CloseInventory() {
        playerUIPanel.SetActive(false);
        EventSystem.current.SetSelectedGameObject(null); // Deselect UI when closing
        OnInventoryToggle?.Invoke(false);
    }

    void CreateSlotUIs() {
        foreach (Transform child in slotInvContainer) {
            Destroy(child.gameObject);
        }
        slotInventoryUIs.Clear();
       
        // Instantiate UI slots based on how many resources there are
        var index = 0;
        int total = App.ResourceSystem.GetAllItems().Count;
        for (int i = 0; i < total; i++) {
            UIInventoryItem slotUI = Instantiate(App.ResourceSystem.GetPrefab<UIInventoryItem>("InventoryItem"), slotInvContainer);
            slotUI.name = $"Slot_{index}"; // For easier debugging
            slotUI.Initialize(this);
            slotInventoryUIs.Add(slotUI);
            UpdateSlotUI(index); // Update visual state immediately
            index++;
        }

        // Controller navigation setup
        // TODO, we'll have to think about if we have a hover feature, or similar, we'll need something like this for the chest atleast
        var columnCount = 4;
        for (int i = 0; i < slotInventoryUIs.Count; i++) {
            Selectable currentSel = slotInventoryUIs[i].GetComponent<Selectable>();
            if (currentSel == null) continue;

            Navigation nav = currentSel.navigation;
            nav.mode = Navigation.Mode.Explicit;

            // Up
            if (i >= columnCount) nav.selectOnUp = slotInventoryUIs[i - columnCount].GetComponent<Selectable>();
            else nav.selectOnUp = null; // Or wrap around to bottom?
                                        // Down
            if (i < slotInventoryUIs.Count - columnCount) nav.selectOnDown = slotInventoryUIs[i + columnCount].GetComponent<Selectable>();
            else nav.selectOnDown = null; // Or wrap to top?
                                          // Left
            if (i % columnCount != 0) nav.selectOnLeft = slotInventoryUIs[i - 1].GetComponent<Selectable>();
            else nav.selectOnLeft = null; // Or wrap to right end of prev row?
                                          // Right
            if ((i + 1) % columnCount != 0 && (i + 1) < slotInventoryUIs.Count) nav.selectOnRight = slotInventoryUIs[i + 1].GetComponent<Selectable>();
            else nav.selectOnRight = null; // Or wrap to left end of next row?

            currentSel.navigation = nav;
        }
        //if (IsOpen && slotInventoryUIs.Count > 0 && _playerInput != null && _playerInput.currentControlScheme == "Gamepad") {
        //    EventSystem.current.SetSelectedGameObject(slotInventoryUIs[0].gameObject);
        //}
    }
    void UpdateSlotUI(int slotIndex) {
        //RefreshUI(); // Uncoment this if UI isn't changing properly
        InventorySlot slotData = _localInventoryManager.GetSlot(slotIndex);
        if (slotIndex >= 0 && slotIndex < slotInventoryUIs.Count) {
            slotInventoryUIs[slotIndex].UpdateSlot(slotData);
        }
    }
    private void TryCloseInventory() {
        if (IsOpen) {
            HandleToggleInventory(); // Pass dummy context
        }
    }
  
    public bool IsCurrentlyDragging() => isDragging;

    internal void HandleToggleInventory() {
        _isOpen = !_isOpen;
        if (_isOpen) {
            OpenInventory();
        } else {
            CloseInventory();
        }
        // If closing while dragging, cancel the drag
        if (!playerUIPanel.activeSelf && isDragging) {
            // EndDrag(true); // Force cancel
        }
    }

    internal void HandleCloseAction(InputAction.CallbackContext context) {
        if (IsOpen) {
            // If inventory is open and nothing held, maybe toggle it closed
            HandleToggleInventory();
        }
    }
}
// InventoryUIManager.cs
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI; // Required for UI elements like Panel
using UnityEngine.InputSystem;
using DG.Tweening;
using TMPro;
using UnityEngine.EventSystems;
using System;

public class InventoryUIManager : MonoBehaviour {
    [Header("UI Elements")]
    [SerializeField] private GameObject inventoryPanel; // The main inventory window panel
    [SerializeField] private GameObject hotbarPanel; 
    [SerializeField] private GameObject[] inventoryTabs; 
    [SerializeField] private Transform slotInvContainer; // Parent transform where UI slots will be instantiated
    [SerializeField] private Transform slotHotbarContainer; // Parent transform where UI slots will be instantiated
    [SerializeField] private GameObject slotPrefab; // Prefab for a single inventory slot UI element
    [SerializeField] private Image draggingImage; // A UI Image used to show the item being dragged
    [SerializeField] private TextMeshProUGUI draggingImageText; // A UI tect used to show the quantity being dragged
    [SerializeField] private RectTransform inventoryUIBounds;
    [Header("Shared container UI")]
    [SerializeField] private GameObject containerPanel; // Separate panel/area for container slots
    [SerializeField] private Transform containerSlotContainer; // Parent for container slot prefabs
    [SerializeField] private TextMeshProUGUI containerTitleText; // Optional: To show container name/type
    // Use the SAME slotPrefab

    private List<InventorySlotUI> containerSlotUIs = new List<InventorySlotUI>();
    private SharedContainer currentViewedContainer = null;

    private InputAction _UItoggleInventoryAction; // Assign your toggle input action asset
    private PlayerInput _playerInput; // Get reference if Input System PlayerInput component is used
    private InputAction _uiInteractAction;   // e.g., Left Mouse / Gamepad A
    private InputAction _uiAltInteractAction; // e.g., Right Mouse / Gamepad X
    private InputAction _uiDropOneAction;     // e.g., Right Mouse (when holding) / Gamepad B
    private InputAction _uiNavigateAction;    // D-Pad / Arrow Keys
    private InputAction _uiPointAction;       // Mouse position for cursor icon
    private InputAction _uiCancelAction;       // Escape / Gamepad Start (to cancel holding)
    private InputAction _playerInteractAction;       
    private InputAction _uiTabLeft; 
    private InputAction _uiTabRight; 

    [SerializeField] private int hotbarSize = 5; // How many slots in the first row act as hotbar

    [Header("References")]
    private InventoryManager _localInventoryManager;
    private ItemSelectionManager _itemSelectionManager;
    // --- Runtime ---
    private GameObject _playerGameObject; // player that own this UI
    private NetworkedPlayerInventory _playerInventory; // player that own this UI
    private List<InventorySlotUI> slotInventoryUIs = new List<InventorySlotUI>();
    private List<InventorySlotUI> slotHotbarUIs = new List<InventorySlotUI>();
    private Image draggingIconImage;
    private bool isDragging = false;
    private bool _isOpen = false;
    private int dragSourceIndex = -1; 
    private int currentTabIndex = 0;
    private bool dropHandled;
    private bool _droppedSameSlot;
    private bool _isInventoryOpenForAction = false; // Tracks if panel was open when action started
    private InventorySlotUI _currentFocusedSlot = null; // For controller navigation

    // --- Properties ---
    public bool IsOpen => inventoryPanel != null && _isOpen;
    public int HotbarSize => hotbarSize; // Expose hotbar size
    public event Action<bool> OnInventoryToggle;
    public void Init(InventoryManager localPlayerInvManager, GameObject owningPlayer) {
        _localInventoryManager = localPlayerInvManager;
        _itemSelectionManager = GetComponent<ItemSelectionManager>();
        _playerGameObject = owningPlayer; // Important for knowing who to pass to item usage
        _playerInventory = _playerGameObject.GetComponent<NetworkedPlayerInventory>();
        GetComponent<UICraftingManager>().Init(_localInventoryManager);
        //GetComponent<PopupManager>().Init(_localInventoryManager);

        if (_localInventoryManager == null || _itemSelectionManager == null || _playerGameObject == null) {
            Debug.LogError("InventoryUIManager received null references during Initialize! UI may not function.", gameObject);
            enabled = false;
            return;
        }

        // Validate essential UI components assigned in prefab
        if (!inventoryPanel || !slotInvContainer || !slotPrefab || !draggingImage || !containerPanel || !containerSlotContainer) {
            Debug.LogError("One or more UI elements missing in InventoryUIPrefab!", gameObject);
            //enabled = false; return;
        }
        // Attempt to find PlayerInput component on the owning player
        _playerInput = _playerGameObject.GetComponent<PlayerInput>();
        if (_playerInput != null) {
            // Assuming action map "UI" and actions named like "Interact", "AltInteract"
            _UItoggleInventoryAction = _playerInput.actions["UI_Toggle"];
            _uiInteractAction = _playerInput.actions["UI_Interact"];
            _uiAltInteractAction = _playerInput.actions["UI_AltInteract"];
            _uiDropOneAction = _playerInput.actions["UI_DropOne"];
            _uiNavigateAction = _playerInput.actions["UI_Navigate"];
            _uiPointAction = _playerInput.actions["UI_Point"];
            _uiCancelAction = _playerInput.actions["UI_Cancel"]; // For cancelling held item
            _uiTabLeft = _playerInput.actions["UI_TabLeft"]; // Opening containers
            _uiTabRight = _playerInput.actions["UI_TabRight"]; // Opening containers
            _playerInteractAction = _playerInput.actions["Interact"]; // Opening containers
            // Subscribe to input actions (do this in OnEnable, unsubscribe in OnDisable)
        } else {
            Debug.LogWarning("PlayerInput component not found on player. Mouse-only or manual input bindings needed.", gameObject);
        }
        draggingIconImage = draggingImage.GetComponent<Image>();
        if (draggingIconImage == null) { /* Error */ enabled = false; return; }
        draggingImage.gameObject.SetActive(false);

        containerPanel.SetActive(false);
        
        // Subscribe to LOCAL events from THIS player's managers
        _localInventoryManager.OnSlotChanged += UpdateSlotUI;
        _itemSelectionManager.OnSelectionChanged += UpdateHotbarHighlight;

        // If player syncer is on the same player object, find it for container events
        NetworkedPlayerInventory playerSyncer = _playerGameObject.GetComponent<NetworkedPlayerInventory>();
        if (playerSyncer != null) {
            NetworkedPlayerInventory.OnContainerOpened += HandleContainerOpen; // Static events are tricky, direct ref better if possible
            NetworkedPlayerInventory.OnContainerClosed += CloseCurrentContainerUI;
        } else { Debug.LogError("PlayerInventorySyncer not found on owning player for container events!"); }

        // Inform ItemSelectionManager about hotbar size
        _itemSelectionManager.Initialize(hotbarSize, _playerGameObject,_localInventoryManager); // Pass player object
        // OR itemSelectionManager.Init(manager.gameObject, manager);
        EnableTab(0); // Set inventory tab as first tab, dissable others
        inventoryPanel.SetActive(false); // ensure inventory is closed
        SubscribeToEvents();
        CreateSlotUIs();
        Debug.Log("InventoryUIManager Initialized for player: " + _playerGameObject.name);
    }
    void OnEnable() { SubscribeToEvents(); }
    void OnDisable() { UnsubscribeFromEvents(); }
    private void SubscribeToEvents() {
        if (_UItoggleInventoryAction != null) _UItoggleInventoryAction.performed += ToggleInventory;
        if (_uiInteractAction != null) _uiInteractAction.performed += HandlePrimaryInteractionPerformed;
        if (_uiAltInteractAction != null) _uiAltInteractAction.performed += HandleSecondaryInteractionPerformed;
        if (_uiCancelAction != null) _uiCancelAction.performed += HandleCloseAction;
        if (_uiTabLeft != null) _uiTabLeft.performed += l => ScrollTabs(-1); // Not unsubscribing but what is the worst that could happen?
        if (_uiTabRight != null) _uiTabRight.performed += l => ScrollTabs(1);
        if (_playerInteractAction != null) _playerInteractAction.performed += HandleInteractAction;
    }

    private void UnsubscribeFromEvents() {
        if (_UItoggleInventoryAction != null) _UItoggleInventoryAction.performed -= ToggleInventory;
        if (_uiInteractAction != null) _uiInteractAction.performed -= HandlePrimaryInteractionPerformed;
        if (_uiAltInteractAction != null) _uiAltInteractAction.performed -= HandleSecondaryInteractionPerformed;
        if (_uiCancelAction != null) _uiCancelAction.performed -= HandleCloseAction;
        if (_localInventoryManager != null) _localInventoryManager.OnSlotChanged -= UpdateSlotUI;
        if (_itemSelectionManager != null) _itemSelectionManager.OnSelectionChanged -= UpdateHotbarHighlight;
        if (_playerInteractAction != null) _playerInteractAction.performed -= HandleInteractAction;
        if (currentViewedContainer != null) currentViewedContainer.OnContainerInventoryChanged -= RefreshUIContents;
    }
  
    // --- Input Handlers called by PlayerInput actions ---
    private void HandlePrimaryInteractionPerformed(InputAction.CallbackContext context) {
        // This is Left Mouse Click / Gamepad A
        //Debug.Log("Primary Interaction Performed");
        ProcessInteraction(PointerEventData.InputButton.Left);
    }
    private void HandleSecondaryInteractionPerformed(InputAction.CallbackContext context) {
        // This is Right Mouse Click / Gamepad X
        Debug.Log("Secondary Interaction Performed");
        ProcessInteraction(PointerEventData.InputButton.Right);
    }
    // --- Central Interaction Logic ---
    private void ProcessInteraction(PointerEventData.InputButton button) {
        InventorySlotUI clickedOrFocusedSlot = GetSlotUnderCursorOrFocused();

        if (!_playerInventory.heldItemStack.IsEmpty()) // Currently "holding" an item
        {
            if (clickedOrFocusedSlot != null) // Clicked on a slot while holding
            {
                _playerInventory.HandlePlaceHeldItem(clickedOrFocusedSlot, button);
            } else { // Clicked outside slots (but inside UI panel boundary) while holding
                if (RectTransformUtility.RectangleContainsScreenPoint(inventoryUIBounds, Input.mousePosition)) {
                    // Clicked on empty UI space, possibly return item or do nothing
                    Debug.Log("Clicked empty UI space while holding. Returning item.");
                    _playerInventory.ReturnHeldItemToSource();
                } else {
                    Debug.Log("DROP");
                    // Clicked outside ALL UI while holding item
                    _playerInventory.HandleDropToWorld(button);
                }
            }
        } else // Not holding an item, trying to pick up from slot
          {
            if (clickedOrFocusedSlot != null) {
                _playerInventory.HandlePickupFromSlot(clickedOrFocusedSlot, button);
            }
        }
    }
    // For containers
    private void HandleInteractAction(InputAction.CallbackContext context) {
        Debug.Log("HAndleinteraction!!1");
        _playerInventory.HandleInteractionInput();
    }
    public void RefreshUI() {
        if (_localInventoryManager == null || _localInventoryManager.Slots == null) {
            Debug.LogWarning("Attempting to refresh UI but localInventoryManager or its slots are null.");
            return;
        }

        for (int i = 0; i < slotInventoryUIs.Count; i++) {
            if (i < _localInventoryManager.Slots.Count) {
                slotInventoryUIs[i].UpdateSlot(_localInventoryManager.GetSlot(i));
            } else {
                // Should not happen if UIManager.Initialize correctly sized uiSlots
                slotInventoryUIs[i].UpdateSlot(new InventorySlot()); // Empty slot
            }
        }
        UpdateHeldItemVisual();
    }

    public void UpdateHeldItemVisual() {
        if (_playerInventory == null)
            return;

        if (!_playerInventory.heldItemStack.IsEmpty()) {
            if (draggingImage != null) {
                draggingImage.gameObject.SetActive(true);
                ItemData data = _playerInventory.heldItemStack.ItemData; // Uses the property from InventorySlot
                draggingImage.sprite = data != null ? data.icon : null;
                draggingImage.transform.position = Input.mousePosition;
            }
            if (draggingImageText != null) {
                draggingImageText.gameObject.SetActive(true);
                draggingImageText.text = _playerInventory.heldItemStack.quantity > 1 ? _playerInventory.heldItemStack.quantity.ToString() : "";
                // Position quantity text relative to image
                if (draggingImage != null)
                    draggingImageText.transform.position = draggingImage.transform.position + new Vector3(15, -15, 0); // Example offset
            }
        } else {
            if (draggingImage != null)
                draggingImage.gameObject.SetActive(false);
            if (draggingImageText != null)
                draggingImageText.gameObject.SetActive(false);
        }
    }
    public void HandleSlotClick(InventorySlotUI clickedSlot, PointerEventData.InputButton button) {
        // This method is now more of a direct call if slots still have IPointerClickHandler
        // It's better if primary/secondary interaction actions directly call ProcessInteraction.
        // This method could be deprecated if PlayerInput actions are used exclusively.
        // For now, let's assume it might still be called by mouse clicks.
        Debug.Log($"Legacy HandleSlotClick on {clickedSlot.name} with button {button}");
        ProcessInteraction(button); // Requires GetSlotUnderCursorOrFocused to resolve to clickedSlot
    }

    private InventorySlotUI GetSlotUnderCursorOrFocused() {
        if (_playerInput != null && _playerInput.currentControlScheme == "Gamepad") {
            // For gamepad, return the currently focused slot by navigation
            return _currentFocusedSlot;
        } else // Mouse
          {
            PointerEventData eventData = new PointerEventData(EventSystem.current);
            eventData.position = _uiPointAction != null ? _uiPointAction.ReadValue<Vector2>() : (Vector2)Input.mousePosition;
            List<RaycastResult> results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(eventData, results);
            foreach (RaycastResult result in results) {
                InventorySlotUI slotUI = result.gameObject.GetComponent<InventorySlotUI>();
                if (slotUI != null) return slotUI;
            }
        }
        return null;
    }

    // --- Controller Navigation Callbacks ---
    public void OnSlotFocused(InventorySlotUI slotUI) {
        _currentFocusedSlot = slotUI;
        slotUI.SetFocus(true);
    }

    public void OnSlotDefocused(InventorySlotUI slotUI) {
        if (_currentFocusedSlot == slotUI) {
            _currentFocusedSlot = null;
        }
        slotUI.SetFocus(false);
    }

    private void HandleCloseAction(InputAction.CallbackContext context) {
        // E.g., Escape key or Gamepad B/Start (if configured to cancel)
        _playerInventory.HandleClose(context);
        if (IsOpen) {
            // If inventory is open and nothing held, maybe toggle it closed
            ToggleInventory(new InputAction.CallbackContext()); // Pass dummy context
        }
    }
    private void ToggleInventory(InputAction.CallbackContext context) {
        if (Console.IsConsoleOpen())
            return;
        _isOpen = !_isOpen;
        if (_isOpen) {
            OpenInventory();
        } else {
            CloseInventory(context);
        }
        // If closing while dragging, cancel the drag
        if (!inventoryPanel.activeSelf && isDragging) {
           // EndDrag(true); // Force cancel
        }
    }
    private void OpenInventory() {
        inventoryPanel.SetActive(true);
        hotbarPanel.SetActive(false); // 
        OnInventoryToggle?.Invoke(true);
    }
    private void CloseInventory(InputAction.CallbackContext c) {
        inventoryPanel.SetActive(false);
        hotbarPanel.SetActive(true);
        EventSystem.current.SetSelectedGameObject(null); // Deselect UI when closing
        _playerInventory.HandleClose(c);
        OnInventoryToggle?.Invoke(false);

    }
    // Called from tab button inspector
    public void OnTabButtonClicked(int i) {
        EnableTab(i);
    }
    private void EnableTab(int i) {
        if (inventoryTabs == null || inventoryTabs.Length == 0) {
            Debug.LogWarning("inventoryTabs array is null or empty!");
            return;
        }

        if (i < 0 || i >= inventoryTabs.Length) {
            Debug.LogWarning($"Tab index {i} is out of range. Valid range: 0 to {inventoryTabs.Length - 1}");
            return;
        }
        currentTabIndex = i;
        for (int j = 0; j < inventoryTabs.Length; j++) {
            if (inventoryTabs[j] != null) {
                inventoryTabs[j].SetActive(j == i);
            } else {
                Debug.LogWarning($"Tab at index {j} is null.");
            }
        }
    }
    // direction should be either -1 or 1 idealy
    public void ScrollTabs(int direction) {
        if (!IsOpen)
            return; // Don't scroll if inventory is not open
        if (inventoryTabs == null || inventoryTabs.Length == 0) {
            Debug.LogWarning("inventoryTabs array is null or empty!");
            return;
        }

        int newIndex = (currentTabIndex + direction + inventoryTabs.Length) % inventoryTabs.Length;

        // Clamp the value to valid range
        //newIndex = Mathf.Clamp(newIndex, 0, inventoryTabs.Length - 1);

        if (newIndex != currentTabIndex) {
            EnableTab(newIndex);
        }
    }
    private void SetInvUnactive() {
        // todo possible enable interactions here
        // inventoryPanel.SetActive(false);
    }
    void CreateSlotUIs() {
        foreach (Transform child in slotInvContainer) {
            Destroy(child.gameObject);
        }
        slotInventoryUIs.Clear();
        slotHotbarUIs.Clear();
        for (int i = 0; i < hotbarSize; i++) {
            GameObject slotGO = Instantiate(slotPrefab, slotHotbarContainer);
            slotGO.name = $"SlotHotbar_{i}"; // For easier debugging

            InventorySlotUI slotUI = slotGO.GetComponent<InventorySlotUI>();
            if (slotUI != null) {
                slotUI.Initialize(this, i,true); // Hotbar slots behave differently, they "mirror" what is in the actual inventory slot
                slotHotbarUIs.Add(slotUI);
                UpdateSlotUI(i); // Update visual state immediately
            } else {
                Debug.LogError($"Slot prefab '{slotPrefab.name}' is missing InventorySlotUI component!");
            }
        }
        // Instantiate UI slots based on inventory size
        for (int i = 0; i < _playerInventory.InventorySize; i++) {
            GameObject slotGO = Instantiate(slotPrefab, slotInvContainer);
            slotGO.name = $"Slot_{i}"; // For easier debugging

            InventorySlotUI slotUI = slotGO.GetComponent<InventorySlotUI>();
            if (slotUI != null) {
                slotUI.Initialize(this, i,false); // Pass reference to this manager and the slot index
                slotInventoryUIs.Add(slotUI);
                UpdateSlotUI(i); // Update visual state immediately
            } else {
                Debug.LogError($"Slot prefab '{slotPrefab.name}' is missing InventorySlotUI component!");
            }
        }
        // Controller navigation setup

        // TODO will have to do this in two passes just like above because inventory is not a perfect grid
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
        if (IsOpen && slotInventoryUIs.Count > 0 && _playerInput != null && _playerInput.currentControlScheme == "Gamepad") {
            EventSystem.current.SetSelectedGameObject(slotInventoryUIs[0].gameObject);
        }

        UpdateHotbarHighlight(_itemSelectionManager.SelectedSlotIndex); 
        Debug.Log($"Created {slotInventoryUIs.Count} UI slots.");
    }

    // Updates the visual representation of a single slot
    void UpdateSlotUI(int slotIndex) {
        InventorySlot slotData = _localInventoryManager.GetSlot(slotIndex);
        if (slotIndex >= 0 && slotIndex < slotInventoryUIs.Count) {
            slotInventoryUIs[slotIndex].UpdateSlot(slotData);
            if(slotIndex < hotbarSize) {
                // Also update the hotbar
                slotHotbarUIs[slotIndex].UpdateSlot(slotData);
                slotInventoryUIs[slotIndex].SetSelected(slotIndex == _itemSelectionManager.SelectedSlotIndex && IsOpen);
            }
        }
    }
  
    // Called by InventorySlotUI when an item is dropped onto it
    public void HandleDrop(int dropTargetIndex) {
        if (!isDragging || dragSourceIndex == -1 || !_localInventoryManager.IsValidIndex(dropTargetIndex)) {
            // Invalid drop scenario, maybe log warning
            return;
        }
        Debug.Log($"Handle Drop: From Slot {dragSourceIndex} To Slot {dropTargetIndex}");

        // Don't swap with self
        if (dragSourceIndex == dropTargetIndex) {
            // Just reset visuals if needed, but EndDrag handles the rest
            slotInventoryUIs[dragSourceIndex].SetVisualsDuringDrag(false);
            // No actual data change needed
            return; // Important: Don't proceed to swap!
        }


        // Perform the swap in the data manager

        // EndDrag will be called automatically by the Input System / EventSystem
        // after OnDrop finishes. It handles resetting the drag state.
        // We don't need to call EndDrag() manually here.
    }

    private void UpdateHotbarHighlight(int newSelectedIndex) {
        // Only show highlight if the inventory panel is open
        bool showHighlight = IsOpen;

        for (int i = 0; i < hotbarSize; i++) {
            if (i < slotInventoryUIs.Count && slotInventoryUIs[i] != null) // Ensure slot UI exists
            {
                slotInventoryUIs[i].SetSelected(showHighlight && i == newSelectedIndex);
            }
        }
        Debug.Log($"Updated Hotbar Highlight. Selected: {newSelectedIndex}, Inventory Open: {showHighlight}");
    }

    // --- Container UI Handling ---

    private void HandleContainerOpen(SharedContainer containerToView) {
        if (!containerPanel || !containerSlotContainer) return; // Container UI not setup

        if (currentViewedContainer != null && currentViewedContainer != containerToView) {
            CloseCurrentContainerUI(); // Close UI current viewed container
        }
        Debug.Log($"[UI] Opening Container View: {containerToView.name}");

        currentViewedContainer = containerToView;
        currentViewedContainer.OnContainerInventoryChanged += RefreshUIContents;
        currentViewedContainer.OnLocalPlayerInteractionStateChanged += HandleInteractionStateChanged;

        // Request server to open. Server will respond, and OnLocalPlayerInteractionStateChanged will show UI.
        currentViewedContainer.CmdRequestOpenContainer();
        // --- Setup Container UI ---
        // Clear old slots
        foreach (Transform child in containerSlotContainer) { Destroy(child.gameObject); }
        containerSlotUIs.Clear();

        // Set Title (Optional)
        if (containerTitleText) containerTitleText.text = containerToView.name; // Or a generic title

        // Update visuals immediately based on current container state
        RefreshUIContents();


        // Make the container panel visible
        containerPanel.SetActive(true);


        // Ensure player inventory is ALSO open when container is open
        if (!inventoryPanel.activeSelf) {
            inventoryPanel.SetActive(true);
            UpdateHotbarHighlight(_itemSelectionManager.SelectedSlotIndex); // Update highlights if opened this way
        }
    }
    private void HandleInteractionStateChanged(bool isOpenForThisPlayer, List<InventorySlot> initialSlots) {
        if (isOpenForThisPlayer) {
            if (containerPanel == null)
                return;
            containerPanel.SetActive(true);
            PopulateContainerSlots(initialSlots ?? new List<InventorySlot>(currentViewedContainer.ContainerSlots)); // Use initial if provided
        } else {
            CloseCurrentContainerUI(false); // Don't send close command again if server initiated closure
        }
    }
    private void PopulateContainerSlots(List<InventorySlot> slotsToDisplay) {
        // Clear existing UI slots
        foreach (Transform child in containerSlotContainer) 
            Destroy(child.gameObject);
        containerSlotUIs.Clear();

        for (int i = 0; i < slotsToDisplay.Count; i++) {
            GameObject slotGO = Instantiate(slotPrefab, containerSlotContainer);
            InventorySlotUI uiSlot = slotGO.GetComponent<InventorySlotUI>();
            if (uiSlot != null) {
                // Initialize it to know it's a container slot and its index
                uiSlot.SetContainerContext(this,i); // Pass reference to this ContainerUIManager
                uiSlot.UpdateSlot(slotsToDisplay[i]);
                containerSlotUIs.Add(uiSlot);
            }
        }
    }

    public void RefreshUIContents() {
        if (currentViewedContainer == null || containerPanel == null || !containerPanel.activeSelf)
            return;

        // This ensures the UI matches the SyncList from the container
        // If counts mismatch, repopulate. This can happen if containerSize changes dynamically (rare).
        if (containerSlotUIs.Count != currentViewedContainer.ContainerSlots.Count) {
            PopulateContainerSlots(new List<InventorySlot>(currentViewedContainer.ContainerSlots));
        } else {
            for (int i = 0; i < currentViewedContainer.ContainerSlots.Count; i++) {
                if (i < containerSlotUIs.Count)
                    containerSlotUIs[i].UpdateSlot(currentViewedContainer.ContainerSlots[i]);
            }
        }
    }

    public void CloseCurrentContainerUI(bool sendServerCommand = true) {
        if (containerPanel != null)
            containerPanel.SetActive(false);

        if (currentViewedContainer != null) {
            if (_playerInventory != null && _playerInventory.IsOwner && currentViewedContainer.InteractingClientId == _playerInventory.OwnerId) {
                currentViewedContainer.CmdRequestCloseContainer();
            }
            currentViewedContainer.OnContainerInventoryChanged -= RefreshUIContents;
            currentViewedContainer.OnLocalPlayerInteractionStateChanged -= HandleInteractionStateChanged;
            currentViewedContainer = null;
        }
        foreach (Transform child in containerSlotContainer) { Destroy(child.gameObject); }
        containerSlotUIs.Clear(); // Clean up UI elements if needed, or just hide parent
    }
    public bool IsCurrentlyDragging() => isDragging;
}
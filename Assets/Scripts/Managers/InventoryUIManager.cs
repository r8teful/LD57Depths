// InventoryUIManager.cs
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI; // Required for UI elements like Panel
using UnityEngine.InputSystem;
using DG.Tweening;
using TMPro;
using UnityEngine.EventSystems;

public class InventoryUIManager : MonoBehaviour {
    [Header("UI Elements")]
    [SerializeField] private GameObject inventoryPanel; // The main inventory window panel
    [SerializeField] private Transform slotInvContainer; // Parent transform where UI slots will be instantiated
    [SerializeField] private Transform slotHotbarContainer; // Parent transform where UI slots will be instantiated
    [SerializeField] private GameObject slotPrefab; // Prefab for a single inventory slot UI element
    [SerializeField] private GameObject draggingIconObject; // A UI Image used to show the item being dragged

    [Header("Shared container UI")]
    [SerializeField] private GameObject containerPanel; // Separate panel/area for container slots
    [SerializeField] private Transform containerSlotContainer; // Parent for container slot prefabs
    [SerializeField] private TextMeshProUGUI containerTitleText; // Optional: To show container name/type
    // Use the SAME slotPrefab

    private List<InventorySlotUI> containerSlotUIs = new List<InventorySlotUI>();
    private SharedContainer currentlyViewedContainer = null;

    private InputAction _UItoggleInventoryAction; // Assign your toggle input action asset
    private PlayerInput _playerInput; // Get reference if Input System PlayerInput component is used
    private InputAction _uiInteractAction;   // e.g., Left Mouse / Gamepad A
    private InputAction _uiAltInteractAction; // e.g., Right Mouse / Gamepad X
    private InputAction _uiDropOneAction;     // e.g., Right Mouse (when holding) / Gamepad B
    private InputAction _uiNavigateAction;    // D-Pad / Arrow Keys
    private InputAction _uiPointAction;       // Mouse position for cursor icon
    private InputAction _uiCancelAction;       // Escape / Gamepad Start (to cancel holding)

    [SerializeField] private int hotbarSize = 5; // How many slots in the first row act as hotbar

    [Header("References")]
    private InventoryManager _localInventoryManager;
    private ItemSelectionManager _itemSelectionManager;
    // --- Runtime ---
    private GameObject _playerGameObject; // player that own this UI
    private PlayerInventorySyncer _playerSyncer; // player that own this UI
    private List<InventorySlotUI> slotUIs = new List<InventorySlotUI>();
    private Image draggingIconImage;
    private bool isDragging = false;
    private bool isExpanded = false;
    private int dragSourceIndex = -1;
    private bool dropHandled;
    private bool _droppedSameSlot;
    private HeldItemStack _heldItemStack = new HeldItemStack();
    private bool _isInventoryOpenForAction = false; // Tracks if panel was open when action started
    private InventorySlotUI _currentFocusedSlot = null; // For controller navigation

    // --- Properties ---
    public bool IsOpen => inventoryPanel != null && isExpanded;
    public int HotbarSize => hotbarSize; // Expose hotbar size

    public void Init(InventoryManager localPlayerInvManager, ItemSelectionManager localPlayerItemSelector, GameObject owningPlayer) {
        _localInventoryManager = localPlayerInvManager;
        _itemSelectionManager = localPlayerItemSelector;
        _playerGameObject = owningPlayer; // Important for knowing who to pass to item usage
        _playerSyncer = _playerGameObject.GetComponent<PlayerInventorySyncer>();
        if (_localInventoryManager == null || _itemSelectionManager == null || _playerGameObject == null) {
            Debug.LogError("InventoryUIManager received null references during Initialize! UI may not function.", gameObject);
            enabled = false;
            return;
        }

        // Validate essential UI components assigned in prefab
        if (!inventoryPanel || !slotInvContainer || !slotPrefab || !draggingIconObject || !containerPanel || !containerSlotContainer) {
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
            // Subscribe to input actions (do this in OnEnable, unsubscribe in OnDisable)
        } else {
            Debug.LogWarning("PlayerInput component not found on player. Mouse-only or manual input bindings needed.", gameObject);
        }
        draggingIconImage = draggingIconObject.GetComponent<Image>();
        if (draggingIconImage == null) { /* Error */ enabled = false; return; }
        draggingIconObject.SetActive(false);

        //containerPanel.SetActive(false); // TODO For multiplayer
        
        // Subscribe to LOCAL events from THIS player's managers
        _localInventoryManager.OnSlotChanged += UpdateSlotUI;
        _itemSelectionManager.OnSelectionChanged += UpdateHotbarHighlight;

        // If player syncer is on the same player object, find it for container events
        PlayerInventorySyncer playerSyncer = _playerGameObject.GetComponent<PlayerInventorySyncer>();
        if (playerSyncer != null) {
            PlayerInventorySyncer.OnContainerOpened += HandleContainerOpen; // Static events are tricky, direct ref better if possible
            PlayerInventorySyncer.OnContainerClosed += HandleContainerClose;
        } else { Debug.LogError("PlayerInventorySyncer not found on owning player for container events!"); }


        // Inform ItemSelectionManager about hotbar size
        _itemSelectionManager.Initialize(hotbarSize, _playerGameObject); // Pass player object
        // OR itemSelectionManager.Init(manager.gameObject, manager);

        inventoryPanel.SetActive(true);
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
    }
    private void UnsubscribeFromEvents() {
        if (_UItoggleInventoryAction != null) _UItoggleInventoryAction.performed -= ToggleInventory;
        if (_uiInteractAction != null) _uiInteractAction.performed -= HandlePrimaryInteractionPerformed;
        if (_uiAltInteractAction != null) _uiAltInteractAction.performed -= HandleSecondaryInteractionPerformed;
        if (_uiCancelAction != null) _uiCancelAction.performed -= HandleCloseAction;
        if (_localInventoryManager != null) _localInventoryManager.OnSlotChanged -= UpdateSlotUI;
        if (_itemSelectionManager != null) _itemSelectionManager.OnSelectionChanged -= UpdateHotbarHighlight;
    }
    void Update() {
        if (!_heldItemStack.IsEmpty()) {
            draggingIconObject.SetActive(true);
            Vector2 cursorPos;
            if (_playerInput != null && _playerInput.currentControlScheme == "Gamepad" && _currentFocusedSlot != null) {
                // Snap to focused slot for gamepad
                cursorPos = _currentFocusedSlot.transform.position;
            } else if (_uiPointAction != null) { // Mouse
                cursorPos = _uiPointAction.ReadValue<Vector2>();
            } else { // Fallback for safety
                cursorPos = Input.mousePosition;
            }
            draggingIconObject.transform.position = cursorPos;
        } else {
            draggingIconObject.SetActive(false);
        }
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
        if (!IsOpen && !_heldItemStack.IsEmpty()) // Inventory closed BUT holding item means trying to drop to world
        {
            HandleDropToWorld(button);
            return;
        }
        if (!IsOpen) return; // Inventory not open and not holding item, do nothing
        InventorySlotUI clickedOrFocusedSlot = GetSlotUnderCursorOrFocused();

        if (!_heldItemStack.IsEmpty()) // Currently "holding" an item
        {
            if (clickedOrFocusedSlot != null) // Clicked on a slot while holding
            {
                HandlePlaceHeldItem(clickedOrFocusedSlot, button);
            } else // Clicked outside slots (but inside UI panel boundary) while holding
              {
                if (EventSystem.current.IsPointerOverGameObject()) // Check if over ANY UI
                 {
                    // Clicked on empty UI space, possibly return item or do nothing
                    Debug.Log("Clicked empty UI space while holding. Returning item.");
                    ReturnHeldItemToSource();
                    _heldItemStack.Clear();
                } else {
                    // Clicked outside ALL UI while holding item
                    HandleDropToWorld(button);
                }
            }
        } else // Not holding an item, trying to pick up from slot
          {
            if (clickedOrFocusedSlot != null) {
                HandlePickupFromSlot(clickedOrFocusedSlot, button);
            }
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
    // --- Pickup/Place/Drop Logic ---

    private void HandlePickupFromSlot(InventorySlotUI slotUI, PointerEventData.InputButton button) {
        PlayerInventorySyncer playerSyncer = _playerGameObject.GetComponent<PlayerInventorySyncer>();
        if (playerSyncer == null) return;

        int quantityToGrab = 0;
        ushort itemIDToGrab = ResourceSystem.InvalidID;
        InventorySlot sourceDataSlot = null;

        if (!slotUI.IsContainerSlot) // Picking from player inventory
        {
            sourceDataSlot = _localInventoryManager.GetSlot(slotUI.SlotIndex);
            if (sourceDataSlot == null || sourceDataSlot.IsEmpty()) return; // Clicked empty player slot

            itemIDToGrab = sourceDataSlot.itemID;
            if (button == PointerEventData.InputButton.Left) { // Left click / Gamepad A
                quantityToGrab = sourceDataSlot.quantity;
            } else if (button == PointerEventData.InputButton.Right) { // Right click / Gamepad X
                quantityToGrab = Mathf.CeilToInt((float)sourceDataSlot.quantity / 2f);
            }

            // Optimistic client-side removal (visual only)
            _localInventoryManager.RemoveItem(slotUI.SlotIndex, quantityToGrab); // Update UI
            // The actual server request will happen when placing or dropping.

            _heldItemStack.SetItem(itemIDToGrab, quantityToGrab, slotUI.SlotIndex);
        } else // Picking from container
          {
            if (currentlyViewedContainer == null) return;
            sourceDataSlot = currentlyViewedContainer.GetSlotReadOnly(slotUI.SlotIndex);
            if (sourceDataSlot == null || sourceDataSlot.IsEmpty()) return; // Clicked empty container slot

            itemIDToGrab = sourceDataSlot.itemID;
            if (button == PointerEventData.InputButton.Left) {
                quantityToGrab = sourceDataSlot.quantity;
            } else if (button == PointerEventData.InputButton.Right) {
                quantityToGrab = Mathf.CeilToInt((float)sourceDataSlot.quantity / 2f);
            }

            // Request server to "hold" this (actually means remove from container and give to player's "hand")
            // This is a complex server operation if we truly want a server-side "held" state.
            // Simpler: Client "takes" it visually, server processes transfer when item is "placed".
            playerSyncer.RequestMoveItemToPlayer(slotUI.SlotIndex, -1, quantityToGrab); // -1 playerIdx means "hold"
                                                                                        // Server needs to handle this new "pickup to hold" intent.
                                                                                        // For now, let's predict & update locally, and when PLACING,
                                                                                        // Player -> Player uses swap, Container -> Player uses the already existing RequestMoveItemToPlayer.

            // Client-side visual update for container (it will be corrected by server eventually)
            InventorySlot tempVisualSlot = new InventorySlot(itemIDToGrab, sourceDataSlot.quantity - quantityToGrab);
            slotUI.UpdateUI(tempVisualSlot); // Show remaining, or empty if all taken

            _heldItemStack.SetItem(itemIDToGrab, quantityToGrab, -1, true, slotUI.SlotIndex);
        }
        Debug.Log($"Picked up: ID {itemIDToGrab}, Qty {quantityToGrab} from {(slotUI.IsContainerSlot ? "Container" : "Player")} Slot {slotUI.SlotIndex}");
    }
    private void HandlePlaceHeldItem(InventorySlotUI targetSlotUI, PointerEventData.InputButton button) {
        PlayerInventorySyncer playerSyncer = _playerGameObject.GetComponent<PlayerInventorySyncer>();
        if (playerSyncer == null || _heldItemStack.IsEmpty()) return;

        ushort heldItemID = _heldItemStack.itemID;
        int quantityToPlace = 0;

        if (button == PointerEventData.InputButton.Left) { // Place all held / Gamepad A
            quantityToPlace = _heldItemStack.quantity;
        } else if (button == PointerEventData.InputButton.Right) { // Place one / Gamepad X or B
            quantityToPlace = 1;
        }
        if (quantityToPlace > _heldItemStack.quantity) quantityToPlace = _heldItemStack.quantity; // Cannot place more than held

        // --- Determine Action based on Source and Target ---
        if (!_heldItemStack.isFromContainer && !targetSlotUI.IsContainerSlot) { // Player Inventory -> Player Inventory
            // This effectively becomes a swap or merge.
            // If target is empty or same item:
            //   Local predict: add quantityToPlace to target, remove from held.
            //   Server: Need an RPC like CmdMovePlayerItemToPlayerSlot(heldItemID, quantityToPlace, _heldItemStack.originalSourceSlotIndex, targetSlotUI.SlotIndex)
            //   The CmdMoveItemToContainer/Player might need to be generalized or new ones created.
            //   For now, simplest is a full SWAP request if different items, or MERGE if same.

            InventorySlot targetDataSlot = _localInventoryManager.GetSlot(targetSlotUI.SlotIndex);
            if (targetDataSlot.IsEmpty() || targetDataSlot.itemID == heldItemID) // Target empty or same item
            {
                // Optimistic Local Update
                _localInventoryManager.AddItem(heldItemID, quantityToPlace, targetSlotUI.SlotIndex); // Target slot gets items
                _heldItemStack.quantity -= quantityToPlace;
                playerSyncer.RequestMergePlayerItem(_heldItemStack.originalSourceSlotIndex, targetSlotUI.SlotIndex, heldItemID, quantityToPlace);

            } else { // Different items, request SWAP
                     // Return what's in targetSlot to heldItem's original slot, then place heldItem in targetSlot
                Debug.Log("Player -> Player (Different Items): Complex SWAP required. Server authoritative SWAP needed.");
                // Put held item back visually for now
                ReturnHeldItemToSource(); // Original source gets held item back
                                          // Server will handle the actual swap via RPC if we build it, then update client
                playerSyncer.RequestSwapPlayerSlots(_heldItemStack.originalSourceSlotIndex, targetSlotUI.SlotIndex);
            }


        } else if (!_heldItemStack.isFromContainer && targetSlotUI.IsContainerSlot) { // Player Inventory -> Container
            playerSyncer.RequestMoveItemToContainer(_heldItemStack.originalSourceSlotIndex, targetSlotUI.SlotIndex, quantityToPlace);
            // Client predict: remove from originalSourceSlot, UI for container updates on server ack.
            _heldItemStack.quantity -= quantityToPlace; // Assume server will succeed for prediction


        } else if (_heldItemStack.isFromContainer && !targetSlotUI.IsContainerSlot) { // Container -> Player Inventory
            playerSyncer.RequestMoveItemToPlayer(_heldItemStack.originalContainerSlotIndex, targetSlotUI.SlotIndex, quantityToPlace);
            // Client predict: nothing really, UI for player inventory updates on server ack.
            _heldItemStack.quantity -= quantityToPlace; // Assume server will succeed for prediction

        } else if (_heldItemStack.isFromContainer && targetSlotUI.IsContainerSlot) { // Container -> Container
            playerSyncer.RequestSwapContainerSlots(_heldItemStack.originalContainerSlotIndex, targetSlotUI.SlotIndex, currentlyViewedContainer.NetworkObject); // Full swap is simplest for now
                                                                                                                                                               // Client predict: Nothing, wait for SyncList.
                                                                                                                                                               // If placing only one, then heldItemStack still has rest.
            _heldItemStack.quantity -= quantityToPlace; // This needs more thought for container->container partial place

        }


        if (_heldItemStack.quantity <= 0) {
            _heldItemStack.Clear();
        }
        Debug.Log($"Placed Item. Held now: {_heldItemStack.quantity}");
    }

    private void HandleDropToWorld(PointerEventData.InputButton button) {
        PlayerInventorySyncer playerSyncer = _playerGameObject.GetComponent<PlayerInventorySyncer>();
        if (playerSyncer == null || _heldItemStack.IsEmpty()) return;

        int quantityToDrop = 0;
        if (button == PointerEventData.InputButton.Left) { // Drop all held
            quantityToDrop = _heldItemStack.quantity;
        } else if (button == PointerEventData.InputButton.Right) { // Drop one
            quantityToDrop = 1;
        }
        if (quantityToDrop > _heldItemStack.quantity) quantityToDrop = _heldItemStack.quantity;


        if (_heldItemStack.isFromContainer) {
            // If item was originally from a container, trying to drop it means it effectively
            // needs to be moved to player's inventory (server-side) and then dropped from there.
            // This is complex server logic to chain.
            // Simplification: Server "gives" item to player's invisible hand (no actual slot), then player drops from hand.
            Debug.LogWarning("Dropping item originally from container to world - needs refined server logic.");
            // For now, let's assume it's like it was in player inv and then dropped:
            playerSyncer.RequestDropHeldItemFromContainer(_heldItemStack.originalContainerSlotIndex, _heldItemStack.itemID, quantityToDrop);

        } else { // Dropping from player's "hand" (originally from player inventory)
            playerSyncer.RequestDropItemFromSlot(_heldItemStack.originalSourceSlotIndex, quantityToDrop);
        }


        // Optimistic client-side update of held stack
        _heldItemStack.quantity -= quantityToDrop;
        if (_heldItemStack.quantity <= 0) {
            _heldItemStack.Clear();
        }
        Debug.Log($"Requested Drop to World. Held now: {_heldItemStack.quantity}");
    }

    private void ReturnHeldItemToSource() {
        if (_heldItemStack.IsEmpty()) return;

        if (!_heldItemStack.isFromContainer && _heldItemStack.originalSourceSlotIndex != -1) {
            // Return to player inventory slot
            _localInventoryManager.AddItem(_heldItemStack.itemID, _heldItemStack.quantity, _heldItemStack.originalSourceSlotIndex);
            // Server doesn't need to be told about this client-side cancel & visual return,
            // as no persistent change was requested yet.
        } else if (_heldItemStack.isFromContainer && _heldItemStack.originalContainerSlotIndex != -1) {
            // Return to container slot (visual only, server will correct if necessary)
            // This is tricky because client can't modify container directly.
            // Best to just clear held and let server state prevail.
            // The visual update on container will lag until next server message or player places successfully.
            Debug.Log("Returning item from container is complex for client prediction. Clearing held stack.");
        }
        _heldItemStack.Clear(); // Clear after returning or deciding not to predict return
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
        if (!_heldItemStack.IsEmpty()) {
            Debug.Log("Close Action: Returning held item.");
            ReturnHeldItemToSource(); // Or just clear if no return logic
            _heldItemStack.Clear();
        } else if (IsOpen) {
            // If inventory is open and nothing held, maybe toggle it closed
            ToggleInventory(new InputAction.CallbackContext()); // Pass dummy context
        }
    }
    private void ToggleInventory(InputAction.CallbackContext context) {
        isExpanded = !isExpanded;
        if (isExpanded) {
            //inventoryPanel.SetActive(setEnabled);
            inventoryPanel.GetComponent<RectTransform>().DOMoveY(0, 0.2f);
        } else {
            inventoryPanel.GetComponent<RectTransform>().DOMoveY(-292, 0.2f).OnComplete(SetInvUnactive);
            // wait...
            EventSystem.current.SetSelectedGameObject(null); // Deselect UI when closing
            if (!_heldItemStack.IsEmpty()) ReturnHeldItemToSource(); // Return held item if inventory closes
        }
        // Optional: You might want to pause game, lock cursor, etc. when inventory is open
        Debug.Log($"Inventory Toggled: {inventoryPanel.activeSelf}");

        // If closing while dragging, cancel the drag
        if (!inventoryPanel.activeSelf && isDragging) {
            EndDrag(true); // Force cancel
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
        slotUIs.Clear();
        for (int i = 0; i < hotbarSize; i++) {
            GameObject slotGO = Instantiate(slotPrefab, slotHotbarContainer);
            slotGO.name = $"Slot_{i}"; // For easier debugging

            InventorySlotUI slotUI = slotGO.GetComponent<InventorySlotUI>();
            if (slotUI != null) {
                slotUI.Initialize(this, i); // Pass reference to this manager and the slot index
                slotUIs.Add(slotUI);
                UpdateSlotUI(i); // Update visual state immediately
            } else {
                Debug.LogError($"Slot prefab '{slotPrefab.name}' is missing InventorySlotUI component!");
            }
        }
        // Instantiate UI slots based on inventory size
        for (int i = hotbarSize; i < _localInventoryManager.InventorySize; i++) {
            GameObject slotGO = Instantiate(slotPrefab, slotInvContainer);
            slotGO.name = $"Slot_{i}"; // For easier debugging

            InventorySlotUI slotUI = slotGO.GetComponent<InventorySlotUI>();
            if (slotUI != null) {
                slotUI.Initialize(this, i); // Pass reference to this manager and the slot index
                slotUIs.Add(slotUI);
                UpdateSlotUI(i); // Update visual state immediately
            } else {
                Debug.LogError($"Slot prefab '{slotPrefab.name}' is missing InventorySlotUI component!");
            }
        }

        // TODO will have to do this in two passes just like above because inventory is not a perfect grid
        var columnCount = 4;
        for (int i = 0; i < slotUIs.Count; i++) {
            Selectable currentSel = slotUIs[i].GetComponent<Selectable>();
            if (currentSel == null) continue;

            Navigation nav = currentSel.navigation;
            nav.mode = Navigation.Mode.Explicit;

            // Up
            if (i >= columnCount) nav.selectOnUp = slotUIs[i - columnCount].GetComponent<Selectable>();
            else nav.selectOnUp = null; // Or wrap around to bottom?
                                        // Down
            if (i < slotUIs.Count - columnCount) nav.selectOnDown = slotUIs[i + columnCount].GetComponent<Selectable>();
            else nav.selectOnDown = null; // Or wrap to top?
                                          // Left
            if (i % columnCount != 0) nav.selectOnLeft = slotUIs[i - 1].GetComponent<Selectable>();
            else nav.selectOnLeft = null; // Or wrap to right end of prev row?
                                          // Right
            if ((i + 1) % columnCount != 0 && (i + 1) < slotUIs.Count) nav.selectOnRight = slotUIs[i + 1].GetComponent<Selectable>();
            else nav.selectOnRight = null; // Or wrap to left end of next row?

            currentSel.navigation = nav;
        }
        if (IsOpen && slotUIs.Count > 0 && _playerInput != null && _playerInput.currentControlScheme == "Gamepad") {
            EventSystem.current.SetSelectedGameObject(slotUIs[0].gameObject);
        }

        UpdateHotbarHighlight(_itemSelectionManager.SelectedSlotIndex); 
        Debug.Log($"Created {slotUIs.Count} UI slots.");
    }

    // Updates the visual representation of a single slot
    void UpdateSlotUI(int slotIndex) {
        if (slotIndex >= 0 && slotIndex < slotUIs.Count) {
            InventorySlot slotData = _localInventoryManager.GetSlot(slotIndex);
            slotUIs[slotIndex].UpdateUI(slotData);
            if (slotIndex < hotbarSize) {
                slotUIs[slotIndex].SetSelected(slotIndex == _itemSelectionManager.SelectedSlotIndex && IsOpen);
            }
        }
    }

    // --- Drag and Drop Handling ---

    public void BeginDrag(int slotIndex) {
        InventorySlot sourceSlot = _localInventoryManager.GetSlot(slotIndex);
        if (sourceSlot == null || sourceSlot.IsEmpty()) return; // Can't drag empty slot

        isDragging = true;
        dragSourceIndex = slotIndex;

        // Update dragging icon
        //draggingIconImage.sprite = sourceSlot.itemData.icon;
        draggingIconImage.color = Color.white; // Make sure it's visible
        draggingIconObject.SetActive(true);
        Vector2 mousePos = Pointer.current != null ? Pointer.current.position.ReadValue() : (Vector2)Input.mousePosition;
        draggingIconObject.transform.position = mousePos; // Position at mouse immediately

        // Optionally make the source slot slightly transparent or hide its icon
        slotUIs[slotIndex].SetVisualsDuringDrag(true);

        Debug.Log($"Begin Drag: Slot {slotIndex}");
    }

    public void EndDrag(bool cancelled = false) {
        if (!isDragging) return;

        // If the drag wasn't cancelled and didn't end on a valid slot (handled by OnDrop),
        // it means it ended somewhere else (e.g., outside inventory).
        // For now, we just cancel/reset the drag. Later, this could trigger dropping the item.
        if (!cancelled) {
            // Check if the pointer is over any UI element at all using EventSystem
            // This is a simple check; more robust checks might involve raycasting UI specifically
            if (UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject()) {
                // Drag ended over some UI, but not necessarily a slot (handled by OnDrop)
                // Assume cancellation / return to original slot for now
                Debug.Log("End Drag: Over UI, but not a slot. Resetting.");
            } else {
                // Drag ended outside of *any* UI. Potential drop location.
                // For now, just reset. Add drop logic later.
                Debug.Log("End Drag: Outside UI. Potential Drop Location. Resetting for now.");
               // _localInventoryManager.GetPlayerInvSyncer().HandleDropInput(dragSourceIndex, 9999);
            }

        }
        // Reset visuals on the source slot if it's still valid

        draggingIconObject.SetActive(false);
        draggingIconImage.sprite = null;

        if (_localInventoryManager.IsValidIndex(dragSourceIndex)) {
            slotUIs[dragSourceIndex].SetVisualsDuringDrag(false, _droppedSameSlot);
            if (_droppedSameSlot) _droppedSameSlot = false;
        }
        // Reset dragging state
        isDragging = false;
        dragSourceIndex = -1;
        Debug.Log("End Drag: Resetting State.");
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
            slotUIs[dragSourceIndex].SetVisualsDuringDrag(false);
            // No actual data change needed
            return; // Important: Don't proceed to swap!
        }


        // Perform the swap in the data manager
        _localInventoryManager.SwapSlots(dragSourceIndex, dropTargetIndex);

        // EndDrag will be called automatically by the Input System / EventSystem
        // after OnDrop finishes. It handles resetting the drag state.
        // We don't need to call EndDrag() manually here.
    }

    private void UpdateHotbarHighlight(int newSelectedIndex) {
        // Only show highlight if the inventory panel is open
        bool showHighlight = IsOpen;

        for (int i = 0; i < hotbarSize; i++) {
            if (i < slotUIs.Count && slotUIs[i] != null) // Ensure slot UI exists
            {
                slotUIs[i].SetSelected(showHighlight && i == newSelectedIndex);
            }
        }
        Debug.Log($"Updated Hotbar Highlight. Selected: {newSelectedIndex}, Inventory Open: {showHighlight}");
    }

    // --- Container UI Handling ---

    private void HandleContainerOpen(SharedContainer containerToView) {
        if (!containerPanel || !containerSlotContainer) return; // Container UI not setup

        currentlyViewedContainer = containerToView;
        if (currentlyViewedContainer == null) {
            HandleContainerClose(); // Close UI if container becomes null somehow
            return;
        }

        Debug.Log($"[UI] Opening Container View: {containerToView.name}");


        // Subscribe to changes for THIS container instance
        currentlyViewedContainer.OnItemsChanged += UpdateContainerUI;


        // --- Setup Container UI ---
        // Clear old slots
        foreach (Transform child in containerSlotContainer) { Destroy(child.gameObject); }
        containerSlotUIs.Clear();

        // Set Title (Optional)
        if (containerTitleText) containerTitleText.text = containerToView.name; // Or a generic title

        // Create new slots
        for (int i = 0; i < currentlyViewedContainer.ContainerSize; ++i) {
            GameObject slotGO = Instantiate(slotPrefab, containerSlotContainer);
            slotGO.name = $"ContainerSlot_{i}";
            InventorySlotUI slotUI = slotGO.GetComponent<InventorySlotUI>();
            if (slotUI != null) {
                // *** IMPORTANT: We need a way for InventorySlotUI to know IF it's a container slot ***
                // Modify Initialize slightly or add a new method/flag
                // Option A: Modify Initialize
                // slotUI.Initialize(this, i, true); // Add isContainerSlot flag
                // Option B: Add SetContainerContext method
                slotUI.SetContainerContext(this, i); // Let's choose this

                containerSlotUIs.Add(slotUI);
                // UpdateSlotUI below will handle initial visuals
            } else { Debug.LogError($"Slot prefab missing InventorySlotUI!"); }
        }


        // Update visuals immediately based on current container state
        UpdateContainerUI();


        // Make the container panel visible
        containerPanel.SetActive(true);


        // Ensure player inventory is ALSO open when container is open
        if (!inventoryPanel.activeSelf) {
            inventoryPanel.SetActive(true);
            UpdateHotbarHighlight(_itemSelectionManager.SelectedSlotIndex); // Update highlights if opened this way
        }
    }

    private void HandleContainerClose() {
        if (!containerPanel) return;

        Debug.Log("[UI] Closing Container View");
        containerPanel.SetActive(false);


        // Unsubscribe from previous container
        if (currentlyViewedContainer != null) {
            currentlyViewedContainer.OnItemsChanged -= UpdateContainerUI;
        }
        currentlyViewedContainer = null;


        // Clear UI Slots
        foreach (Transform child in containerSlotContainer) { Destroy(child.gameObject); }
        containerSlotUIs.Clear();
    }


    // Called when the currently viewed container's SyncList changes
    private void UpdateContainerUI() {
        if (currentlyViewedContainer == null || !containerPanel.activeSelf) return;

        Debug.Log($"[UI] Updating Container UI for {currentlyViewedContainer.name}");
        for (int i = 0; i < containerSlotUIs.Count && i < currentlyViewedContainer.ContainerSize; i++) {
            InventorySlot slotData = currentlyViewedContainer.GetSlotReadOnly(i);
            containerSlotUIs[i].UpdateUI(slotData); // Use existing UpdateUI
            containerSlotUIs[i].SetSelected(false); // Container slots usually aren't 'selected' like hotbar
        }
    }

     public void HandleDrop(InventorySlotUI sourceSlotUI, InventorySlotUI targetSlotUI) {
        if (!IsCurrentlyDragging() || sourceSlotUI == null || targetSlotUI == null) {
            Debug.LogWarning("HandleDrop called with invalid state or null slots.");
            if (IsCurrentlyDragging()) EndDrag(true); // Cancel drag if something is wrong
            return;
        }


        // Determine source and target panels using the flags on the UI components
        bool sourceIsPlayer = !sourceSlotUI.IsContainerSlot;
        bool targetIsPlayer = !targetSlotUI.IsContainerSlot;
        bool targetIsContainer = targetSlotUI.IsContainerSlot;


        // We currently only allow dragging FROM player slots (sourceIsPlayer should always be true)
        if (!sourceIsPlayer) {
            Debug.LogError("HandleDrop initiated but source was not a player slot (Drag should have been prevented). Cancelling.");
            EndDrag(true);
            return;
        }


        int sourceActualIndex = sourceSlotUI.SlotIndex; // Index within player inventory
        int targetActualIndex = targetSlotUI.SlotIndex; // Index within player OR container


        Debug.Log($"Handle Drop: Source Panel: Player (Index {sourceActualIndex}) -> Target Panel: {(targetIsPlayer ? "Player" : "Container")} (Index {targetActualIndex})");


        // --- Logic ---
        if (targetIsPlayer) {
            // Player -> Player: Request Swap
            if (sourceActualIndex != targetActualIndex) { // Check if dropping onto a *different* player slot
                Debug.Log($"Requesting Player Swap: {sourceActualIndex} <-> {targetActualIndex}");
                _playerSyncer.RequestSwapPlayerSlots(sourceActualIndex, targetActualIndex);


                // Optional: Predict locally?
                // inventoryManager.SwapSlots(sourceActualIndex, targetActualIndex);
                // UpdateHotbarHighlight(itemSelectionManager.SelectedSlotIndex);
            } else {
                // If dropped onto the same slot
                _droppedSameSlot = true;
            }


        } else if (targetIsContainer) {
            // Player -> Container: Request move item
            Debug.Log($"Requesting Move Player[{sourceActualIndex}] -> Container[{targetActualIndex}]");


            InventorySlot sourceData = _localInventoryManager.GetSlot(sourceActualIndex);
            SharedContainer targetContainer = currentlyViewedContainer; // Use the cached container reference


            if (sourceData != null && !sourceData.IsEmpty() && targetContainer != null) {
                // Request move of entire stack quantity for simplicity
                _playerSyncer.RequestMoveItemToContainer(sourceActualIndex, targetActualIndex, sourceData.quantity);


                // Optional: Predict removal from player inventory locally?
                // inventoryManager.RemoveItem(sourceActualIndex, sourceData.quantity);
                // UpdateHotbarHighlight(itemSelectionManager.SelectedSlotIndex);
            } else {
                Debug.LogWarning("Cannot move item to container: Invalid source data or target container not open/found.");
            }


        } else {
            Debug.LogWarning("Unhandled drop target scenario.");
        }


        // EndDrag state reset will happen automatically via EventSystem calling EndDrag on the source slot
    }


    public bool IsCurrentlyDragging() => isDragging;
}
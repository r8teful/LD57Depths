// InventoryUIManager.cs
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI; // Required for UI elements like Panel
using UnityEngine.InputSystem;
using DG.Tweening;
using TMPro; 

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
    [Header("Input")]
    [SerializeField] private InputActionReference toggleInventoryAction; // Assign your toggle input action asset

    [SerializeField] private int hotbarSize = 5; // How many slots in the first row act as hotbar

    [Header("References")]
    private InventoryManager inventoryManager; // Reference to the data manager
    [SerializeField] private ItemSelectionManager itemSelectionManager; // Reference needed
    // --- Runtime ---
    private List<InventorySlotUI> slotUIs = new List<InventorySlotUI>();
    private Image draggingIconImage;
    private bool isDragging = false;
    private bool isExpanded = false;
    private int dragSourceIndex = -1;

    // --- Properties ---
    public bool IsOpen => inventoryPanel != null && inventoryPanel.activeSelf;
    public int HotbarSize => hotbarSize; // Expose hotbar size
    
    public void Init(InventoryManager manager) {
        inventoryManager = manager;
        itemSelectionManager.Init(manager.gameObject, manager);
    }
    
    void Start() {
        if (!inventoryPanel || !slotInvContainer || !slotPrefab || !draggingIconObject) {
            Debug.LogError("One or more UI element references are missing on InventoryUIManager!");
            enabled = false; // Disable script if essential references are missing
            return;
        }


        // Initialize dragging icon
        draggingIconImage = draggingIconObject.GetComponent<Image>();
        if (draggingIconImage == null) {
            Debug.LogError("Dragging Icon Object must have an Image component!");
            enabled = false;
            return;
        }
        draggingIconObject.SetActive(true);

        // Subscribe to inventory events
        inventoryManager.OnSlotChanged += UpdateSlotUI;      // Update specific UI slot when data changes
        itemSelectionManager.OnSelectionChanged += UpdateHotbarHighlight;
        CreateSlotUIs();
        // Initial state
        inventoryPanel.SetActive(true);
        itemSelectionManager.Initialize(hotbarSize);
        // Input setup
        if (toggleInventoryAction != null) {
            toggleInventoryAction.action.performed += ToggleInventory;
            toggleInventoryAction.action.Enable();
        } else {
            Debug.LogWarning("Toggle Inventory Action not assigned.");
        }
    }

    void OnDestroy() // Unsubscribe from events when destroyed
   {
        if (inventoryManager != null) {
            inventoryManager.OnSlotChanged -= UpdateSlotUI;
        }
        if (toggleInventoryAction != null) {
            toggleInventoryAction.action.performed -= ToggleInventory;
        }
    }


    private void Update() {
        // Update dragging icon position if dragging
        if (isDragging) {
            // Using new Input System Pointer position
            Vector2 mousePos = Pointer.current != null ? Pointer.current.position.ReadValue() : (Vector2)Input.mousePosition;
            draggingIconObject.transform.position = mousePos;
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
        for (int i = hotbarSize; i < inventoryManager.InventorySize; i++) {
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
        Debug.Log($"Created {slotUIs.Count} UI slots.");
    }

    // Updates the visual representation of a single slot
    void UpdateSlotUI(int slotIndex) {
        if (slotIndex >= 0 && slotIndex < slotUIs.Count) {
            InventorySlot slotData = inventoryManager.GetSlot(slotIndex);
            slotUIs[slotIndex].UpdateUI(slotData);
            if (slotIndex < hotbarSize) {
                slotUIs[slotIndex].SetSelected(slotIndex == itemSelectionManager.SelectedSlotIndex && IsOpen);
            }
        }
    }

    // --- Drag and Drop Handling ---

    public void BeginDrag(int slotIndex) {
        InventorySlot sourceSlot = inventoryManager.GetSlot(slotIndex);
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
            }

        }

        // Reset visuals on the source slot if it's still valid
        if (inventoryManager.IsValidIndex(dragSourceIndex)) {
            slotUIs[dragSourceIndex].SetVisualsDuringDrag(false);
        }

        // Reset dragging state
        isDragging = false;
        dragSourceIndex = -1;
        draggingIconObject.SetActive(false);
        draggingIconImage.sprite = null;

        Debug.Log("End Drag: Resetting State.");
    }


    // Called by InventorySlotUI when an item is dropped onto it
    public void HandleDrop(int dropTargetIndex) {
        if (!isDragging || dragSourceIndex == -1 || !inventoryManager.IsValidIndex(dropTargetIndex)) {
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
        inventoryManager.SwapSlots(dragSourceIndex, dropTargetIndex);

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
            UpdateHotbarHighlight(itemSelectionManager.SelectedSlotIndex); // Update highlights if opened this way
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


    // --- Modify Drag and Drop to handle Player <-> Container ---

/*    public void HandleDrop(int dropTargetIndex) { // Keep this method name for InventorySlotUI compatibility
        if (!isDragging || dragSourceIndex == -1) return;


        InventorySlotUI sourceSlotUI = GetSlotUIByIndex(dragSourceIndex);
        InventorySlotUI targetSlotUI = GetSlotUIByIndex(dropTargetIndex);


        if (sourceSlotUI == null || targetSlotUI == null) {
            Debug.LogError($"Drop error: Couldn't find UI for indices {dragSourceIndex} or {dropTargetIndex}");
            EndDrag(true); // Cancel if something went wrong
            return;
        }


        // Determine source and target panels (Player or Container)
        bool sourceIsPlayer = slotUIs.Contains(sourceSlotUI);
        bool sourceIsContainer = containerSlotUIs.Contains(sourceSlotUI);
        bool targetIsPlayer = slotUIs.Contains(targetSlotUI);
        bool targetIsContainer = containerSlotUIs.Contains(targetSlotUI);

        int sourceActualIndex = sourceSlotUI.SlotIndex; // Get the index *within its panel*
        int targetActualIndex = targetSlotUI.SlotIndex;


        Debug.Log($"Handle Drop: Source Panel: {(sourceIsPlayer ? "Player" : (sourceIsContainer ? "Container" : "Unknown"))} (Index {sourceActualIndex}) -> Target Panel: {(targetIsPlayer ? "Player" : (targetIsContainer ? "Container" : "Unknown"))} (Index {targetActualIndex})");


        // --- Logic ---
        PlayerInventorySyncer playerSyncer = FindObjectOfType<PlayerInventorySyncer>(); // Get the syncer


        if (sourceIsPlayer && targetIsPlayer) {
            // Player -> Player: Standard Swap (using ServerRpc now if not predicting)
            // For simplicity let's ASSUME InventoryManager.SwapSlots is LOCAL ONLY now.
            // We need an RPC to request a swap.
            // inventoryManager.SwapSlots(sourceActualIndex, targetActualIndex); // OLD LOCAL WAY

            // NEW RPC WAY (TODO: Implement CmdSwapPlayerSlots on PlayerInventorySyncer)
            // playerSyncer?.RequestSwapPlayerSlots(sourceActualIndex, targetActualIndex);
            // --- For now, let's just keep the LOCAL swap for responsiveness ---
            if (dragSourceIndex != dropTargetIndex) { // Ensure indices are mapped correctly if lists differ
                inventoryManager.SwapSlots(sourceActualIndex, targetActualIndex);
                // Highlight update might be needed if swapping in/out of hotbar
                UpdateHotbarHighlight(itemSelectionManager.SelectedSlotIndex);
            }


        } else if (sourceIsContainer && targetIsContainer) {
            // Container -> Container: Request server swap (cannot do locally easily with SyncList)
            Debug.Log("Container -> Container swap requested (Needs Server RPC)");
            playerSyncer?.RequestSwapContainerSlots(sourceActualIndex, targetActualIndex, currentlyViewedContainer.NetworkObject); // TODO: Implement this RPC


        } else if (sourceIsPlayer && targetIsContainer) {
            // Player -> Container: Request move item (Pass player index, container index, quantity=all for now)
            Debug.Log($"Requesting Move Player[{sourceActualIndex}] -> Container[{targetActualIndex}]");
            // Get quantity from source slot BEFORE potential local prediction happens
            InventorySlot sourceData = inventoryManager.GetSlot(sourceActualIndex);
            if (sourceData != null && !sourceData.IsEmpty()) {
                playerSyncer?.RequestMoveItemToContainer(sourceActualIndex, targetActualIndex, sourceData.quantity);
            }


        } else if (sourceIsContainer && targetIsPlayer) {
            // Container -> Player: Request move item (Pass container index, player index, quantity=all for now)
            Debug.Log($"Requesting Move Container[{sourceActualIndex}] -> Player[{targetActualIndex}]");
            InventorySlot sourceData = currentlyViewedContainer?.GetSlotReadOnly(sourceActualIndex);
            if (sourceData != null && !sourceData.IsEmpty()) {
                playerSyncer?.RequestMoveItemToPlayer(sourceActualIndex, targetActualIndex, sourceData.quantity);
            }
        } else {
            Debug.LogWarning("Unhandled drag/drop scenario.");
        }
        // EndDrag state reset will happen automatically via EventSystem
    }
*/
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
        PlayerInventorySyncer playerSyncer = FindFirstObjectByType<PlayerInventorySyncer>(); // TODO: Cache this reference


        if (targetIsPlayer) {
            // Player -> Player: Request Swap
            if (sourceActualIndex != targetActualIndex) { // Check if dropping onto a *different* player slot
                Debug.Log($"Requesting Player Swap: {sourceActualIndex} <-> {targetActualIndex}");
                playerSyncer.RequestSwapPlayerSlots(sourceActualIndex, targetActualIndex);


                // Optional: Predict locally?
                // inventoryManager.SwapSlots(sourceActualIndex, targetActualIndex);
                // UpdateHotbarHighlight(itemSelectionManager.SelectedSlotIndex);
            }
            // If dropped onto the same slot, do nothing (swap isn't needed)


        } else if (targetIsContainer) {
            // Player -> Container: Request move item
            Debug.Log($"Requesting Move Player[{sourceActualIndex}] -> Container[{targetActualIndex}]");


            InventorySlot sourceData = inventoryManager.GetSlot(sourceActualIndex);
            SharedContainer targetContainer = currentlyViewedContainer; // Use the cached container reference


            if (sourceData != null && !sourceData.IsEmpty() && targetContainer != null) {
                // Request move of entire stack quantity for simplicity
                playerSyncer?.RequestMoveItemToContainer(sourceActualIndex, targetActualIndex, sourceData.quantity);


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
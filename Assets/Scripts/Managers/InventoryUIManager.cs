// InventoryUIManager.cs
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI; // Required for UI elements like Panel
using UnityEngine.InputSystem;
using DG.Tweening; // Using new Input System for toggle

public class InventoryUIManager : MonoBehaviour {
    [Header("UI Elements")]
    [SerializeField] private GameObject inventoryPanel; // The main inventory window panel
    [SerializeField] private Transform slotInvContainer; // Parent transform where UI slots will be instantiated
    [SerializeField] private Transform slotHotbarContainer; // Parent transform where UI slots will be instantiated
    [SerializeField] private GameObject slotPrefab; // Prefab for a single inventory slot UI element
    [SerializeField] private GameObject draggingIconObject; // A UI Image used to show the item being dragged

    [Header("Input")]
    [SerializeField] private InputActionReference toggleInventoryAction; // Assign your toggle input action asset

    [SerializeField] private int hotbarSize = 5; // How many slots in the first row act as hotbar

    [Header("References")]
    [SerializeField] private InventoryManager inventoryManager; // Reference to the data manager
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
    void Start() {
        if (!inventoryManager) {
            Debug.LogError("InventoryManager reference not set on InventoryUIManager!");
            inventoryManager = InventoryManager.Instance; // Attempt to get singleton instance
        }
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
        draggingIconImage.sprite = sourceSlot.itemData.icon;
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
    public bool IsCurrentlyDragging() => isDragging;
}
// InventorySlotUI.cs
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems; // Required for Drag Handlers
using TMPro; // Required for TextMeshPro

public class InventorySlotUI : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler {
    [Header("UI Elements")]
    [SerializeField] private Image itemIconImage; // Assign the child Image component for the icon
    [SerializeField] private TextMeshProUGUI quantityText; // Assign the child TextMeshProUGUI component
    [SerializeField] private Image highlightBorder; // Assign a child Image used as a border/outline
    [Header("Settings")]
    [SerializeField] private Color emptySlotColor = new Color(1, 1, 1, 0.5f); // Slightly transparent white when empty
    [SerializeField] private Color draggingMaskColor = new Color(0.5f, 0.5f, 0.5f, 0.5f); // Color when being dragged from

    // --- Runtime ---
    private InventoryUIManager uiManager;
    private int slotIndex = -1;
    private Image backgroundImage; // Reference to self image if needed for color changes

    private bool isContainerSlot = false; // New flag
    public int SlotIndex => slotIndex; 
    public bool IsContainerSlot => isContainerSlot; // Expose context flag
    void Awake() {
        backgroundImage = GetComponent<Image>(); // Get Image on this object if needed
        if (!itemIconImage || !quantityText) {
            Debug.LogError($"Slot UI on {gameObject.name} is missing references to Icon Image or Quantity Text!", gameObject);
        }
        if (!highlightBorder) {
            Debug.LogWarning($"Slot UI on {gameObject.name} has no highlight border assigned. Selection won't be visible.", gameObject);
        } else {
            highlightBorder.enabled = false; // Start hidden
        }
    }

    // Called by InventoryUIManager during setup
    public void Initialize(InventoryUIManager manager, int index) {
        uiManager = manager;
        slotIndex = index;
        isContainerSlot = false;
    }
    public void SetContainerContext(InventoryUIManager manager, int index) {
        uiManager = manager; // Still need manager for drag events
        slotIndex = index; // Index *within the container*
        isContainerSlot = true;
    }

    // Updates the visual elements based on the slot data
    public void UpdateUI(InventorySlot slotData) {
        if (slotData == null || slotData.IsEmpty()) {
            // Slot is empty
            itemIconImage.enabled = false;
            quantityText.enabled = false;
            if (backgroundImage) backgroundImage.color = emptySlotColor; // Make slot visually 'empty'
        } else {
            // Slot has an item
            itemIconImage.sprite = App.ResourceSystem.GetItemByID(slotData.itemID).icon; // We have to get it from here because
            // slotData doesn't store the image
            itemIconImage.enabled = true;
            quantityText.enabled = slotData.quantity > 1;
            if (quantityText.enabled) quantityText.text = slotData.quantity.ToString();
            if (backgroundImage) backgroundImage.color = Color.white; // Reset slot background color
        }

        // Reset drag mask just in case
        SetVisualsDuringDrag(false);
    }

    // Used to visually change the slot when its item is being dragged FROM it
    public void SetVisualsDuringDrag(bool isBeingDragged) {
        if (itemIconImage) {
            // Option 1: Just hide the icon
            // itemIconImage.enabled = !isBeingDragged;

            // Option 2: Make icon semi-transparent (use a color)
            itemIconImage.color = isBeingDragged ? draggingMaskColor : Color.white;
        }
        if (quantityText) {
            quantityText.enabled = !isBeingDragged && quantityText.text != "" && quantityText.text != "0" && quantityText.enabled; // Also re-check conditions
        }
    }
    public void SetSelected(bool isSelected) {
        if (highlightBorder != null) {
            highlightBorder.enabled = isSelected;
        }
    }

    // --- IBeginDragHandler ---
    public void OnBeginDrag(PointerEventData eventData) {
        // Only allow dragging with the primary button (usually left mouse)
        if (eventData.button != PointerEventData.InputButton.Left) return;
        if (!isContainerSlot && uiManager != null) {
            uiManager.BeginDrag(slotIndex); // Begin drag using player slot index
        } else if (isContainerSlot) {
            Debug.Log("Dragging directly from Container Slot is currently disabled.");
            // Optionally provide visual feedback that it's disabled.
            eventData.pointerDrag = null; // Prevent drag operation
        }
    }

    // --- IDragHandler ---
    public void OnDrag(PointerEventData eventData) {
        // The UI Manager handles updating the floating icon position in its Update()
        return;
    }

    // --- IEndDragHandler ---
    public void OnEndDrag(PointerEventData eventData) {
        // Only allow dragging with the primary button
        if (eventData.button != PointerEventData.InputButton.Left) return;

        if (uiManager != null) {
            if (uiManager.IsCurrentlyDragging())
                uiManager.EndDrag();
        }
    }

    // --- IDropHandler ---
    // Called on the slot UI element *receiving* the drop
    public void OnDrop(PointerEventData eventData) {
        // Only allow dropping with the primary button
        if (eventData.button != PointerEventData.InputButton.Left) return;
        GameObject draggedObject = eventData.pointerDrag;
        InventorySlotUI sourceSlotUI = draggedObject.GetComponent<InventorySlotUI>();
        // Check if we are actually dragging something from our inventory system
        if (uiManager != null && uiManager.IsCurrentlyDragging() && sourceSlotUI != null) {
            uiManager.HandleDrop(sourceSlotUI, this);
        } else {
            Debug.LogWarning($"OnDrop ignored: No active drag operation or missing source slot UI. Dragging? {uiManager.IsCurrentlyDragging()} Source? {sourceSlotUI != null}");
        }
    }
}
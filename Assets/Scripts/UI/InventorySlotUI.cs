// InventorySlotUI.cs
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems; // Required for Drag Handlers
using TMPro; // Required for TextMeshPro

public class InventorySlotUI : MonoBehaviour, IPointerClickHandler, ISelectHandler, IDeselectHandler {
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
    private bool isHotBarSlot;
    private bool focusBorder;

    public int SlotIndex => slotIndex; 
    public bool IsContainerSlot => isContainerSlot; // Expose context flag
    public bool IsHotBarSlot => isHotBarSlot; // Expose context flag
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
        var selectable = GetComponent<Selectable>();
        if (selectable == null) {
            Debug.LogError("Need selectable on component!");
            // Add Selectable if not present. Requires Image component on same object for default interaction.
            // selectable = gameObject.AddComponent<Selectable>();
            // selectable.transition = Selectable.Transition.None; // Or set up color tint/sprite swap
        }
    }

    // Called by InventoryUIManager during setup
    public void Initialize(InventoryUIManager manager, int index, bool isHotbar) {
        uiManager = manager;
        slotIndex = index;
        isContainerSlot = false;
        isHotBarSlot = isHotbar;
    }
    public void SetContainerContext(InventoryUIManager manager, int index) {
        uiManager = manager; // Still need manager for drag events
        slotIndex = index; // Index *within the container*
        isContainerSlot = true;
    }

    // Updates the visual elements based on the slot data
    public void UpdateSlot(InventorySlot slotData) {
        if (slotData == null || slotData.IsEmpty()) {
            // Slot is empty
            itemIconImage.enabled = false;
            quantityText.enabled = false;
            //if (backgroundImage) backgroundImage.color = emptySlotColor; // Make slot visually 'empty'
        } else {
            // Slot has an item
            itemIconImage.sprite = slotData.ItemData.icon; 
            itemIconImage.enabled = true;
            quantityText.enabled = slotData.quantity > 1;
            if (quantityText.enabled) quantityText.text = slotData.quantity.ToString();
          //  if (backgroundImage) backgroundImage.color = Color.white; // Reset slot background color
        }

        // Reset drag mask just in case
        SetVisualsDuringDrag(false);
    }

    // Used to visually change the slot when its item is being dragged FROM it
    public void SetVisualsDuringDrag(bool isBeingDragged, bool sameSlot = false) {
        if (itemIconImage) {
            // Option 1: Just hide the icon
            // itemIconImage.enabled = !isBeingDragged;

            // Option 2: Make icon semi-transparent (use a color)
            itemIconImage.color = isBeingDragged ? draggingMaskColor : Color.white;
        }
        if (quantityText) {
            quantityText.enabled = !isBeingDragged && quantityText.text != "" && quantityText.text != "0" && quantityText.enabled; // Also re-check conditions
            if (sameSlot) quantityText.enabled = true;
        }
    }
    public void SetSelected(bool isSelected) {
        if (highlightBorder != null) {
            highlightBorder.enabled = isSelected;
        }
    }

    // SetFocus is for controller navigation/mouse hover
    public void SetFocus(bool hasFocus) {
       // if (focusBorder) focusBorder.enabled = hasFocus;
    }

    // --- IPointerClickHandler ---
    public void OnPointerClick(PointerEventData eventData) {
        if (uiManager == null) return;

        // Forward click event to the UIManager
        // eventData.button tells us PointerEventData.InputButton.Left, .Right, .Middle
        //uiManager.HandleSlotClick(this, eventData.button);
    }

    // --- Unity UI Navigation (ISelectHandler, IDeselectHandler) ---
    public void OnSelect(BaseEventData eventData) {
        // Called when this UI element becomes the selected one (e.g., by controller D-Pad)
        if (uiManager) uiManager.OnSlotFocused(this);
        SetFocus(true);
    }

    public void OnDeselect(BaseEventData eventData) {
        // Called when this UI element loses selection
        if (uiManager) uiManager.OnSlotDefocused(this);
        SetFocus(false);
    }

}
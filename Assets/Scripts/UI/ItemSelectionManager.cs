// ItemSelectionManager.cs
using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class ItemSelectionManager : MonoBehaviour {
    [Header("References")]
    private InventoryManager inventoryManager;
    private GameObject playerObject; // The object that uses the items (needed for ItemData.Use)

    [Header("Input Action")]
    [SerializeField] private InputActionReference useItemAction; // Action to trigger item usage
    [SerializeField] private InputActionReference hotbarSelection; // Action to trigger hotbar selection 

    // --- Runtime Data ---
    private int hotbarSize;
    private int currentSelectedIndex = 0; // Start with the first slot selected
    private bool isInitialized = false;

    // Optional: Cooldown tracking per slot
    private float[] slotCooldownTimers;


    // --- Events ---
    public event Action<int> OnSelectionChanged; // int: new selected index

    // --- Properties ---
    public int SelectedSlotIndex => currentSelectedIndex;

    public void Init(GameObject player, InventoryManager inv) {
        playerObject = player;
        inventoryManager = inv;
    }

    void Start() {
        if (!inventoryManager) {
            Debug.LogError("InventoryManager not found!", gameObject);
            enabled = false;
            return;
        }

        if (useItemAction != null) {
            useItemAction.action.performed += HandleUseInput;
            useItemAction.action.Enable();
        } else {
            Debug.LogError("Use Item Action is not assigned!", gameObject);
        }
        hotbarSelection.action.performed += OnHotbarSelection;
        // Initialization called by HotbarUIManager AFTER it determines the size
    }

  

    void Update() {
        // Update cooldown timers if you implement them
        if (slotCooldownTimers != null) {
            for (int i = 0; i < slotCooldownTimers.Length; i++) {
                if (slotCooldownTimers[i] > 0) {
                    slotCooldownTimers[i] -= Time.deltaTime;
                }
            }
        }
    }

    void OnDestroy() {
        if (useItemAction != null) {
            useItemAction.action.performed -= HandleUseInput;
        }
    }


    // Called by HotbarUIManager during its Start sequence
    public void Initialize(int size) {
        hotbarSize = size;
        slotCooldownTimers = new float[hotbarSize]; // Initialize cooldown array
        SetSelectedSlot(currentSelectedIndex); // Set initial selection & trigger event
        isInitialized = true;
        slotCooldownTimers = new float[hotbarSize];
        // Clamp current index just in case hotbar size decreased
        currentSelectedIndex = Mathf.Clamp(currentSelectedIndex, 0, hotbarSize - 1);

        isInitialized = true;
        Debug.Log($"ItemSelectionManager Initialized with Hotbar Size: {hotbarSize}");

        // Trigger initial selection event
        OnSelectionChanged?.Invoke(currentSelectedIndex);
    }
    private void OnHotbarSelection(InputAction.CallbackContext context) {
        int slotIndex = (int)context.ReadValue<float>() - 1;
        SetSelectedSlot(slotIndex);
    }
    /// <summary>
    /// Sets the currently selected hotbar slot index.
    /// </summary>
    /// <param name="index">The index to select (0 to hotbarSize-1).</param>
    public void SetSelectedSlot(int index) {
        if (!isInitialized) {
            Debug.LogWarning("ItemSelectionManager not yet initialized.");
            // Store intended index if needed before init? For now, just wait.
            currentSelectedIndex = Mathf.Clamp(index, 0, hotbarSize > 0 ? hotbarSize - 1 : 0); // Clamp based on potential size
            return;
        }


        index = Mathf.Clamp(index, 0, hotbarSize - 1); // Ensure index is valid

        if (index != currentSelectedIndex) {
            currentSelectedIndex = index;
            Debug.Log($"Selected Hotbar Slot: {currentSelectedIndex}");
            OnSelectionChanged?.Invoke(currentSelectedIndex); // Notify listeners (like HotbarUIManager for highlight)
        } else if (!isInitialized) // Trigger initial event even if index is 0
          {
            OnSelectionChanged?.Invoke(currentSelectedIndex);
        }
    }
    public void SelectNextSlot() {
        if (!isInitialized || hotbarSize <= 0) return;
        int nextIndex = (currentSelectedIndex + 1) % hotbarSize;
        SetSelectedSlot(nextIndex);
    }

    public void SelectPreviousSlot() {
        if (!isInitialized || hotbarSize <= 0) return;
        int prevIndex = (currentSelectedIndex - 1 + hotbarSize) % hotbarSize; // Modulo handles wrap-around correctly
        SetSelectedSlot(prevIndex);
    }
    private void HandleUseInput(InputAction.CallbackContext context) {
        if (!isInitialized || playerObject == null) {
            Debug.LogWarning("Cannot use item: Manager not initialized or player object missing.");
            return;
        }

        Debug.Log($"Use Input Received. Selected Slot: {currentSelectedIndex}");

        InventorySlot selectedSlot = inventoryManager.GetSlot(currentSelectedIndex);

        if (selectedSlot == null || selectedSlot.IsEmpty()) {
            Debug.Log("Selected slot is empty.");
            return;
        }

        ItemData itemToUse = selectedSlot.ItemData;

        if (itemToUse == null || !itemToUse.isUsable) {
            Debug.Log($"{itemToUse?.itemName ?? "Item"} is not usable.");
            return;
        }

        // --- !!! MULTIPLAYER NOTE !!! ---
        // THIS is where you would send a ServerRpc to the server asking to use the item.
        // The server would validate, perform the action, update inventory, and sync back.
        // Example: playerNetworkComponent.CmdTryUseItem(currentSelectedIndex);
        // For now, we execute client-side directly.
        // ---

        // Cooldown Check (Client-side prediction / prevention)
        if (slotCooldownTimers != null && currentSelectedIndex < slotCooldownTimers.Length && slotCooldownTimers[currentSelectedIndex] > 0) {
            Debug.Log($"{itemToUse.itemName} is on cooldown.");
            return; // Don't attempt use if on cooldown client-side
        }


        Debug.Log($"Attempting to use: {itemToUse.itemName}");
        bool success = itemToUse.Use(playerObject); // Call the item's specific Use logic

        if (success) {
            Debug.Log($"{itemToUse.itemName} used successfully.");
            // Start cooldown if applicable
            if (itemToUse.usageCooldown > 0 && slotCooldownTimers != null && currentSelectedIndex < slotCooldownTimers.Length) {
                // Assuming cooldown is in seconds now for Time.deltaTime
                slotCooldownTimers[currentSelectedIndex] = itemToUse.usageCooldown;
            }

            // Consume the item if needed (server should handle this really)
            if (itemToUse.isConsumable) {
                inventoryManager.RemoveItem(currentSelectedIndex, 1);
                Debug.Log($"{itemToUse.itemName} consumed.");
            }
        } else {
            Debug.Log($"{itemToUse.itemName} use failed.");
            // Use failed (e.g., couldn't apply effect, conditions not met in derived Use method)
        }
    }
}
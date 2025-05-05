using UnityEngine;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using System.Collections.Generic;

public class SharedContainer : NetworkBehaviour {
    [Header("Settings")]
    [SerializeField] private int containerSize = 12;
    [SerializeField] private float interactionRadius = 2.0f;

    // The synchronized list of items. This is the core data.
    // IMPORTANT: InventorySlot needs to be serializable by FishNet.
    // This might require custom serializers if ItemData refs don't sync.
    // Requires FishNet V4+ for SyncList<YourClassOrStruct> support typically.
    private readonly SyncList<InventorySlot> items = new SyncList<InventorySlot>();

    // Event for the UI to subscribe to (fired locally on clients when 'items' changes)
    public event System.Action OnItemsChanged;

    public int ContainerSize => containerSize;

    // --- Initialization & Sync Callbacks ---

    public override void OnStartServer() {
        base.OnStartServer();
        // Initialize the list size on the server
        InitializeSlots();
    }

    public override void OnStartClient() {
        base.OnStartClient();
        // Subscribe to SyncList changes
        items.OnChange += HandleSyncListChanged;

        // Initial population if data already exists when client joins
        // (The OnChange should fire for existing items too on initial sync)
        if (items.Count == containerSize) { // Check if list seems initialized
            OnItemsChanged?.Invoke(); // Trigger UI update
        } else if (items.Count != 0 && items.Count != containerSize) {
            Debug.LogWarning($"[Client] Container {gameObject.name} SyncList has unexpected count {items.Count} vs expected {containerSize}. Waiting for proper sync.", this);
            // May indicate partial sync or server hasn't initialized fully yet. Wait for OnChange.
        } else {
            // Count is 0, likely awaiting initial sync data.
        }
    }

    public override void OnStopClient() {
        base.OnStopClient();
        // Unsubscribe
        items.OnChange -= HandleSyncListChanged;
    }

    [Server]
    private void InitializeSlots() {
        // Only run this once on the server
        if (items.Count > 0) return; // Already initialized

        for (int i = 0; i < containerSize; i++) {
            items.Add(new InventorySlot()); // Add empty slots
        }
        Debug.Log($"[Server] Initialized Shared Container {gameObject.name} with {containerSize} slots.");
    }

    // Callback when the SyncList changes on the client
    private void HandleSyncListChanged(SyncListOperation op, int index, InventorySlot oldItem, InventorySlot newItem, bool asServer) {
        if (asServer) return;
        Debug.Log($"[Client] Container {gameObject.name} SyncList Changed. Op: {op}, Index: {index}. Triggering UI Refresh.");
        OnItemsChanged?.Invoke(); // Trigger general UI refresh
    }

    // --- Accessing Items (Mainly for UI) ---

    /// <summary>
    /// Gets the item data at a specific index. Use for displaying UI. READ ONLY from client.
    /// </summary>
    public InventorySlot GetSlotReadOnly(int index) {
        if (index < 0 || index >= items.Count) {
            Debug.LogError($"Invalid index {index} requested for container {gameObject.name}");
            return new InventorySlot(); // Return empty default
        }
        // Return the current state of the synchronized list item
        return items[index];
    }


    // --- Interaction Logic (Called by PlayerInventorySyncer ServerRpc) ---

    [Server]
    public bool ServerTryInteract(NetworkObject interactor) {
        // Server validates distance before allowing interaction / UI open
        float distSq = (transform.position - interactor.transform.position).sqrMagnitude;
        if (distSq > (interactionRadius * interactionRadius)) {
            Debug.LogWarning($"[Server] Client {interactor.Owner.ClientId} tried to interact with container {gameObject.name} from too far.");
            // Maybe send TargetRpc failure message to player?
            return false;
        }

        // Interaction approved (e.g., player can now open the UI locally)
        // We might send a TargetRpc TO THE PLAYER confirming interaction success
        Debug.Log($"[Server] Client {interactor.Owner.ClientId} successfully interacted with container {gameObject.name}.");
        // The Player's script will handle opening the UI on the client after getting confirmation.
        return true;
    }


    // --- Item Movement (Called by PlayerInventorySyncer ServerRpc) ---

    [Server]
    public bool ServerTryAddItem(ushort itemID, int quantity, int preferredIndex = -1) // Accept ID
     {
        if (itemID == ItemDatabase.InvalidID || quantity <= 0) return false;

        // Need ItemData for maxStackSize lookup on server
        ItemData data = ItemDatabase.Instance.GetItemByID(itemID);
        if (data == null) {
            Debug.LogError($"[Server] Invalid ItemID {itemID} trying to be added to container {gameObject.name}.");
            return false;
        }


        // --- Stacking ---
        if (data.maxStackSize > 1) {
            for (int i = 0; i < items.Count; i++) {
                // Compare by ItemID
                if (items[i].itemID == itemID) {
                    int currentQuantity = items[i].quantity;
                    int stackSpace = data.maxStackSize - currentQuantity;
                    if (stackSpace > 0) {
                        int amountToAdd = Mathf.Min(quantity, stackSpace);
                        if (amountToAdd > 0) {
                            InventorySlot updatedSlot = new InventorySlot(itemID, currentQuantity + amountToAdd);
                            items[i] = updatedSlot;
                            quantity -= amountToAdd;
                            if (quantity <= 0) return true;
                        }
                    }
                }
            }
        }


        // --- Preferred Index ---
        if (preferredIndex >= 0 && preferredIndex < items.Count && items[preferredIndex].IsEmpty()) {
            int amountToAdd = Mathf.Min(quantity, data.maxStackSize);
            InventorySlot newSlot = new InventorySlot(itemID, amountToAdd);
            items[preferredIndex] = newSlot;
            quantity -= amountToAdd;
            if (quantity <= 0) return true;
        }


        // --- Empty Slots ---
        for (int i = 0; i < items.Count; i++) {
            if (items[i].IsEmpty()) {
                int amountToAdd = Mathf.Min(quantity, data.maxStackSize);
                InventorySlot newSlot = new InventorySlot(itemID, amountToAdd);
                items[i] = newSlot;
                quantity -= amountToAdd;
                if (quantity <= 0) return true;
            }
        }


        Debug.LogWarning($"[Server] Shared container {gameObject.name} full. Could not add remaining {quantity} of ID:{itemID} ({data.name}).");
        return quantity <= 0;
    }
    [Server]
    public InventorySlot ServerTryRemoveItem(int index, int quantity = 1) // Returns slot with ID and removed quantity
    {
        if (index < 0 || index >= items.Count || items[index].IsEmpty() || quantity <= 0) {
            return new InventorySlot(); // Return empty
        }

        InventorySlot slotToRemoveFrom = items[index]; // Contains itemID
        int quantityToRemove = Mathf.Min(quantity, slotToRemoveFrom.quantity);

        InventorySlot removedItemPortion = new InventorySlot(slotToRemoveFrom.itemID, quantityToRemove);

        InventorySlot updatedSlot = new InventorySlot(slotToRemoveFrom.itemID, slotToRemoveFrom.quantity - quantityToRemove);
        if (updatedSlot.quantity <= 0) updatedSlot.Clear();

        items[index] = updatedSlot;

        string itemName = ItemDatabase.Instance.GetItemByID(removedItemPortion.itemID)?.name ?? $"ID:{removedItemPortion.itemID}";
        Debug.Log($"[Server] Removed {quantityToRemove} of {itemName} from container {gameObject.name} at index {index}.");
        return removedItemPortion;
    }

    [Server]
    public bool ServerSwapSlots(int indexA, int indexB) {
        if (indexA < 0 || indexA >= items.Count || indexB < 0 || indexB >= items.Count || indexA == indexB) return false;
        InventorySlot temp = items[indexA];
        items[indexA] = items[indexB];
        items[indexB] = temp;
        return true;
    }
}
using UnityEngine;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using System.Collections.Generic;
using System;
using FishNet.Connection;
using System.Linq;

public class SharedContainer : NetworkBehaviour, IVisibilityEntity {
    [Header("Settings")]
    [SerializeField] private int containerSize = 12;
    [SerializeField] private float interactionRadius = 2.0f;

    // The synchronized list of items. This is the core data.
    public readonly SyncList<InventorySlot> ContainerSlots = new SyncList<InventorySlot>();
    // Tracks which client connection is currently interacting. -1 if none.
    private readonly SyncVar<int> _interactingClientId = new SyncVar<int>(-1, new SyncTypeSettings(WritePermission.ServerOnly));

    // Visual state, can be observed by all.
    private readonly SyncVar<bool> _isVisuallyOpen = new SyncVar<bool>(new SyncTypeSettings(WritePermission.ServerOnly));
    public bool IsVisuallyOpen => _isVisuallyOpen.Value;
    public int InteractingClientId => _interactingClientId.Value; // Who is currently allowed to send commands

    // Client-side event for UI to react to data changes
    public event Action OnContainerInventoryChanged;
    // Client-side event for UI to react to open/close state specifically for the *local player*
    public event Action<bool, List<InventorySlot>> OnLocalPlayerInteractionStateChanged;

    public int ContainerSize => containerSize;

    public VisibilityLayerType VisibilityScope => throw new NotImplementedException();

    public string AssociatedInteriorId => throw new NotImplementedException();

    // --- Initialization & Sync Callbacks ---

    #region Initialization & Sync Callbacks
    public override void OnStartServer() {
        base.OnStartServer();
        if (ContainerSlots.Count == 0 && containerSize > 0) // Initialize only if empty and size is defined
        {
            for (int i = 0; i < containerSize; i++) {
                ContainerSlots.Add(new InventorySlot());
            }
        }
        UpdateVisuals(_isVisuallyOpen.Value);
    }

    public override void OnStartClient() {
        base.OnStartClient();
        ContainerSlots.OnChange += SyncListChanged;
        _isVisuallyOpen.OnChange += OnVisualStateChangedCallback;
        _interactingClientId.OnChange += OnInteractingClientChangedCallback; // So client knows if it can interact

        UpdateVisuals(_isVisuallyOpen.Value); // Initial visual state
        // Initial population for UI if already populated by server, via the OnChange event
        if (ContainerSlots.Count > 0) {
            SyncListChanged(SyncListOperation.Complete, 0, default, default, false);
        }
    }

    public override void OnStopClient() {
        base.OnStopClient();
        ContainerSlots.OnChange -= SyncListChanged;
        _isVisuallyOpen.OnChange -= OnVisualStateChangedCallback;
        _interactingClientId.OnChange -= OnInteractingClientChangedCallback;
    }

    private void SyncListChanged(SyncListOperation op, int index, InventorySlot oldItem, InventorySlot newItem, bool asServer) {
        if (asServer)
            return; // Server already knows
        OnContainerInventoryChanged?.Invoke();
    }

    private void OnVisualStateChangedCallback(bool prev, bool next, bool asServer) {
        if (asServer)
            return;
        UpdateVisuals(next);
    }

    private void OnInteractingClientChangedCallback(int prevId, int newId, bool asServer) {
        if (asServer)
            return;

        bool isInteractingWithThisClient = (base.ClientManager.Connection != null && newId == base.ClientManager.Connection.ClientId);
        bool wasInteractingWithThisClient = (base.ClientManager.Connection != null && prevId == base.ClientManager.Connection.ClientId);

        if (isInteractingWithThisClient && !wasInteractingWithThisClient) // Just started interacting
        {
            // Send current slots for immediate UI population
            OnLocalPlayerInteractionStateChanged?.Invoke(true, new List<InventorySlot>(ContainerSlots));
        } else if (!isInteractingWithThisClient && wasInteractingWithThisClient) // Just stopped interacting
          {
            OnLocalPlayerInteractionStateChanged?.Invoke(false, null);
        }
    }


    private void UpdateVisuals(bool isOpen) {
        // TODO 
        Debug.LogWarning("Need open visual!");
        //if (openVisual != null)
        //    openVisual.SetActive(isOpen);
        //if (closedVisual != null)
        //    closedVisual.SetActive(!isOpen);
    }
    #endregion

    #region Server RPCs (Requests from Client)

    // Client requests to open this container
    [ServerRpc(RequireOwnership = false)] // Allow any client to request
    public void CmdRequestOpenContainer(NetworkConnection sender = null) // sender is implicitly passed
    {
        if (_interactingClientId.Value != -1 && _interactingClientId.Value != sender.ClientId) {
            // Optionally: TargetRpc back to sender: "Container in use"
            Debug.LogWarning($"Container {ObjectId} already in use by client {_interactingClientId.Value}. Client {sender.ClientId} cannot open.");
            TargetRpcOpenContainerResult(sender, false, "Container is already in use by another player.");
            return;
        }

        _interactingClientId.Value = sender.ClientId;
        _isVisuallyOpen.Value = true;
        Debug.Log($"Server: Client {sender.ClientId} opened container {ObjectId}.");
        // The _interactingClientId.OnChange callback on the client will trigger UI.
        // No need to send TargetRpc with full list if SyncList handles it well with OnChange.
        // However, sending it ensures the client gets the state *immediately* for UI building
        // before SyncList might fully process all items on a large list.
        TargetRpcOpenContainerResult(sender, true, "Successfully opened.", new List<InventorySlot>(ContainerSlots));

    }

    // Client requests to close this container
    [ServerRpc(RequireOwnership = false)]
    public void CmdRequestCloseContainer(NetworkConnection sender = null) {
        if (_interactingClientId.Value == sender.ClientId) {
            _interactingClientId.Value = -1;
            _isVisuallyOpen.Value = false; // Could stay visually open if desired, or close too.
            Debug.Log($"Server: Client {sender.ClientId} closed container {ObjectId}.");
            // Client will see _interactingClientId change and close its UI via OnLocalPlayerInteractionStateChanged
        }
    }

    // Client requests to place an item from their inventory into this container
    [ServerRpc(RequireOwnership = false)]
    public void CmdPlaceItemIntoContainer(ushort itemId, int quantity, int preferredSlot, NetworkConnection sender = null) {
        if (_interactingClientId.Value != sender.ClientId) {
            TargetRpcItemTransferResult(sender, false, itemId, 0, "You are not interacting with this container.");
            return;
        }
        if (itemId == ResourceSystem.InvalidID || quantity <= 0) {
            TargetRpcItemTransferResult(sender, false, itemId, 0, "Invalid item or quantity.");
            return;
        }

        ItemData itemData = App.ResourceSystem.GetItemByID(itemId);
        if (itemData == null) {
            TargetRpcItemTransferResult(sender, false, itemId, 0, "Item data not found.");
            return;
        }

        int maxStack = itemData.maxStackSize;
        int quantityActuallyPlaced = 0;

        // 1. Try preferred slot
        if (preferredSlot >= 0 && preferredSlot < ContainerSlots.Count) {
            InventorySlot slot = ContainerSlots[preferredSlot];
            if (slot.IsEmpty() || (slot.itemID == itemId && slot.quantity < maxStack)) {
                if (slot.IsEmpty())
                    slot.itemID = itemId;
                int canAdd = maxStack - slot.quantity;
                int amountToAdd = Mathf.Min(quantity, canAdd);

                slot.quantity += amountToAdd;
                ContainerSlots[preferredSlot] = slot; // Important for SyncList<struct>
                quantityActuallyPlaced += amountToAdd;
            }
        }

        // 2. If more to place, try stacking anywhere
        int remainingToPlace = quantity - quantityActuallyPlaced;
        if (remainingToPlace > 0) {
            for (int i = 0; i < ContainerSlots.Count && remainingToPlace > 0; i++) {
                if (i == preferredSlot && ContainerSlots[i].itemID == itemId)
                    continue; // Skip if already handled
                InventorySlot slot = ContainerSlots[i];
                if (slot.itemID == itemId && slot.quantity < maxStack) {
                    int canAdd = maxStack - slot.quantity;
                    int amountToAdd = Mathf.Min(remainingToPlace, canAdd);
                    slot.quantity += amountToAdd;
                    ContainerSlots[i] = slot;
                    quantityActuallyPlaced += amountToAdd;
                    remainingToPlace -= amountToAdd;
                }
            }
        }

        // 3. If still more to place, try empty slots
        if (remainingToPlace > 0) {
            for (int i = 0; i < ContainerSlots.Count && remainingToPlace > 0; i++) {
                if (i == preferredSlot && ContainerSlots[i].itemID == itemId)
                    continue;
                if (ContainerSlots[i].itemID != itemId && ContainerSlots[i].itemID != ResourceSystem.InvalidID)
                    continue; // skip slot with different item

                InventorySlot slot = ContainerSlots[i];
                if (slot.IsEmpty()) {
                    slot.itemID = itemId;
                    int amountToAdd = Mathf.Min(remainingToPlace, maxStack);
                    slot.quantity = amountToAdd; // Assign directly, not +=
                    ContainerSlots[i] = slot;
                    quantityActuallyPlaced += amountToAdd;
                    remainingToPlace -= amountToAdd;
                }
            }
        }

        if (quantityActuallyPlaced > 0) {
            // Tell client success, and how much was *actually* placed.
            // Client is responsible for removing 'quantityActuallyPlaced' from their own inventory.
            TargetRpcItemTransferResult(sender, true, itemId, quantityActuallyPlaced, $"{quantityActuallyPlaced} of item: {itemId} placed in container.");
        } else {
            TargetRpcItemTransferResult(sender, false, itemId, 0, "Could not place item(s) in container (full or no compatible stacks).");
        }
    }

    // Client requests to take an item from this container into their inventory
    [ServerRpc(RequireOwnership = false)]
    public void CmdTakeItemFromContainer(int containerSlotIndex, int quantityToTake, NetworkConnection sender = null) {
        if (_interactingClientId.Value != sender.ClientId) {
            TargetRpcItemTransferResult(sender, false, ResourceSystem.InvalidID, 0, "You are not interacting with this container.");
            return;
        }
        if (containerSlotIndex < 0 || containerSlotIndex >= ContainerSlots.Count || quantityToTake <= 0) {
            TargetRpcItemTransferResult(sender, false, ResourceSystem.InvalidID, 0, "Invalid slot or quantity.");
            return;
        }

        InventorySlot slot = ContainerSlots[containerSlotIndex];
        if (slot.IsEmpty()) {
            TargetRpcItemTransferResult(sender, false, ResourceSystem.InvalidID, 0, "Selected slot is empty.");
            return;
        }

        ushort itemIdToGive = slot.itemID;
        int quantityActuallyTaken = Mathf.Min(quantityToTake, slot.quantity);

        if (quantityActuallyTaken > 0) {
            slot.quantity -= quantityActuallyTaken;
            if (slot.quantity <= 0) {
                slot.itemID = ResourceSystem.InvalidID; // Clear slot
            }
            ContainerSlots[containerSlotIndex] = slot; // Update SyncList

            // Tell client success, itemID, and how much was *actually* taken.
            // Client is responsible for adding this to their own inventory.
            TargetRpcItemTransferResult(sender, true, itemIdToGive, quantityActuallyTaken, $"Quantity of {quantityActuallyTaken} of {itemIdToGive} taken from container.", true); // true indicates item taken BY player
        } else {
            TargetRpcItemTransferResult(sender, false, itemIdToGive, 0, "Could not take item (not enough quantity?).");
        }
    }
    #endregion

    #region Target RPCs (Results to Client)
    [TargetRpc]
    private void TargetRpcOpenContainerResult(NetworkConnection conn, bool success, string message, List<InventorySlot> currentSlots = null) {
        // The OnInteractingClientChangedCallback will primarily handle UI. This is more for immediate data.
        if (success) {
            Debug.Log($"Client: {message}");
            // This is a bit redundant if OnInteractingClientChangedCallback + SyncList.OnChange handles it,
            // but ensures the client can build its UI immediately.
            OnLocalPlayerInteractionStateChanged?.Invoke(true, currentSlots);
        } else {
            Debug.LogWarning($"Client: Failed to open container - {message}");
            OnLocalPlayerInteractionStateChanged?.Invoke(false, null);
        }
    }

    [TargetRpc]
    private void TargetRpcItemTransferResult(NetworkConnection conn, bool success, ushort itemId, int quantityTransferred, string message, bool itemTakenByPlayer = false) {
        NetworkedPlayerInventory playerInv = conn.FirstObject?.GetComponent<NetworkedPlayerInventory>();
        // Fallback if FirstObject isn't the player (e.g., player has multiple NetworkObjects)
        if (playerInv == null || !playerInv.IsOwner) {
            playerInv = FindObjectsByType<NetworkedPlayerInventory>(FindObjectsSortMode.None).FirstOrDefault(inv => inv.Owner == conn && inv.IsOwner);
        }


        if (playerInv == null) {
            Debug.LogError("TargetRpcItemTransferResult: Could not find NetworkedPlayerInventory for the connection.");
            return;
        }

        if (success) {
            Debug.Log($"Client: {message}");
            if (itemTakenByPlayer) // Item moved FROM container TO player
            {
                playerInv.heldItemStack.SetItem(itemId, quantityTransferred);
                //playerInv.AddItem(itemId, quantityTransferred); // If we do this it is like a shift click, but right now we just want to  put it into the hand
            } else // Item moved FROM player TO container
              {
                // playerInv.RemoveItem(itemId, quantityTransferred); // AGAIN this fucker took me 20 minutes to hunt down
                // It is a shift click like action, we already did a RemoveItem on HandlePickupFromSlot, so if we do it agian here we pretty much
                // Remove double the quantity, which is not good, but it would be good if we didn't have it in the hand, (which is a shift click)


                // If player was holding this item, clear/reduce held item

                // HI, I honestly don't know if we need to do this either, because we are handling it within other functions, such as HandlePickupFromSlot, before we even get here 
                /*if (!playerInv.heldItemStack.IsEmpty() && playerInv.heldItemStack.itemID == itemId) {
                    playerInv.heldItemStack.quantity -= quantityTransferred;
                    if (playerInv.heldItemStack.quantity <= 0) {
                        playerInv.heldItemStack.itemID = ResourceSystem.InvalidID;
                    }
                }*/
            }
            playerInv.RefreshUI(); // Refresh player inventory UI
        } else {
            Debug.LogWarning($"Client: Item transfer failed - {message}");
            // Optionally, provide feedback to the player via UI
        }
    }
    #endregion


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
        if (itemID == ResourceSystem.InvalidID || quantity <= 0) return false;

        // Need ItemData for maxStackSize lookup on server
        ItemData data = App.ResourceSystem.GetItemByID(itemID);
        if (data == null) {
            Debug.LogError($"[Server] Invalid ItemID {itemID} trying to be added to container {gameObject.name}.");
            return false;
        }


        // --- Stacking ---
        if (data.maxStackSize > 1) {
            for (int i = 0; i < ContainerSlots.Count; i++) {
                // Compare by ItemID
                if (ContainerSlots[i].itemID == itemID) {
                    int currentQuantity = ContainerSlots[i].quantity;
                    int stackSpace = data.maxStackSize - currentQuantity;
                    if (stackSpace > 0) {
                        int amountToAdd = Mathf.Min(quantity, stackSpace);
                        if (amountToAdd > 0) {
                            InventorySlot updatedSlot = new InventorySlot(itemID, currentQuantity + amountToAdd);
                            ContainerSlots[i] = updatedSlot;
                            quantity -= amountToAdd;
                            if (quantity <= 0) return true;
                        }
                    }
                }
            }
        }


        // --- Preferred Index ---
        if (preferredIndex >= 0 && preferredIndex < ContainerSlots.Count && ContainerSlots[preferredIndex].IsEmpty()) {
            int amountToAdd = Mathf.Min(quantity, data.maxStackSize);
            InventorySlot newSlot = new InventorySlot(itemID, amountToAdd);
            ContainerSlots[preferredIndex] = newSlot;
            quantity -= amountToAdd;
            if (quantity <= 0) return true;
        }


        // --- Empty Slots ---
        for (int i = 0; i < ContainerSlots.Count; i++) {
            if (ContainerSlots[i].IsEmpty()) {
                int amountToAdd = Mathf.Min(quantity, data.maxStackSize);
                InventorySlot newSlot = new InventorySlot(itemID, amountToAdd);
                ContainerSlots[i] = newSlot;
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
        if (index < 0 || index >= ContainerSlots.Count || ContainerSlots[index].IsEmpty() || quantity <= 0) {
            return new InventorySlot(); // Return empty
        }

        InventorySlot slotToRemoveFrom = ContainerSlots[index]; // Contains itemID
        int quantityToRemove = Mathf.Min(quantity, slotToRemoveFrom.quantity);

        InventorySlot removedItemPortion = new InventorySlot(slotToRemoveFrom.itemID, quantityToRemove);

        InventorySlot updatedSlot = new InventorySlot(slotToRemoveFrom.itemID, slotToRemoveFrom.quantity - quantityToRemove);
        if (updatedSlot.quantity <= 0) updatedSlot.Clear();

        ContainerSlots[index] = updatedSlot;

        string itemName = App.ResourceSystem.GetItemByID(removedItemPortion.itemID)?.name ?? $"ID:{removedItemPortion.itemID}";
        Debug.Log($"[Server] Removed {quantityToRemove} of {itemName} from container {gameObject.name} at index {index}.");
        return removedItemPortion;
    }

    [Server]
    public bool ServerSwapSlots(int indexA, int indexB) {
        if (indexA < 0 || indexA >= ContainerSlots.Count || indexB < 0 || indexB >= ContainerSlots.Count || indexA == indexB) return false;
        InventorySlot temp = ContainerSlots[indexA];
        ContainerSlots[indexA] = ContainerSlots[indexB];
        ContainerSlots[indexB] = temp;
        return true;
    }

    public void SetObjectVisibility(bool isVisible) {
        foreach (Renderer r in GetComponentsInChildren<Renderer>(true)) // Include inactive children
            if (r != null)
                r.enabled = isVisible;
    }
}
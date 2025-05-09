// PlayerInventorySyncer.cs
using UnityEngine;
using FishNet.Object;
using FishNet.Connection; // Required for NetworkConnection
using System.Collections.Generic;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using System;
using System.Linq; // For List

public class NetworkedPlayerInventory : NetworkBehaviour {
    [Header("References")]
    [SerializeField] private InventoryUIManager inventoryUIPrefab; // Players spawn their own inventory UIs 
    private InventoryUIManager _uiManager; 
    [SerializeField] private Transform dropPoint; // Where items are physically dropped
    [SerializeField] private float pickupRadius = 1.5f;
    [SerializeField] private LayerMask pickupLayerMask; // Set this to the layer your WorldItem prefabs are on
    private HashSet<NetworkObject> pickupRequested = new HashSet<NetworkObject>();

    [Header("Container Interaction")]
    [SerializeField] private LayerMask containerLayerMask; // Layer your containers are on
    private SharedContainer currentOpenContainer = null; // Track which container UI is open LOCALLY
    public static event System.Action<SharedContainer> OnContainerOpened; // UI listens to this
    public static event System.Action OnContainerClosed;       // UI listens to this


    [Header("Inventory Settings")]
    [SerializeField] private int inventorySize = 20; // How many slots
    private InventoryManager inventoryManager;
    public HeldItemStack heldItemStack = new HeldItemStack();
    private bool hasRequestedPickup;

    public int InventorySize => inventorySize;
    public override void OnStartClient() {
        base.OnStartClient();
        if (base.IsOwner) {
            InitializeInventory();
            _uiManager = Instantiate(inventoryUIPrefab);
            _uiManager.Init(inventoryManager, gameObject);
        } else {
            base.enabled = false;
        }
    }
    public override void OnStopClient() {
        base.OnStopClient();
        // Clean up instantiated UI if this player object is destroyed (and was owner)
        if (_uiManager != null) {
            Destroy(_uiManager);
        }
    }
    private void InitializeInventory() {
        List<InventorySlot> slots = new List<InventorySlot>(inventorySize);
        for (int i = 0; i < inventorySize; i++) {
            slots.Add(new InventorySlot());
        }
        inventoryManager = new InventoryManager(slots); // Assuming such a constructor
        Debug.Log($"Inventory Initialized with {inventorySize} slots.");
    }

    void Update() {
        if (!IsOwner)
            return;
        // UImanager handles visuals
        _uiManager.UpdateHeldItemVisual();
    }
    private void FixedUpdate() {
        if (!base.IsOwner) return;
        // Client checks for nearby items first (reduces unnecessary server calls)
        TryClientPickupCheck();
    }
    // --- Drag and Drop Handling ---
    // --- Pickup/Place/Drop Logic ---

    public void HandlePickupFromSlot(InventorySlotUI slotUI, PointerEventData.InputButton button) {
        int quantityToGrab = 0;
        ushort itemIDToGrab = ResourceSystem.InvalidID;
        InventorySlot sourceDataSlot = null;

        if (!slotUI.IsContainerSlot) // Picking from player inventory
        {
            sourceDataSlot = inventoryManager.GetSlot(slotUI.SlotIndex);
            if (sourceDataSlot == null || sourceDataSlot.IsEmpty())
                return; // Clicked empty player slot

            itemIDToGrab = sourceDataSlot.itemID;
            if (button == PointerEventData.InputButton.Left) { // Left click / Gamepad A
                quantityToGrab = sourceDataSlot.quantity;
            } else if (button == PointerEventData.InputButton.Right) { // Right click / Gamepad X
                quantityToGrab = Mathf.CeilToInt((float)sourceDataSlot.quantity / 2f);
            }

            inventoryManager.RemoveItem(slotUI.SlotIndex, quantityToGrab); // Update UI
            heldItemStack.SetItem(itemIDToGrab, quantityToGrab, slotUI.SlotIndex);
        } else // Picking from container
          {
            if (currentOpenContainer == null)
                return;
            sourceDataSlot = currentOpenContainer.GetSlotReadOnly(slotUI.SlotIndex);
            if (sourceDataSlot == null || sourceDataSlot.IsEmpty())
                return; // Clicked empty container slot

            itemIDToGrab = sourceDataSlot.itemID;
            if (button == PointerEventData.InputButton.Left) {
                quantityToGrab = sourceDataSlot.quantity;
            } else if (button == PointerEventData.InputButton.Right) {
                quantityToGrab = Mathf.CeilToInt((float)sourceDataSlot.quantity / 2f);
            }

            // Request server to "hold" this (actually means remove from container and give to player's "hand")
            // This is a complex server operation if we truly want a server-side "held" state.
            // Simpler: Client "takes" it visually, server processes transfer when item is "placed".
            //RequestMoveItemToPlayer(slotUI.SlotIndex, -1, quantityToGrab); // -1 playerIdx means "hold"
                                                                                        // Server needs to handle this new "pickup to hold" intent.
                                                                                        // For now, let's predict & update locally, and when PLACING,
                                                                                        // Player -> Player uses swap, Container -> Player uses the already existing RequestMoveItemToPlayer.

            InventorySlot tempVisualSlot = new InventorySlot(itemIDToGrab, sourceDataSlot.quantity - quantityToGrab);
            slotUI.UpdateSlot(tempVisualSlot); // Show remaining, or empty if all taken

            heldItemStack.SetItem(itemIDToGrab, quantityToGrab,slotUI.SlotIndex);
        }
        Debug.Log($"Picked up: ID {itemIDToGrab}, Qty {quantityToGrab} from {(slotUI.IsContainerSlot ? "Container" : "Player")} Slot {slotUI.SlotIndex}");
    }
    public void HandlePlaceHeldItem(InventorySlotUI targetSlotUI, PointerEventData.InputButton button) {
        if (heldItemStack.IsEmpty())
            return;

        ushort heldItemID = heldItemStack.itemID;
        int quantityToPlace = 0;

        if (button == PointerEventData.InputButton.Left) { // Place all held / Gamepad A
            quantityToPlace = heldItemStack.quantity;
        } else if (button == PointerEventData.InputButton.Right) { // Place one / Gamepad X or B
            quantityToPlace = 1;
        }
        if (quantityToPlace > heldItemStack.quantity)
            quantityToPlace = heldItemStack.quantity; // Cannot place more than held

        // --- Determine Action based on Source and Target ---
        if (!heldItemStack.isFromContainer && !targetSlotUI.IsContainerSlot) { // Player Inventory -> Player Inventory
            // This effectively becomes a swap or merge.
            InventorySlot targetDataSlot = inventoryManager.GetSlot(targetSlotUI.SlotIndex);
            if (targetDataSlot.IsEmpty() || targetDataSlot.itemID == heldItemID) // Target empty or same item
            {
                inventoryManager.AddItem(heldItemID, quantityToPlace, targetSlotUI.SlotIndex); // Target slot gets items
                heldItemStack.quantity -= quantityToPlace;
            } else { // Different items, remove from target, placea that in hand only when all quantity can be swaped with action
                TrySwapWithHand(targetSlotUI, quantityToPlace);
            }
        } else if (!heldItemStack.isFromContainer && targetSlotUI.IsContainerSlot) { // Player Inventory -> Container
            RequestMoveItemToContainer(heldItemStack.originalSourceSlotIndex, targetSlotUI.SlotIndex, quantityToPlace);
            // Client predict: remove from originalSourceSlot, UI for container updates on server ack.
            heldItemStack.quantity -= quantityToPlace; // Assume server will succeed for prediction


        } else if (heldItemStack.isFromContainer && !targetSlotUI.IsContainerSlot) { // Container -> Player Inventory
            RequestMoveItemToPlayer(heldItemStack.originalContainerSlotIndex, targetSlotUI.SlotIndex, quantityToPlace);
            // Client predict: nothing really, UI for player inventory updates on server ack.
            heldItemStack.quantity -= quantityToPlace; // Assume server will succeed for prediction

        } else if (heldItemStack.isFromContainer && targetSlotUI.IsContainerSlot) { // Container -> Container
            RequestSwapContainerSlots(heldItemStack.originalContainerSlotIndex, targetSlotUI.SlotIndex, currentOpenContainer.NetworkObject); // Full swap is simplest for now
                                                                                                                                                               // Client predict: Nothing, wait for SyncList.
                                                                                                                                                               // If placing only one, then heldItemStack still has rest.
            heldItemStack.quantity -= quantityToPlace; // This needs more thought for container->container partial place

        }


        if (heldItemStack.quantity <= 0) {
            heldItemStack.Clear();
        }
        Debug.Log($"Placed Item. Held now: {heldItemStack.quantity}");
    }

    private bool TrySwapWithHand(InventorySlotUI targetSlotUI, int quantityToPlace) {
        if (quantityToPlace >= heldItemStack.quantity) {
            var temp = inventoryManager.GetSlot(targetSlotUI.SlotIndex);
            inventoryManager.RemoveItem(targetSlotUI.SlotIndex, quantityToPlace);
            heldItemStack.SetItem(temp.itemID, quantityToPlace,targetSlotUI.SlotIndex);
            return true;
        }
        return false;
    }

    public void HandleDropToWorld(PointerEventData.InputButton button) {
        if (heldItemStack.IsEmpty())
            return;

        int quantityToDrop = 0;
        if (button == PointerEventData.InputButton.Left) { // Drop all held
            quantityToDrop = heldItemStack.quantity;
        } else if (button == PointerEventData.InputButton.Right) { // Drop one
            quantityToDrop = 1;
        }
        if (quantityToDrop > heldItemStack.quantity)
            quantityToDrop = heldItemStack.quantity;

        // TODO we might need to check for isFromContainer here 

        CmdDropItem(heldItemStack.itemID, quantityToDrop);
        
        // Optimistic client-side update of held stack
        heldItemStack.quantity -= quantityToDrop;
        if (heldItemStack.quantity <= 0) {
            heldItemStack.Clear();
        }
        Debug.Log($"Requested Drop to World. Held now: {heldItemStack.quantity}");
    }

    public void ReturnHeldItemToSource() {
        if (heldItemStack.IsEmpty())
            return;

        if (heldItemStack.originalSourceSlotIndex != -1) {
            // Return to player inventory slot
            inventoryManager.AddItem(heldItemStack.itemID, heldItemStack.quantity, heldItemStack.originalSourceSlotIndex);
        }
        heldItemStack.Clear(); // Clear after returning or deciding not to predict return
    }

    public void HandlePickupInput() // Called when player presses Interact key
    {
        if (!base.IsOwner) return;
        // Client checks for nearby items first (reduces unnecessary server calls)
        TryClientPickupCheck();
    }

    // --- Picking Up Logic ---

    // Client performs a local check for nearby items
    private void TryClientPickupCheck() {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, pickupRadius, pickupLayerMask);
        // If nothing in range, allow next pickup request
        if (hits.Length <= 0) {
            return;
        }
        pickupRequested.RemoveWhere(nob =>!hits.Any(h => h.GetComponent<DroppedEntity>()?.NetworkObject == nob));
        // find closest
        DroppedEntity closest = null;
        float bestDist = float.MaxValue;
        foreach (Collider2D hit in hits) {
            var item = hit.GetComponent<DroppedEntity>();
            if (item != null && item.CanPickup &&
                !pickupRequested.Contains(item.NetworkObject)) {
                float d = (item.transform.position - transform.position).sqrMagnitude;
                if (d < bestDist) {
                    bestDist = d;
                    closest = item;
                }
            }
        }

        // 4) Send one RPC for that item, and record it
        if (closest != null) {
            CmdPickupItem(closest.NetworkObject);
            pickupRequested.Add(closest.NetworkObject);
        }
    }

    // ServerRpc to request pickup
    [ServerRpc(RequireOwnership = true)]
    private void CmdPickupItem(NetworkObject itemNetworkObject) {
        if (itemNetworkObject == null) { Debug.LogWarning("[Server] Client sent null NetworkObject for pickup."); return; }

        DroppedEntity worldItem = itemNetworkObject.GetComponent<DroppedEntity>();

        if (worldItem == null) {
            Debug.LogWarning($"[Server] NetworkObject {itemNetworkObject.ObjectId} does not have a WorldItem component.");
            // Optional: Send TargetRpc back: TargetPickupFailed(base.Owner, "Invalid item.");
            return;
        }

        ushort itemID = worldItem.ItemID.Value;
        int quantity = worldItem.Quantity.Value;

        // Award item to client and despawn world object
        TargetRpcAwardItemToClient(Owner, itemID, quantity); // Owner is the NetworkConnection of the client who sent the RPC
        ServerManager.Despawn(itemNetworkObject); // Despawn the DroppedEntity
        Debug.Log($"Server: Awarded item {itemID} x{quantity} to client {Owner.ClientId}");
    }
    [TargetRpc]
    private void TargetRpcAwardItemToClient(NetworkConnection conn, ushort itemID, int quantity) {
        // This code runs on the client that picked up the item
        bool added = inventoryManager.AddItem(itemID, quantity); // Assume AddItem returns bool for success
        if (added) {
            Debug.Log($"Client: Picked up and added item {itemID} x{quantity} to inventory.");
        } else {
            Debug.LogWarning($"Client: Could not add item {itemID} x{quantity} to inventory (full?).");
            // Optionally, tell server to re-drop if client can't take it.
        }
        if (_uiManager != null)
            _uiManager.RefreshUI();
    }
    // --- Target RPCs (Server sending messages to a specific client) ---

    [TargetRpc] // Specify connection in the call: Target_UpdateSlot(connectionToClient, ...)
    public void Target_UpdateSlot(NetworkConnection conn, int slotIndex, ushort itemID, int quantity) {
        // This code runs ONLY on the client specified by 'conn'
        if (_uiManager == null) return;

        Debug.Log($"[Client {base.Owner.ClientId}] Received update for Slot {slotIndex}: {itemID} x{quantity}");

        // Directly update the local inventory data
        // InventorySlot localSlot = _uiManager.GetSlot(slotIndex);
        InventorySlot localSlot = null;
        if (localSlot != null) {
            // Check if data actually changed to prevent redundant UI updates
            bool changed = localSlot.itemID != itemID || localSlot.quantity != quantity;
            localSlot.SetSlot(itemID, quantity);
            if (localSlot.quantity <= 0) localSlot.Clear(); // Ensure empty slots are cleared


            //if (changed) {
                // Manually trigger the local event to update the UI
               // _uiManager.TriggerOnSlotChanged(slotIndex); // Need to add this helper method TODO
            //}

        } else {
            Debug.LogError($"[Client {base.Owner.ClientId}] Received update for invalid slot index {slotIndex}");
        }
    }


    [TargetRpc]
    public void TargetDropFailed(NetworkConnection conn, string reason) {
        // Runs on the client who failed to drop
        Debug.LogWarning($"[Client {base.Owner.ClientId}] Server denied drop request: {reason}");
        // Optional: Show UI message to player
        // UIManager.ShowNotification($"Drop failed: {reason}");
    }


    [TargetRpc]
    public void TargetPickupFailed(NetworkConnection conn, string reason) {
        // Runs on the client who failed to pickup
        Debug.LogWarning($"[Client {base.Owner.ClientId}] Server denied pickup request: {reason}");
        // Optional: Show UI message to player
        // UIManager.ShowNotification($"Pickup failed: {reason}");
    }


    // --- Container Open/Close (VERY simple stub as requested) ---
    // Example: Player interacts with a NetworkObject that is a container
    public void RequestOpenContainer(NetworkObject containerObject) {
        if (!base.IsOwner || containerObject == null)
            return;
        CmdRequestOpenContainer(containerObject);
    }

    [ServerRpc(RequireOwnership = true)]
    private void CmdRequestOpenContainer(NetworkObject containerNetObj) {
        // Server validates if this player can open this container
        // For example, check distance, if container exists, if it's locked, etc.
        Debug.Log($"Server: Player {Owner.ClientId} requested to open container {containerNetObj.ObjectId}");

        // Get the container component (you'll need a script on your container objects)
        // NetworkedContainer container = containerNetObj.GetComponent<NetworkedContainer>();
        // if (container != null && container.CanBeOpenedBy(Owner)) {
        //    container.ServerSetOpenState(true, Owner); // Server changes state, syncs to clients
        //    TargetRpcContainerOpened(Owner, containerNetObj, true);
        // } else {
        //    TargetRpcContainerOpened(Owner, containerNetObj, false); // Tell client it failed
        // }

        // Simplest: just tell the client "ok you opened it" (client handles UI)
        // Or tell ALL clients if it's a shared visual state change.
        // For now, just a log. You'd add RPCs to sync actual state.
        TargetRpcContainerInteractionResult(Owner, containerNetObj, true); // True for "opened"
    }

    // Server can call this on client to confirm container interaction
    [TargetRpc]
    public void TargetRpcContainerInteractionResult(NetworkConnection conn, NetworkObject containerNetObj, bool success) {
        if (success) {
            Debug.Log($"Client: Successfully interacted with container {containerNetObj.ObjectId}. Opening UI...");
            // Client would then open its local UI for that container.
            // The container's items might be on a SyncList on the container's NetworkBehaviour,
            // or this RPC could include the initial item list.
            // For "very simple", server just says "opened", client figures out what to show.
        } else {
            Debug.Log($"Client: Failed to interact with container {containerNetObj.ObjectId}.");
        }
    }
    // Called by Player Input when Interact key is pressed
    public void HandleInteractionInput() {
        if (!base.IsOwner) return;

        // If a container is already open, maybe close it? Or require explicit close action.
        if (currentOpenContainer != null) {
            CloseContainer(); // Simple: Interact again closes.
            return;
        }
        // Check for nearby containers
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, pickupRadius, containerLayerMask); // Reuse pickupRadius? Or separate radius?
        SharedContainer targetContainer = null;
        float closestDistSq = float.MaxValue;

        foreach (Collider2D hit in hits) {
            SharedContainer container = hit.GetComponentInParent<SharedContainer>();
            if (container != null) {
                float distSq = (container.transform.position - transform.position).sqrMagnitude;
                if (distSq < closestDistSq) {
                    closestDistSq = distSq;
                    targetContainer = container;
                }
            }
        }
        if (targetContainer != null) {
            // Found container, request interaction from server
            CmdInteractWithContainer(targetContainer.NetworkObject);
        } 
    }
    [ServerRpc(RequireOwnership = true)]
    private void CmdInteractWithContainer(NetworkObject containerNob) {
        if (containerNob == null) return;
        SharedContainer container = containerNob.GetComponent<SharedContainer>();
        if (container == null) return;


        // Server verifies distance and other interaction rules
        bool canInteract = container.ServerTryInteract(base.NetworkObject);

        if (canInteract) {
            // Tell the client they successfully interacted and can open the UI
            Target_NotifyContainerInteractionSuccess(base.Owner, containerNob);
        }
        // Maybe send failure notification if needed: Target_NotifyContainerInteractionFailed(base.Owner, "Too far away.");
    }


    [TargetRpc]
    private void Target_NotifyContainerInteractionSuccess(NetworkConnection conn, NetworkObject containerNob) {
        // This client successfully interacted, FIND the container locally and open UI
        SharedContainer container = containerNob.GetComponent<SharedContainer>();
        if (container != null) {
            Debug.Log($"[Client {base.Owner.ClientId}] Interaction with container {container.name} approved by server. Opening UI.");
            currentOpenContainer = container;
            OnContainerOpened?.Invoke(currentOpenContainer); // Notify UI Manager to display container
        } else {
            Debug.LogError($"[Client {base.Owner.ClientId}] Server approved interaction with container NOB {containerNob.ObjectId}, but couldn't find SharedContainer component locally!");
        }
    }

    // Method to close container (called by InteractInput or UI button)
    public void CloseContainer() {
        if (currentOpenContainer != null) {
            Debug.Log($"[Client {base.Owner.ClientId}] Closing container {currentOpenContainer.name} UI.");
            currentOpenContainer = null;
            OnContainerClosed?.Invoke(); // Notify UI to hide container panel
        }
    }

    public void RequestSwapContainerSlots(int containerSlotIndexA, int containerSlotIndexB, NetworkObject containerNob) {
        if (!base.IsOwner || containerNob == null) return;
        CmdSwapContainerSlots(containerSlotIndexA, containerSlotIndexB, containerNob);
        // Client prediction for SyncList swap is complex, do not predict for now.
    }


    [ServerRpc(RequireOwnership = true)]
    private void CmdSwapContainerSlots(int indexA, int indexB, NetworkObject containerNob) {
        if (containerNob == null) return;
        SharedContainer container = containerNob.GetComponent<SharedContainer>();
        if (container == null) return;


        // Let the container handle the swap logic internally for its SyncList
        bool success = container.ServerSwapSlots(indexA, indexB); // Need to add ServerSwapSlots to SharedContainer


        if (success) {
            Debug.Log($"[Server] Swapped slots {indexA} <-> {indexB} in Container {container.name}");
        } else {
            Debug.LogWarning($"[Server] Failed to swap slots {indexA} <-> {indexB} in Container {container.name}. Invalid indices?");
            // Send failure TargetRpc to player?
        }
    }
    // Called by UI when dragging Player -> Container
    public void RequestMoveItemToContainer(int playerSlotIndex, int containerSlotIndex, int quantity) {
        if (!base.IsOwner || currentOpenContainer == null) return;

        //InventorySlot localSlot = _uiManager.GetSlot(playerSlotIndex); // TODO
        InventorySlot localSlot = null; 
        if (localSlot == null || localSlot.IsEmpty()) return;
        CmdMoveItemToContainer(playerSlotIndex, containerSlotIndex, quantity, currentOpenContainer.NetworkObject);
        // Add client prediction? Optional.
    }


    [ServerRpc(RequireOwnership = true)]
    private void CmdMoveItemToContainer(int playerSlotIndex, int containerSlotIndex, int quantity, NetworkObject containerNob) {
        if (containerNob == null) { Debug.LogError("[Server] Container NetworkObject is null!"); return; }

        SharedContainer container = containerNob.GetComponent<SharedContainer>();
        if (container == null) { Debug.LogError($"[Server] NetworkObject {containerNob.ObjectId} is not a SharedContainer!"); return; }


        // 1. Validate item exists in player inventory
        InventorySlot playerSlot = inventoryManager.GetSlot(playerSlotIndex);
        if (playerSlot == null || playerSlot.IsEmpty() || quantity <= 0) { /* Send failure? */ return; }

        int quantityToMove = Mathf.Min(quantity, playerSlot.quantity);
        ushort idToMove = playerSlot.itemID;
        //ItemData dataToMove = playerSlot.itemData;

        // 2. Attempt to add to container (SERVER SIDE)
        bool addedToContainer = container.ServerTryAddItem(idToMove, quantityToMove, containerSlotIndex);
        // 3. If successful, remove from player (SERVER SIDE)
        if (addedToContainer) {
            //bool removedFromPlayer = Server_RemoveItem(playerSlotIndex, quantityToMove); // This already sends Target_UpdateSlot
            bool removedFromPlayer = false;
            if (!removedFromPlayer) {
                // This is bad - item added to container but failed to remove from player. Need rollback.
                Debug.LogError($"[Server] CRITICAL: Added item {idToMove} to container but failed to remove from player {base.Owner.ClientId}. Rolling back container add.");
                // Rollback: Remove the item we just added from the container
                InventorySlot removedForRollback = container.ServerTryRemoveItem(containerSlotIndex, quantityToMove); // Or find where it landed
                                                                                                                      // TODO: More robust rollback needed. Finding exact item/quantity could be tricky if stacked/split.
            } else {
                Debug.Log($"[Server] Moved {quantityToMove} of {idToMove} from Player {base.Owner.ClientId} (Slot {playerSlotIndex}) to Container {container.name} (Slot {containerSlotIndex})");
            }
        } else {
            // Failed to add to container (likely full)
            Debug.Log($"[Server] Failed to move {idToMove} to container {container.name} (likely full).");
            // Send TargetRpc notification to player? Target_MoveFailed(base.Owner, "Container is full.");
        }
    }

    // Called by UI when dragging Container -> Player
    public void RequestMoveItemToPlayer(int containerSlotIndex, int playerSlotIndex, int quantity) {
        if (!base.IsOwner || currentOpenContainer == null) return;
        CmdMoveItemToPlayer(containerSlotIndex, playerSlotIndex, quantity, currentOpenContainer.NetworkObject);
        // Add client prediction? Optional.
    }


    [ServerRpc(RequireOwnership = true)]
    private void CmdMoveItemToPlayer(int containerSlotIndex, int playerSlotIndex, int quantity, NetworkObject containerNob) {
        if (containerNob == null) { Debug.LogError("[Server] Container NetworkObject is null!"); return; }
        SharedContainer container = containerNob.GetComponent<SharedContainer>();
        if (container == null) { Debug.LogError($"[Server] NetworkObject {containerNob.ObjectId} is not a SharedContainer!"); return; }


        // 1. Attempt to remove item from container (SERVER SIDE)
        // Note: ServerTryRemoveItem returns the item portion that WAS removed.
        InventorySlot itemRemovedFromContainer = container.ServerTryRemoveItem(containerSlotIndex, quantity);

        // 2. If successful, attempt to add to player (SERVER SIDE)
        if (itemRemovedFromContainer != null && !itemRemovedFromContainer.IsEmpty()) {
            // Use Server_TryAddItem which handles simulation and TargetRpc updates on success
            //bool addedToPlayer = Server_TryAddItem(itemRemovedFromContainer.itemData, itemRemovedFromContainer.quantity); // Let AddItem find best slot

            //bool addedToPlayer = Server_TryAddItem(itemRemovedFromContainer.itemID, itemRemovedFromContainer.quantity); // Let AddItem find best slot
            bool addedToPlayer = false;
            if (!addedToPlayer) {
                // Failed to add to player (inventory full), ROLLBACK needed!
                Debug.LogError($"[Server] CRITICAL: Removed {itemRemovedFromContainer.itemID} from container but failed to add to player {base.Owner.ClientId}. Rolling back container removal.");
                // Rollback: Put the item back into the container
                bool rollbackSuccess = container.ServerTryAddItem(itemRemovedFromContainer.itemID, itemRemovedFromContainer.quantity, containerSlotIndex); // Try putting back in original slot
                if (!rollbackSuccess) Debug.LogError($"[Server] !!! FAILED TO ROLLBACK CONTAINER ITEM {itemRemovedFromContainer.itemID} !!! Container state may be inconsistent.");
            } else {
                Debug.Log($"[Server] Moved {itemRemovedFromContainer.quantity} of {itemRemovedFromContainer.itemID} from Container {container.name} (Slot {containerSlotIndex}) to Player {base.Owner.ClientId}");
            }
        } else {
            // Failed to remove item from container (invalid index, empty slot, or invalid quantity?)
            Debug.Log($"[Server] Failed to remove item from container {container.name} at index {containerSlotIndex}.");
            // Send TargetRpc notification? Target_MoveFailed(base.Owner, "Item not found in container or quantity invalid.");
        }
    }

    // --- Dropping Logic ---

    [ServerRpc(RequireOwnership = true)]
    private void CmdDropItem(ushort itemID, int quantity) {
        ItemData data = App.ResourceSystem.GetItemByID(itemID); // Lookup data for prefab info
        if (data == null) { TargetDropFailed(base.Owner, "Item data missing on server."); return; }
        if (data.droppedPrefab != null) {
            // 2. Spawn the item in the world
            GameObject prefab = data.droppedPrefab;
            Vector3 position = dropPoint != null ? dropPoint.position : transform.position + transform.forward; // Use drop point or position in front
            Quaternion rotation = Quaternion.identity;

            GameObject spawnedItem = Instantiate(prefab, position, rotation);
            NetworkObject nob = spawnedItem.GetComponent<NetworkObject>(); // Get the NOB after instantiation

            if (nob == null) {
                Debug.LogError($"[Server] Dropped prefab {prefab.name} is missing a NetworkObject component!");
                Destroy(spawnedItem); // Clean up invalid spawn
                                      // Optionally, refund the item to the player here? Complex.
                TargetDropFailed(base.Owner, "Item prefab configuration error.");
                return;
            }


            // Spawn the object over the network
            ServerManager.Spawn(nob);

            // Initialize the WorldItem component on the server
            DroppedEntity worldItem = spawnedItem.GetComponent<DroppedEntity>();
            if (worldItem != null) {
                worldItem.ServerInitialize(itemID, quantity);
                Debug.Log($"[Server] Player {base.Owner.ClientId} dropped {quantity} of {worldItem.name}.");
                // No need to send TargetRpc for success IF Server_RemoveItem sends update
            } else {
                Debug.LogError($"[Server] Dropped prefab {prefab.name} is missing a WorldItem component!");
                ServerManager.Despawn(nob); // Despawn broken item
                                            // Refund?
                TargetDropFailed(base.Owner, "Item prefab configuration error.");
            }
        }
        // If dataToDrop.droppedPrefab == null, item just vanishes silently (or add TargetRpc notification)
    }

    internal void HandleClose(InputAction.CallbackContext context) {
        if (!heldItemStack.IsEmpty()) {
            Debug.Log("Close Action: Returning held item.");
            ReturnHeldItemToSource(); // Or just clear if no return logic
            heldItemStack.Clear();
        }
    }

    internal void AddItem(ushort itemId, int quantityTransferred) {
        inventoryManager.AddItem(itemId, quantityTransferred);
    }

    internal void RemoveItem(ushort itemId, int quantityTransferred) {
        inventoryManager.RemoveItem(itemId, quantityTransferred);
    }

    internal void RefreshUI() {
        _uiManager.RefreshUI();
    }
}
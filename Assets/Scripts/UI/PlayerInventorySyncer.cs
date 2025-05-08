// PlayerInventorySyncer.cs
using UnityEngine;
using FishNet.Object;
using FishNet.Connection; // Required for NetworkConnection
using FishNet.Transporting; // Required for Target Rpc Channel option
using System.Collections.Generic; // For List

public class PlayerInventorySyncer : NetworkBehaviour {
    [Header("References")]
    [SerializeField] private InventoryManager localInventoryManager; // Reference to the player's LOCAL manager
    [SerializeField] private Transform dropPoint; // Where items are physically dropped
    [SerializeField] private float pickupRadius = 1.5f;
    [SerializeField] private LayerMask pickupLayerMask; // Set this to the layer your WorldItem prefabs are on

    [Header("Container Interaction")]
    [SerializeField] private LayerMask containerLayerMask; // Layer your containers are on
    private SharedContainer currentOpenContainer = null; // Track which container UI is open LOCALLY
    public static event System.Action<SharedContainer> OnContainerOpened; // UI listens to this
    public static event System.Action OnContainerClosed;       // UI listens to this
    // Used to store the SERVER-SIDE inventory for this specific player instance
    // NOTE: This assumes InventoryManager ISN'T a Singleton anymore OR
    // we create a separate non-monobehaviour inventory class for the server data.
    // Let's stick with the InventoryManager instance for now, assuming it's on the player prefab.
    // If InventoryManager is a scene singleton, this needs rework. Assuming it's PER PLAYER.
    private InventoryManager _serverInventory; // Only valid on the server


    public override void OnStartServer() {
        base.OnStartServer();
        // Get the InventoryManager instance ON THE SERVER INSTANCE of this player prefab
        _serverInventory = GetComponent<InventoryManager>();
        if (_serverInventory == null) {
            Debug.LogError("Player Prefab missing InventoryManager component on the server!", this.gameObject);
            // Initialize a dummy one if needed, or handle error
        }
        // Server doesn't need to subscribe to its own manager's local events for syncing
    }


    public override void OnStartClient() {
        base.OnStartClient();
        // Get the local inventory manager instance for THIS client
        if (localInventoryManager == null) {
            localInventoryManager = GetComponent<InventoryManager>(); // Assuming it's on the same object
        }

        if (localInventoryManager == null) {
            Debug.LogError("Player Prefab missing assigned local InventoryManager reference!", this.gameObject);
            enabled = false; // Disable if critical ref is missing
            return;
        }

        // *** IMPORTANT: Link local manager events to Sync Requests ONLY FOR THE OWNER ***
        if (base.IsOwner) {
            // Subscribe to LOCAL changes IF NEEDED (e.g., for drag/drop swap)
            // Usually, RPCs handle specific actions like Drop, Pickup, MoveToContainer
            // A full local sync might be too complex/redundant if RPCs manage state.

            // We primarily need INPUT triggers -> ServerRpc calls
            // And TargetRpcs -> Update localInventoryManager calls
        } else {
            // Non-owners typically don't need direct access to other players' full inventories
            // Disable this component for non-owners? Or just don't subscribe to events.
            // this.enabled = false; // Maybe too aggressive?
        }
    }

    // --- Input Handling (Example - Call this from your player input script) ---
    public void HandleDropInput(int slotIndex, int quantity = 1) // Usually triggered by UI drag-out or keybind
    {
        if (!base.IsOwner) return; // Only owner can request drop
        if (localInventoryManager.GetSlot(slotIndex) == null || localInventoryManager.GetSlot(slotIndex).IsEmpty()) return; // Cannot drop empty

        // Optimistic client-side prediction (Optional but feels better)
        //localInventoryManager.RemoveItem(slotIndex, quantity); 
        // We can remove the item visually here, but server is final authority
        // For now, we WILL predict removal, we (won't?)wait for server confirmation via Target_UpdateSlot

        CmdDropItem(slotIndex, quantity);
    }

    public void HandlePickupInput() // Called when player presses Interact key
    {
        if (!base.IsOwner) return;
        // Client checks for nearby items first (reduces unnecessary server calls)
        TryClientPickupCheck();
    }

    private void FixedUpdate() {
        if (!base.IsOwner) return;
        // Client checks for nearby items first (reduces unnecessary server calls)
        TryClientPickupCheck();
    }
    // --- Dropping Logic ---

    [ServerRpc(RequireOwnership = true)]
    private void CmdDropItem(int slotIndex, int quantity) {
        if (_serverInventory == null) { Debug.LogError("[Server] ServerInventory missing!"); return; } // Safety check

        InventorySlot slot = _serverInventory.GetSlot(slotIndex);
        if (slot == null || slot.IsEmpty() || quantity <= 0) {
            // Maybe send TargetRpc back telling client the drop failed?
            TargetDropFailed(base.Owner, "Invalid item or quantity.");
            return;
        }

        int quantityToDrop = Mathf.Min(quantity, slot.quantity); // Can't drop more than you have
        ushort idToDrop = slot.itemID;
        ItemData data = App.ResourceSystem.GetItemByID(idToDrop); // Lookup data for prefab info
        if (data == null) { TargetDropFailed(base.Owner, "Item data missing on server."); return; }
        // 1. Remove item from SERVER inventory
        // Use a specific Server_ method to avoid triggering local client events on server
        bool removed = Server_RemoveItem(slotIndex, quantityToDrop);

        if (removed && data.droppedPrefab != null) {
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
                worldItem.ServerInitialize(idToDrop, quantityToDrop);
                Debug.Log($"[Server] Player {base.Owner.ClientId} dropped {quantityToDrop} of {worldItem.name}.");
                // No need to send TargetRpc for success IF Server_RemoveItem sends update
            } else {
                Debug.LogError($"[Server] Dropped prefab {prefab.name} is missing a WorldItem component!");
                ServerManager.Despawn(nob); // Despawn broken item
                                            // Refund?
                TargetDropFailed(base.Owner, "Item prefab configuration error.");
            }

        } else if (!removed) {
            // This shouldn't happen if checks above are correct, but good practice
            TargetDropFailed(base.Owner, "Failed to remove item from server inventory.");
        }
        // If dataToDrop.droppedPrefab == null, item just vanishes silently (or add TargetRpc notification)
    }


    // --- Picking Up Logic ---

    // Client performs a local check for nearby items
    private void TryClientPickupCheck() {
        // Sphere cast or Overlap Sphere to find nearby items
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, pickupRadius, pickupLayerMask);
        //Debug.Log("Casting rays");
        if (hits.Length > 0) {
            DroppedEntity closestItem = null;
            float closestDistSq = float.MaxValue;

            foreach (Collider2D hit in hits) {
                // Check if we hit a WorldItem directly or via Rigidbody parent etc.
                DroppedEntity item = hit.GetComponent<DroppedEntity>();
                if (item != null && item.CanPickup) // Check pickup delay
               {
                    float distSq = (item.transform.position - transform.position).sqrMagnitude;
                    if (distSq < closestDistSq) {
                        closestDistSq = distSq;
                        closestItem = item;
                    }
                }
            }

            if (closestItem != null) {
                Debug.Log($"[Client] Found nearby item: {closestItem.ItemID.Value}. Requesting pickup.");
                // Found an item, request pickup from server, pass the NetworkObject ID
                CmdPickupItem(closestItem.NetworkObject); // Send reference to the NetworkObject
            } else {
                Debug.Log("[Client] No nearby items available for pickup.");
            }
        }
    }

    // ServerRpc to request pickup
    [ServerRpc(RequireOwnership = true)]
    private void CmdPickupItem(NetworkObject itemNetworkObject) {
        if (_serverInventory == null) { Debug.LogError("[Server] ServerInventory missing!"); return; }
        if (itemNetworkObject == null) { Debug.LogWarning("[Server] Client sent null NetworkObject for pickup."); return; }

        DroppedEntity worldItem = itemNetworkObject.GetComponent<DroppedEntity>();

        if (worldItem == null) {
            Debug.LogWarning($"[Server] NetworkObject {itemNetworkObject.ObjectId} does not have a WorldItem component.");
            // Optional: Send TargetRpc back: TargetPickupFailed(base.Owner, "Invalid item.");
            return;
        }

        // Server-side distance check (important anti-cheat measure)
        float distSq = (worldItem.transform.position - transform.position).sqrMagnitude;
        if (distSq > (pickupRadius * pickupRadius * 1.1f)) // Allow slight buffer
        {
            Debug.LogWarning($"[Server] Player {base.Owner.ClientId} tried to pick up item {worldItem.ItemID.Value} from too far away.");
            TargetPickupFailed(base.Owner, "Too far away.");
            return;
        }

        // Attempt the pickup (which includes adding to server inventory and despawning)
        bool success = worldItem.ServerTryPickup(base.NetworkObject); // Pass this player's NetworkObject

        // Note: If successful, Server_TryAddItem will trigger the Target_UpdateSlot to client.
        // If failed, WorldItem's ServerTryPickup might send TargetPickupFailed.
    }


    // --- Server-Side Inventory Modification (To avoid triggering local events on server) ---
    [Server]
    private bool Server_RemoveItem(int slotIndex, int quantity) {
        if (_serverInventory == null || !_serverInventory.IsValidIndex(slotIndex)) return false;

        InventorySlot slot = _serverInventory.GetSlot(slotIndex);
        if (slot.IsEmpty() || quantity <= 0) return false;

        int quantityToRemove = Mathf.Min(quantity, slot.quantity);
        slot.RemoveQuantity(quantityToRemove); // Use the slot's logic
        // *** Notify the OWNER client about the change ***
        Target_UpdateSlot(base.Owner, slotIndex, slot.itemID, slot.quantity);

        string itemName = App.ResourceSystem.GetItemByID(slot.itemID)?.name ?? $"ID:{slot.itemID}"; // Lookup name for log
        Debug.Log($"[Server] Removed {quantityToRemove} of {itemName} from slot {slotIndex} for client {base.Owner.ClientId}. New Qty: {slot.quantity}");
        return true; // Indicate success
    }

    [Server]
    public bool Server_TryAddItem(ushort itemID, int quantity) {
        if (_serverInventory == null) return false;

        bool addedSuccessfully = _serverInventory.AddItem(itemID, quantity);
        if (addedSuccessfully) {
            // Need to notify the client about potentially MULTIPLE slots changing (due to stacking)
            // This is tricky. Let's simplify: Find the slot(s) that changed and send updates.
            // A better way might be a TargetRpc that sends the whole inventory or just the delta.
            // FOR NOW: Just call AddItem, which should trigger OnSlotChanged locally *if* we hadn't
            // bypassed events. We MUST explicitly send updates for changes made on the server.
            // Let's find the slots that now contain the item and send updates.
            List<int> updatedIndices = _serverInventory.FindSlotsContainingID(itemID);
           // List<int> updatedIndices = _serverInventory.FindSlotsContaining(item);
            foreach (int index in updatedIndices) {
                InventorySlot currentSlotData = _serverInventory.GetSlot(index);
                Target_UpdateSlot(base.Owner, index, currentSlotData.itemID, currentSlotData.quantity);
            }
            string itemName = App.ResourceSystem.GetItemByID(itemID).name ?? $"ID:{itemID}";
            Debug.Log($"[Server] Added {quantity} of {itemName} to client {base.Owner.ClientId}");
            return true;
        } else {
            // This case shouldn't be reached if CalculateHowMuchCanBeAdded worked correctly.
            Debug.LogError("[Server] AddItem failed unexpectedly after simulation predicted success.");
            return false;
        }

    }

    // --- Target RPCs (Server sending messages to a specific client) ---

    [TargetRpc] // Specify connection in the call: Target_UpdateSlot(connectionToClient, ...)
    public void Target_UpdateSlot(NetworkConnection conn, int slotIndex, ushort itemID, int quantity) {
        // This code runs ONLY on the client specified by 'conn'
        if (localInventoryManager == null) return;

        Debug.Log($"[Client {base.Owner.ClientId}] Received update for Slot {slotIndex}: {itemID} x{quantity}");

        // Directly update the local inventory data
        InventorySlot localSlot = localInventoryManager.GetSlot(slotIndex);
        if (localSlot != null) {
            // Check if data actually changed to prevent redundant UI updates
            bool changed = localSlot.itemID != itemID || localSlot.quantity != quantity;
            localSlot.SetSlot(itemID, quantity);
            if (localSlot.quantity <= 0) localSlot.Clear(); // Ensure empty slots are cleared


            //if (changed) {
                // Manually trigger the local event to update the UI
                localInventoryManager.TriggerOnSlotChanged(slotIndex); // Need to add this helper method
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


    // --- Moving Items between Player Inventory and Container ---
    public void RequestSwapPlayerSlots(int playerSlotIndexA, int playerSlotIndexB) {
        if (!base.IsOwner) return;
        CmdSwapPlayerSlots(playerSlotIndexA, playerSlotIndexB);
        // Optional: Predict swap locally for responsiveness
        // InventoryManager.Instance.SwapSlots(playerSlotIndexA, playerSlotIndexB);
        // UpdateHotbarHighlight(...); // If needed
    }
    [ServerRpc(RequireOwnership = true)]
    private void CmdSwapPlayerSlots(int indexA, int indexB) {
        if (_serverInventory == null) return;
        if (!_serverInventory.IsValidIndex(indexA) || !_serverInventory.IsValidIndex(indexB)) {
            Debug.LogWarning($"[Server] Invalid indices for player slot swap: {indexA}, {indexB}");
            // Send failure TargetRpc?
            return;
        }


        // Perform swap on server data
        _serverInventory.SwapSlots(indexA, indexB); // Use the manager's existing Swap method on server copy


        // *** Notify the OWNER client about BOTH changes ***
        // (We rely on the manager's internal OnSlotChanged *not* being triggered, so manual update needed)
        InventorySlot slotA_Data = _serverInventory.GetSlot(indexA);
        InventorySlot slotB_Data = _serverInventory.GetSlot(indexB);
        Target_UpdateSlot(base.Owner, indexA, slotA_Data.itemID, slotA_Data.quantity);
        Target_UpdateSlot(base.Owner, indexB, slotB_Data.itemID, slotB_Data.quantity);


        Debug.Log($"[Server] Swapped Player {base.Owner.ClientId} inventory slots {indexA} <-> {indexB}");
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
        InventorySlot localSlot = localInventoryManager.GetSlot(playerSlotIndex);
        if (localSlot == null || localSlot.IsEmpty()) return;
        CmdMoveItemToContainer(playerSlotIndex, containerSlotIndex, quantity, currentOpenContainer.NetworkObject);
        // Add client prediction? Optional.
    }


    [ServerRpc(RequireOwnership = true)]
    private void CmdMoveItemToContainer(int playerSlotIndex, int containerSlotIndex, int quantity, NetworkObject containerNob) {
        if (_serverInventory == null) { Debug.LogError("[Server] Player ServerInventory missing!"); return; }
        if (containerNob == null) { Debug.LogError("[Server] Container NetworkObject is null!"); return; }

        SharedContainer container = containerNob.GetComponent<SharedContainer>();
        if (container == null) { Debug.LogError($"[Server] NetworkObject {containerNob.ObjectId} is not a SharedContainer!"); return; }


        // 1. Validate item exists in player inventory
        InventorySlot playerSlot = _serverInventory.GetSlot(playerSlotIndex);
        if (playerSlot == null || playerSlot.IsEmpty() || quantity <= 0) { /* Send failure? */ return; }

        int quantityToMove = Mathf.Min(quantity, playerSlot.quantity);
        ushort idToMove = playerSlot.itemID;
        //ItemData dataToMove = playerSlot.itemData;

        // 2. Attempt to add to container (SERVER SIDE)
        bool addedToContainer = container.ServerTryAddItem(idToMove, quantityToMove, containerSlotIndex);
        // 3. If successful, remove from player (SERVER SIDE)
        if (addedToContainer) {
            bool removedFromPlayer = Server_RemoveItem(playerSlotIndex, quantityToMove); // This already sends Target_UpdateSlot
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
        if (_serverInventory == null) { Debug.LogError("[Server] Player ServerInventory missing!"); return; }
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
            bool addedToPlayer = Server_TryAddItem(itemRemovedFromContainer.itemID, itemRemovedFromContainer.quantity); // Let AddItem find best slot

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

    // NEW RPC for server-authoritative merging/placing within player inventory
    public void RequestMergePlayerItem(int sourceSlotIndex, int targetSlotIndex, ushort itemID, int quantityToPlace) {
        if (!base.IsOwner) return;
        CmdMergePlayerItem(sourceSlotIndex, targetSlotIndex, itemID, quantityToPlace);
    }

    [ServerRpc(RequireOwnership = true)]
    private void CmdMergePlayerItem(int fromSlotIdx, int toSlotIdx, ushort itemID, int quantity) {
        if (_serverInventory == null || !_serverInventory.IsValidIndex(fromSlotIdx) || !_serverInventory.IsValidIndex(toSlotIdx)) return;
        InventorySlot sourceServerSlot = _serverInventory.GetSlot(fromSlotIdx);
        if (sourceServerSlot.itemID != itemID || sourceServerSlot.quantity < quantity) {
            Debug.LogWarning($"[Server] CmdMergePlayerItem: Mismatch or insufficient items in source slot {fromSlotIdx}. Client data might be desynced.");
            // Force full sync of source and target slots to client?
            Target_UpdateSlot(base.Owner, fromSlotIdx, sourceServerSlot.itemID, sourceServerSlot.quantity);
            InventorySlot targetServerSlotPre = _serverInventory.GetSlot(toSlotIdx);
            Target_UpdateSlot(base.Owner, toSlotIdx, targetServerSlotPre.itemID, targetServerSlotPre.quantity);
            return;
        }

        // Remove from source on server
        _serverInventory.RemoveItem(fromSlotIdx, quantity, false); // New param: sendTargetRpcUpdate=false
        
        // Add to target on server
        bool added = _serverInventory.AddItem(itemID, quantity, toSlotIdx); // New param: sendTargetRpcUpdate=false

        if (!added) {
            // Failed to add to target (e.g. wrong item type, full), so rollback removal from source
            _serverInventory.AddItem(itemID, quantity, fromSlotIdx); // Put it back
            Debug.LogWarning($"[Server] CmdMergePlayerItem: Failed to add to target slot {toSlotIdx}. Rolled back.");
        }

        // Now send authoritative updates for both slots
        InventorySlot finalSourceSlot = _serverInventory.GetSlot(fromSlotIdx);
        Target_UpdateSlot(base.Owner, fromSlotIdx, finalSourceSlot.itemID, finalSourceSlot.quantity);
        InventorySlot finalTargetSlot = _serverInventory.GetSlot(toSlotIdx);
        Target_UpdateSlot(base.Owner, toSlotIdx, finalTargetSlot.itemID, finalTargetSlot.quantity);

        Debug.Log($"[Server] Merged/Moved Item. Source {fromSlotIdx}, Target {toSlotIdx}");
    }
    
    // RPC for dropping held item (originally from player inventory) to world
    public void RequestDropItemFromSlot(int playerSlotIndexFromWhichItWasTaken, int quantityToDrop) {
        if (!IsOwner) return;
        CmdDropItemFromSlot(playerSlotIndexFromWhichItWasTaken, quantityToDrop);
    }

    [ServerRpc(RequireOwnership = true)]
    private void CmdDropItemFromSlot(int sourcePlayerSlotIndex, int quantityToDrop) {
        if (_serverInventory == null || !_serverInventory.IsValidIndex(sourcePlayerSlotIndex)) return;

        InventorySlot serverSlot = _serverInventory.GetSlot(sourcePlayerSlotIndex);
        if (serverSlot.IsEmpty() || serverSlot.quantity < quantityToDrop) {
            TargetDropFailed(Owner, "Item not found or insufficient quantity in source slot for drop.");
            // Send client authoritative state of that slot
            Target_UpdateSlot(Owner, sourcePlayerSlotIndex, serverSlot.itemID, serverSlot.quantity);
            return;
        }

        ushort itemIDToDrop = serverSlot.itemID;
        ItemData dataForPrefab = App.ResourceSystem.GetItemByID(itemIDToDrop);
        if (dataForPrefab == null || dataForPrefab.droppedPrefab == null) {
            TargetDropFailed(Owner, "Item cannot be dropped (missing data or prefab).");
            return;
        }

        // Successfully removed, now spawn world item
        if (Server_RemoveItem(sourcePlayerSlotIndex, quantityToDrop)) // This will send Target_UpdateSlot
        {
            GameObject prefab = dataForPrefab.droppedPrefab;
            GameObject spawnedItem = Instantiate(prefab, dropPoint.position, Quaternion.identity);
            NetworkObject nob = spawnedItem.GetComponent<NetworkObject>();
            // ... (Spawn logic for WorldItem as in CmdDropItem) ...
            ServerManager.Spawn(nob);
            DroppedEntity worldItem = spawnedItem.GetComponent<DroppedEntity>();
            if (worldItem) worldItem.ServerInitialize(itemIDToDrop, quantityToDrop);
            else { ServerManager.Despawn(nob); /* error */ }
        }
    }


    // RPC for dropping held item (originally from container) to world
    public void RequestDropHeldItemFromContainer(int originalContainerSlotIndex, ushort heldItemID, int quantityToDrop) {
        if (!IsOwner || currentOpenContainer == null) return; // Ensure container context is valid
        CmdDropHeldItemFromContainer(originalContainerSlotIndex, heldItemID, quantityToDrop, currentOpenContainer.NetworkObject);
    }


    [ServerRpc(RequireOwnership = true)]
    private void CmdDropHeldItemFromContainer(int containerSlotIdx, ushort itemID, int quantity, NetworkObject containerNob) {
        if (containerNob == null) return;
        SharedContainer container = containerNob.GetComponent<SharedContainer>();
        if (container == null) return;

        // 1. Server verifies and removes item from container
        InventorySlot removedItem = container.ServerTryRemoveItem(containerSlotIdx, quantity);
        if (removedItem == null || removedItem.IsEmpty() || removedItem.itemID != itemID || removedItem.quantity != quantity) {
            TargetDropFailed(Owner, "Failed to verify and remove item from container for drop.");
            // Container's SyncList will update client if mismatch happened.
            return;
        }

        // 2. Item is now "in server's hand", spawn it.
        ItemData dataForPrefab = App.ResourceSystem.GetItemByID(itemID);
        if (dataForPrefab == null || dataForPrefab.droppedPrefab == null) {
            TargetDropFailed(Owner, "Item cannot be dropped (missing data or prefab).");
            // CRITICAL: Item removed from container but can't be dropped. Attempt to put it back.
            container.ServerTryAddItem(itemID, quantity, containerSlotIdx); // Try to return
            return;
        }
        GameObject prefab = dataForPrefab.droppedPrefab;
        GameObject spawnedItem = Instantiate(prefab, dropPoint.position, Quaternion.identity);
        NetworkObject nob = spawnedItem.GetComponent<NetworkObject>();
        // ... (Spawn logic for WorldItem) ...
        ServerManager.Spawn(nob);
        DroppedEntity worldItem = spawnedItem.GetComponent<DroppedEntity>();
        if (worldItem) worldItem.ServerInitialize(itemID, quantity);
        else { ServerManager.Despawn(nob); /* error, also try to return to container */ }
    }
}
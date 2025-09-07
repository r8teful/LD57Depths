using UnityEngine;
using FishNet.Object;
using FishNet.Connection; // Required for NetworkConnection
using System.Collections.Generic;
using UnityEngine.EventSystems;
using System.Linq;
using Sirenix.OdinInspector;

public class NetworkedPlayerInventory : NetworkBehaviour {
    [Header("References")]
   
    [SerializeField] private Transform dropPoint; // Where items are physically dropped
    [SerializeField] private float pickupRadius = 1.5f;
    [SerializeField] private LayerMask pickupLayerMask; // Set this to the layer your WorldItem prefabs are on
    private HashSet<NetworkObject> pickupRequested = new HashSet<NetworkObject>();

    [Header("Container Interaction")]
    [SerializeField] private LayerMask containerLayerMask; // Layer your containers are on
    private SharedContainer currentOpenContainer = null; // Track which container UI is open LOCALLY
    public event System.Action<SharedContainer> OnContainerOpened; // UI listens to this
    public event System.Action<bool> OnContainerClosed;       // UI listens to this

    [ShowInInspector]
    private InventoryManager inventoryManager;

    public InventoryManager GetInventoryManager() => inventoryManager;
    public void Initialize() {

        InitializeInventory();
    }
    public override void OnStartClient() {
        base.OnStartClient();
        if (!base.IsOwner) {
            base.enabled = false;
        }
    }

    private void InitializeInventory() {
        inventoryManager = new InventoryManager(App.ResourceSystem.GetAllItems()); 
    }
    private void FixedUpdate() {
        if (!base.IsOwner) return;
        TryClientPickupCheck();
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
        if (itemNetworkObject == null || !itemNetworkObject.IsSpawned) { 
            Debug.LogWarning("[Server] NetworkObject is null or has not spawned"); 
            return; 
        }

        DroppedEntity worldItem = itemNetworkObject.GetComponent<DroppedEntity>();
       
        if (worldItem == null) {
            Debug.LogWarning($"[Server] NetworkObject {itemNetworkObject.ObjectId} does not have a WorldItem component.");
            // Optional: Send TargetRpc back: TargetPickupFailed(base.Owner, "Invalid item.");
            return;
        }

        ushort itemID = worldItem.ItemID.Value;
        int quantity = worldItem.Quantity.Value;

        ServerManager.Despawn(itemNetworkObject); // Despawn the DroppedEntity
        // Award item to client and despawn world object
        TargetRpcAwardItemToClient(Owner, itemID, quantity); // Owner is the NetworkConnection of the client who sent the RPC
        Debug.Log($"Server: Awarded item {itemID} x{quantity} to client {Owner.ClientId}");
    }
    [TargetRpc]
    private void TargetRpcAwardItemToClient(NetworkConnection conn, ushort itemID, int quantity) {
        // This code runs on the client that picked up the item
        bool added = inventoryManager.AddItem(itemID, quantity); // Assume AddItem returns bool for success
        if (added) {
            Debug.Log($"Client: Picked up and added item {itemID} x{quantity} to inventory.");
            DiscoveryManager.Instance.ServerDiscoverResource(itemID);
            AudioController.Instance.PlaySound2D("popPickup", 0.1f, pitch: new AudioParams.Pitch(AudioParams.Pitch.Variation.Small));
        } else {
            Debug.LogWarning($"Client: Could not add item {itemID} x{quantity} to inventory (full?).");
            // Optionally, tell server to re-drop if client can't take it.
        }
    }
    // --- Target RPCs (Server sending messages to a specific client) ---

    [TargetRpc] // Specify connection in the call: Target_UpdateSlot(connectionToClient, ...)
    public void Target_UpdateSlot(NetworkConnection conn, int slotIndex, ushort itemID, int quantity) {
        // This code runs ONLY on the client specified by 'conn'

        Debug.Log($"[Client {base.Owner.ClientId}] Received update for Slot {slotIndex}: {itemID} x{quantity}");

        // Directly update the local inventory data
        // InventorySlot localSlot = _uiManager.GetSlot(slotIndex);
        InventorySlot localSlot = null;
        if (localSlot != null) {
            // Check if data actually changed to prevent redundant UI updates
            bool changed = localSlot.itemID != itemID || localSlot.quantity != quantity;
            //localSlot.SetSlot(itemID, quantity); // TODODO!!
            if (localSlot.quantity <= 0) localSlot.Clear(); // Ensure empty slots are cleared


            //if (changed) {
                // Manually trigger the local event to update the UI
               // _uiManager.TriggerOnSlotChanged(slotIndex); // Need to add this helper method TODO
            //}

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

    public bool GrantItem(ushort itemID, int quantity) {
        bool added = inventoryManager.AddItem(itemID, quantity); // Assume AddItem returns bool for success
        if (added) {
            Debug.Log($"Client: Added item {itemID} x{quantity} to inventory.");
        } else {
            Debug.LogWarning($"Client: Could not add item {itemID} x{quantity} to inventory (full?).");
            // Optionally, tell server to re-drop if client can't take it.
        }
        return added;
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
    
    [ServerRpc(RequireOwnership = true)]
    public void CmdInteractWithContainer(NetworkObject containerNob) {
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
        Debug.Log("CLOSE CONTAINRE"+ currentOpenContainer); // conainer -> UI -> container, breaks, must not set currentopenctainer correctly
        if (currentOpenContainer != null) {
            Debug.Log($"[Client {base.Owner.ClientId}] Closing container {currentOpenContainer.name} UI.");
            currentOpenContainer = null;
            OnContainerClosed?.Invoke(true); // Notify UI to hide container panel
        }
    }

    // Called by UI when dragging Player -> Container
    public void RequestMoveItemToContainer(ushort itemID, int containerSlotIndex, int quantity) {
        if (!base.IsOwner || currentOpenContainer == null) return;

        //InventorySlot localSlot = _uiManager.GetSlot(playerSlotIndex); // TODO
        //InventorySlot localSlot = null; 
        //if (localSlot == null || localSlot.IsEmpty()) return;
        //CmdMoveItemToContainer(playerSlotIndex, containerSlotIndex, quantity, currentOpenContainer.NetworkObject);

        currentOpenContainer.CmdPlaceItemIntoContainer(itemID, quantity, containerSlotIndex);
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
        currentOpenContainer.CmdTakeItemFromContainer(containerSlotIndex, quantity);
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
                worldItem.ServerInitialize(itemID, quantity,false);
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

    internal void AddItem(ushort itemId, int quantityTransferred) {
        inventoryManager.AddItem(itemId, quantityTransferred);
    }

    internal void RemoveItem(ushort itemId, int quantityTransferred) {
        inventoryManager.RemoveItem(itemId, quantityTransferred);
    }

#if UNITY_EDITOR
    public void DEBUGGIVE(int ID, int amount) {
        inventoryManager.AddItem((ushort)ID, amount);
    }
#endif
}
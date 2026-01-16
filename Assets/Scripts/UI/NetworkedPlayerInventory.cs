using FishNet.Connection; // Required for NetworkConnection
using FishNet.Object;
using Sirenix.OdinInspector;
using System;
using UnityEngine;

public class NetworkedPlayerInventory : NetworkBehaviour {
    [Header("References")]
 


    [Header("Container Interaction")]
    [SerializeField] private LayerMask containerLayerMask; // Layer your containers are on
    private SharedContainer currentOpenContainer = null; // Track which container UI is open LOCALLY
    public event Action<SharedContainer> OnContainerOpened; // UI listens to this
    public event Action<bool> OnContainerClosed;       // UI listens to this


    // Local event that gets called when we pickup an item
    public static event Action<ushort,int> OnItemPickup;       
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
 
    public void AwardItem(ushort itemID) {
        bool added = inventoryManager.AddItem(itemID, 1); 
        if (added) {
            OnItemPickup?.Invoke(itemID, 1);
            DiscoveryManager.Instance.ServerDiscoverResource(itemID);
        } else {
            Debug.LogWarning($"Client: Could not add item {itemID} to inventory (full?).");
        }
    }
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


    internal void AddItem(ushort itemId, int quantityTransferred) {
        inventoryManager.AddItem(itemId, quantityTransferred);
    }

    internal void RemoveItem(ushort itemId, int quantityTransferred) {
        inventoryManager.RemoveItem(itemId, quantityTransferred);
    }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    public void DEBUGGIVE(int ID, int amount) {
        inventoryManager.AddItem((ushort)ID, amount);
    }
#endif
}
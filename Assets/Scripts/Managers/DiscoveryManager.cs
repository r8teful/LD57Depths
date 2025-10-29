using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;

public class DiscoveryManager : NetworkBehaviour {
    public static DiscoveryManager Instance { get; private set; }

    // This is the magic. It's a HashSet that automatically syncs from server to clients.
    private readonly SyncHashSet<ushort> _discoveredResourceIds = new SyncHashSet<ushort>();
    private readonly SyncHashSet<ushort> _discoveredBiomeIds = new SyncHashSet<ushort>();

    // We still need an event for the UI to listen to, but it's triggered by the SyncHashSet's own callback.
    public static event System.Action<ushort> OnResourceDiscovered;
    public static event System.Action<ushort> OnBiomeDiscovered;

    private void Awake() {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public override void OnStartClient() {
        base.OnStartClient();
        // Subscribe to the OnChange event for both sets. This is how the client knows when something is added.
        _discoveredResourceIds.OnChange += OnDiscoveredResourcesChanged;
        _discoveredBiomeIds.OnChange += OnDiscoveredBiomesChanged;
    }

    public override void OnStopClient() {
        base.OnStopClient();
        // Always unsubscribe
        _discoveredResourceIds.OnChange -= OnDiscoveredResourcesChanged;
        _discoveredBiomeIds.OnChange -= OnDiscoveredBiomesChanged;
    }

    private void OnDiscoveredResourcesChanged(SyncHashSetOperation op, ushort item, bool asServer) {
        // We only care when an item is added, and we want to fire a clean event for the UI.
        if (op == SyncHashSetOperation.Add) {
            Debug.Log($"Client received discovery update for resource: {item}");
            OnResourceDiscovered?.Invoke(item);
        }
    }

    private void OnDiscoveredBiomesChanged(SyncHashSetOperation op, ushort item, bool asServer) {
        if (op == SyncHashSetOperation.Add) {
            Debug.Log($"Client received discovery update for biome: {item}");
            OnBiomeDiscovered?.Invoke(item);
        }
    }

    // This RPC is still needed for clients to tell the server what they found.
    [ServerRpc(RequireOwnership = false)]
    public void ServerDiscoverResource(ushort resourceId) {
        // The magic is here: just add the item to the set on the server.
        // FishNet will detect the change and sync it to all clients.
        // The Add() method returns true if the item was new, false if it already existed.
        if (_discoveredResourceIds.Add(resourceId)) {
            Debug.Log($"Server added resource '{resourceId}' to SyncHashSet.");
            // TODO here you'd broadcast a message to all clients that a new resource has been discovered!! OOOH exciting stuff!!
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void ServerDiscoverBiome(ushort biomeId) {
        if (_discoveredBiomeIds.Add(biomeId)) {
            Debug.Log($"Server added biome '{biomeId}' to SyncHashSet.");
        }
    }

    // A helper method for the UI to check the state.
    public bool IsDiscovered(ItemData item) {
        if (item is ItemData)
            return _discoveredResourceIds.Contains(item.ID);
       // todo implement some kind of run time biome thing
       // if (item is WorldGenBiomeSO) 
         //   return _discoveredBiomeIds.Contains(item.ID);

        return false;
    }
}
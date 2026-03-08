using System.Collections.Generic;
using UnityEngine;

public class DiscoveryManager : StaticInstance<DiscoveryManager> {

    // This is the magic. It's a HashSet that automatically syncs from server to clients.
    private HashSet<ushort> _discoveredResourceIds = new HashSet<ushort>();
    private HashSet<ushort> _discoveredBiomeIds = new HashSet<ushort>();

    // We still need an event for the UI to listen to, but it's triggered by the SyncHashSet's own callback.
    public event System.Action<ushort> OnResourceDiscovered;
    public event System.Action<ushort> OnBiomeDiscovered;

    public void ServerDiscoverResource(ushort resourceId) {
        if (_discoveredResourceIds.Add(resourceId)) {
            Debug.Log($"Server added resource '{resourceId}' to SyncHashSet.");
            OnResourceDiscovered?.Invoke(resourceId);
        }
    }

    public void ServerDiscoverBiome(ushort biomeId) {
        if (_discoveredBiomeIds.Add(biomeId)) {
            Debug.Log($"Server added biome '{biomeId}' to SyncHashSet.");
            OnBiomeDiscovered?.Invoke(biomeId);
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
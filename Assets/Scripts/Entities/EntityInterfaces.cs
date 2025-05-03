using UnityEngine;
using UnityEngine.Tilemaps;
using FishNet.Object;
using System.Collections.Generic; // For NetworkObject

// Interface for components reacting to nearby tile changes
public interface ITileChangeReactor {
    // Called SERVER-SIDE when a relevant tile changes nearby
    void OnTileChangedNearby(Vector3Int cellPosition, int newTileID);
}

// Interface for components that need info about nearby players
public interface IPlayerAwareness {
    // Called SERVER-SIDE periodically or on demand to update player info
    void UpdateNearbyPlayers(List<NetworkObject> nearbyPlayerNobs);
}

// Interface for components needing info about nearby entities
public interface IEntityAwareness {
    // Called SERVER-SIDE periodically or on demand
    void UpdateNearbyEntities(List<NetworkObject> nearbyEntityNobs);
}
// Maybe a WorldState awareness? (water oxygen, biome, cleannes)
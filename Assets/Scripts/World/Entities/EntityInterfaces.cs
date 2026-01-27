using UnityEngine;
using System.Collections.Generic;

// Interface for components reacting to nearby tile changes
public interface ITileChangeReactor {
    // Called SERVER-SIDE when a relevant tile changes nearby
    void OnTileChangedNearby(Vector3Int cellPosition, int newTileID);
}

// Interface for components needing info about nearby entities
public interface IEntityAwareness {
    // Called SERVER-SIDE periodically or on demand
    void UpdateNearbyEntities(List<GameObject> nearbyEntityNobs);
}
// Maybe a WorldState awareness? (water oxygen, biome, cleannes)
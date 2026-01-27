using UnityEngine;
using System;
public enum VisibilityLayerType {
    Exterior, // Visible only when the local player is in the Exterior layer
    Interior  // Visible only when the local player is in a matching Interior layer
}

public class PlayerLayerController : MonoBehaviour, IPlayerModule {
    private VisibilityLayerType _currentLayer;

    public VisibilityLayerType CurrentLayer => _currentLayer;

    public int InitializationOrder => 100;

    public static event Action<VisibilityLayerType> OnPlayerVisibilityChanged;

    public void InitializeOnOwner(PlayerManager playerParent) {
        //_playerParent = playerParent;  
    }


    public void PortalInteraction(SubPortal portal) {
        // Invert
        _currentLayer = _currentLayer == VisibilityLayerType.Exterior ? VisibilityLayerType.Interior : VisibilityLayerType.Exterior;
        SubmarineManager.Instance.MoveInterior(_currentLayer);
        if (_currentLayer == VisibilityLayerType.Exterior && portal.IsEntrance) {
            // First move submarine there
        } else if (_currentLayer == VisibilityLayerType.Interior && !portal.IsEntrance) {
            // Move submarine out the way
        }
        MovePlayerToPortalDest(portal);
        OnPlayerVisibilityChanged?.Invoke(_currentLayer);
    }

    private void MovePlayerToPortalDest(SubPortal portal) {
        Vector3 worldSpawnPosition = portal.PortalDestination.position;
        this.transform.position = worldSpawnPosition;
        if (TryGetComponent<Rigidbody2D>(out var rb)) rb.linearVelocity = Vector2.zero;
    }

}
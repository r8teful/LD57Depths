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

    public void PutPlayerInSub() {
        _currentLayer = VisibilityLayerType.Interior;
        SubmarineManager.Instance.MoveInterior(_currentLayer);
        MovePlayerToPos(SubmarineManager.Instance.InteriorSpawnPoint.position);
        OnPlayerVisibilityChanged?.Invoke(_currentLayer);
    }

    public void PortalInteraction(SubPortal portal) {
        // Invert
        _currentLayer = _currentLayer == VisibilityLayerType.Exterior ? VisibilityLayerType.Interior : VisibilityLayerType.Exterior;
        SubmarineManager.Instance.MoveInterior(_currentLayer);
        MovePlayerToPos(portal.PortalDestination.transform.position);
        OnPlayerVisibilityChanged?.Invoke(_currentLayer);
    }

    private void MovePlayerToPos(Vector3 worldSpawnPosition) {
        this.transform.position = worldSpawnPosition;
        if (TryGetComponent<Rigidbody2D>(out var rb)) rb.linearVelocity = Vector2.zero;
    }

}
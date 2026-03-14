using UnityEngine;
using System;
public enum VisibilityLayerType {
    Exterior, // Visible only when the local player is in the Exterior layer
    Interior  // Visible only when the local player is in a matching Interior layer
}

public class PlayerLayerController : MonoBehaviour, IPlayerModule {
    private VisibilityLayerType _currentLayer;

    public VisibilityLayerType CurrentLayer => _currentLayer;
    public bool IsInSub => _currentLayer == VisibilityLayerType.Interior;

    public int InitializationOrder => 100;

    public static event Action<VisibilityLayerType> OnPlayerVisibilityChanged;

    public void InitializeOnOwner(PlayerManager playerParent) {
        //_playerParent = playerParent;  
    }

    public void PutPlayerInSub() {
        ChangeLayer(VisibilityLayerType.Interior, SubmarineManager.Instance.InteriorSpawnPoint);
    }
    public void PutPlayerOutsideSub() {
        // This doesn't change the player position but because the inside of the sub is right where the outside one is it works ( I think)
        ChangeLayer(VisibilityLayerType.Interior);
    }

    public void PortalInteraction(SubPortal portal) {
        // Invert
        var newLayer = _currentLayer == VisibilityLayerType.Exterior ? VisibilityLayerType.Interior : VisibilityLayerType.Exterior;
        ChangeLayer(newLayer,portal.PortalDestination);
    }
    private void ChangeLayer(VisibilityLayerType layer, Transform setPlayerPos = null) {
        App.Backdrop.DoWaveTransition(layer == VisibilityLayerType.Interior, () => {
            _currentLayer = layer;
            SubmarineManager.Instance.MoveInterior(_currentLayer);
            if(setPlayerPos != null) MovePlayerToPos((Vector3)setPlayerPos.position);
            OnPlayerVisibilityChanged?.Invoke(_currentLayer);
        }
     );
    }

    private void MovePlayerToPos(Vector3 worldSpawnPosition) {
        this.transform.position = worldSpawnPosition;
        if (TryGetComponent<Rigidbody2D>(out var rb)) rb.linearVelocity = Vector2.zero;
    }

}
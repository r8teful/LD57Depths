using UnityEngine;

public class DissableColliderOnPlayerLayerChange : MonoBehaviour {
    private Collider2D _collider;

    private void Awake() {
        _collider =  GetComponent<Collider2D>();
        PlayerLayerController.OnPlayerVisibilityChanged += OnLayerChange;
    }

    private void OnLayerChange(VisibilityLayerType newLayer) {
        switch (newLayer) {
            case VisibilityLayerType.Exterior:
                _collider.enabled = true ;
                break;
            case VisibilityLayerType.Interior:
                _collider.enabled = false;
                break;
            default:
                break;
        }
    }
}

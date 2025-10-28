using FishNet.Object;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(Interactable))]
public class InteriorPortal : MonoBehaviour {
    private Interactable _interactable;
    [Tooltip("Is this portal an entrance (Exterior -> Interior)? If false, it's an Exit (Interior -> Exterior).")]
    public bool IsEntrance = true;
    public string AssociatedInteriorId; 

    private void Awake() {
        GetComponent<Collider2D>().isTrigger = true;
        _interactable = GetComponent<Interactable>();
        // Validation
        if (string.IsNullOrEmpty(AssociatedInteriorId)) {
            Debug.LogError($"Portal '{gameObject.name}' needs an 'Associated Interior Id' set!", this);
        }
    }
    private void OnEnable() {
        if (_interactable != null) {
            _interactable.OnInteract += HandleInteraction;
        }
    }
    private void OnDisable() {
        if (_interactable != null) {
            _interactable.OnInteract -= HandleInteraction;
        }
    }

    private void OnTriggerEnter2D(Collider2D other) {
        if (other.CompareTag("Player")) {
           
        }
    }
    private void HandleInteraction(NetworkObject player) {
        PlayerLayerController playerController = player.GetComponent<PlayerLayerController>();
        if (playerController != null && playerController.IsOwner) {
            playerController.InteractWithPortal(this);
        }
    }

}
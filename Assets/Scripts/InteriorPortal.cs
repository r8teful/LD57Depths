using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class InteriorPortal : MonoBehaviour {
    [Tooltip("Is this portal an entrance (Exterior -> Interior)? If false, it's an Exit (Interior -> Exterior).")]
    public bool IsEntrance = true;
    public string AssociatedInteriorId; 

    private void Awake() {
        GetComponent<Collider2D>().isTrigger = true;
        // Validation
        if (string.IsNullOrEmpty(AssociatedInteriorId)) {
            Debug.LogError($"Portal '{gameObject.name}' needs an 'Associated Interior Id' set!", this);
        }
    }

    private void OnTriggerEnter2D(Collider2D other) {
        if (other.CompareTag("Player")) {
            PlayerLayerController playerController = other.GetComponent<PlayerLayerController>();
            if (playerController != null && playerController.IsOwner) {
                playerController.InteractWithPortal(this);
            }
        }
    }
}
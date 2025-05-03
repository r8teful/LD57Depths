using UnityEngine;
using FishNet.Object;

// Attach this component to the ROOT of any NetworkObject prefab
// that should be hidden when the player enters an interior.
public class ExteriorObject : NetworkBehaviour {
    // Cache components for efficiency if needed
    // [SerializeField] private Renderer[] renderers;
    // [SerializeField] private Collider2D[] colliders;

    public override void OnStartClient() {
        base.OnStartClient();
        // Register this object with the central manager when it spawns on the client
        WorldVisibilityManager.Instance.RegisterExteriorObject(this);
    }

    public override void OnStopClient() {
        base.OnStopClient();
        // Deregister when it's despawned/destroyed on the client
        // Add a null check for the instance in case the manager is destroyed first during shutdown
        if (WorldVisibilityManager.Instance != null) {
            WorldVisibilityManager.Instance.DeregisterExteriorObject(this);
        }
    }

    // Helper method that the WorldVisibilityManager will call
    public void SetVisibility(bool isVisible) {
        // Find components each time - safer if children are added/removed dynamically,
        // though usually NetworkObject children are fixed by the prefab.
        foreach (Renderer r in GetComponentsInChildren<Renderer>(true)) // Include inactive children
            if (r != null) r.enabled = isVisible;
        foreach (Collider2D c in GetComponentsInChildren<Collider2D>(true))
            if (c != null) c.enabled = isVisible;
        foreach (UnityEngine.Rendering.Universal.Light2D l2d in GetComponentsInChildren<UnityEngine.Rendering.Universal.Light2D>(true))
            if (l2d != null) l2d.enabled = isVisible;
    }
}
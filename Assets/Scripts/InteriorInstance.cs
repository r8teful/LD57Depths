using System.Collections.Generic;
using UnityEngine.Tilemaps;
using UnityEngine;
using FishNet.Object;

public class InteriorInstance : NetworkBehaviour {
    [Tooltip("Unique identifier for this specific interior.")]
    public string InteriorId;

    [Tooltip("Reference to the GameObject in the EXTERIOR world that represents this interior")]
    [field: SerializeField] public GameObject ExteriorAnchor { get; set; }
    [Tooltip("Spawn position player gets put when we exit this interior")]
    public Transform ExteriorSpawnPoint { get; private set; }
    [field: SerializeField] public Transform InteriorSpawnPoint { get; private set; }

    public List<GameObject> InteriorRootObjects = new List<GameObject>();

    // Store original position if you want to reset it on deactivation
    private Vector3 _originalPosition;
    private bool _hasBeenPositioned = false; // Track if positioning occurred


    public override void OnStartClient() {
        base.OnStartClient();
        if (string.IsNullOrEmpty(InteriorId)) {
            Debug.LogError($"Interior Instance on {gameObject.name} needs a unique InteriorId!", this);
        }
        if (ExteriorAnchor == null) {
            Debug.LogError($"Interior Instance '{InteriorId}' on {gameObject.name} needs an ExteriorAnchor assigned!", this);
        }

        _originalPosition = transform.position; // Store initial position
        ExteriorSpawnPoint = ExteriorAnchor.transform.Find("ExitSpawn"); // This could be a function in some kind of base "exterior" class
        InteriorManager.Instance.RegisterInterior(this);
        PopuplateInteriorObjects();
        // Start deactivated visually/physically
        SetInteriorActive(false);
    }

    private void PopuplateInteriorObjects() {
        // Instance must be on root object for this to work
        for (int i = 0; i < transform.childCount; i++) {
            InteriorRootObjects.Add(transform.GetChild(i).gameObject);
        }
    }

    private void OnDestroy() {
        InteriorManager.Instance.UnregisterInterior(this);
    }

    // Call this to activate/deactivate all elements of the interior
    public void SetInteriorActive(bool isActive) {
        // Optimization: If already in the desired state, do nothing.
        // This requires tracking the active state, which can be inferred from component checks
        // bool currentlyActive = (InteriorRootObjects.Count > 0 && InteriorRootObjects[0] != null && InteriorRootObjects[0].activeSelf); // Example check
        // if (currentlyActive == isActive) return;


        foreach (GameObject rootObj in InteriorRootObjects) {
            if (rootObj != null) {
                // Using component enabling/disabling is generally safer
                SetComponentsActive<Renderer>(rootObj, isActive);
                SetComponentsActive<Collider2D>(rootObj, isActive);
                SetComponentsActive<UnityEngine.Rendering.Universal.Light2D>(rootObj, isActive); // Example URP
                SetComponentsActive<TilemapRenderer>(rootObj, isActive);
                SetComponentsActive<TilemapCollider2D>(rootObj, isActive);
                // Add other relevant component types
            }
        }
        // Optionally: Only activate/deactivate the main GameObject if component approach isn't used
        // gameObject.SetActive(isActive);
    }

    // Positions the interior based on its anchor ****
    public void PositionToAnchor() {
        if (ExteriorAnchor != null) {
            transform.position = ExteriorAnchor.transform.position;
            _hasBeenPositioned = true;
            // Debug.Log($"Interior '{InteriorId}' positioned to Anchor {ExteriorAnchor.name} at {transform.position}");
        } else {
            Debug.LogError($"Cannot position Interior '{InteriorId}', ExteriorAnchor is missing!", this);
        }
    }

    // Helper to enable/disable specific component types recursively
    private void SetComponentsActive<T>(GameObject targetObject, bool isActive) where T : Component {
        T[] components = targetObject.GetComponentsInChildren<T>(true); // Include inactive
        foreach (T component in components) {
            if (component is Behaviour behaviour) behaviour.enabled = isActive;
            else if (component is Renderer renderer) renderer.enabled = isActive;
            else if (component is Collider2D collider) collider.enabled = isActive;
            // Add more specific types if needed (Light, TilemapRenderer, etc.)
        }
    }

    // Optional: Reset position when fully deactivated
    public void ResetPosition() {
        transform.position = _originalPosition;
        _hasBeenPositioned = false;
    }
}
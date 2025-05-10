using UnityEngine;
using System.Collections.Generic; 
using FishNet;
using UnityEngine.Tilemaps;
using UnityEngine.Rendering.Universal;


public class WorldVisibilityManager : Singleton<WorldVisibilityManager> {
    [Tooltip("Drag the root GameObject containing all exterior world elements (chunks, scenery, etc.)")]
    public GameObject ExteriorWorldRoot;
    private string PlayerTag = "Player"; 
    private PlayerLayerController _localPlayerController = null;
    private List<PlayerLayerController> _remotePlayers = new List<PlayerLayerController>(); // Keep track of others
    private readonly HashSet<IVisibilityEntity> _trackedEntities = new HashSet<IVisibilityEntity>();

    private VisibilityLayerType _currentLocalLayer = VisibilityLayerType.Exterior;
    private string _currentLocalInteriorId = "";
    // State variable to know if the exterior is currently supposed to be visible
    private bool _isExteriorVisible = true;
    void Start() {
        if (ExteriorWorldRoot == null) {
            Debug.LogError("ExteriorWorldRoot is not assigned in WorldVisibilityManager!");
            this.enabled = false; // Disable script if misconfigured
            return;
        }
        // Optionally find and cache players initially
        // FindAllPlayers(); // Better to dynamically track via OnStartClient/OnStopClient events maybe

        // Ensure exterior is initially visible if nothing else dictates state
        ApplyVisibilityForAllObjects(VisibilityLayerType.Exterior, "");
        InteriorManager.Instance.DeactivateAllInteriors(); // Ensure interiors start deactivated

    }
    public void RegisterPlayer(PlayerLayerController playerController) {
        if (playerController.IsOwner) // It's the Local Player
        {
            if (_localPlayerController != null && _localPlayerController != playerController) {
                Debug.LogWarning($"WorldVisibilityManager: Trying to register a new local player when one ({_localPlayerController.OwnerId}) is already registered. Replacing.");
                // Potentially deregister the old one first if needed
            }
            _localPlayerController = playerController;
            Debug.Log($"WorldVisibilityManager: Local Player {playerController.OwnerId} Registered.");
            // Immediately apply visibility based on the local player's *current* state
            ApplyVisibilityForAllObjects(playerController.CurrentLayer.Value,playerController.CurrentInteriorId.Value);
        } else // It's a Remote Player
          {
            if (!_remotePlayers.Contains(playerController)) {
                _remotePlayers.Add(playerController);
                Debug.Log($"WorldVisibilityManager: Remote Player {playerController.OwnerId} Registered.");
                // Immediately apply visibility *for this remote player* based on local player's state
                UpdateRemotePlayerVisibility(playerController);
            }
        }
    }

    // Called by PlayerLayerController.OnStopClient
    public void DeregisterPlayer(PlayerLayerController playerController) {
        if (_localPlayerController == playerController) {
            Debug.Log($"WorldVisibilityManager: Local Player {playerController.OwnerId} Deregistered.");
            _localPlayerController = null;
            //ApplyVisibilityState(PlayerWorldLayer.Exterior, "");
        } else {
            if (_remotePlayers.Remove(playerController)) {
                Debug.Log($"WorldVisibilityManager: Remote Player {playerController.OwnerId} Deregistered.");
            }
        }
    }
    public void RegisterObject(IVisibilityEntity obj) {
        if (obj != null && _trackedEntities.Add(obj)) {
            // Immediately set the correct visibility for the new object based on the *manager's* current state
            bool shouldBeVisible = ShouldObjectBeVisible(obj, _currentLocalLayer, _currentLocalInteriorId);
            obj.SetObjectVisibility(shouldBeVisible);
            // Debug.Log($"WVM Registered: {obj.NetworkObject.name}. Layer: {obj.VisibilityScope}, ID: '{obj.AssociatedInteriorId}'. Initial visibility: {shouldBeVisible}");
        }
    }
    public void DeregisterObject(IVisibilityEntity obj) {
        if (obj != null) {
            _trackedEntities.Remove(obj);
            // Debug.Log($"WVM Deregistered: {obj.NetworkObject.name}");
        }
    }
    /*
    void Update() {
        // Simple periodic check for players joining/leaving.
        // A better approach uses callbacks from FishNet's ClientManager.OnClientStarted/Stopped events
        // or SceneManager.OnClientPresenceChangeEvent if you track players globally.
        if (Time.frameCount % 60 == 0) // Check roughly once per second
        {
            RefreshPlayerReferences();
        }

        // Continuously update visibility based on the local player's current state.
        // This might be redundant if OnChanged handles everything, but good for safety.
        if (_localPlayerController != null && _localPlayerController.IsOwner) {
            UpdateVisibilityForLocalPlayer(_localPlayerController.CurrentLayer.Value, _localPlayerController.CurrentInteriorId.Value);
            // Update visibility of remote players from local player's perspective
            foreach (var remotePlayer in _remotePlayers) {
                UpdateRemotePlayerVisibility(remotePlayer);
            }
        } else {
            // Attempt to find local player if not found yet
            if (InstanceFinder.IsClientStarted && InstanceFinder.ClientManager != null && InstanceFinder.ClientManager.Connection.FirstObject != null) {
                _localPlayerController = InstanceFinder.ClientManager.Connection.FirstObject.GetComponent<PlayerLayerController>();
            }
        }
    }*/

    // Apply the visibility rules based on the local player's context

    // Note to self, this and LocalPlayerConttextChanged replaces UpdateVisibilityForLocalPlayer
    private void ApplyVisibilityForAllObjects(VisibilityLayerType localPlayerLayer, string localPlayerInteriorId) {
        // Cache the new state
        _currentLocalLayer = localPlayerLayer;
        _currentLocalInteriorId = localPlayerInteriorId;

        // Debug.Log($"WVM: Applying Global Visibility. Local Context -> Layer: {_currentLocalLayer}, Interior ID: '{_currentLocalInteriorId}'");


        // 1. Handle STATIC Exterior Root (if used)
        SetExteriorWorldActive(_currentLocalLayer == VisibilityLayerType.Exterior);

        // 2. Handle ALL dynamically tracked objects (Interior & Exterior)
        foreach (var entity in _trackedEntities) {
            if (entity != null) // Check for destroyed/nulled objects
            {
                bool shouldBeVisible = ShouldObjectBeVisible(entity, _currentLocalLayer, _currentLocalInteriorId);
                entity.SetObjectVisibility(shouldBeVisible);
                // Debug.Log($"-- Updating {obj.NetworkObject.name}: shouldBeVisible = {shouldBeVisible}");
            }
            // Consider adding logic here to occasionally clean the HashSet of null entries if they occur
        }

        // 3. Activate/Deactivate Interior GROUPS via InteriorManager
        if (_currentLocalLayer == VisibilityLayerType.Exterior) {
            InteriorManager.Instance.DeactivateAllInteriors();
        } else {
            // InteriorManager handles positioning and activation of the specific interior *instance*
            InteriorManager.Instance.ActivateInterior(_currentLocalInteriorId);
        }
        foreach (var remotePlayer in _remotePlayers) UpdateRemotePlayerVisibility(remotePlayer);
    }
    // Called when local player state CHANGES (via OnChange)
    public void LocalPlayerContextChanged() {
        if (_localPlayerController == null) {
            // This case should ideally be handled by DeregisterPlayer setting default view
            ApplyVisibilityForAllObjects(VisibilityLayerType.Exterior, "");
            HideAllRemotePlayers();
            return;
        }

        // Determine the new state
        VisibilityLayerType newLayer = _localPlayerController.CurrentLayer.Value;
        string newInteriorId = _localPlayerController.CurrentInteriorId.Value;

        // Apply the new state to ALL tracked objects
        ApplyVisibilityForAllObjects(newLayer, newInteriorId);

        // Update visibility of remote players relative to the new local context
        foreach (var remotePlayer in _remotePlayers) {
            UpdateRemotePlayerVisibility(remotePlayer);
        }
    }

    // Determines if a REMOTE player should be visible to the LOCAL player
    public void UpdateRemotePlayerVisibility(PlayerLayerController remotePlayer) {
        if (remotePlayer == null || remotePlayer.IsOwner || _localPlayerController == null) {
            Debug.Log("Returning...");
            return; // Ignore local player or if local player isn't known
        }

        Debug.Log("Trying to set remote player visibility");
        bool shouldBeVisible = false;
        var localLayer = _localPlayerController.CurrentLayer.Value;
        string localInteriorId = _localPlayerController.CurrentInteriorId.Value;

        var remoteLayer = remotePlayer.CurrentLayer.Value;
        string remoteInteriorId = remotePlayer.CurrentInteriorId.Value;

        if (localLayer == VisibilityLayerType.Interior && remoteLayer == VisibilityLayerType.Interior) {
            shouldBeVisible = (localInteriorId == remoteInteriorId);
        } else if (localLayer == remoteLayer) {
            // Same layer, show remote player.
            shouldBeVisible = true;
            // Both inside? Show remote player ONLY if they are in the SAME interior.
        } else {
            // One is inside, one is outside? Hide remote player.
            shouldBeVisible = false;
        }
        // Apply visibility (enable/disable renderers, colliders, maybe the whole GameObject)
        // Disabling the whole GO is simplest but might interfere with other logic.
        // Disabling components is safer.
        SetGameObjectComponentsActive(remotePlayer.gameObject, shouldBeVisible);
        //Debug.Log($"Setting remote player {remotePlayer.OwnerId} visibility to {shouldBeVisible} (Local: {localLayer}/{localInteriorId}, Remote: {remoteLayer}/{remoteInteriorId})");
    }
    private void HideAllRemotePlayers() { foreach (var remote in _remotePlayers) if (remote != null) SetGameObjectComponentsActive(remote.gameObject, false); }

    private void SetGameObjectComponentsActive(GameObject go, bool isActive) {
        // Example: Disable Renderers and Colliders
        foreach (Renderer r in go.GetComponentsInChildren<Renderer>(true)) // Include inactive children
            r.enabled = isActive;
        foreach (Collider2D c in go.GetComponentsInChildren<Collider2D>(true))
            c.enabled = isActive;
        // Optionally disable specific Behaviours/Scripts too
        // foreach (NetworkBehaviour nb in go.GetComponentsInChildren<NetworkBehaviour>(true))
        //     if(!(nb is PlayerLayerController)) // Don't disable the controller itself!
        //          nb.enabled = isActive; // Be careful with this - might break sync
    }


    // Helper to activate/deactivate the EXTERIOR world elements
    private void SetExteriorWorldActive(bool isActive) {
        if (ExteriorWorldRoot == null) return;

        // Option 1: Activate/Deactivate the root object (simpler, but less granular)
        //ExteriorWorldRoot.SetActive(isActive);

        // Option 2: Activate/Deactivate components (more robust)
        SetComponentsActiveRecursive<Renderer>(ExteriorWorldRoot, isActive);
        SetComponentsActiveRecursive<Collider2D>(ExteriorWorldRoot, isActive);
        SetComponentsActiveRecursive<TilemapRenderer>(ExteriorWorldRoot, isActive); // For Tilemaps
        SetComponentsActiveRecursive<TilemapCollider2D>(ExteriorWorldRoot, isActive); // For Tilemaps
        SetComponentsActiveRecursive<UnityEngine.Rendering.Universal.Light2D>(ExteriorWorldRoot, isActive); // Example URP Lights

        //Debug.Log($"Setting Exterior World Active: {isActive}");
    }

    // Generic recursive component activation/deactivation helper
    private void SetComponentsActiveRecursive<T>(GameObject targetObject, bool isActive) where T : Component {
        // Find components ONLY within the target object and its children
        T[] components = targetObject.GetComponentsInChildren<T>(true); // include inactive ones
        foreach (T component in components) {
            // Enable/disable based on the component type's relevant property
            if (component is Behaviour behaviour) behaviour.enabled = isActive;
            else if (component is Renderer renderer) renderer.enabled = isActive;
            else if (component is Collider2D collider) collider.enabled = isActive;
            // Add more types if necessary (Light, ParticleSystem, etc.)
        }
    }
    // Determines if a specific managed object should be visible given the local player's context
    private bool ShouldObjectBeVisible(IVisibilityEntity obj, VisibilityLayerType localPlayerLayer, string localPlayerInteriorId) {
        if (obj == null) return false;

        switch (localPlayerLayer) {
            case VisibilityLayerType.Exterior:
                // Show ONLY objects marked as Exterior
                return obj.VisibilityScope == VisibilityLayerType.Exterior;

            case VisibilityLayerType.Interior:
                // Show ONLY objects marked as Interior AND matching the current interior ID
                return obj.VisibilityScope == VisibilityLayerType.Interior &&
                       obj.AssociatedInteriorId == localPlayerInteriorId;
            default:
                return false;
        }
    }
}
using UnityEngine;
using System.Collections.Generic; 
using FishNet;
using UnityEngine.Tilemaps;


public class WorldVisibilityManager : Singleton<WorldVisibilityManager> {
    [Tooltip("Drag the root GameObject containing all exterior world elements (chunks, scenery, etc.)")]
    public GameObject ExteriorWorldRoot;

    private string PlayerTag = "Player"; 
    private PlayerLayerController _localPlayerController = null;
    private List<PlayerLayerController> _remotePlayers = new List<PlayerLayerController>(); // Keep track of others


    void Start() {
        if (ExteriorWorldRoot == null) {
            Debug.LogError("ExteriorWorldRoot is not assigned in WorldVisibilityManager!");
            this.enabled = false; // Disable script if misconfigured
            return;
        }
        // Optionally find and cache players initially
        // FindAllPlayers(); // Better to dynamically track via OnStartClient/OnStopClient events maybe

        // Ensure exterior is initially visible if nothing else dictates state
        SetExteriorWorldActive(true);
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
            UpdateVisibilityForLocalPlayer(playerController.CurrentLayer.Value,playerController.CurrentInteriorId.Value);
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

    // Main function called when the LOCAL player's state changes
    public void UpdateVisibilityForLocalPlayer(PlayerWorldLayer localPlayerLayer, string localPlayerInteriorId) {
        bool isExteriorVisible = (localPlayerLayer == PlayerWorldLayer.Exterior);

        // 1. Handle Exterior World Visibility
        SetExteriorWorldActive(isExteriorVisible);

        // 2. Handle Interior World Visibility
        if (isExteriorVisible) {
            // Player is outside, deactivate all interiors
            InteriorManager.Instance.DeactivateAllInteriors();
        } else {
            // Player is inside, activate the specific interior, deactivate others
            InteriorManager.Instance.ActivateInterior(localPlayerInteriorId);
        }

        // 3. Update visibility of REMOTE players based on the local player's location
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
        PlayerWorldLayer localLayer = _localPlayerController.CurrentLayer.Value;
        string localInteriorId = _localPlayerController.CurrentInteriorId.Value;

        PlayerWorldLayer remoteLayer = remotePlayer.CurrentLayer.Value;
        string remoteInteriorId = remotePlayer.CurrentInteriorId.Value;

        if (localLayer == PlayerWorldLayer.Exterior && remoteLayer == PlayerWorldLayer.Exterior) {
            // Both outside? Show remote player.
            shouldBeVisible = true;
        } else if (localLayer == PlayerWorldLayer.Interior && remoteLayer == PlayerWorldLayer.Interior) {
            // Both inside? Show remote player ONLY if they are in the SAME interior.
            shouldBeVisible = (localInteriorId == remoteInteriorId);
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
}
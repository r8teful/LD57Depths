using UnityEngine;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using FishNet.Connection;
using DG.Tweening;


public class PlayerLayerController : NetworkBehaviour {
    // --- State ---
    // Synced variable to track the current layer across the network.
    private readonly SyncVar<VisibilityLayerType> _currentLayer = 
        new SyncVar<VisibilityLayerType>(VisibilityLayerType.Exterior,new SyncTypeSettings(ReadPermission.Observers));

    private readonly SyncVar<string> _currentInteriorId = new SyncVar<string>("", new SyncTypeSettings(ReadPermission.Observers));
    public SyncVar<VisibilityLayerType> CurrentLayer => _currentLayer;
    public SyncVar<string> CurrentInteriorId => _currentInteriorId;

    // --- Client-Side References & Logic ---
    private WorldVisibilityManager _visibilityManager;
    private Camera _playerCamera;
    private PlayerController _playerController;
    private void Awake() {
        _currentLayer.OnChange += OnLayerChanged;
        _currentInteriorId.OnChange += OnInteriorIdChanged;
    }
    public override void OnStartClient() {
        base.OnStartClient();
        // Find the client-side manager responsible for visibility
        _playerCamera = GetComponentInChildren<Camera>();
        _playerController = GetComponent<PlayerController>();
        _visibilityManager = FindFirstObjectByType<WorldVisibilityManager>();
        WorldVisibilityManager.Instance.RegisterPlayer(this);
        if (_visibilityManager == null) {
            Debug.LogError("WorldVisibilityManager not found on the scene!");
            return;
        }

        // Apply initial state visibility if this is the local player
        if (base.IsOwner) {
            HandleClientContextChange();
        }
        // Apply visibility state for this (potentially remote) player from the perspective of the local player
        _visibilityManager.UpdateRemotePlayerVisibility(this);
    }
    public override void OnStopClient() {
        base.OnStopClient();
        // Deregister from the manager when the object is destroyed/disabled on the client
        if (WorldVisibilityManager.Instance != null) {
            WorldVisibilityManager.Instance.DeregisterPlayer(this);
        }
    }
    // --- Server-Side Transition Logic ---
    [ServerRpc(RequireOwnership = true)] // Only owner should trigger transitions for themselves
    public void RequestEnterInterior(string interiorId, NetworkConnection sender) // Removed entryPosition argument
    {
        Debug.Log("Enter request enter");
        if (_currentLayer.Value == VisibilityLayerType.Interior || string.IsNullOrEmpty(interiorId)) return;

        // Find the InteriorInstance server-side
        var targetInterior = InteriorManager.Instance.GetInteriorById(interiorId);
        if (targetInterior == null) {
            Debug.LogError($"Server: Could not find InteriorInstance with ID: {interiorId}");
            return;
        }
        if (targetInterior.ExteriorAnchor == null) {
            Debug.LogError($"Server: InteriorInstance '{interiorId}' is missing its ExteriorAnchor!");
            return;
        }
        // --- Server Authoritative State Change & Positioning ---
        _currentInteriorId.Value = interiorId;
        _currentLayer.Value = VisibilityLayerType.Interior;

        // Calculate the world spawn position. Pretty much predicts that the interior will move, which could be VERY BAD
        Vector3 worldSpawnPosition = targetInterior.ExteriorAnchor.transform.position + targetInterior.InteriorSpawnPoint.localPosition;
        
        // Teleport player physically on the server
        this.transform.position = worldSpawnPosition;
        SetPlayerClientPos(sender, worldSpawnPosition);
        if (TryGetComponent<Rigidbody2D>(out var rb)) rb.linearVelocity = Vector2.zero;

        Debug.Log($"Server: Player {OwnerId} entering Interior '{interiorId}' at {worldSpawnPosition}");

        // Optional: Notify other server systems if needed
        // Observer broadcast of SyncVars handles client updates automatically.
        if (_playerCamera == null)
            _playerCamera = GetComponentInChildren<Camera>();
        if (_playerController == null)
            _playerController = GetComponent<PlayerController>();
        _playerController.ChangeState(PlayerController.PlayerState.Interior);
        _playerCamera.DOOrthoSize(13, 1);
    }


    [ServerRpc(RequireOwnership = true)]
    public void RequestExitInterior(string interiorID, NetworkConnection sender) {
        if (_currentLayer.Value == VisibilityLayerType.Exterior) return;
        // --- Server Authoritative State Change ---
        string previousInteriorId = _currentInteriorId.Value; // Store before clearing
        _currentInteriorId.Value = "";
        _currentLayer.Value = VisibilityLayerType.Exterior;
        // Teleport player physically on the server

        var targetInterior = InteriorManager.Instance.GetInteriorById(interiorID);
        SetPlayerClientPos(sender, targetInterior.ExteriorSpawnPoint.position);

        if (_playerController == null)
            _playerController = GetComponent<PlayerController>();
        _playerController.ChangeState(PlayerController.PlayerState.Swimming);
        _playerCamera.DOOrthoSize(15f, 2);
        Debug.Log($"Server: Player {OwnerId} exiting Interior '{previousInteriorId}' to {interiorID}");
        // Optional: Server logic after exiting (e.g., maybe tell InteriorManager the interior might be empty now)
    }
    [TargetRpc]
    private void SetPlayerClientPos(NetworkConnection target, Vector3 newPos) {
        transform.position = newPos;
        if (TryGetComponent<Rigidbody2D>(out var rb)) rb.linearVelocity = Vector2.zero;
    }
    // --- SyncVar Callbacks (Triggered on Clients) ---
    private void OnLayerChanged(VisibilityLayerType prev, VisibilityLayerType next, bool asServer) {
        if (asServer) return;
        HandleClientContextChange();
    }
    private void OnInteriorIdChanged(string prev, string next, bool asServer)
    {
        if (asServer) return;
        HandleClientContextChange();
    }

    public void InteractWithPortal(InteriorPortal portal) {
        if (!base.IsOwner) return; // Only owner initiates
                                   
        string portalInteriorId = portal.AssociatedInteriorId;
        if (string.IsNullOrEmpty(portalInteriorId)) {
            Debug.LogError($"Portal {portal.gameObject.name} has no AssociatedInteriorId set!");
            return;
        }

        if (_currentLayer.Value == VisibilityLayerType.Exterior && portal.IsEntrance) {
            Debug.Log($"Requesting Entry to {portalInteriorId}");
            RequestEnterInterior(portalInteriorId,Owner); // Server calculates internal position
        } else if (_currentLayer.Value == VisibilityLayerType.Interior && !portal.IsEntrance) {
            // Check if the portal's associated ID matches the player's CURRENT interior ID
            if (portalInteriorId == _currentInteriorId.Value) {
                RequestExitInterior(portalInteriorId,Owner);
            } else {
                Debug.LogWarning($"Trying to use Exit portal associated with '{portalInteriorId}' but currently in '{_currentInteriorId}'. Interaction ignored.");
            }
        } else {
            Debug.LogWarning($"Portal interaction logic issue: CurrentLayer={_currentLayer}, IsEntrance={portal.IsEntrance}");
        }
    }
    // Consolidated handler called by BOTH OnChange callbacks
    private void HandleClientContextChange() {
        if (WorldVisibilityManager.Instance == null) return; // Safety check

        if (base.IsOwner) {
            // My context changed, update the entire world view
            WorldVisibilityManager.Instance.LocalPlayerContextChanged();
        } else {
            // A remote player's context changed, just update *their* visibility relative to me
            WorldVisibilityManager.Instance.UpdateRemotePlayerVisibility(this);
        }
    }
}
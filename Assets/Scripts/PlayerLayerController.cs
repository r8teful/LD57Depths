using UnityEngine;
using FishNet.Object;
using FishNet.Object.Synchronizing;


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
    private void Awake() {
        _currentLayer.OnChange += OnLayerChanged;
        _currentInteriorId.OnChange += OnInteriorIdChanged;
    }
    public override void OnStartClient() {
        base.OnStartClient();
        // Find the client-side manager responsible for visibility
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
    public void RequestEnterInterior(string interiorId) // Removed entryPosition argument
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

        // Calculate the world spawn position based on the anchor and offset
        Vector3 worldSpawnPosition = targetInterior.ExteriorAnchor.transform.position + targetInterior.EntrySpawnOffset;

        // Teleport player physically on the server
        this.transform.position = worldSpawnPosition;
        if (TryGetComponent<Rigidbody2D>(out var rb)) rb.linearVelocity = Vector2.zero;

        Debug.Log($"Server: Player {OwnerId} entering Interior '{interiorId}' at {worldSpawnPosition}");

        // Optional: Notify other server systems if needed
        // Observer broadcast of SyncVars handles client updates automatically.
    }


    [ServerRpc(RequireOwnership = true)]
    public void RequestExitInterior(string interiorID) {
        if (_currentLayer.Value == VisibilityLayerType.Exterior) return;
        // --- Server Authoritative State Change ---
        string previousInteriorId = _currentInteriorId.Value; // Store before clearing
        _currentInteriorId.Value = "";
        _currentLayer.Value = VisibilityLayerType.Exterior;
        // Teleport player physically on the server

        var targetInterior = InteriorManager.Instance.GetInteriorById(interiorID);
        this.transform.position = targetInterior.ExteriorSpawnPoint.position;
        if (TryGetComponent<Rigidbody2D>(out var rb)) rb.linearVelocity = Vector2.zero;


        Debug.Log($"Server: Player {OwnerId} exiting Interior '{previousInteriorId}' to {interiorID}");

        // Optional: Server logic after exiting (e.g., maybe tell InteriorManager the interior might be empty now)
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
            RequestEnterInterior(portalInteriorId); // Server calculates internal position
        } else if (_currentLayer.Value == VisibilityLayerType.Interior && !portal.IsEntrance) {
            // Check if the portal's associated ID matches the player's CURRENT interior ID
            if (portalInteriorId == _currentInteriorId.Value) {
                RequestExitInterior(portalInteriorId);
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
using UnityEngine;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using FishNet.Connection;


public class PlayerLayerController : NetworkBehaviour, INetworkedPlayerModule {
    // --- State ---
    // Synced variable to track the current layer across the network.
    private readonly SyncVar<VisibilityLayerType> _currentLayer = 
        new SyncVar<VisibilityLayerType>(VisibilityLayerType.Exterior,new SyncTypeSettings(ReadPermission.Observers));

    private readonly SyncVar<string> _currentInteriorId = new SyncVar<string>("", new SyncTypeSettings(ReadPermission.Observers));
    public SyncVar<VisibilityLayerType> CurrentLayer => _currentLayer;
    public SyncVar<string> CurrentInteriorId => _currentInteriorId;

    public int InitializationOrder => 90;

    private void OnEnable() {
        _currentLayer.OnChange += OnLayerChanged;
        _currentInteriorId.OnChange += OnInteriorIdChanged;
    }
    private void OnDisable() {
        _currentLayer.OnChange -= OnLayerChanged;
        _currentInteriorId.OnChange -= OnInteriorIdChanged;
    }
    public void Initialize(NetworkedPlayer playerParent) {
        WorldVisibilityManager.Instance.InitLocal(this);
        // Apply initial state visibility if this is the local player
        HandleClientContextChange();    
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

        // Optional: Server logic after exiting (e.g., maybe tell InteriorManager the interior might be empty now)
    }
    [TargetRpc]
    private void SetPlayerClientPos(NetworkConnection target, Vector3 newPos) {
        transform.position = newPos;
        if (TryGetComponent<Rigidbody2D>(out var rb)) rb.linearVelocity = Vector2.zero;
    }
    // --- SyncVar Callbacks (Triggered on Clients) ---
    private void OnLayerChanged(VisibilityLayerType prev, VisibilityLayerType next, bool asServer) {
        //if (asServer) return;
        HandleClientContextChange();
    }
    private void OnInteriorIdChanged(string prev, string next, bool asServer)
    {
        //if (asServer) return;
        HandleClientContextChange();
    }

    public void InteractWithPortal(InteriorPortal portal) {
        Debug.Log($"Interacting with portal: {portal.gameObject.name} - CurrentLayer={_currentLayer.Value}, AssociatedInteriorId={portal.AssociatedInteriorId}, IsEntrance={portal.IsEntrance}");
        if (!base.IsOwner) return; // Only owner initiates
        Debug.Log($"We are owner interacting with portal: {portal.gameObject.name} - CurrentLayer={_currentLayer.Value}, AssociatedInteriorId={portal.AssociatedInteriorId}, IsEntrance={portal.IsEntrance}");
                                   
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
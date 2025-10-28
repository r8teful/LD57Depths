using FishNet;
using FishNet.Connection;
using FishNet.Object;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// Should handle all things related to moving the submarine
public class SubMovementManager : NetworkBehaviour {
    public static SubMovementManager Instance { get; private set; }
    [Tooltip("If true, a disconnecting required player counts as an immediate decline (fail). If false, they are removed from the required set.")]
    public bool treatDisconnectAsDecline = false;

    [Tooltip("If true, players who join during an active request become required participants. If false, they are not required.")]
    public bool includeNewJoinersAsRequired = false;
    private Transform _sub;

    // Snapshot of who must respond
    private HashSet<int> _requiredClients = new HashSet<int>();
    // Only accepted clients tracked here
    private HashSet<int> _acceptedClients = new HashSet<int>();
    private bool _isRequestActive;
    private int _requesterId;
    private int _requestedZoneId;

    private void Awake() {
        if (Instance != null && Instance != this) Destroy(gameObject);
        else Instance = this;
    }
    public override void OnStartServer() {
        base.OnStartServer();
        _isRequestActive = false; // server side only var
        _sub = WorldManager.Instance.GetSubTransform();
    }

    // This will then be called by the UI when all players have confirmed the movement,
    // Then the pos will just sync for everyone because the subexterior has a networktransform
    public void MoveSub(int index) {
        Debug.Log("Moving submarine to index: " + index);
        _sub.position = new(0,WorldManager.Instance.GetWorldLayerYPos(index));
        SubmarineManager.Instance.SetSubPosIndex(index);
    }

    // ---- Server: Player starts a movement request ----
    [ServerRpc(RequireOwnership = false)]
    public void RequestMovement(NetworkConnection sender, int zoneID) {
        var senderID = sender.ClientId;
        Debug.Log($"Player: {senderID} has requested to move to zone {zoneID}");
        if (_isRequestActive) {
            // tell only the requester it failed to start a request
       
            TargetRequestRejected(sender, "Another movement request is already active.");
            return;
        }
        // Build snapshot of required participants (adapt GetConnectedPlayerIds to your system)
        var connected =  GetConnectedPlayerIds().ToList();
        // Start request
        _isRequestActive = true;
        _requesterId = senderID;
        _requiredClients = new HashSet<int>(connected);
        _acceptedClients.Clear();
        _requestedZoneId = zoneID;
        // requester auto-accepts
        _acceptedClients.Add(senderID);

        if (AllAccepted()) {
            StartMovement(); // Start movement if we're solo. No need to do all the fancy networking stuff
            return; 
        }

        // Broadcast initial state
        BroadcastRequestStart($"Player {GetPlayerDisplayName(senderID)} requested submarine movement.");
    }
    // ---- Server: Accept / Decline ----
    [ServerRpc(RequireOwnership = false)]
    public void RespondToRequest(bool accept, NetworkConnection sender) {
        var senderID = sender.ClientId;
        if (!_isRequestActive) {
            TargetRequestRejected(sender, "No active movement request.");
            return;
        }

        if (!_requiredClients.Contains(senderID)) {
            TargetRequestRejected(sender, "You are not required to respond to this request.");
            return;
        }

        // Already accepted? ignore repeated accepts
        if (accept) {
            if (_acceptedClients.Contains(senderID)) {
                TargetRequestRejected(sender, "You already accepted.");
                return;
            }

            _acceptedClients.Add(senderID);
            BroadcastRequestUpdate($"Player {GetPlayerDisplayName(senderID)} accepted.");

            if (AllAccepted()) {
                StartMovement();
            }
        } else {
            // immediate fail on any decline
            FailRequest($"Player {GetPlayerDisplayName(senderID)} declined the movement.");
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void CancelRequest(NetworkConnection sender) {
        if (!_isRequestActive) {
            TargetRequestRejected(sender, "No active movement request to cancel.");
            return;
        }

        if (sender.ClientId != _requesterId) {
            TargetRequestRejected(sender, "Only the original requester can cancel the request.");
            return;
        }

        FailRequest("Requester cancelled the movement.");
    }

    // ---- Helpers (server-side) ----
    private IEnumerable<int> GetConnectedPlayerIds() {
        if (NetworkedPlayersManager.Instance != null)
            return NetworkedPlayersManager.Instance.GetAllPlayersIDs();
        return InstanceFinder.ServerManager.Clients.Select(c => c.Value.ClientId);
    }
    // ---- Connection hooks -- call these from your PlayerRegistry or hookup server events ----
    // When a player connects while request is active
    public void OnPlayerConnectedDuringRequest(NetworkConnection client) {
        if (!_isRequestActive) return;

        // If new joiners are required, add them to the required set (and they are initially pending).
        if (includeNewJoinersAsRequired) {
            _requiredClients.Add(client.ClientId);
            BroadcastRequestUpdate($"Player {GetPlayerDisplayName(client.ClientId)} joined and is now required to respond.");
        } else {
            // Inform the new player about the active request but do not make them required.
            TargetNotifyActiveRequest(client, _requesterId, GetPlayerDisplayName(_requesterId), _acceptedClients.ToArray(), _requiredClients.Except(_acceptedClients).ToArray());
        }
    }

    // When a player disconnects while request is active
    public void OnPlayerDisconnectedDuringRequest(int clientId) {
        if (!_isRequestActive) return;

        // If the requester disconnected -> fail request
        if (clientId == _requesterId) {
            FailRequest("Requester disconnected — movement request aborted.");
            return;
        }

        if (!_requiredClients.Contains(clientId)) {
            // Not required — nothing to do
            return;
        }

        if (treatDisconnectAsDecline) {
            FailRequest($"Player {GetPlayerDisplayName(clientId)} disconnected (treated as decline).");
            return;
        } else {
            // Remove them from required; also remove from accepted if they were accepted
            _requiredClients.Remove(clientId);
            _acceptedClients.Remove(clientId);

            BroadcastRequestUpdate($"Player {GetPlayerDisplayName(clientId)} disconnected and was removed from required participants.");

            // If removing them makes everyone accepted, start movement
            if (AllAccepted()) {
                StartMovement();
            }
        }
    }

    // ---- Helpers & lifecycle ----
    private bool AllAccepted() {
        // Everyone in requiredClients must be in acceptedClients
        return _requiredClients.Count > 0 && _requiredClients.All(id => _acceptedClients.Contains(id));
    }

    private void StartMovement() {
        BroadcastMovementStarted($"All required players accepted — starting movement.");

        // Server-authoritative movement call
        MoveSub(_requestedZoneId);

        ClearRequestState();
    }

    private void FailRequest(string reason) {
        BroadcastRequestFailed(reason);
        ClearRequestState();
    }

    private void ClearRequestState() {
        _isRequestActive = false;
        _requesterId = -1;
        _requiredClients.Clear();
        _acceptedClients.Clear();

    }

    // ---- RPCs to clients ----
    private void BroadcastRequestStart(string message) {
        Debug.Log("Request Start: " + message);
        var zoneID = _requestedZoneId;
        RpcRequestStart(_requesterId, zoneID, message);
    }
    private void BroadcastRequestUpdate(string message) {
        Debug.Log("Request Update: " + message);
        var accepted = _acceptedClients.ToArray();
        var pending = _requiredClients.Where(id => !_acceptedClients.Contains(id)).ToArray();
        RpcRequestUpdated(_requesterId, GetPlayerDisplayName(_requesterId), accepted, pending, message);
    }

    private void BroadcastRequestFailed(string reason) {
        Debug.Log("Request failed: " + reason);
        var accepted = _acceptedClients.ToArray();
        var pending = _requiredClients.Where(id => !_acceptedClients.Contains(id)).ToArray();
        RpcRequestFailed(_requesterId, GetPlayerDisplayName(_requesterId), accepted, pending, reason);
    }

    private void BroadcastMovementStarted(string message) {
        Debug.Log("MOvement Started: " + message);
        var accepted = _acceptedClients.ToArray();
        var pending = _requiredClients.Where(id => !_acceptedClients.Contains(id)).ToArray();
        RpcMovementStarted(_requesterId, GetPlayerDisplayName(_requesterId), accepted, pending, message);
    }

    [ObserversRpc]
    private void RpcRequestStart(int requesterId, int zoneId, string message) {
        var isRequester = LocalConnection.ClientId == requesterId;
        Debug.Log($"LocalID {LocalConnection.ClientId} requestID: {requesterId}");
        NetworkedPlayer.LocalInstance.UiManager.UISubControlPanel.OnMovementRequestStart(isRequester, zoneId, message);
    }
    [ObserversRpc]
    private void RpcRequestUpdated(int requesterId, string requesterName, int[] acceptedIds, int[] pendingIds, string message) {

        Debug.Log($"Request updated requesterId: {requesterId}");
        NetworkedPlayer.LocalInstance.UiManager.UISubControlPanel.OnMovementRequestUpdated(requesterId, requesterName, acceptedIds, pendingIds, message);
    }

    [ObserversRpc]
    private void RpcRequestFailed(int requesterId, string requesterName, int[] acceptedIds, int[] pendingIds, string message) {
        NetworkedPlayer.LocalInstance.UiManager.UISubControlPanel.OnMovementRequestFailed(requesterId, requesterName, acceptedIds, pendingIds, message);
    }

    [ObserversRpc]
    private void RpcMovementStarted(int requesterId, string requesterName, int[] acceptedIds, int[] pendingIds, string message) {
        NetworkedPlayer.LocalInstance.UiManager.UISubControlPanel.OnMovementStarted(requesterId, requesterName, acceptedIds, pendingIds, message);
    }

    // Inform a single newly-joined client about the active request (if they are not required)
    [TargetRpc]
    private void TargetNotifyActiveRequest(NetworkConnection target, int requesterId, string requesterName, int[] acceptedIds, int[] pendingIds) {
        NetworkedPlayer.LocalInstance.UiManager.UISubControlPanel.OnNotifyActiveRequest(requesterId, requesterName, acceptedIds, pendingIds);
    }

    // Local-only rejection message
    [TargetRpc]
    private void TargetRequestRejected(NetworkConnection target, string message) {
        Debug.Log("Rejected with message: " + message);
        NetworkedPlayer.LocalInstance.UiManager.UISubControlPanel.OnRequestActionRejected(message);
    }

    // Map clientId -> display name (implement per your project)
    private string GetPlayerDisplayName(int clientId) {
        if (NetworkedPlayersManager.Instance != null)
            if(NetworkedPlayersManager.Instance.TryGetPlayer(clientId, out var p)) 
                return p.GetPlayerName();
        
        return $"Player_{clientId}";
    }
    public struct MovementData {

    }
}
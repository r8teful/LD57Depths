using FishNet;
using FishNet.Connection;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using FishNet.Transporting;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class NetworkedPlayersManager : NetworkBehaviour {
    public static NetworkedPlayersManager Instance { get; private set; }

    /// <summary>
    /// The synchronized dictionary of all players. The key is the client's ID.
    /// This is the single source of truth for player data.
    /// It's automatically kept in sync by FishNet.
    /// </summary>
    public readonly SyncDictionary<int, NetworkedPlayer> Players = new();

    /// <summary>
    /// A simpler C# event that other scripts can subscribe to.
    /// Fired on both server and client whenever the Players dictionary changes.
    /// The bool is true if the event is fired on the server.
    /// </summary>
    public static event Action<SyncDictionaryOperation, int, NetworkedPlayer, bool> OnPlayersListChanged;
    private void Awake() {
        if (Instance != null && Instance != this) {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // Subscribe to the OnChange event of our SyncDictionary.
        Players.OnChange += OnPlayersDictionaryChange;
    }

    private void OnDestroy() {
        if (Players != null) {
            Players.OnChange -= OnPlayersDictionaryChange;
        }
    }
    public override void OnStartServer() {
        base.OnStartServer();
        InstanceFinder.ServerManager.OnRemoteConnectionState += HandleRemoteConnectionState;
    }
    public override void OnStopServer() {
        base.OnStopServer();
        if (base.ServerManager != null) InstanceFinder.ServerManager.OnRemoteConnectionState -= HandleRemoteConnectionState;
    }

    /// <summary>
    /// This callback is fired by the SyncDictionary whenever its contents change.
    /// We use it to fire our own cleaner, static event.
    /// </summary>
    private void OnPlayersDictionaryChange(SyncDictionaryOperation op, int key, NetworkedPlayer value, bool asServer) {
        OnPlayersListChanged?.Invoke(op, key, value, asServer);
    }

    #region Server-Side Logic

 

    private void HandleRemoteConnectionState(NetworkConnection conn, RemoteConnectionStateArgs args) {
        // If a client has disconnected, their state will be Stopped.
        if (args.ConnectionState == RemoteConnectionState.Stopped) {
            // If the player was in our list, remove them. The SyncDictionary will
            // automatically propagate this change to all clients.
            if (Players.ContainsKey(conn.ClientId)) {
                Players.Remove(conn.ClientId);
                Debug.Log($"Player {conn.ClientId} disconnected and was removed from the list.");
            }
        }
    }

    /// <summary>
    /// [SERVER-ONLY] Called by a player object when it has been fully initialized on the client.
    /// </summary>
    [Server]
    public void Server_RegisterPlayer(NetworkConnection conn, NetworkedPlayer playerSetup) {
        // Add the player to the sync dictionary. This change will be sent to all clients.
        Players.Add(conn.ClientId, playerSetup);
        playerSetup.gameObject.name = $"PlayerOnline: {conn.ClientId}";
        Debug.Log($"Player {conn.ClientId} registered successfully.");
    }

    #endregion

    #region Public Accessors (Client & Server)

    /// <summary>
    /// Gets a list of all currently registered players.
    /// </summary>
    public List<NetworkedPlayer> GetAllPlayers() {
        return Players.Values.ToList();
    }

    /// <summary>
    /// Tries to get the player setup associated with a specific ClientId.
    /// </summary>
    public bool TryGetPlayer(int clientId, out NetworkedPlayer playerSetup) {
        return Players.TryGetValue(clientId, out playerSetup);
    }

    #endregion
}
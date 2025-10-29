using FishNet.Connection;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
/*
 How it works:
 - Host calls HostSetGameSettings(...) which sets SyncVars and modifies the SyncList of modifiers on the server.
 - Clients call SubmitLocalCharacterData(...) which calls a ServerRpc with primitives; the server writes into the
   SyncDictionary<int, CharacterDataSimple> keyed by connection.ClientId.
 - Other systems subscribe to OnHostSettingsChanged and OnPlayerCharacterDataChanged
*/

[DefaultExecutionOrder(-100)]
public class GameSetupManager : NetworkBehaviour {
    public static GameSetupManager LocalInstance { get; private set; }

    private readonly SyncVar<GameSettings> _worldSettings = new();
    private readonly SyncList<ushort> _enabledModifiers = new();
    // int is clientID
    private readonly SyncDictionary<int, CharacterData> _playerData = new();
    private GameSettings _cachedHostSettings;

    public event Action<GameSettings> OnHostSettingsChanged;
    public event Action<int, CharacterData> OnPlayerCharacterDataChanged;

    // TODO remove this
    private string _upgradeTreeName = "DefaultTree"; // Would depend on what the player chooses for tools etc
    public string GetUpgradeTreeName() => _upgradeTreeName;
    private void Awake() {
        if (LocalInstance != null && LocalInstance != this) {
            Debug.LogWarning("Multiple GameSetupManager instances present. Destroying duplicate.");
            Destroy(this);
            return;
        }
        LocalInstance = this;
        // If we later want some kind of lobby scene we would have to have the _enabledModifiers.OnChange, this will let everyone see the level select at once
    }
    // Client initialization: build a local snapshot from SyncTypes once they are available
    public override void OnStartClient() {
        base.OnStartClient();
        _cachedHostSettings = _worldSettings.Value;
        // Notify listeners once with the full snapshot
        if (_cachedHostSettings != null)
            OnHostSettingsChanged?.Invoke(_cachedHostSettings);

        // Notify listeners for every player currently present in the SyncDictionary
        foreach (var kv in _playerData)
            OnPlayerCharacterDataChanged?.Invoke(kv.Key, kv.Value);
    }

    #region Host (Server) API

    /// <summary>
    /// Host-only: set authoritative game settings. Call this on the hosting client (server).
    /// SyncTypes will propagate changes to clients automatically.
    /// </summary>
    public void HostSetGameSettings(GameSettings settings) {
        if (!IsServerInitialized) {
            Debug.LogWarning("HostSetGameSettings called on non-server. Use only on host/server.");
            return;
        }

        _worldSettings.Value = settings;

        // replace modifiers atomically: clear then add
        _enabledModifiers.Clear();
        if (settings.EnabledModifierIds != null) {
            for (int i = 0; i < settings.EnabledModifierIds.Length; i++)
                _enabledModifiers.Add(settings.EnabledModifierIds[i]);
        }

        // Update local cache and notify
        _cachedHostSettings = settings;
        OnHostSettingsChanged?.Invoke(_cachedHostSettings);
    }

    #endregion

    #region Client API

    /// <summary>
    /// Called by a client (local player) to submit their chosen character config.
    /// This sends a ServerRpc with primitive parameters; server will update the SyncDictionary.
    /// </summary>
    public void SubmitLocalCharacterData(ushort characterId, ushort cosmeticId, ushort[] startingEquipmentIds) {
        // Optimistically update local cache if we already know our client id
        if (IsClientInitialized) {
            int localId = Owner?.ClientId ?? -1;
            if (localId != -1) {
                var local = new CharacterData {
                    CharacterId = characterId,
                    CosmeticId = cosmeticId,
                    StartingEquipmentIds = startingEquipmentIds ?? Array.Empty<ushort>()
                };

                // Local cache will be overwritten by server sync when it arrives, but this is useful for immediate UI
                OnPlayerCharacterDataChanged?.Invoke(localId, local);
            }
        }

        // Send to server to be accepted and stored in the authoritative SyncDictionary
        SubmitCharacterDataServerRpc(characterId, cosmeticId, startingEquipmentIds);
    }

    /// <summary>
    /// Convenience getters for other systems
    /// </summary>
    public bool TryGetHostSettings(out GameSettings settings) {
        if (_cachedHostSettings == null && (_worldSettings.Value != default || _enabledModifiers.Count > 0)) {
            // Build cache from SyncTypes if not already cached
            //settings = new GameSettings {
            // WorldSeed = _worldSettings.Value,
            //    EnabledModifierIds = _enabledModifiers.Count > 0 ? _enabledModifiers.ToArray() : Array.Empty<ushort>()
            //};
            //_cachedHostSettings = settings;
            settings = null;
        }

        settings = _cachedHostSettings;
        return settings != null;
    }

    public bool TryGetPlayerData(int clientId, out CharacterData data) {
        return _playerData.TryGetValue(clientId, out data);
    }

    public IReadOnlyDictionary<int, CharacterData> AllPlayerData => _playerData;

    #endregion


    [ServerRpc(RequireOwnership = false)]
    private void SubmitCharacterDataServerRpc(ushort characterId, ushort cosmeticId, ushort[] startingEquipmentIds, NetworkConnection conn = null) {
        if (conn == null) return;

        int clientId = conn.ClientId;

        var parsed = new CharacterData {
            CharacterId = characterId,
            CosmeticId = cosmeticId,
            StartingEquipmentIds = startingEquipmentIds ?? Array.Empty<ushort>()
        };

        // Server authoritative: write into the SyncDictionary. This will automatically propagate to clients.
        _playerData[clientId] = parsed;
    }


}

[Serializable]
public class GameSettings {
    public int WorldSeed;
    public ushort WorldGenID;
    public ushort[] EnabledModifierIds = Array.Empty<ushort>();

    public GameSettings(int worldSeed, ushort[] enabledModifierIds) {
        WorldSeed = worldSeed;
        EnabledModifierIds = enabledModifierIds;
    }
    public GameSettings(WorldGenSettingSO worldGenData) {
        WorldSeed = worldGenData.seed;
        EnabledModifierIds = Array.Empty<ushort>();
    }

    public GameSettings() {
    }
}

[Serializable]
public struct CharacterData {
    public ushort CharacterId;
    public ushort CosmeticId;
    public ushort[] StartingEquipmentIds;
}

public enum Difficulty {
    Easy = 0,
    Normal = 1,
    Hard = 2,
    Custom = 3
}
  
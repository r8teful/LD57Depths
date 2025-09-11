using FishNet.Object.Synchronizing;
using FishNet.Object;
using System.Collections.Generic;
using System;
using UnityEngine;
[System.Serializable]
public class StatDefault {
    public StatType Stat;
    public float BaseValue;
}
public enum StatType {
    // MINING
    MiningRange,
    MiningDamage,
    MiningHandling,

    // PLAYER
    PlayerSpeedMax,
    PlayerAcceleration,
    PlayerOxygenMax,
    PlayerLightRange,
    PlayerLightIntensity
}
[RequireComponent(typeof(NetworkedPlayer))]
public class PlayerStatsManager : NetworkBehaviour, INetworkedPlayerModule {
    [Header("Configuration")]
    [Tooltip("Define the base values for all stats here. These are the starting values before any upgrades.")]
    [SerializeField] private List<StatDefault> _baseStats;

    private readonly SyncDictionary<StatType, float> _finalStats = new(); // Server stored for each client

    public int InitializationOrder => 99;

    // The crucial event system. Other components (like PlayerMovement, ToolController)
    // will listen to this to know when a stat they care about has changed.
    public event Action<StatType, float> OnStatChanged;

    #region Initialization
    public void InitializeOnOwner(NetworkedPlayer playerParent) {
        InitializeStats();
    }
    public override void OnStartClient() {
        base.OnStartClient();

        // Subscribe to the OnChange event of the SyncDictionary.
        // This allows us to fire our local C# event whenever a stat changes,
        // which is essential for updating logic on ALL clients (owner and observers).
        _finalStats.OnChange += OnFinalStatChanged;

        if (!base.IsOwner) {
            // For non-owners, we need to populate initial values and fire events
            // for any stats that are already in the synced dictionary.
            foreach (var kvp in _finalStats) {
                OnStatChanged?.Invoke(kvp.Key, kvp.Value);
            }
        }
    }

    public override void OnStopClient() {
        base.OnStopClient();
        if (_finalStats != null) {
            _finalStats.OnChange -= OnFinalStatChanged;
        }
    }

    private void InitializeStats() {
        foreach (var statDefault in _baseStats) {
            if (!_finalStats.ContainsKey(statDefault.Stat)) {
                _finalStats.Add(statDefault.Stat, statDefault.BaseValue);
            } else {
                Debug.LogWarning($"Stat {statDefault.Stat} is already initialized. Check for duplicates in Base Stats list.");
            }
        }
    }

    #endregion

    #region Public API

    /// <summary>
    /// The primary way for other scripts to get a stat value.
    /// </summary>
    public float GetStat(StatType stat) {
        if (_finalStats.TryGetValue(stat, out float value)) {
            return value;
        } else {
            Debug.LogWarning($"Attempted to get stat '{stat}' but it was not initialized. Returning 0.");
            return 0f;
        }
    }

    /// <summary>
    /// The method used by UpgradeEffects to modify a stat.
    /// This should ONLY be called on the owning client.
    /// </summary>
    public void ModifyStat(StatType stat, float value, IncreaseType increaseType) {
        if (!base.IsOwner) {
            Debug.LogWarning("ModifyStat was called on a non-owning client. This should not happen in a client-authoritative model.");
            return;
        }

        if (!_finalStats.ContainsKey(stat)) {
            Debug.LogError($"Cannot modify stat '{stat}' because it has not been initialized.");
            return;
        }

        // Here you would implement your calculation logic.
        // This is a placeholder for your UpgradeCalculator.
        float currentValue = _finalStats[stat];
        float newValue = UpgradeCalculator.CalculateUpgradeIncrease(currentValue, increaseType, value);

        // Updating the SyncDictionary will automatically send the change over the network.
        _finalStats[stat] = newValue;
    }

    #endregion

    #region Event Handling

    private void OnFinalStatChanged(SyncDictionaryOperation op, StatType key, float value, bool asServer) {
        // This method is called on ALL clients whenever the dictionary changes.
        switch (op) {
            case SyncDictionaryOperation.Add:
            case SyncDictionaryOperation.Set:
                OnStatChanged?.Invoke(key, value);
                break;
            case SyncDictionaryOperation.Remove:
                // Handle stat removal if that's a feature you need
                break;
            case SyncDictionaryOperation.Clear:
                // Handle dictionary being cleared
                break;
        }
    }
    #endregion
}
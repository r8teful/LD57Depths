using FishNet.Object;
using FishNet.Object.Synchronizing;
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections.LowLevel.Unsafe;
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
    MiningRotationSpeed,
    MiningKnockback,
    MiningFalloff,

    // PLAYER
    PlayerSpeedMax,
    PlayerAcceleration,
    PlayerOxygenMax,
    PlayerLightRange,
    PlayerLightIntensity
}
[RequireComponent(typeof(NetworkedPlayer))]
public class PlayerStatsManager : NetworkBehaviour, INetworkedPlayerModule {
    [SerializeField] private PlayerBaseStatsSO _baseStats;

    private readonly SyncDictionary<StatType, float> _finalStats = new(); // Permament + modifiers

    private readonly SyncDictionary<StatType, float> _permanentStats = new(); // Just stats without modifiers, we REVERT to this
    
    // This list ONLY exists on the owning client. It does not need to be synced
    // because only the owner calculates their stats and syncs the result via _finalStats.
    private readonly List<StatModifier> _activeModifiers = new();
    
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
        foreach (var statDefault in _baseStats.BaseStats) {
            if (!_permanentStats.ContainsKey(statDefault.Stat)) {
                _permanentStats.Add(statDefault.Stat, statDefault.BaseValue);
                // After initializing, we must calculate the final stat for the first time.
                RecalculateStat(statDefault.Stat);
            } else {
                Debug.LogWarning($"Stat {statDefault.Stat} is already initialized. Check for duplicates in Base Stats list.");
            }
        }
    }

    #endregion

    private void RecalculateStat(StatType stat) {
        if (!base.IsOwner) return;
        if (!_permanentStats.ContainsKey(stat)) return;

        float finalValue = _permanentStats[stat];

        // 1. Apply all ADDITIVE modifiers first.
        foreach (var mod in _activeModifiers) {
            if (mod.Stat == stat && mod.Type == IncreaseType.Add) {
                finalValue += mod.Value;
            }
        }

        // 2. Apply all MULTIPLIER modifiers next.
        foreach (var mod in _activeModifiers) {
            if (mod.Stat == stat && mod.Type == IncreaseType.Multiply) {
                finalValue *= mod.Value;
            }
        }

        // Update the synced dictionary. This triggers the OnChange event on all clients.
        _finalStats[stat] = finalValue;
    }
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
    public void ModifyPermamentStat(StatType stat, float value, IncreaseType increaseType) {
        if (!base.IsOwner) {
            Debug.LogWarning("ModifyStat was called on a non-owning client. This should not happen in a client-authoritative model.");
            return;
        }

        if (!_finalStats.ContainsKey(stat)) {
            Debug.LogError($"Cannot modify stat '{stat}' because it has not been initialized.");
            return;
        }

        float currentValue = _permanentStats[stat];
        float newValue = UpgradeCalculator.CalculateUpgradeChange(currentValue, increaseType, value);

        _permanentStats[stat] = newValue;
        RecalculateStat(stat);
    }
    /// <summary>
    /// Adds a temporary stat modifier (e.g., from an ability or location boost).
    /// </summary>
    public void AddModifier(StatModifier modifier) {
        if (!base.IsOwner) return;

        _activeModifiers.Add(modifier);
        RecalculateStat(modifier.Stat);
    }
    public void AddModifiers(IEnumerable<StatModifier> modifiers) {
        if (!base.IsOwner) return;

        var affectedStats = new HashSet<StatType>();
        foreach (var mod in modifiers) {
            _activeModifiers.Add(mod);
            affectedStats.Add(mod.Stat);
        }

        foreach (var stat in affectedStats) {
            RecalculateStat(stat);
        }
    }  
    /// <summary>
    /// Removes all temporary modifiers that came from a specific source.
    /// </summary>
    public void RemoveModifiersFromSource(object source) {
        if (!base.IsOwner) return;

        // Find which stats will be affected BEFORE we remove the modifiers
        var statsToRecalculate = _activeModifiers
            .Where(mod => mod.Source == source)
            .Select(mod => mod.Stat)
            .Distinct()
            .ToList();

        _activeModifiers.RemoveAll(mod => mod.Source == source);

        // Now recalculate only the stats that were changed
        foreach (var stat in statsToRecalculate) {
            RecalculateStat(stat);
        }
    }
    #endregion

    #region Event Handling

    private void OnFinalStatChanged(SyncDictionaryOperation op, StatType key, float value, bool asServer) {
        // This method is called on ALL clients whenever the dictionary changes.
        Debug.Log("STAT CHANGE!");
        switch (op) {
            case SyncDictionaryOperation.Add:
                OnStatChanged?.Invoke(key, value);
                break;
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
#if UNITY_EDITOR
    public void DEBUGSetStat(StatType stat, float value) {
        _finalStats[stat] = value;
    }
#endif
}
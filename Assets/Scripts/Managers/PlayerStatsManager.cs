using FishNet.Connection;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
[System.Serializable]
public class StatDefault {
    public StatType Stat;
    public float BaseValue;
}
public enum StatType {
    // MINING
    MiningRange = 0,
    MiningDamage = 1,
    MiningRotationSpeed = 2,
    MiningKnockback = 3,
    MiningFalloff = 4,
    MiningCombo = 5,

    // Lazer blast
    BlastDamage = 10,
    BlastRecharge = 11,
    BlastDuration = 12,
    BlastRange = 13,

    // PLAYER 
    PlayerSpeedMax = 20,
    PlayerAcceleration = 21,
    PlayerDrag = 22,
    PlayerMagnetism = 23,
    PlayerOxygenMax = 24,
    
    // Player dash
    DashSpeed = 30,
    DashRecharge = 31,
    DashDistance = 32,
    
    // Block oxygen
    BlockOxygenReleased = 40,
    BlockOxygenChance = 41
}
// We use this instance to get RUNTIME information about the buff, we'll need it for UI
public class ActiveBuff {
    public AbilityBaseSO ability;
    public float timeRemaining; // -1 for conditional

    public BuffHandle handle;
    public Action onConditionEnd;
    internal float startTime;
    internal float duration;
    internal float endTime;
    internal object source;
}
// We return this object which other classes can utilize, it's a very cool class, and very usefull! I'm understanding more complicated concepts lol
public sealed class BuffHandle {
    public AbilityBaseSO ability;
    public Action onRemove; // called when buff ends
    public void Remove() {
        onRemove?.Invoke();
    }
}
[RequireComponent(typeof(NetworkedPlayer))]
public class PlayerStatsManager : NetworkBehaviour, INetworkedPlayerModule {
    [SerializeField] private PlayerBaseStatsSO _baseStats;

    private readonly SyncDictionary<StatType, float> _finalStats = new(); // Permament + modifiers

    private readonly SyncDictionary<StatType, float> _permanentStats = new(); // Just stats without modifiers, we REVERT to this
    
    // This list ONLY exists on the owning client. It does not need to be synced
    // because only the owner calculates their stats and syncs the result via _finalStats.
    private readonly List<ActiveBuff> _activeBuffs = new(); // This now changes to the active buff instances
    private readonly Dictionary<AbilityBaseSO, ActiveBuff> _buffLookup = new();
    // activeModifers 

    private bool _isInitialized = false;
    public bool IsInitialized => _isInitialized;

    public event Action OnInitialized;
    public int InitializationOrder => 91;

    // The crucial event system. Other components (like PlayerMovement, ToolController)
    // will listen to this to know when a stat they care about has changed.
    public event Action<StatType, float> OnStatChanged;
    public event Action OnBuffAdded;

    #region Initialization
    public void InitializeOnOwner(NetworkedPlayer playerParent) {
        StatType[] statTypes = _baseStats.BaseStats.Select(s => s.Stat).ToArray();
        float[] baseValues = _baseStats.BaseStats.Select(s => s.BaseValue).ToArray();

        ServerInitializeStats(statTypes, baseValues);
        //InitializeStats();
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
        _finalStats.OnChange -= OnFinalStatChanged;
    }
    [ServerRpc(RequireOwnership = true)]
    private void ServerInitializeStats(StatType[] statTypes, float[] baseValues) {
        if (_permanentStats.Count > 0) {
            Debug.LogWarning("ServerInitializeStats called, but stats are already initialized.");
            return; // Already initialized, do nothing.
        }
        for (int i = 0; i < statTypes.Length; i++) {
            StatType stat = statTypes[i];
            float value = baseValues[i];

            if (!_permanentStats.ContainsKey(stat)) {
                _permanentStats.Add(stat, value);
                _finalStats.Add(stat, value); // Initially, final stats are the same as permanent.
            }
        }
        foreach (var statDefault in _baseStats.BaseStats) {
            if (!_permanentStats.ContainsKey(statDefault.Stat)) {
                _permanentStats.Add(statDefault.Stat, statDefault.BaseValue);
                // After initializing, we must calculate the final stat for the first time.
                RecalculateStat(statDefault.Stat);
            } else {
                Debug.LogWarning($"Stat {statDefault.Stat} is already initialized. Check for duplicates in Base Stats list.");
            }
        }
        Debug.Log("Server init done for " + Owner.ClientId);
        RpcInitDone(Owner);

    }
    [TargetRpc]
    private void RpcInitDone(NetworkConnection conn) {
        _isInitialized = true;
        Debug.Log("RPC recieved init donw!");
        // Fire the event to notify all other scripts that they can now safely access stats.
        OnInitialized?.Invoke();

        // Fire the OnStatChanged event for every stat so that UI and other systems
        // can grab their initial values.
        foreach (var kvp in _finalStats) {
            OnStatChanged?.Invoke(kvp.Key, kvp.Value);
        }
    }
    #endregion

    private void RecalculateStat(StatType stat) {
        if (!base.IsOwner) return;
        if (!_permanentStats.ContainsKey(stat)) return;

        float permanentValue = _permanentStats[stat];

        float finalValue = permanentValue;
        // 1. Apply all ADDITIVE modifiers first.
        foreach (var buff in _activeBuffs) {
            foreach (var mod in buff.ability.Modifiers) {
                if (mod.Stat == stat && mod.Type == IncreaseType.Add) {
                    finalValue += mod.Value;
                }
            }
        }

        // 2. Apply all MULTIPLIER modifiers next.
        foreach (var buff in _activeBuffs) {
            foreach (var mod in buff.ability.Modifiers) {
                if (mod.Stat == stat && mod.Type == IncreaseType.Multiply) {
                    finalValue *= mod.Value;
                }
            }
        }
        ServerUpdateFinalStat(stat, finalValue);
    }
    #region Public API

    /// <summary>
    /// The primary way for other scripts to get a stat value.
    /// </summary>
    public float GetStat(StatType stat) {
        if (!_isInitialized) {
            Debug.LogWarning($"Attempted to GetStat({stat}) before PlayerStatsManager was initialized!");
            // Return a sensible default from the local SO if possible.
            var statDefault = _baseStats.BaseStats.FirstOrDefault(s => s.Stat == stat);
            return statDefault?.BaseValue ?? 0f;
        }
        if (_finalStats.TryGetValue(stat, out float value)) {
            return value;
        } else {
            Debug.LogWarning($"Attempted to get stat '{stat}' but it was not initialized. Returning 0.");
            return 0f;
        }
    }
    /// <summary>
    /// Get the stat without any of the extra modifiers attached to it, useful for UI
    /// </summary>
    public float GetStatBase(StatType stat) {

        if (!_isInitialized) {
            Debug.LogWarning($"Attempted to GetStat({stat}) before PlayerStatsManager was initialized!");
            // Return a sensible default from the local SO if possible.
            var statDefault = _baseStats.BaseStats.FirstOrDefault(s => s.Stat == stat);
            return statDefault?.BaseValue ?? 0f;
        }
        if (_permanentStats.TryGetValue(stat, out float value)) {
            Debug.Log($"Getting base stat for {stat} ... returned {value}");
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
        Debug.Log($"Modifying permanent stat {stat} from {currentValue} to {newValue}!");
        //RecalculateStat(stat); // We could do this here, we recalcualte with an "override" with the new value, this way, we get instant feedback
        ServerUpdatePermanentStat(stat, newValue);
        _permanentStats[stat] = newValue; // BADDD?? I DONT KNOW BUT ITS NOT ACUTALLY CHANGING IT ON THE SERVER IN TIME FOR ME TO SEE IT ON THE UPGRADE UI SCREEN POP WHEN I PURCHASE THE UPGRADE
    }
    [ServerRpc(RequireOwnership = true)]
    private void ServerUpdatePermanentStat(StatType stat, float newValue) {
        _permanentStats[stat] = newValue;
    }

    [ServerRpc(RequireOwnership = true)]
    private void ServerUpdateFinalStat(StatType stat, float newValue) {
        // Update the synced dictionary. This triggers the OnChange event on all clients.
        _finalStats[stat] = newValue;
    }

    // Its wierd we used to add and track modifiers in a list, but now we just
    // track the entire buff, so here instead we just recalculate the new modifiers we've added
    public void RecalculateModifiers(IEnumerable<StatModifier> modifiers) {
        if (!base.IsOwner) return;

        var affectedStats = new HashSet<StatType>();
        foreach (var mod in modifiers) {
            affectedStats.Add(mod.Stat);
        }
        foreach (var stat in affectedStats) {
            RecalculateStat(stat);
        }
    }
    internal void AddTimedModifiers(List<StatModifier> modifiersToAdd, object source, float duration, Action onComplete) {
        StartCoroutine(AddTimedModifiersRoutine(modifiersToAdd, duration, source, onComplete));
    }
    internal BuffHandle TriggerAbility(AbilityBaseSO ability, Action externalConditionEnd, object source) {
        // Prevent duplicates unless ability is explicitly stackable
        if (_buffLookup.TryGetValue(ability, out var existing)) {
            Debug.Log("Buff already applied. What would you like to happen? CODE IT!!");
            return null;
            // TODO
            //if (ability.refreshOnReapply) {
            //    if (existing.duration > 0) {
            //        existing.startTime = Time.time;
            //        existing.duration = durationOverride ?? ability.duration;
            //        existing.endTime = existing.startTime + existing.duration;
            //        OnBuffUpdated?.Invoke();
            //    }
            //}
            //return existing.handle;
        }
        // Create runtime buff instance
        var buff = new ActiveBuff {
            ability = ability,
            startTime = Time.time,
            duration = ability.Duration,
            endTime = ability.Duration > 0 ? Time.time + ability.Duration : -1f, // indefinite
            source = source
        };

        var modifiersToAdd = new List<StatModifier>();
        foreach (var modData in ability.Modifiers) {
            modifiersToAdd.Add(new StatModifier(modData.Value, modData.Stat, modData.Type, ability));
        }
        RecalculateModifiers(modifiersToAdd);

        // Conditional? Subscribe to external condition so we can remove it when its met
        if (ability.Type == AbilityType.Conditional && externalConditionEnd != null) {
            buff.onConditionEnd = externalConditionEnd;
            externalConditionEnd += () => RemoveModifiersFromSource(ability);
        }
        // Build handle
        buff.handle = new BuffHandle {
            ability = ability,
            onRemove = () => RemoveModifiersFromSource(ability)
        };
        _activeBuffs.Add(buff);
        _buffLookup[ability] = buff;

        OnBuffAdded?.Invoke();
        return buff.handle;
        //AddTimedModifiers(modifiersToAdd, ability, ability.Duration, externalConditionEnd);

        //OnPlayerAbilityStart?.Invoke(ability);
        // HERE, now we have the actual SO, with data about the ability, now set that as the visual info 
        // HOW? Could do it with an event, or direct call, event seems clean 
    }
    /// <summary>
    /// Removes all temporary modifiers that came from a specific source.
    /// </summary>
    public void RemoveModifiersFromSource(object source) {
        if (!base.IsOwner) return;

        // Find which stats will be affected BEFORE we remove the modifiers
        // l

        List<StatType> statsToRecalculate = new();
        foreach(var buff in _activeBuffs) {
            if (buff.source != source) continue;
            foreach (var mod in buff.ability.Modifiers) {
                statsToRecalculate.Add(mod.Stat);
            }
        }
        statsToRecalculate.Distinct();
        //   .Where(mod => mod.source == source)
        //   .Select(mod => mod.ability.Modifiers.)
        //   .Distinct()
        //   .ToList();
        _activeBuffs.RemoveAll(buff => buff.source == source); //  IDK ???

        // Now recalculate only the stats that were changed
        foreach (var stat in statsToRecalculate) {
            RecalculateStat(stat);
        }
    }


    public MiningToolData GetToolData() {
        return new MiningToolData {
            ToolRange = GetStat(StatType.MiningRange),
            ToolWidth = Mathf.Min(GetStat(StatType.MiningDamage) * 0.02f, 1f), //_isUsingAbility ? Mathf.Min(DamagePerHit * 0.3f, 0.6f) : 0.05f * DamagePerHit, // OLD
            toolTier = 0 //TODO
        };
    }

    #endregion
    private IEnumerator AddTimedModifiersRoutine(List<StatModifier> modifiersToAdd, float duration, object source, Action onComplete) {
        RecalculateModifiers(modifiersToAdd); // Add first
        yield return new WaitForSeconds(duration); // Wait
        RemoveModifiersFromSource(source); // Remove;
        onComplete?.Invoke(); // Notify
    }
    
    private void OnFinalStatChanged(SyncDictionaryOperation op, StatType key, float value, bool asServer) {
        // This method is called on ALL clients whenever the dictionary changes.
       // Debug.Log("STAT CHANGE!");
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
#if UNITY_EDITOR || DEVELOPMENT_BUILD
    public void DEBUGSetStat(StatType stat, float value) {
        _permanentStats[stat] = value;
    }
#endif
}
// Should probably not be here, or be there at all, working on a solution!!!
// Created and sent to the Visual part so that we know how to draw it properly
public struct MiningToolData {
    public float ToolRange;
    public float ToolWidth;
    public int toolTier;
    // Add more as needed
}
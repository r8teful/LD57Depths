using FishNet.Connection;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using UnityEngine;

// We use this instance to get RUNTIME information about the buff, we'll need it for UI,
// but also if we increase the buff strength we'll modify its Stat modifiers
public class BuffInstance {
    public ushort buffID;
    public float timeRemaining; // -1 for conditional
    public BuffHandle handle;
    internal float startTime;
    internal float duration;
    internal float expiresAt;
    public List<StatModifier> RuntimeModifiers { get; private set; } // Can be null and then we just use BuffSO.Modifiers

    public BuffSO GetBuffData() => App.ResourceSystem.GetBuffByID(buffID);
    public bool IsExpired => Time.time >= expiresAt;
    public BuffInstance() { }

    // Factory: create a BuffInstance from a BuffSO
    public static BuffInstance CreateFromSO(BuffSO so, float durationOverride = -1f) {
        var inst = new BuffInstance {
            buffID = so.ID,
            startTime = Time.time,
            duration = durationOverride > 0f ? durationOverride : so.Duration,
            timeRemaining = durationOverride > 0f ? durationOverride : so.Duration,
            expiresAt = (durationOverride > 0f ? Time.time + durationOverride :
                       so.Duration > 0 ? Time.time + so.Duration : -1)
        };

        // deep copy modifiers
        // NO, when we create it, we don't have any runtime modfiers. Runtime modifiers get added when we UPGRADE it. that is the whole point of this
        inst.RuntimeModifiers = so.Modifiers?.Select(
            m => new StatModifier(m.Value, m.Stat, m.Type, so.ID)).ToList() ?? new List<StatModifier>();

        return inst;
    }
    /// <summary>
    /// Use this only if you want to change the strength of the buff
    /// </summary>
    /// <param name="source"></param>
    public void ApplyAbilityInstanceModifiers(AbilityInstance source) {
        if (RuntimeModifiers == null) return; 
        foreach (var mod in RuntimeModifiers) {
            // get totals from the source (helpers below)
            float flatFromSource = source.GetTotalFlatModifier(mod.Stat);     // e.g. Mining knockback + 20
            float percentFromSource = source.GetTotalPercentModifier(mod.Stat); // e.g. Damage + 20%

            // It doesn't work when you say (mod.Type == IncreaseType.Add) because it needs to be the source mod more (or something!?)
            // This is the problem:
            // For the brimstone effect, if we upgrade it twice, flatFromSource will be 0.4 (0.2+0.2) 
            // mod.Value is 1.5, so we would need to ADD it to 1.5. But, the mod.Type doesn't determine how we add 
            // flatFromSource to the base ( or percentFromSource ), I want it to be that because flatFromSource is added using (0.2+0.2)
            // that would also mean that the 0.4 needs to be ADDED to the 1.5 value. Its all very fucking annoying and we need a clear way to say, 
            // This number will add a procent, and this number will add a numeric value. That's it. 
            if (flatFromSource > percentFromSource) {
                // treat Value as a flat base
                // newValue = (base + flatFromSource) * (1 + percentFromSource)
                float baseVal = mod.Value;
                baseVal += flatFromSource;
                baseVal *= (1f + percentFromSource);
                mod.Value = baseVal;
            } else { // Percent mode
                // treat mod.Value as a percent (e.g. 0.5 for +50%)
                float combinedPercent = mod.Value + percentFromSource;
                // Optionally also incorporate flatFromSource in a sensible way:
                // if flatFromSource should affect absolute value, you'd need the base stat value.
                mod.Value = combinedPercent;
            }
        }
    }
    public StatModifier GetModifierFor(StatType stat) {
        return RuntimeModifiers.FirstOrDefault(m => m.Stat == stat);
    }
}
// We return this object which other classes can utilize, it's a very cool class, and very usefull! I'm understanding more complicated concepts lol
public sealed class BuffHandle {
    public ushort buffID;
    private readonly Action removeAction; // this will call StatsManager.RemoveAbility(id)
    public Action OnRemoved; // called my StatsManager when buff ends, suscribe to this from other scripts to handle buff end
    public BuffHandle(ushort abilityId, Action removeAction) {
        this.buffID = abilityId;
        this.removeAction = removeAction;
    }
    public void Remove() { // Can be called from other scripts to request removal 
        removeAction?.Invoke();
    }
    /// <summary>Called by the StatsManager when the buff is actually removed (expiry / conditional / manual).</summary>
    internal void NotifyRemoved() {
        try { OnRemoved?.Invoke(); } catch (Exception ex) { UnityEngine.Debug.LogException(ex); }
    }
    public BuffSO GetAbilityData() => App.ResourceSystem.GetBuffByID(buffID);
}
[RequireComponent(typeof(NetworkedPlayer))]
public class PlayerStatsManager : NetworkBehaviour, INetworkedPlayerModule {
    [SerializeField] private PlayerBaseStatsSO _baseStats;

    private readonly SyncDictionary<StatType, float> _finalStats = new(); // Permament + modifiers

    private readonly SyncDictionary<StatType, float> _rawStats = new(); // Just stats without modifiers, we REVERT to this
    
    // This list ONLY exists on the owning client. It does not need to be synced
    // because only the owner calculates their stats and syncs the result via _finalStats.
    private readonly List<BuffInstance> _activeBuffs = new(); // This now changes to the active buff instances
    private readonly Dictionary<ushort, BuffInstance> _activeBuffsByID = new();
    // activeModifers 

    private bool _isInitialized = false;
    private List<BuffSnapshot> _snapshotCache = new();
    private float _uiAccumulator;
    const float UI_UPDATE_INTERVAL = 0.2f; // Ui updates every 0.2s
    public bool IsInitialized => _isInitialized;

    public event Action OnInitialized;
    public int InitializationOrder => 91;

    // The crucial event system. Other components (like PlayerMovement, ToolController)
    // will listen to this to know when a stat they care about has changed.
    public event Action<StatType, float> OnStatChanged;

    public event Action OnBuffListChanged;  // add/remove
    public event Action OnBuffsUpdated;     // periodic tick

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
        if (_rawStats.Count > 0) {
            Debug.LogWarning("ServerInitializeStats called, but stats are already initialized.");
            return; // Already initialized, do nothing.
        }
        for (int i = 0; i < statTypes.Length; i++) {
            StatType stat = statTypes[i];
            float value = baseValues[i];

            if (!_rawStats.ContainsKey(stat)) {
                _rawStats.Add(stat, value);
                _finalStats.Add(stat, value); // Initially, final stats are the same as permanent.
            }
        }
        foreach (var statDefault in _baseStats.BaseStats) {
            if (!_rawStats.ContainsKey(statDefault.Stat)) {
                _rawStats.Add(statDefault.Stat, statDefault.BaseValue);
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

    void Update() {
        // Handle timed expirations
        if (_activeBuffs.Count > 0) {
            for (int i = _activeBuffs.Count - 1; i >= 0; i--) {
                var b = _activeBuffs[i];
                if (b.duration > 0 && Time.time >= b.expiresAt) {
                    RemoveBuff(b.buffID);
                }
            }
        }

        // periodic UI update event
        _uiAccumulator += Time.deltaTime;
        if (_uiAccumulator >= UI_UPDATE_INTERVAL) {
            _uiAccumulator = 0f;
            OnBuffsUpdated?.Invoke();
        }
    }

    private void RecalculateStat(StatType stat) {
        if (!base.IsOwner) return;
        if (!_rawStats.ContainsKey(stat)) return;

        float permanentValue = _rawStats[stat];
        float finalValue = permanentValue;

        // 1. Apply all ADDITIVE modifiers first.
        foreach (var buff in _activeBuffs) {
            foreach (var mod in buff.GetBuffData().Modifiers) {
                if (mod.Stat == stat && mod.Type == StatModifyType.Add) {
                    finalValue += mod.Value;
                }
            }
        }

        // 2. Apply all MULTIPLIER modifiers next.
        foreach (var buff in _activeBuffs) {
            foreach (var mod in buff.GetBuffData().Modifiers) {
                if (mod.Stat == stat && mod.Type == StatModifyType.Multiply) {
                    finalValue *= mod.Value;
                }
            }
        }
        Debug.Log($"Recalculated stat {stat} from {permanentValue} to {finalValue}");
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
        if (_rawStats.TryGetValue(stat, out float value)) {
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
    public void ModifyPermamentStat(StatType stat, float value, StatModifyType increaseType) {
        if (!base.IsOwner) {
            Debug.LogWarning("ModifyStat was called on a non-owning client. This should not happen in a client-authoritative model.");
            return;
        }

        if (!_finalStats.ContainsKey(stat)) {
            Debug.LogError($"Cannot modify stat '{stat}' because it has not been initialized.");
            return;
        }
        float currentValue = _rawStats[stat];
        float newValue = UpgradeCalculator.CalculateUpgradeChange(currentValue, increaseType, value);
        Debug.Log($"Modifying permanent stat {stat} from {currentValue} to {newValue}!");
        //RecalculateStat(stat); // We could do this here, we recalcualte with an "override" with the new value, this way, we get instant feedback
        ServerUpdatePermanentStat(stat, newValue);
        _rawStats[stat] = newValue; // BADDD?? I DONT KNOW BUT ITS NOT ACUTALLY CHANGING IT ON THE SERVER IN TIME FOR ME TO SEE IT ON THE UPGRADE UI SCREEN POP WHEN I PURCHASE THE UPGRADE
        RecalculateStat(stat);
    }

    public IReadOnlyList<BuffSnapshot> GetBuffSnapshots() {
        // Snapshots are only used for UI. Would you not want it to return buffs with the same source?
        // Eg. If a biome gives two buffs, you'd want it to 
        _snapshotCache.Clear();
        foreach (var b in _activeBuffs) {
            float remaining = b.duration > 0 ? Mathf.Max(0f, b.expiresAt - Time.time) : -1f;
            float total = b.duration > 0 ? b.duration : -1f;
            _snapshotCache.Add(new BuffSnapshot {
                buffID = b.buffID,
                displayName = b.GetBuffData().Title,
                icon = b.GetBuffData().Icon,
                remainingSeconds = remaining,
                totalSeconds = total
            });
        }
        return _snapshotCache;
    }

    public float GetRemainingTime(ushort abilityId) {
        if (!_activeBuffsByID.TryGetValue(abilityId, out var b)) return -1f;
        return b.duration > 0 ? Mathf.Max(0f, b.expiresAt - Time.time) : -1f;
    }


    /// <summary>
    /// Trigger a buff, note that this will take the base buff stats, and upgrading buff isn't possible from here (yet). 
    /// </summary> 
    /// <param name="registerUnsubscribe">optional callback where StatsManager will give the caller the "remove action" so the caller can subscribe it to its own events.</param> 
    internal BuffHandle TriggerBuff(BuffSO buffData, Action<Action> registerUnsubscribe = null) {
        // Prevent duplicates unless ability is explicitly stackable
        var id = buffData.ID;
        if (_activeBuffsByID.TryGetValue(id, out var existing)) {
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
        var buff = new BuffInstance {
            buffID = id,
            startTime = Time.time,
            duration = buffData.Duration,
            expiresAt = buffData.Duration > 0 ? Time.time + buffData.Duration : -1f, // indefinite
        };

        // Actions are so fancy, so this basically points to this function which when we invoke the action will call, and we can pass the action around 
        Action removeAction = () => RemoveBuff(id);
        // Build handle
        buff.handle = new BuffHandle(id, removeAction);

        // Now we can pass the action to the other script, which can then invoke removeAction which will call RemoveAbility
        registerUnsubscribe?.Invoke(removeAction);

        _activeBuffs.Add(buff);
        _activeBuffsByID[id] = buff;
        //registerUnsubscribe?.Invoke(removeAction);// We od it either before or after 
        var modifiersToAdd = new List<StatModifier>();
        foreach (var modData in buffData.Modifiers) {
            modifiersToAdd.Add(new StatModifier(modData.Value, modData.Stat, modData.Type, buffData));
        }
        RecalculateModifiers(modifiersToAdd);

        OnBuffListChanged?.Invoke();
        return buff.handle;
        //AddTimedModifiers(modifiersToAdd, ability, ability.Duration, externalConditionEnd);

        //OnPlayerAbilityStart?.Invoke(ability);
        // HERE, now we have the actual SO, with data about the ability, now set that as the visual info 
        // HOW? Could do it with an event, or direct call, event seems clean 
    }

    private void RemoveBuff(BuffSO ability) => RemoveBuff(ability.ID);

    /// <summary>
    /// Removes all temporary modifiers that came from a specific source.
    /// </summary>
    public void RemoveBuff(ushort abilityID) {
        if (!base.IsOwner) return;

        // Find which stats will be affected BEFORE we remove the modifiers
        if (!_activeBuffsByID.TryGetValue(abilityID, out var buff)) {
            Debug.LogWarning($"Tried to remove buff with ID {abilityID} which isn't active");
            return;
        }

        List<StatType> statsToRecalculate = new();
        foreach(var mod in buff.GetBuffData().Modifiers) {
            statsToRecalculate.Add(mod.Stat);
        }
        statsToRecalculate.Distinct();

        // We do it before so we could still access activeBuffs and its details
        buff.handle?.NotifyRemoved();
        
        _activeBuffs.Remove(buff);
        _activeBuffsByID.Remove(abilityID);
        // Now recalculate only the stats that were changed
        foreach (var stat in statsToRecalculate) {
            RecalculateStat(stat);
        }

        // For UI
        OnBuffListChanged?.Invoke();
    }


    public MiningToolData GetToolData() {
        return new MiningToolData {
            ToolRange = GetStat(StatType.MiningRange),
            ToolWidth = Mathf.Min(GetStat(StatType.MiningDamage) * 0.02f, 1f), //_isUsingAbility ? Mathf.Min(DamagePerHit * 0.3f, 0.6f) : 0.05f * DamagePerHit, // OLD
            toolTier = 0 //TODO
        };
    }

    #endregion

    [ServerRpc(RequireOwnership = true)]
    private void ServerUpdatePermanentStat(StatType stat, float newValue) {
        _rawStats[stat] = newValue;
    }

    [ServerRpc(RequireOwnership = true)]
    private void ServerUpdateFinalStat(StatType stat, float newValue) {
        // Update the synced dictionary. This triggers the OnChange event on all clients.
        _finalStats[stat] = newValue;
    }

    // Its wierd we used to add and track modifiers in a list, but now we just
    // track the entire buff, so here instead we just recalculate the new modifiers we've added
    private void RecalculateModifiers(IEnumerable<StatModifier> modifiers) {
        if (!base.IsOwner) return;

        var affectedStats = new HashSet<StatType>();
        foreach (var mod in modifiers) {
            affectedStats.Add(mod.Stat);
        }
        foreach (var stat in affectedStats) {
            RecalculateStat(stat);
        }
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
        _rawStats[stat] = value;
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

public struct BuffSnapshot {
    public ushort buffID;
    public string displayName;
    public Sprite icon;
    public float remainingSeconds; // -1 => indefinite
    public float totalSeconds;     // -1 => indefinite
}
using r8teful;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


[RequireComponent(typeof(PlayerManager))]
public class PlayerStatsManager : MonoBehaviour, IPlayerModule {
    [SerializeField] private PlayerBaseStatsSO _baseStats;

    private Dictionary<StatType, Stat> _stats = new(); 
    
    private readonly List<BuffInstance> _activeBuffs = new(); 
    private readonly Dictionary<ushort, BuffInstance> _activeBuffsByID = new();
    // activeModifers 

    private bool _isInitialized = false;
    private List<BuffSnapshot> _snapshotCache = new();
    private float _uiAccumulator;
    const float UI_UPDATE_INTERVAL = 0.2f; // Ui updates every 0.2s
    public bool IsInitialized => _isInitialized;

    public event Action OnInitialized;
    public int InitializationOrder => 91;

    public event Action OnStatChanged;

    public event Action OnBuffListChanged;  // add/remove
    public event Action OnBuffsUpdated;     // periodic tick

    public void InitializeOnOwner(PlayerManager playerParent) {
        StatType[] statTypes = _baseStats.BaseStats.Select(s => s.Stat).ToArray();
        float[] baseValues = _baseStats.BaseStats.Select(s => s.BaseValue).ToArray();

        InitStats(statTypes, baseValues);
        //InitializeStats();
        _isInitialized = true;
    }

    private void InitStats(StatType[] statTypes, float[] baseValues) {
        for (int i = 0; i < statTypes.Length; i++) {
            StatType stat = statTypes[i];
            float value = baseValues[i];
            if (!_stats.ContainsKey(stat)) {
                _stats.Add(stat,new(value));
            }
        }
    }
  
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
        if (_stats.TryGetValue(stat, out var StatClass)) {
            return StatClass.Value;
        } else {
            Debug.LogWarning($"Attempted to get stat '{stat}' but it was not initialized. Returning 0.");
            return 0f;
        }
    }
    public float GetStatBase(StatType stat) {
        var baseStat = _baseStats.BaseStats.FirstOrDefault(s => s.Stat == stat);
        if(baseStat != null) {
            return baseStat.BaseValue;
        }
        Debug.LogError("Could not find base stat value in baseStat scriptableobject!");
        return 0; 
    }


    internal float GetProcentStat(StatType stat, StatModifier tempMod = null) {
        if (_stats.TryGetValue(stat, out var StatClass)) {
            return StatClass.GetTotalIncrease(tempMod);
        } else {
            Debug.LogWarning($"Attempted to get stat '{stat}' but it was not initialized. Returning 0.");
            return 0f;
        }
    }
    public IReadOnlyList<BuffSnapshot> GetBuffSnapshots() {
        // Snapshots are only used for UI.
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

    public BuffHandle TriggerBuff(BuffSO buffData, Action<Action> registerUnsubscribe = null) {
        if (buffData == null) throw new ArgumentNullException(nameof(buffData));

        var buff = new BuffInstance {
            buffID = buffData.ID,
            startTime = Time.time,
            duration = buffData.GetDuration(),
            expiresAt = buffData.GetDuration() > 0 ? Time.time + buffData.GetDuration() : -1f,
            // optionally store reference to SO if BuffInstance has a field for that:
            // source = buffData
        };

        return InternalTriggerBuff(buff, registerUnsubscribe);
    }

    public BuffHandle TriggerBuff(BuffInstance buffInstance, Action<Action> registerUnsubscribe = null) {
        if (buffInstance == null) throw new ArgumentNullException(nameof(buffInstance));
        return InternalTriggerBuff(buffInstance, registerUnsubscribe);
    }


    // Centralized logic
    private BuffHandle InternalTriggerBuff(BuffInstance buff, Action<Action> registerUnsubscribe) {
        var id = buff.buffID;

        // Duplicate handling - simple current behavior: ignore duplicates and return null.
        // You might want to replace this with Refresh/Stack/Replace behavior later.
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

        // Build remove action and handle
        Action removeAction = () => RemoveBuff(id);
        buff.handle = new BuffHandle(id, removeAction);

        // If external code wants the unsubscribe action, give it.
        registerUnsubscribe?.Invoke(removeAction);

        // Register buff in collections BEFORE applying so Apply() can observe manager state if needed.
        _activeBuffs.Add(buff);
        _activeBuffsByID[id] = buff;

        // Apply the buff's effects now that it's registered
        buff.Apply(this);

        // Notify listeners
        OnBuffListChanged?.Invoke();

        return buff.handle;
    }
    /// <summary>
    /// Removes all temporary modifiers that came from a specific source.
    /// </summary>
    public void RemoveBuff(ushort abilityID) {
        // Find which stats will be affected BEFORE we remove the modifiers
        if (!_activeBuffsByID.TryGetValue(abilityID, out var buff)) {
            Debug.LogWarning($"Tried to remove buff with ID {abilityID} which isn't active");
            return;
        }  
        _activeBuffs.Remove(buff);
        _activeBuffsByID.Remove(abilityID);

        buff.Remove(this);
        // For UI
        OnBuffListChanged?.Invoke();
    }

    public void RemoveModifiersFromSource(object source) {
        bool changed = false;

        foreach (var statContainer in _stats.Values) {
            if (statContainer.RemoveModifiersFromSource(source)) {
                changed = true;
            }
        }
        OnStatChanged?.Invoke();
    }

    public void AddInstanceModifier(StatModifier mod) {
        // If the ability doesn't have this stat, we ignore it 
        if (_stats.TryGetValue(mod.Stat, out Stat statContainer)) {
            statContainer.AddModifier(mod);
            OnStatChanged?.Invoke();
        }
    }

    public MiningToolData GetToolData() {
        return new MiningToolData {
            ToolRange = GetStat(StatType.MiningRange),
            ToolWidth = Mathf.Min(GetStat(StatType.MiningDamage) * 0.02f, 1f), //_isUsingAbility ? Mathf.Min(DamagePerHit * 0.3f, 0.6f) : 0.05f * DamagePerHit, // OLD
            toolTier = 0 //TODO
        };
    }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    public void DEBUGADDSTAT(StatType stat, float value) {
        StatModifier mod = new(value, stat, StatModifyType.Add, this);
        AddInstanceModifier(mod);
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
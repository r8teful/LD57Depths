using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// Holds runtime information about abilities.
// Abilities could be: Tools, 
public class AbilityInstance {
    public AbilitySO data { get; }

    private NetworkedPlayer _player;

    public float cooldownRemaining { get; private set; }

    private List<StatModifier> _instanceMods = new();
    private readonly List<BuffInstance> _activeBuffs = new(); 
    private readonly Dictionary<ushort, BuffInstance> _activeBuffsByID = new();
    
    public bool IsReady => cooldownRemaining <= 0f;
    public bool IsBeingUsed => !IsReady;


    public event Action<float> OnCooldownChanged; // sends fraction 0..1 or raw remaining
    public event Action OnReady;
    public event Action OnUsed; // optional, e.g. to play VFX

    public event Action OnModifiersChanged;
    public AbilityInstance(AbilitySO data, NetworkedPlayer player) {
        this.data = data;
        _player = player;
        cooldownRemaining = 0f;
    }
    public bool HasStatModifier(StatType stat) {
        return _instanceMods.Any(s => s.Stat == stat);
    }
    public bool HasBuff(ushort id) {
        return _activeBuffsByID.ContainsKey(id);
    }
    // call each frame from player controller
    public void Tick(float dt) {
        // First check if any of our timed buffs are still valid
        if (_activeBuffs.Count > 0) {
            for (int i = _activeBuffs.Count - 1; i >= 0; i--) { // inverted for loop suports removing within loop
                var b = _activeBuffs[i];
                if (b.duration > 0 && Time.time >= b.endTime) {
                    RemoveBuff(b.buffID);
                }
            }
        }
        // Update cooldown
        if (cooldownRemaining <= 0f) return;
        cooldownRemaining = Mathf.Max(0f, cooldownRemaining - dt);
        OnCooldownChanged?.Invoke(cooldownRemaining);
        if (cooldownRemaining == 0f) OnReady?.Invoke();
    }
    public void AddInstanceModifier(StatModifier mod) {
        //mod.Source = id; // tie to this ability instance (or to upgrade id)
        _instanceMods.Add(mod);
        OnModifiersChanged?.Invoke();
    }
    public void RemoveInstanceModifier(StatType stat) {
        _instanceMods.RemoveAll(m => m.Stat == stat);
        OnModifiersChanged?.Invoke();
    }
    private void AddInstanceModifiers(List<StatModifier> modifiersToAdd) {
        foreach (StatModifier mod in modifiersToAdd) { 
            _instanceMods.Add(mod);
        } 
        OnModifiersChanged?.Invoke();
    }

    // combine base cooldown with modifiers: ability modifiers first, then global StatManager if desired
    public float GetFinalAbilityMultiplier(StatType stat) {
        float baseCd = data.GetBaseModifierForStat(stat);

        // apply instance-level mods
        var instAdds = GetTotalFlatModifier(stat);
        var instMult = GetTotalPercentModifier(stat);
        //var lastSet = _instanceMods.Where(m => m.Stat == StatType.Cooldown && m.op == ModifierOp.Set).LastOrDefault();
        float cd = (baseCd + instAdds) * instMult;
        //if (lastSet.op == ModifierOp.Set) cd = lastSet.value;

        // Optionally incorporate global stat manager effects (if cooldowns are globally affected)
        // TODO Idk how we would do it here but lets just try to get it working first
        //float globalAdd = _statsManager.GetStat(StatType.Cooldown); // if you model cooldown in stat manager
        // If you do, choose how to combine. Here, assume statmanager returns a multiplier, or ignore if not used.

        return cd;
    }
    public float GetTotalFlatModifier(StatType stat) {
        return _instanceMods.Where(m => m.Stat == stat && m.Type == IncreaseType.Add).Sum(m => m.Value);
    }
    public float GetTotalPercentModifier(StatType stat) {
        //return _instanceMods.Where(m => m.Stat == stat && m.Type == IncreaseType.Multiply).Sum(m => m.Value);
        return _instanceMods.Where(m => m.Stat == stat && m.Type == IncreaseType.Multiply).Aggregate(1f, (a, m) => a * m.Value);
    }
    public float GetEffectiveStat(StatType stat) {
        float baseStat = _player.PlayerStats.GetStat(stat);
        float finalMult = GetFinalAbilityMultiplier(stat);
        return baseStat * finalMult;

    }
    public BuffHandle TriggerBuff(BuffInstance buff) {
        var id = buff.buffID;
        if (_activeBuffsByID.TryGetValue(id, out var existing)) {
            Debug.Log("Buff already applied to instance. What would you like to happen? CODE IT!!");
            return null;
        }
        // handle
        Action removeAction = () => RemoveBuff(id);
        // Build handle
        buff.handle = new BuffHandle(id, removeAction);
        _activeBuffs.Add(buff);
        _activeBuffsByID[id] = buff;
        var modifiersToAdd = new List<StatModifier>();
        foreach (var modData in buff.RuntimeModifiers) {
            modifiersToAdd.Add(modData);
        }
        AddInstanceModifiers(modifiersToAdd);
        return buff.handle;
    }
    public BuffHandle TriggerBuff(BuffSO buffData) {
        // Prevent duplicates unless ability is explicitly stackable
        var id = buffData.ID;
        if (_activeBuffsByID.TryGetValue(id, out var existing)) {
            Debug.Log("Buff already applied to instance. What would you like to happen? CODE IT!!");
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
            endTime = buffData.Duration > 0 ? Time.time + buffData.Duration : -1f, // indefinite
        };

        // Actions are so fancy, so this basically points to this function which when we invoke the action will call, and we can pass the action around 
        Action removeAction = () => RemoveBuff(id);
        // Build handle
        buff.handle = new BuffHandle(id, removeAction);
        _activeBuffs.Add(buff);
        _activeBuffsByID[id] = buff;
        var modifiersToAdd = new List<StatModifier>();
        foreach (var modData in buffData.Modifiers) {
            modifiersToAdd.Add(new StatModifier(modData.Value, modData.Stat, modData.Type, buffData));
        }
        AddInstanceModifiers(modifiersToAdd);
        return buff.handle;
    }

    public void RemoveBuff(ushort abilityID) {
        Debug.Log($"removing buff with ID {abilityID}");
        // Find which stats will be affected BEFORE we remove the modifiers
        if (!_activeBuffsByID.TryGetValue(abilityID, out var buff)) {
            Debug.LogWarning($"Tried to remove buff with ID {abilityID} which isn't active");
            return;
        }

        List<StatModifier> modsToRemove = new();
        foreach (var mod in buff.GetBuffData().Modifiers) {
            modsToRemove.Add(mod);
        }
        // We do it before so we could still access activeBuffs and its details
        buff.handle?.NotifyRemoved();
        _activeBuffs.Remove(buff);
        _activeBuffsByID.Remove(abilityID);
        // Now recalculate only the stats that were changed
        foreach (var mod in modsToRemove) {
            RemoveInstanceModifier(mod.Stat);
        }
    }

    // Tick and Use similar to before but use GetEffectiveCooldown() when setting cooldownRemaining
    public bool Use(Func<bool> performEffect) {
        if (data.type != AbilityType.Active) return false;
        Debug.Log($"Trying to use ability {data.displayName}, we have {cooldownRemaining} time left");
        if (!IsReady) return false;
        bool effectSuccess = performEffect?.Invoke() ?? true;
        if (!effectSuccess) return false;
        // We've now performed the effects and can update cooldowns

        cooldownRemaining = GetFinalAbilityMultiplier(StatType.Cooldown);
        OnUsed?.Invoke();
        OnCooldownChanged?.Invoke(cooldownRemaining);
        return true;
    }
}
// Ability data -> Ability Instance -> Ability Effect -> Buff instance -> statmanager
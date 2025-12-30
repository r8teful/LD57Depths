using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// Holds runtime information about abilities.
// Think of abilities as being behaviours we can add to the player.
// Like a mining tool, or an ability does makes that mining tool more powerfull, or something that applies other abilities (like the biome buffs)
public class AbilityInstance {
    public AbilitySO Data { get; }

    private NetworkedPlayer _player;

    private List<StatModifier> _instanceMods = new();
    private readonly List<BuffInstance> _activeBuffs = new(); 
    private readonly Dictionary<ushort, BuffInstance> _activeBuffsByID = new();
    
    // Timing
    private float _activeRemaining = 0f;
    private float _cooldownRemaining = 0f;
    public bool IsActive => _activeRemaining > 0f;
    public bool IsReady => _cooldownRemaining <= 0f && _activeRemaining <= 0f;

    public event Action<float> OnCooldownChanged; // sends fraction 0..1 or raw remaining
    public event Action<float> OnActiveTimeChanged;
    
    public event Action OnActivated; // When player presses the ability button and we start to use it
    public event Action OnDeactivated; // active finished (before cooldown)
    public event Action OnReady; // Ability is ready to be used
    public event Action OnUsed; // Ability is used. (For when any abilities is used) 

    public event Action OnModifiersChanged;
    public AbilityInstance(AbilitySO data, NetworkedPlayer player) {
        this.Data = data;
        _player = player;
        _cooldownRemaining = 0f;
    }
    public bool HasStatModifier(StatType stat) {
        return _instanceMods.Any(s => s.Stat == stat);
    }
    public bool HasBuff(ushort id) {
        return _activeBuffsByID.ContainsKey(id);
    }
    internal float GetAbilityTime() {
        if (!Data.isTimed) return 0f; 
        var buff = Data.effects?.OfType<IEffectBuff>().FirstOrDefault();
        return buff.Buff.Duration;
    }
    internal float GetBuffStatStrength(StatType stat) {
        var buff = Data.effects?.OfType<IEffectBuff>().FirstOrDefault();
        if(buff == null) return 0f;
        var mod = buff.Buff.Modifiers.FirstOrDefault(m => m.Stat == stat);
        if(mod == null) return 0f;
        return GetFinalAbilityMultiplier(mod.Stat, mod.Value); // Quite beautiful
    }
    
    // call each frame from player controller
    public void Tick(float dt) {
        // First check if any of our timed buffs are still valid
        if (_activeBuffs.Count > 0) {
            for (int i = _activeBuffs.Count - 1; i >= 0; i--) { // inverted for loop suports removing within loop
                var b = _activeBuffs[i];
                if (b.duration > 0 && Time.time >= b.expiresAt) {
                    RemoveBuff(b.buffID);
                }
            }
        }
        // Update cooldowns
        if (_activeRemaining > 0f) {
            _activeRemaining = Mathf.Max(0f, _activeRemaining - dt);
            OnActiveTimeChanged?.Invoke(_activeRemaining);
            if (_activeRemaining == 0f) {
                // active ended -> start cooldown
                OnDeactivated?.Invoke();
                _cooldownRemaining = GetFinalAbilityMultiplier(StatType.Cooldown);
                OnCooldownChanged?.Invoke(_cooldownRemaining);
            }
        // The else here makes it so that the cooldown only starts after we have no active time remaining!
        } else if (_cooldownRemaining > 0f) { 
            _cooldownRemaining = Mathf.Max(0f, _cooldownRemaining - dt);
            OnCooldownChanged?.Invoke(_cooldownRemaining);
            if (_cooldownRemaining == 0f) OnReady?.Invoke();
        }
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
    public float GetFinalAbilityMultiplier(StatType stat, float baseOverride = -1) {
        float baseCd = baseOverride == -1 ? Data.GetBaseModifierForStat(stat) : baseOverride; 

        // apply instance-level mods
        var instAdds = GetTotalFlatModifier(stat);
        var instMult = GetTotalPercentModifier(stat); // Instance mods mining damage -> 2.5... WHY??
        //var lastSet = _instanceMods.Where(m => m.Stat == StatType.Cooldown && m.op == ModifierOp.Set).LastOrDefault();
        float cd = (baseCd + instAdds) * Mathf.Max(1,instMult); // don't multiply with 0 if we have no multiplier
        //if (lastSet.op == ModifierOp.Set) cd = lastSet.value;

        // Optionally incorporate global stat manager effects (if cooldowns are globally affected)
        // TODO Idk how we would do it here but lets just try to get it working first
        //float globalAdd = _statsManager.GetStat(StatType.Cooldown); // if you model cooldown in stat manager
        // If you do, choose how to combine. Here, assume statmanager returns a multiplier, or ignore if not used.

        return cd;
    }
    public float GetTotalFlatModifier(StatType stat) {
        return _instanceMods.Where(m => m.Stat == stat && m.Type == StatModifyType.Add).Sum(m => m.Value);
    }
    public float GetTotalPercentModifier(StatType stat) {
        return _instanceMods.Where(m => m.Stat == stat && m.Type == StatModifyType.Multiply).Sum(m => m.Value);
       //return _instanceMods.Where(m => m.Stat == stat && m.Type == IncreaseType.Multiply).Aggregate(1f, (a, m) => a * m.Value);
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
            expiresAt = buffData.Duration > 0 ? Time.time + buffData.Duration : -1f, // indefinite
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
        if (Data.type == AbilityType.Passive) return false;
        Debug.Log($"Trying to use ability {Data.displayName}, we have {_cooldownRemaining} time left");
        if (!IsReady) return false;
        bool effectSuccess = performEffect?.Invoke() ?? true;
        if (!effectSuccess) return false;
        // We've now performed the effects and can update cooldowns
        var abilityT = GetAbilityTime();
        if (GetAbilityTime() > 0) {
            // This ability has a certain time it has to be active for 
            _activeRemaining = abilityT;
            OnActivated?.Invoke();
            OnActiveTimeChanged?.Invoke(_activeRemaining);
        } else {
            // Instant ability 
            _cooldownRemaining = GetFinalAbilityMultiplier(StatType.Cooldown);
            OnCooldownChanged?.Invoke(_cooldownRemaining);
        }
        OnUsed?.Invoke();
        return true;
    }

    // allow effects or external systems to prematurely end active phase (optionally start cooldown)
    // Might come in handy later
    public void EndActiveEarly(bool startCooldown = true, float remainingCooldownOverride = -1f) {
        if (_activeRemaining <= 0f) return;
        _activeRemaining = 0f;
        OnActiveTimeChanged?.Invoke(0f);
        OnDeactivated?.Invoke();
        if (startCooldown) {
            _cooldownRemaining = remainingCooldownOverride >= 0f ?
                remainingCooldownOverride : GetFinalAbilityMultiplier(StatType.Cooldown);
            OnCooldownChanged?.Invoke(_cooldownRemaining);
        }
    }
}
// Ability data -> Ability Instance -> Ability Effect -> Buff instance -> statmanager
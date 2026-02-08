using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Stat = r8teful.Stat;
// Holds runtime information about abilities.
// Think of abilities as being behaviours we can add to the player.
// Like a mining tool, or an ability does makes that mining tool more powerfull, or something that applies other abilities (like the biome buffs)

public class AbilityInstance {
    public AbilitySO Data { get; }
    // For getting script reference if we had spawned the effect
    public GameObject Object { get; private set; }
    private PlayerManager _player;
    [ShowInInspector]
    private Dictionary<StatType, Stat> _stats = new();
    public Dictionary<StatType, Stat> Stats => _stats;
    private readonly List<BuffInstance> _activeBuffs = new(); 
    private readonly Dictionary<ushort, BuffInstance> _activeBuffsByID = new();
    
    // Timing
    private float _activeRemaining = 0f;
    private float _cooldownRemaining = 0f;
    public bool IsActive => _activeRemaining > 0f;
    public bool IsReady => _cooldownRemaining <= 0f && _activeRemaining <= 0f;


    public event Action<float,float> OnCooldownChanged; // remaining, max
    public event Action<float> OnActiveTimeChanged;
    
    public event Action OnActivated; // When player presses the ability button and we start to use it
    public event Action OnDeactivated; // active finished (before cooldown)
    public event Action OnReady; // Ability is ready to be used
    public event Action OnUsed; // Ability is used. (For when any abilities is used) 
    public event Action<BuffInstance> OnBuffExpired; 

    public event Action OnModifiersChanged;
    internal void SetGameObject(GameObject gameObject) => Object = gameObject;


   public bool TryGetBuffEffect(out IEffectBuff buff) {
        buff = Data.effects?.OfType<IEffectBuff>().FirstOrDefault();
        return buff != null;
    }
    // If true, that means that the stats of this ability DEPEND on a different ability, will have to further
    // extend this or find a better way of doing it if one ability has a target, and another one not.
    // Anyway, because currently this is the case with the brimstone buff, we set this to true, and then
    // we will look through the effect list, find the buff that has another ability target, and then we multiply this abilities stat increases by
    // the effective value of the buff target. Meaning, if base is 10, lazer is 20 damage, and brimstone is 1.5 well get 30 output and not 15, because brimstone depends on lazer 
    // Basically we need a bool like this to know if when we get the effective value we use multiply by the base stat or the target ability stat
  
    public AbilityInstance(AbilitySO data, PlayerManager player, GameObject @object = null) {
        Data = data;
        _player = player;
        _cooldownRemaining = 0f;
        Object = @object;
        InitStats();

    }

    private void InitStats() {
        // Cooldown is specific to the ability and not the buff, thats why we add it here
        if(Data.cooldown > 0) _stats.Add(StatType.Cooldown, new(Data.cooldown));
        if(TryGetBuffEffect(out var buff)) {
            foreach (var baseStat in buff.Buff.Modifiers) {
                // Buff acts as stat defaults
                _stats.Add(baseStat.Stat, new(baseStat.Value));
            }
            return;
        } else {
            foreach (var baseStat in Data.StatTypes) {
                _stats.Add(baseStat.Stat, new(baseStat.BaseValue));
            }
        }
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
                if (b.duration > 0 && Time.time >= b.expiresAt) {
                    RemoveBuff(b);
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
                _cooldownRemaining = GetEffectiveStat(StatType.Cooldown);
                OnCooldownChanged?.Invoke(_cooldownRemaining, GetEffectiveStat(StatType.Cooldown));
            }
        // The else here makes it so that the cooldown only starts after we have no active time remaining!
        } else if (_cooldownRemaining > 0f) { 
            _cooldownRemaining = Mathf.Max(0f, _cooldownRemaining - dt);
            OnCooldownChanged?.Invoke(_cooldownRemaining, GetEffectiveStat(StatType.Cooldown));
            if (_cooldownRemaining == 0f) OnReady?.Invoke();
        }
    }

    public void AddInstanceModifier(StatModifier mod) {
        // If the ability doesn't have this stat, we ignore it 
        if (_stats.TryGetValue(mod.Stat, out Stat statContainer)) {
            statContainer.AddModifier(mod);
            OnModifiersChanged?.Invoke();
        }
    }
    public void RemoveModifiersFromSource(object source) {
        bool changed = false;

        foreach (var statContainer in _stats.Values) {
            if (statContainer.RemoveModifiersFromSource(source)) {
                changed = true;
            }
        }

        if (changed) OnModifiersChanged?.Invoke();
    }
    /// <summary>
    /// Get the final value of this stat
    /// </summary>
    /// <param name="statType"></param>
    /// <param name="tempMod">Optional parameter to calculate the next value</param>
    /// <returns></returns>
    public float GetEffectiveStat(StatType stat, StatModifier tempMod = null) {
        // Handle "Internal Stats" (Direct Lookup)
        if (_stats.TryGetValue(stat, out Stat rawStat)) {
            var modToPass = (tempMod != null && tempMod.Stat == stat) ? tempMod : null;

            float baseStat;
            bool isAbilitySpecificStat = (stat == StatType.Cooldown) || (stat == StatType.Duration); // Cooldown and duration are always specific to THIS ability
            // If this ability has a buff with a valid target, the effective stats of this ability is multiplied by that targets effective stats and not the players base stat
            // This works because we would already have multiplied the targets stats by the base stats
            if (!isAbilitySpecificStat && TryGetBuffEffect(out var buffEffect)) {
                baseStat = buffEffect.GetEffectiveStat(stat); // The interface implements where the effective stat comes from, either from a target ability instance (the case for brimstone), or the player (the case for dash)
            } else {
                baseStat = GetPlayerStat(stat);
            }
            var modifier = rawStat.CalculateFinalValue(modToPass);
            if (stat == StatType.Cooldown) {
                // cooldown is also special, the more cooldown we have, the quicker the cooldown will be
                return baseStat / MathF.Max(0.001f, modifier);
            }
            return baseStat * modifier;
            // The line above basically says that all raw stat are treated as a multiplier,
            // We might not want this to happen for certain abilities, or stats, but right now this is the case
        }
        return 0;
    }

    /// <summary>
    /// Raw is simply the final multiplier this instance has to the base stat
    /// </summary>
    public float GetRawStat(StatType stat) {
        if (_stats.TryGetValue(stat, out Stat rawStat)) {
            return rawStat.Value;
        }
        return -1;
    }


    internal float GetProcentStat(StatType stat, StatModifier tempMod = null) {
        if (_stats.TryGetValue(stat, out Stat rawStat)) {
            return rawStat.GetTotalIncrease(tempMod);
        }
        return -1f;
    }
    public Stat GetStat(StatType stat) {
        if (_stats.TryGetValue(stat, out Stat rawStat)) {
            return rawStat;
        }
        return null;
    }
    public float GetPlayerStat(StatType stat) => _player.PlayerStats.GetStat(stat);
    public float GetBaseStat(StatType stat) => _player.PlayerStats.GetStatBase(stat);
    
    public BuffHandle TriggerBuff(BuffInstance buff) {
        var id = buff.buffID;
        if (_activeBuffsByID.TryGetValue(id, out var existing)) {
            Debug.Log("Buff already applied to instance. What would you like to happen? CODE IT!!");
            return null;
        }
        // handle
        Action removeAction = () => RemoveBuff(buff);
        // Build handle
        buff.handle = new BuffHandle(id, removeAction);
        _activeBuffs.Add(buff);
        _activeBuffsByID[id] = buff;
        buff.Apply(this); // This will then add the censisarry instance mods to this instance
        return buff.handle;
    }

    public void RemoveBuff(BuffInstance buff) {
        if (!_activeBuffs.Contains(buff)) {
            Debug.LogWarning($"Tried to remove buff {buff} with ID {buff.buffID} which isn't active");
            return;
        }
        _activeBuffs.Remove(buff);
        _activeBuffsByID.Remove(buff.buffID);
        // The buff knows how to clean up the modifiers it created
        buff.Remove(this);
        OnBuffExpired?.Invoke(buff);
        Debug.Log($"Removed buff {buff.buffID}");
    }
    // Tick and Use similar to before but use GetEffectiveCooldown() when setting cooldownRemaining
    public bool Use(Func<bool> performEffect) {
        if (Data.type == AbilityType.Passive) return false;
        Debug.Log($"Trying to use ability {Data.displayName}, we have {_cooldownRemaining} time left");
        if (!IsReady) return false;
        bool effectSuccess = performEffect?.Invoke() ?? true;
        if (!effectSuccess) return false;
        // We've now performed the effects and can update cooldowns
        var abilityT = GetEffectiveStat(StatType.Duration);
        if (abilityT > 0) {
            // This ability has a certain time it has to be active for 
            _activeRemaining = abilityT;
            OnActivated?.Invoke();
            OnActiveTimeChanged?.Invoke(_activeRemaining);
        } else {
            // Instant ability 
            _cooldownRemaining = GetEffectiveStat(StatType.Cooldown);
            OnCooldownChanged?.Invoke(_cooldownRemaining, GetEffectiveStat(StatType.Cooldown));
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
                remainingCooldownOverride : GetEffectiveStat(StatType.Cooldown);
            OnCooldownChanged?.Invoke(_cooldownRemaining, GetEffectiveStat(StatType.Cooldown));
        }
    }

}
// Ability data -> Ability Instance -> Ability Effect -> Buff instance -> statmanager
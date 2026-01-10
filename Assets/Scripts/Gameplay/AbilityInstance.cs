using r8teful;
using Sirenix.OdinInspector;
using Steamworks.Data;
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
    private NetworkedPlayer _player;
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


    public event Action<float> OnCooldownChanged; // sends fraction 0..1 or raw remaining
    public event Action<float> OnActiveTimeChanged;
    
    public event Action OnActivated; // When player presses the ability button and we start to use it
    public event Action OnDeactivated; // active finished (before cooldown)
    public event Action OnReady; // Ability is ready to be used
    public event Action OnUsed; // Ability is used. (For when any abilities is used) 

    public event Action OnModifiersChanged;
    internal void SetGameObject(GameObject gameObject) => Object = gameObject;
    public bool TryGetBuffEffect(out IEffectBuff buff) {
        buff = Data.effects?.OfType<IEffectBuff>().FirstOrDefault();
        return buff != null;
    }
    public AbilityInstance(AbilitySO data, NetworkedPlayer player, GameObject @object = null) {
        Data = data;
        _player = player;
        _cooldownRemaining = 0f;
        Object = @object;
        InitStats();
    
    }

    private void InitStats() {
        if(TryGetBuffEffect(out var buff)) {
            foreach (var baseStat in buff.Buff.Modifiers) {
                // Buff acts as stat defaults
                _stats.Add(baseStat.Stat, new(baseStat.Value));
            }
            return;
        } else {
            foreach (var baseStat in Data.StatTypes) {
                _stats.Add(baseStat.stat, new(baseStat.baseMofifier));
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

    public float GetEffectiveStat(StatType statType) {
        return GetEffectiveStatInternal(statType, null);
    }
    // Used for UI to get the next value
    public float GetPreviewStat(StatType statType, StatModifier tempMod) {
        return GetEffectiveStatInternal(statType, tempMod);
    }
    private float GetEffectiveStatInternal(StatType stat, StatModifier tempMod) {
        // Handle "Internal Stats" (Direct Lookup)
        if(_stats.TryGetValue(stat, out Stat rawStat)) {
            var modToPass = (tempMod != null && tempMod.Stat == stat) ? tempMod : null;

            var baseStat = GetBaseStat(stat);
            return baseStat * rawStat.CalculateFinalValue(modToPass);
        }
        return 0;
    }


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
                remainingCooldownOverride : GetEffectiveStat(StatType.Cooldown);
            OnCooldownChanged?.Invoke(_cooldownRemaining);
        }
    }

    internal float GetNextValue(StatType stat, StatModifier tempMod) {
        if (TryGetBuffEffect(out var buff)) {
            if(buff.Target == null) {
                Debug.LogError("Found buff effect but it has no target!");
                return -1;
            }
            var ab = _player.PlayerAbilities.GetAbilityInstance(buff.Target.ID);
            if (ab == null) {
                Debug.LogError("Found target but ability is not spawned!");
                return -1;
            }
            var statV = ab.GetEffectiveStat(stat);
            return statV * tempMod.Value;
        }
        // No buff ability, simply get current stat?
        return GetEffectiveStat(stat) * tempMod.Value;
    }

}
// Ability data -> Ability Instance -> Ability Effect -> Buff instance -> statmanager
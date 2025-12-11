using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// Holds runtime information about abilities.
// Abilities could be: Tools, 
public class AbilityInstance : MonoBehaviour {
    public AbilitySO data { get; }

    private NetworkedPlayer _player;

    public float cooldownRemaining { get; private set; }

    private List<StatModifier> _instanceMods = new();
    public bool IsReady => cooldownRemaining <= 0f;

    public event Action<float> OnCooldownChanged; // sends fraction 0..1 or raw remaining
    public event Action OnReady;
    public event Action OnUsed; // optional, e.g. to play VFX

    public event Action OnModifiersChanged;
    public AbilityInstance(AbilitySO data, NetworkedPlayer player) {
        this.data = data;
        _player = player;
        cooldownRemaining = 0f;
    }

    // call each frame from player controller
    public void Tick(float dt) {
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
    // combine base cooldown with modifiers: ability modifiers first, then global StatManager if desired
    public float GetFinalAbilityMultiplier(StatType stat) {
        float baseCd = data.GetBaseModifierForStat(stat);

        // apply instance-level mods
        var instAdds = _instanceMods.Where(m => m.Stat == stat && m.Type == IncreaseType.Add).Sum(m => m.Value);
        var instMult = _instanceMods.Where(m => m.Stat == stat && m.Type == IncreaseType.Multiply).Aggregate(1f, (a, m) => a * m.Value);
        //var lastSet = _instanceMods.Where(m => m.Stat == StatType.Cooldown && m.op == ModifierOp.Set).LastOrDefault();
        float cd = (baseCd + instAdds) * instMult;
        //if (lastSet.op == ModifierOp.Set) cd = lastSet.value;

        // Optionally incorporate global stat manager effects (if cooldowns are globally affected)
        // TODO Idk how we would do it here but lets just try to get it working first
        //float globalAdd = _statsManager.GetStat(StatType.Cooldown); // if you model cooldown in stat manager
        // If you do, choose how to combine. Here, assume statmanager returns a multiplier, or ignore if not used.

        return cd;
    }
    public float GetEffectiveStat(StatType stat) {
        float baseStat = _player.PlayerStats.GetStat(stat);
        float finalMult = GetFinalAbilityMultiplier(stat);
        return baseStat * finalMult;

    }
    // Tick and Use similar to before but use GetEffectiveCooldown() when setting cooldownRemaining
    public bool Use(Func<bool> performEffect) {
        if (data.type != AbilityType.Active) return false;
        if (!IsReady) return false;

        if (!(performEffect?.Invoke() ?? true)) return false;

        // run ability effects
        foreach (var e in data.effects) {
            if (e is IEffectActive effect)
                effect.Execute(this, _player);
        }

        cooldownRemaining = GetFinalAbilityMultiplier(StatType.Cooldown);
        OnUsed?.Invoke();
        OnCooldownChanged?.Invoke(cooldownRemaining);
        return true;
    }
}
// Ability data -> Ability Instance -> Ability Effect -> Buff instance -> statmanager
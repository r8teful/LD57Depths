// We use this instance to get RUNTIME information about the buff, we'll need it for UI,
// but also if we increase the buff strength we'll modify its Stat modifiers
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class BuffInstance {
    public ushort buffID;
    public float timeRemaining; // -1 for conditional
    public BuffHandle handle;
    internal float startTime;
    internal float duration;
    internal float expiresAt;
    public List<StatModifier> Modifiers { get; private set; }
    public BuffSO GetBuffData() => App.ResourceSystem.GetBuffByID(buffID);
    public bool IsExpired => Time.time >= expiresAt;
    public BuffInstance() { }

    // Factory: create a BuffInstance from a BuffSO
    public static BuffInstance CreateFromSO(BuffSO so, float durationOverride = -1f) {
        var dur = so.GetDuration();
        var inst = new BuffInstance {
            buffID = so.ID,
            startTime = Time.time,
            duration = durationOverride > 0f ? durationOverride : dur,
            timeRemaining = durationOverride > 0f ? durationOverride : dur,
            expiresAt = (durationOverride > 0f ? Time.time + durationOverride :
                       dur > 0 ? Time.time + dur : -1),
            Modifiers = new()
        };
        // deep copy modifiers. We want to do this because these modifiers is what actually give the buffs
        inst.Modifiers = so.Modifiers?.Select(
             m => new StatModifier(m.Value, m.Stat, m.Type, so)).ToList() ?? new List<StatModifier>();

        return inst;
    }
    /// <summary>
    /// Use this only if you want to change the strength of the buff. We ADD the modifiers the source ability has into this buff
    /// </summary>
    /// <param name="source"></param>
    public void ApplyAbilityInstanceModifiers(AbilityInstance source) {
        //if (!source.HasStatModifiers()) return;
        foreach (var mod in Modifiers) {
    
            // Here we take the modifiers from the ability, and add them to the runtimeModifiers of the buff. This way, when we add the buff to another ability, that abilitu will now simply have a stronger buff

            // This calculation now works for when we have a multiplier value, and add another multiplier value to it.
            float baseBuffValue = mod.Value;
            mod.Value = baseBuffValue + source.GetTotalPercentModifier(mod.Stat);
            if (mod.Stat == StatType.Duration) {
                var dur = mod.Value * source.GetBaseStat(mod.Stat); // This kind of is like source.GetEffective stat, but we take into acount the baseBuffValue
                duration = dur;
                timeRemaining = dur;
                expiresAt = Time.time + dur;
            }
            continue;
            // get totals from the source
            float flatFromSource = source.GetTotalFlatModifier(mod.Stat);     // e.g. Mining knockback + 20
            float percentFromSource = source.GetTotalPercentModifier(mod.Stat); // e.g. Damage + 20%
            // We dont have a way to have both flat from source, and percentFrom source right now, have to find a way to combine them
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
        return Modifiers.FirstOrDefault(m => m.Stat == stat);
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
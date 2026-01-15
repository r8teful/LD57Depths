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
    public List<StatModifier> Modifiers { get; private set; } = new();
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
        m => new StatModifier(m.Value, m.Stat, m.Type, inst)).ToList() ?? new List<StatModifier>();
      

        return inst;
    }

    public void IncreaseBuffPower(AbilityInstance source) {
        foreach (var mod in Modifiers) {
            // We need to check if the source stat has modifiers. Source stat is the brimstoneBuffAbility, if that has 
            // any modifiers on its stats, we need to add those modifiers onto the modifiers of the buff, this is what makes it stronger

            // But instead of doing that, we could simple SET the buff value to the value of what the stats are in the buff ability, right?
            // This could work because we set the _stats equal to the buffSO values, and if we'd upgrade the ability those stats would change aswell
            mod.Value = source.GetRawStat(mod.Stat);

            // However, because this simply can't be that easy, the duration needs to know the effective because that is
            // on the source itself
            if (mod.Stat == StatType.Duration) {
                var dur = source.GetEffectiveStat(mod.Stat);
                mod.Value = dur;
                duration = dur;
                timeRemaining = dur;
                expiresAt = Time.time + dur;
            }
        }
    }
    public void Remove(AbilityInstance targetAbility) {
        targetAbility.RemoveModifiersFromSource(this);
        handle?.NotifyRemoved();
        //Modifiers.Clear();
    }

    public void Apply(AbilityInstance targetAbility) {
        foreach (var mod in Modifiers) {
            targetAbility.AddInstanceModifier(mod); // Add to the Stat Dictionary
        }
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
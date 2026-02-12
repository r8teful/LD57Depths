using System;
using UnityEngine;

[Serializable] // So it can be viewed in the Inspector if needed
public class StatModifier {
    public float Value;
    public StatType Stat; // The stat we want to modify
    public StatModifyType Type;

    // The "Source" is a unique identifier for who/what applied this modifier.
    // Used for removing it later when the stat is done. Also, if we apply FROM the same source, we should replace the buff
    public readonly object Source;
    public StatModifier(float value, StatType stat, StatModifyType type, object source) {
        Value = value;
        Stat = stat;
        Type = type;
        Source = source;
    }
    public StatChangeStatus GetStatus(AbilityInstance ability) {
        var statName = ResourceSystem.GetStatString(Stat);
        float currentIncrease, nextIncrease;
        // We need different ways to display it, for damage, it needs to be "abstract"
        // so 10% damage -> 20% would be 2x the damage
        // But with things like crit chance, we need the ACTAUL value, 
        // so 5% crit chacnce really means 5% 
        if (Stat == StatType.MiningCritChance) {
            currentIncrease = ability.GetEffectiveStat(Stat);
            nextIncrease = ability.GetEffectiveStat(Stat, this);
        } else {
            currentIncrease = ability.GetProcentStat(Stat) * 0.1f;
            nextIncrease = ability.GetProcentStat(Stat, this) * 0.1f;

        }
        int currentProcent = Mathf.RoundToInt(currentIncrease * 100f);
        int nextProcent = Mathf.RoundToInt(nextIncrease * 100f);
        return new(statName, $"{currentProcent}%", $"{nextProcent}%", ResourceSystem.IsLowerBad(Stat));

    }
    // could combine the two if we simply made an interface that was like "IStatHoldable" or something but eh cba
    internal StatChangeStatus GetStatus(PlayerStatsManager playerStats) {
        var statName = ResourceSystem.GetStatString(Stat);
        var currentIncrease = playerStats.GetProcentStat(Stat) * 0.1f;
        var nextIncrease = playerStats.GetProcentStat(Stat, this) * 0.1f;

        int currentProcent = Mathf.RoundToInt(currentIncrease * 100f);
        int nextProcent = Mathf.RoundToInt(nextIncrease * 100f);
        var isLowerBad = ResourceSystem.IsLowerBad(Stat);
        return new(statName, $"{currentProcent}%", $"{nextProcent}%", isLowerBad);
    }
  
}
using System;
using UnityEngine;

/// <summary>
/// Very similar to StatModifier but its a more generic value instead of a stat
/// </summary>
[Serializable] // So it can be viewed in the Inspector if needed
public class ValueModifier { 
    public float Value;
    public ValueKey Key; // The stat we want to modify
    public StatModifyType Type;

    // The "Source" is a unique identifier for who/what applied this modifier.
    // Used for removing it later when the stat is done. Also, if we apply FROM the same source, we should replace the buff
    public readonly object Source;
    public ValueModifier(float value, ValueKey stat, StatModifyType type, object source) {
        Value = value;
        Key = stat;
        Type = type;
        Source = source;
    }
    public StatChangeStatus GetStatus(IValueModifiable script) {
        var valueBase = script.GetValueBase(Key);
        var valueNow = script.GetValueNow(Key);
        var valueNext = UpgradeCalculator.CalculateUpgradeChange(valueNow, Type, Value);

        // make it a procent change duh
        float percentNow = valueNow / (float)valueBase;
        float percentNext = valueNext / (float)valueBase;

        int currentProcent = Mathf.RoundToInt(percentNow * 100f);
        int nextProcent = Mathf.RoundToInt(percentNext * 100f);

        return new("todo", $"{currentProcent}%", $"{nextProcent}%", true);
    }
}
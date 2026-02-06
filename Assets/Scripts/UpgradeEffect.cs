using UnityEngine;
// Finaly we have the good solution for this, the effect is just applied in some way, and we can derive from effect to whatever we want it to do really...
// I originally had derived different effects from the scriptable object, but we just want the effects to be different, not the upgrade data type
public abstract class UpgradeEffect : ScriptableObject, IExecutable {
    public abstract void Execute(ExecutionContext context); // This should really just be the networkedplayer
    public abstract StatChangeStatus GetChangeStatus();
}

public struct StatChangeStatus {
    public string StatName { get; }
    public string ValueNow { get; }
    public string ValueNext { get; }
    public bool IsBadChange { get; }
    public bool IsEmpty => StatName is null && ValueNow is null && ValueNext is null && !IsBadChange;

    public StatChangeStatus(string statName, float valueNow, float valueNext, bool isBadChange) {
        StatName = statName;
        ValueNow = valueNow.ToString("F2");
        ValueNext = valueNext.ToString("F2");
        IsBadChange = isBadChange;
    }
    public StatChangeStatus(string statName, string valueNow, string valueNext, bool isBadChange) {
        StatName = statName;
        ValueNow = valueNow;
        ValueNext = valueNext;
        IsBadChange = isBadChange;
    }
}
public enum StatModifyType { 
    Add, 
    PercentAdd, 
    PercentMult 
}
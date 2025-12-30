using System;

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
}
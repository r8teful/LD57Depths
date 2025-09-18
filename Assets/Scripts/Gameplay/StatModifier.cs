using System;

[Serializable] // So it can be viewed in the Inspector if needed
public class StatModifier {
    public float Value;
    public StatType Stat; // The stat we want to modify
    public IncreaseType Type;

    // The "Source" is a unique identifier for who/what applied this modifier.
    // Used for removing it later when the stat is done
    public readonly object Source;

    public StatModifier(float value, StatType stat, IncreaseType type, object source) {
        Value = value;
        Stat = stat;
        Type = type;
        Source = source;
    }
}
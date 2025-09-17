using System;

[Serializable] // So it can be viewed in the Inspector if needed
public class StatModifier {
    public readonly float Value;
    public readonly IncreaseType Type;
    public readonly StatType Stat; // The stat we want to modify

    // The "Source" is a unique identifier for who/what applied this modifier.
    // Used for removing it later when the stat is done
    public readonly object Source;

    public StatModifier(float value, IncreaseType type, object source) {
        Value = value;
        Type = type;
        Source = source;
    }
}
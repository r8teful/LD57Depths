using System;

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
}
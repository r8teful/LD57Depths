using UnityEngine;
// Finaly we have the good solution for this, the effect is just applied in some way, and we can derive from effect to whatever we want it to do really...
// I originally had derived different effects from the scriptable object, but we just want the effects to be different, not the upgrade data type
public abstract class UpgradeEffect : ScriptableObject{
    public abstract void Apply(GameObject target);
}


public enum IncreaseType { Add, Multiply }
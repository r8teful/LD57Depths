using System;
using UnityEngine;

// Base class for entity-specific data
[Serializable]
public abstract class EntitySpecificData {
    // Method to apply the data to a spawned GameObject
    public abstract void ApplyTo(GameObject go);
}

public class BreakEntityData : EntitySpecificData {
    public bool isBroken;

    public BreakEntityData(bool isBroken) {
        this.isBroken = isBroken;
    }

    public override void ApplyTo(GameObject go) {
        go.GetComponent<FixableEntity>().SetIsBrokenBool(isBroken);
    }
}
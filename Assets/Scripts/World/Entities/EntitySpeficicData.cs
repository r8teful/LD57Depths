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
        go.GetComponent<FixableEntity>().SetFixed(!isBroken);
    }
}
public class GrowthEntityData : EntitySpecificData {
    public int GrowthStage;
    public GrowthEntityData(int stage) {
        GrowthStage = stage;
    }
    public override void ApplyTo(GameObject go) {
        go.GetComponent<GrowableEntity>().SetGrowthStage(GrowthStage);
    }
}
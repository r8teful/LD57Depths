using System;
using UnityEngine;

// Base class for entity-specific data
[Serializable]
public abstract class EntitySpecificData {
    // Method to apply the data to a spawned GameObject
    public abstract void ApplyTo(GameObject go);
}

public class GrowthEntityData : EntitySpecificData {
    public int GrowthStage;
    public GrowthEntityData(int stage) {
        GrowthStage = stage;
    }
    public override void ApplyTo(GameObject go) {
       // todo
        // go.GetComponent<GrowableEntity>().SetGrowthStage(GrowthStage);
    }
}
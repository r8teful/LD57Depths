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
        go.GetComponent<FixableEntity>().SetFixedRpc(!isBroken);
    }
}
public class LightEntityData : EntitySpecificData {
    public float LightLevel;

    public LightEntityData(float lightLevel) {
        this.LightLevel = lightLevel;
    }

    public override void ApplyTo(GameObject go) {
        throw new NotImplementedException();
        //go.GetComponent<FixableEntity>().SetFixedRpc(!isBroken);
    }
}
public class OxygenEntityData : EntitySpecificData {
    public float OxygenLevel;

    public OxygenEntityData(float oxygenLevel) {
        this.OxygenLevel = oxygenLevel;
    }

    public override void ApplyTo(GameObject go) {
        throw new NotImplementedException();
        //go.GetComponent<FixableEntity>().SetFixedRpc(!isBroken);
    }
}
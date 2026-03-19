using System;
using UnityEngine;

// Base class for entity-specific data
[Serializable]
public abstract class EntitySpecificData {
    // Method to apply the data to a spawned GameObject
    public abstract void ApplyTo(GameObject go);
}

public class IsUsedData : EntitySpecificData {
    public bool IsEntityUsed;
    public IsUsedData(bool isUsed) {
        IsEntityUsed = isUsed;
    }
    public override void ApplyTo(GameObject go) {
        // Try chest
        var chest = go.GetComponent<Chest>();
        if (chest == null) chest = go.GetComponentInChildren<Chest>();
        if (chest != null) {
            chest.SetInteractable(IsEntityUsed);
            return;
        }
        // Try shrine
        var shrine = go.GetComponent<Shrine>();
        if (shrine == null) shrine = go.GetComponentInChildren<Shrine>();
        if (shrine != null) {
            shrine.SetInteractable(IsEntityUsed);
            return;
        }
        Debug.LogError("Could not apply IsUsed specific data to instance!");
    }
    public void TrySave(GameObject go) {

        // Try chest
        var chest = go.GetComponent<Chest>();
        if (chest == null) chest = go.GetComponentInChildren<Chest>();
        if (chest != null) {
            IsEntityUsed = chest.HasUsed;
            return;
        }
        // Try shrine
        var shrine = go.GetComponent<Shrine>();
        if (shrine == null) shrine = go.GetComponentInChildren<Shrine>();
        if (shrine != null) {
            IsEntityUsed = shrine.HasUsed;
            return;
        }
        Debug.LogError("Could not apply IsUsed specific data to instance!");
    }
}
public class ArtifactData : EntitySpecificData {
    public byte BiomeIndex;
    public ArtifactData(BiomeType b) {
        BiomeIndex = (byte)b;
    }
    public override void ApplyTo(GameObject go) {
        var g = go.GetComponent<Artifact>();
        if (g == null) g = go.GetComponentInChildren<Artifact>();
        if (g == null) Debug.LogError("Can't find artifact script!");
        g.Init((BiomeType)BiomeIndex);
    }
}
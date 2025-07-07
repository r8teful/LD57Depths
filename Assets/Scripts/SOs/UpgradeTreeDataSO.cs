using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[Serializable]
public struct UpgradeTreeCosts {
    public UpgradeType upgradeType;
    public float baseValue;
    public float increasePerLevel;
    public float linearIncrease; // How much points are added each level
    public float expIncrease; // Procent of points added each level
}

// Defines how the upgrade tree should look
public class UpgradeTreeDataSO : ScriptableObject {
    public UpgradeTreeType type;
    public UpgradeTreeCosts costsValues;
    // Dictionary<Level,Upgrade that level>
    public Dictionary<int,UpgradeRecipeSO> upgradeTree;
}
public enum UpgradeTreeType {
    Speed,
    Mining,
    Light
    // etc
}
public enum UpgradeType {
    MiningSpeed,
    MiningDamange,
    MovementSpeed,
    OxygenCapacity,
    ResourceCapacity,
    Light
}
public enum IncreaseType {
    Add,
    Multiply
}
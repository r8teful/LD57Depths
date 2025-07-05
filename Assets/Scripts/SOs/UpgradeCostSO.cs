using UnityEngine;

[CreateAssetMenu(fileName = "UpgradeCostSO", menuName = "ScriptableObjects/UpgradeCostSO", order = 1)]
// Holds upgrade cost data for one tree, for example, it could start at 100 points, and increase in a certain kind of way
public class UpgradeCostSO : ScriptableObject {
    public UpgradeType upgradeType;
    public float baseValue;
    public float increasePerLevel; 
    public float linearIncrease; // How much points are added each level
    public float expIncrease; // Procent of points added each level
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
using UnityEngine;

[CreateAssetMenu(fileName = "UpgradeDataSO", menuName = "ScriptableObjects/UpgradeDataSO", order = 2)]
public class UpgradeDataSO : ScriptableObject {

    public UpgradeType type;
    public IncreaseType increaseType;
    public float baseValue;
    public float increasePerLevel;
    public int maxLevel;
    public UpgradeCostSO[] costData; // Array to hold costs for different resource
}
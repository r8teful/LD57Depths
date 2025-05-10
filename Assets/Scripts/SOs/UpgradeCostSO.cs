using UnityEngine;

[CreateAssetMenu(fileName = "UpgradeCostSO", menuName = "ScriptableObjects/UpgradeCostSO", order = 1)]
public class UpgradeCostSO : ScriptableObject {
    public ItemData resourceType;
    public float baseCost;
    public float increasePerLevel;
    public IncreaseType increaseType;
    public int requiredAtLevel; 
    public int stopsAtLevel;
}
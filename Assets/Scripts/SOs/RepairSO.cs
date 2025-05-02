using UnityEngine;

[CreateAssetMenu(fileName = "RepairSO", menuName = "ScriptableObjects/RepairSO", order = 3)]
public class RepairSO : ScriptableObject{
    public UpgradeCostSO[] costData;
    public RepairType RepairType;
}
public enum RepairType {
    Logic,
    Hull,
    Engine
}
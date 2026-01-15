using System.Collections.Generic;
using UnityEngine;


[CreateAssetMenu(fileName = "AbilityUpgradeDefition", menuName = "ScriptableObjects/Abilities/AbilityUpgradeDefition", order = 8)]
public class AbilityUpgradeDefition : ScriptableObject {
    public List<StatModifierDef> initialStats;
    public List<StatModifierDef> upgradeAmount;
}
public class StatModifierDef {
    public StatType Stat;
    public float InitialValue;
    public float IncreaseValue;
    public StatModifyType IncreaseType;
}
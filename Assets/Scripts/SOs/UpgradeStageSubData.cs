
using Sirenix.OdinInspector;
using UnityEngine;

[CreateAssetMenu(fileName = "SubStageData", menuName = "ScriptableObjects/Upgrades/SubStageData")]
public class UpgradeStageSubData : UpgradeStageExtraDataSO {
    public Sprite UpgradeIcon;
    public bool isLast;
    [ShowIf("isLast")]
    public Sprite UpgradeIconComplete;
}

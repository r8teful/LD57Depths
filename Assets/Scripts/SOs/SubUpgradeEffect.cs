using UnityEngine;
[CreateAssetMenu(fileName = "SubUpgradeEffect", menuName = "ScriptableObjects/Upgrades/SubUpgradeEffect")]
public class SubUpgradeEffect : UpgradeEffect {
    public Sprite SpriteExterior;
    public Sprite SpriteInterior;
    public UpgradeNodeSO upgrade; 
    public bool isMajor;  
    public override void Execute(ExecutionContext context) {
        SubmarineManager.Instance.NewSubUpgrade(this);
    }

    public override UIExecuteStatus GetExecuteStatus() {
        return null;
    }
}
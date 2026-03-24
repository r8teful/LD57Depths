using UnityEngine;
[CreateAssetMenu(fileName = "SubUpgradeEffect", menuName = "ScriptableObjects/Upgrades/SubUpgradeEffect")]
public class SubUpgradeEffect : UpgradeEffect {
    public Sprite SpriteExterior;
    public Sprite SpriteInterior;
    [Tooltip("used for both cutscene checks, and ID mappings")]
    public UpgradeNodeSO upgrade; 

    public bool unlocksNextZone;
    public override void Execute(ExecutionContext context) {
        SubmarineManager.Instance.NewSubUpgrade(this);
    }

    public override UIExecuteStatus GetExecuteStatus() {
        return null;
    }
}
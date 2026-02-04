using System.Collections;
using UnityEngine;

[CreateAssetMenu(fileName = "SubUpgradeEffect", menuName = "ScriptableObjects/Upgrades/SubUpgradeEffect")]

public class SubUpgradeEffect : UpgradeEffect {
    // Something that characterizes this upgrade
    // How the sub will visually change after this effect
    public Sprite SpriteExterior;
    public Sprite SpriteInterior;
    public SubRecipeSO upgrade; // So we can simply look at the ID and be like, this has been upgraded
    public override void Execute(ExecutionContext context) {
        SubmarineManager.Instance.NewSubUpgrade(upgrade, this);
    }

    public override StatChangeStatus GetChangeStatus() {
        return new();
    }
}
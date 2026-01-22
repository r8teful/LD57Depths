using System.Collections;
using UnityEngine;
[CreateAssetMenu(fileName = "TileUpgradeEffectSO", menuName = "ScriptableObjects/Upgrades/TileUpgradeEffectSO")]

public class TileUpgradeEffectSO : UpgradeEffect {
    [SerializeField] private TileSO tileToUpgrade;
    [SerializeField] private int dropIncrease; // Again, here we could add more upgradable things
    public override void Execute(ExecutionContext context) {
        // I don't care just call the manager directly what could go wrong!?
        TileDropManager.Instance.NewTileUpgrade(tileToUpgrade.ID, dropIncrease);
    }

    public override StatChangeStatus GetChangeStatus() {
        return new(); // How would we show this? 
    }
}
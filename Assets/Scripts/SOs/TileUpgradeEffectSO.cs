using UnityEngine;
[CreateAssetMenu(fileName = "TileUpgradeEffectSO", menuName = "ScriptableObjects/Upgrades/TileUpgradeEffectSO")]

public class TileUpgradeEffectSO : UpgradeEffect {
    [SerializeField] private TileSO tileToUpgrade;
    [SerializeField] private int dropIncrease; // Again, here we could add more upgradable things
    public override void Execute(ExecutionContext context) {
        // I don't care just call the manager directly what could go wrong!?
        WorldDropManager.Instance.NewTileUpgrade(tileToUpgrade.ID, dropIncrease);
    }

    public override StatChangeStatus GetChangeStatus() {
        var current =  WorldDropManager.Instance.GetTileDropAmount(tileToUpgrade);
        var next =  WorldDropManager.Instance.GetTileDropAmount(tileToUpgrade, dropIncrease);
        int currentProcent = Mathf.RoundToInt(current * 100f);
        int nextProcent = Mathf.RoundToInt(next * 100f);
        return new("Copper", $"{currentProcent}%", $"{nextProcent}%", true); 
    }
}
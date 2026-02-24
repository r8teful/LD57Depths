using UnityEngine;
[CreateAssetMenu(fileName = "TileUpgradeEffectSO", menuName = "ScriptableObjects/Upgrades/TileUpgradeEffectSO")]

public class TileUpgradeEffectSO : UpgradeEffect {
    [SerializeField] private TileSO tileToUpgrade;
    [SerializeField] private int dropIncrease; // Again, here we could add more upgradable things
    [SerializeField] private float durabilityIncrease = 0; // multiply ADD 
    public override void Execute(ExecutionContext context) {
        // I don't care just call the manager directly what could go wrong!?
        WorldTileManager.Instance.NewTileUpgrade(tileToUpgrade.ID, dropIncrease, durabilityIncrease);
    }

    public override UIExecuteStatus GetExecuteStatus() {
        var current =  WorldTileManager.Instance.GetExtraTileDropAmount(tileToUpgrade);
        var next =  WorldTileManager.Instance.GetExtraTileDropAmount(tileToUpgrade, dropIncrease);
        int startProcent = 100;
        int currentProcent = Mathf.RoundToInt(current * 100f) + startProcent;
        int nextProcent = Mathf.RoundToInt(next * 100f) + startProcent;
        return new StatChangeStatus("Copper", $"{currentProcent}%", $"{nextProcent}%", true); 
    }
}
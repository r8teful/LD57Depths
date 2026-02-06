using UnityEngine;

[CreateAssetMenu(fileName = "StatUpgradeEffect", menuName = "ScriptableObjects/Upgrades/StatUpgradeEffect", order = 2)]
public class StatUpgradeEffectSO : UpgradeEffect {

    public StatType upgradeType;
    public StatModifyType increaseType;
    public float modificationValue;
    public override void Execute(ExecutionContext target) {
        // The effect's job is to find the relevant component and tell it to update.
        var playerStats = target.Player.PlayerStats;
        if (playerStats != null) {
            var inst = new StatModifier(modificationValue, upgradeType, increaseType, this);
            playerStats.AddInstanceModifier(inst);
            Debug.Log($"Applied stat upgrade: {upgradeType} by {modificationValue}");
        } else {
            Debug.LogWarning($"Could not find PlayerStats component on {target} to apply {upgradeType} upgrade.");
        }
    }

    public override StatChangeStatus GetChangeStatus() {
        var name = ResourceSystem.GetStatString(upgradeType);

        StatModifier tempMod = new(modificationValue, upgradeType, increaseType, this);
        var currentIncrease = PlayerManager.LocalInstance.PlayerStats.GetProcentStat(upgradeType) * 0.1f;
        var nextIncrease = PlayerManager.LocalInstance.PlayerStats.GetProcentStat(upgradeType, tempMod) * 0.1f;

        int currentProcent = Mathf.RoundToInt(currentIncrease * 100f);
        int nextProcent = Mathf.RoundToInt(nextIncrease * 100f);
        var isLowerBad = ResourceSystem.IsLowerBad(upgradeType);
        return new(name, $"{currentProcent}%", $"{nextProcent}%", isLowerBad);

    }
}
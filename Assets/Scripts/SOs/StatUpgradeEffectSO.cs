using UnityEngine;

[CreateAssetMenu(fileName = "StatUpgradeEffect", menuName = "ScriptableObjects/Upgrades/StatUpgradeEffect", order = 2)]
public class StatUpgradeEffectSO : UpgradeEffect {

    public StatType upgradeType;
    public IncreaseType increaseType;
    public float modificationValue;
    public override void Apply(NetworkedPlayer target) {
        // The effect's job is to find the relevant component and tell it to update.
        var playerStats = target.PlayerStats;
        if (playerStats != null) {
            playerStats.ModifyPermamentStat(upgradeType, modificationValue, increaseType);
            Debug.Log($"Applied stat upgrade: {upgradeType} by {modificationValue}");
        } else {
            Debug.LogWarning($"Could not find PlayerStats component on {target.name} to apply {upgradeType} upgrade.");
        }
    }

    public override StatChangeStatus GetChangeStatus() {
        var name = ResourceSystem.GetStatString(upgradeType);
        var currentValue = NetworkedPlayer.LocalInstance.PlayerStats.GetStatBase(upgradeType); // Need go get the BASE values, not the final
        var nextValue = UpgradeCalculator.CalculateUpgradeChange(currentValue, increaseType, modificationValue);
        var isLowerBad = ResourceSystem.IsLowerBad(upgradeType);
        return new(name, currentValue, nextValue, isLowerBad);

    }
}
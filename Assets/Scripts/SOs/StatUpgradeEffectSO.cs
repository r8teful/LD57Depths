using UnityEngine;

[CreateAssetMenu(fileName = "StatUpgradeEffect", menuName = "ScriptableObjects/Upgrades/StatUpgradeEffect", order = 2)]
public class StatUpgradeEffectSO : UpgradeEffect {

    public StatType upgradeType;
    public IncreaseType increaseType;
    public float modificationValue;
    public override void Apply(GameObject target) {
        // The effect's job is to find the relevant component and tell it to update.
        var playerStats = target.GetComponent<PlayerStatsManager>(); // Assuming you have a central stat manager
        if (playerStats != null) {
            playerStats.ModifyPermamentStat(upgradeType, modificationValue, increaseType);
            Debug.Log($"Applied stat upgrade: {upgradeType} by {modificationValue}");
        } else {
            Debug.LogWarning($"Could not find PlayerStats component on {target.name} to apply {upgradeType} upgrade.");
        }
    }
    public StatChangeStatus GetStatChange() {
        var stat = upgradeType;
        var currentValue = NetworkedPlayer.LocalInstance.PlayerStats.GetStatBase(stat); // Need go get the BASE values, not the final
        var nextValue = UpgradeCalculator.CalculateUpgradeChange(currentValue, increaseType, modificationValue);
        return new(stat, currentValue, nextValue);
    }
}
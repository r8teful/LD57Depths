using UnityEngine;

// Unlocks an ability

[CreateAssetMenu(fileName = "AbilityUnlockEffectSO", menuName = "ScriptableObjects/Upgrades/AbilityUnlockEffectSO")]
public class AbilityUnlockEffectSO : UpgradeEffect {
    public string UnlockedAbilityName; // Idk if we want string but just placeholder for now
    public override void Apply(NetworkedPlayer target) {
        // Unlock the ability
    }

    public override StatChangeStatus GetChangeStatus() {
        return new StatChangeStatus();
    }
}
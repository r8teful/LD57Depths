using UnityEngine;

// Unlocks an ability

[CreateAssetMenu(fileName = "AbilityUnlockEffectSO", menuName = "ScriptableObjects/Upgrades/AbilityUnlockEffectSO")]
public class AbilityUnlockEffectSO : UpgradeEffect {
    public AbilitySO abilityToUnlock;
    public override void Execute(ExecutionContext context) {
        // Unlock the ability
        PlayerManager.LocalInstance.PlayerAbilities.AddAbility(abilityToUnlock);
    }

    public override StatChangeStatus GetChangeStatus() {
        return new StatChangeStatus();
    }
}
using Sirenix.OdinInspector;
using UnityEngine;

// Unlocks an ability

[CreateAssetMenu(fileName = "AbilityUnlockEffectSO", menuName = "ScriptableObjects/Upgrades/AbilityUnlockEffectSO")]
public class AbilityUnlockEffectSO : UpgradeEffect {
    [InlineEditor]
    public AbilitySO abilityToUnlock;
    public override void Execute(ExecutionContext context) {
        // Unlock the ability
        PlayerManager.Instance.PlayerAbilities.AddAbility(abilityToUnlock);
    }

    public override UIExecuteStatus GetExecuteStatus() {
        return null;
    }
}
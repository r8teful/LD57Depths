using UnityEngine;


// Ads a StatModifier to a specific ability target
[CreateAssetMenu(fileName = "StatModAbilityEffectSO", menuName = "ScriptableObjects/Upgrades/StatModAbilityEffectSO")]
public class StatModAbilityEffectSO : UpgradeEffect {

    public StatType upgradeType;
    // So what does this actually mean, when we say multiply? What will happen if we stack multiplication ones?
    public StatModifyType increaseType; 
    public float modificationValue;

    public AbilitySO targetAbility;

    public override void Execute(ExecutionContext target) {
        Debug.Log($"Applying ability buff to {targetAbility.displayName}");
        var mod = new StatModifier(modificationValue, upgradeType, increaseType, this);
        target.Player.PlayerAbilities.GetAbilityInstance(targetAbility.ID).AddInstanceModifier(mod);
    }
    public StatModifier GetStatModifer() {
        return new StatModifier(modificationValue, upgradeType, increaseType, this);
    }

    public override StatChangeStatus GetChangeStatus() {
        
        var statName = ResourceSystem.GetStatString(upgradeType);
        // First we need the current multiplier value, which we need to pull from our targetAbility instance
        var abilityInstance = PlayerManager.LocalInstance.PlayerAbilities.GetAbilityInstance(targetAbility.ID);
        if(abilityInstance == null) {
            //Debug.LogError("Can't get target ability for upgrade. We probably don't have it unlocked yet");
            return new();
        }
        // For brimstone
        // Target: Brimstone
        // Brimstone targets lazer ability
        // Solution: 
       
        StatModifier tempMod = new(modificationValue, upgradeType, increaseType, this);

        var currentValue = abilityInstance.GetEffectiveStat(upgradeType); 
        var nextValue = abilityInstance.GetEffectiveStat(upgradeType,tempMod);
 
        return new(statName, currentValue.ToString("F2"), nextValue.ToString("F2"), ResourceSystem.IsLowerBad(upgradeType));
    }
}
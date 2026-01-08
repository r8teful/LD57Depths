using System;
using System.Collections;
using UnityEngine;


// Ads a StatModifier to a specific ability target
[CreateAssetMenu(fileName = "StatModAbilityEffectSO", menuName = "ScriptableObjects/Upgrades/StatModAbilityEffectSO")]
public class StatModAbilityEffectSO : UpgradeEffect {

    public StatType upgradeType;
    // So what does this actually mean, when we say multiply? What will happen if we stack multiplication ones?
    public StatModifyType increaseType; 
    public float modificationValue;

    public AbilitySO targetAbility;

    public override void Apply(NetworkedPlayer target) {
        Debug.Log($"Applying ability buff to {targetAbility.displayName}");
        var mod = new StatModifier(modificationValue, upgradeType, increaseType, this);
        target.PlayerAbilities.GetAbilityInstance(targetAbility.ID).AddInstanceModifier(mod);
    }
    public StatModifier GetStatModifer() {
        return new StatModifier(modificationValue, upgradeType, increaseType, this);
    }

    public override StatChangeStatus GetChangeStatus() {
        
        var statName = ResourceSystem.GetStatString(upgradeType);
        // First we need the current multiplier value, which we need to pull from our targetAbility instance
        var abilityInstance = NetworkedPlayer.LocalInstance.PlayerAbilities.GetAbilityInstance(targetAbility.ID);
        if(abilityInstance == null) {
            Debug.LogError("Can't get target ability for upgrade. We probably don't have it unlocked yet");
            return new();
        }
        var increaseString = increaseType == StatModifyType.Multiply ? "X" : ""; 
        // If this works I'm a genius 
        // This below doesn't work because the GetTotal function just returns the modified values ( like if we'd have a buff)
        //var currentValue = increaseType == IncreaseType.Multiply ? abilityInstance.GetTotalPercentModifier(upgradeType) :
        //    abilityInstance.GetTotalFlatModifier(upgradeType);
        var currentValue = abilityInstance.GetBuffStatStrength(upgradeType,increaseType);
        //var nextValue = UpgradeCalculator.CalculateUpgradeChange(currentValue, increaseType, modificationValue);
        var nextValue = currentValue + modificationValue; // Next value always additive, no matter its increase type (I think)
        return new(statName, currentValue.ToString("F2")+increaseString, nextValue.ToString("F2") + increaseString, ResourceSystem.IsLowerBad(upgradeType));
    }
}
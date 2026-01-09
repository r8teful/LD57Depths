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
        // If this works I'm a genius 
        // This below doesn't work because the GetTotal function just returns the modified values ( like if we'd have a buff)
        //var currentValue = increaseType == IncreaseType.Multiply ? abilityInstance.GetTotalPercentModifier(upgradeType) :
        //    abilityInstance.GetTotalFlatModifier(upgradeType);
        //var nextValue = UpgradeCalculator.CalculateUpgradeChange(currentValue, increaseType, modificationValue);
        var currentValue = abilityInstance.GetBuffStatStrength(upgradeType,increaseType);
        var nextValue = 0f;
        if (increaseType == StatModifyType.Multiply) { 
            // current value represents a MULTIPLICATIVE element, meaning we ADD the modification type, and then times it with the base stat
            var increaseValue= currentValue + modificationValue;
            nextValue = abilityInstance.GetEffectiveStat(upgradeType) * increaseValue; // This shouldn't be base but depending on the value we are targeting
            // convert the "multiplicative currentValue to an actual one
            currentValue = currentValue * abilityInstance.GetBaseStat(upgradeType);
        } else {
            currentValue= abilityInstance.GetBuffStatStrength(upgradeType,increaseType);
            nextValue = currentValue + modificationValue;

        }
        return new(statName, currentValue.ToString("F2"), nextValue.ToString("F2"), ResourceSystem.IsLowerBad(upgradeType));
    }
}
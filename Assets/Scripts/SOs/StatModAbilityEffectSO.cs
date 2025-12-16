using System;
using System.Collections;
using UnityEngine;


// Ads a StatModifier to a specific ability target

[CreateAssetMenu(fileName = "StatModAbilityEffectSO", menuName = "ScriptableObjects/Upgrades/StatModAbilityEffectSO")]
public class StatModAbilityEffectSO : UpgradeEffect {

    public StatType upgradeType;
    public IncreaseType increaseType;
    public float modificationValue;

    public AbilitySO targetAbility;


    public override void Apply(NetworkedPlayer target) {
        Debug.Log("Applying brimstone buff");
        var mod = new StatModifier(modificationValue, upgradeType, increaseType, this);
        target.PlayerAbilities.GetAbilityInstance(targetAbility.ID).AddInstanceModifier(mod);
    }
    public StatModifier GetStatModifer() {
        return new StatModifier(modificationValue, upgradeType, increaseType, this);
    }

    public override StatChangeStatus GetChangeStatus() {
        var statName = "Damage multiplier"; //  This will depend on the upgrade obviously right?

        // First we need the current multiplier value, which we need to pull from our targetAbility instance
        var abilityInstance = NetworkedPlayer.LocalInstance.PlayerAbilities.GetAbilityInstance(targetAbility.ID);

        // If this works I'm a genius 
        // This below doesn't work because the GetTotal function just returns the modified values ( like if we'd have a buff)
        //var currentValue = increaseType == IncreaseType.Multiply ? abilityInstance.GetTotalPercentModifier(upgradeType) :
        //    abilityInstance.GetTotalFlatModifier(upgradeType);
        var currentValue = abilityInstance.GetBuffStatStrength(upgradeType);
        var nextValue = UpgradeCalculator.CalculateUpgradeChange(currentValue, increaseType, modificationValue);
        return new(statName, currentValue, nextValue,ResourceSystem.IsLowerBad(upgradeType));
    }
}
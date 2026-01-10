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
        // For brimstone
        // Target: Brimstone
        // Brimstone targets lazer ability
        // Solution: 
        // Let each AbilityInstance implement their own logic of how to get current and next value. 
        // Instead of having it all in here
        StatModifier tempMod = new(modificationValue, upgradeType, increaseType, this);

        // Get pure multiplicative values first


        // Now check if the ability we are targeting is applying buff, if so, we need to get the effective stat of the buff target and display that instead
        //var currentValue = abilityInstance.GetCurValue(upgradeType) * abilityInstance.GetEffectiveStat(upgradeType);
        var currentValue = 0; // todo
        var nextValue = abilityInstance.GetNextValue(upgradeType,tempMod);
        //var nextValue = abilityInstance.GetRawValue(upgradeType) + modificationValue; // This will break if modification isn't adding multipliers
        
        return new(statName, currentValue.ToString("F2"), nextValue.ToString("F2"), ResourceSystem.IsLowerBad(upgradeType));
    }
}
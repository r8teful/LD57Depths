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
        StatModifier tempMod = new(modificationValue, upgradeType, increaseType, this);
        // We need different ways to display it, for damage, it needs to be "abstract"
        // so 10% damage -> 20% would be 2x the damage
        // But with things like crit chance, we need the ACTAUL value, 
        // so 5% crit chacnce really means 5% 
        float currentIncrease, nextIncrease;
        if(upgradeType == StatType.MiningCritChance) {
            currentIncrease = abilityInstance.GetEffectiveStat(upgradeType);
            nextIncrease = abilityInstance.GetEffectiveStat(upgradeType,tempMod);
        } else {
            currentIncrease = abilityInstance.GetProcentStat(upgradeType) * 0.1f; 
            nextIncrease = abilityInstance.GetProcentStat(upgradeType,tempMod) * 0.1f;

        }
        int currentProcent =  Mathf.RoundToInt(currentIncrease * 100f);
        int nextProcent=  Mathf.RoundToInt(nextIncrease * 100f);
        return new(statName, $"{currentProcent}%", $"{nextProcent}%", ResourceSystem.IsLowerBad(upgradeType));

    }
}
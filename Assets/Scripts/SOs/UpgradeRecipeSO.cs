using System.Collections.Generic;
using System.Linq;
using UnityEngine;


// Holds data for one specific upgrade node

[CreateAssetMenu(fileName = "UpgradeRecipeSO", menuName = "ScriptableObjects/Upgrades/UpgradeRecipeSO", order = 9)]
public class UpgradeRecipeSO : RecipeBaseSO {
    [SerializeReference]
    public List<UpgradeEffect> effects = new List<UpgradeEffect>(); // The results the upgrade has when purchased 
   
    public List<StatModAbilityEffectSO> GetTargetEffects() {
        return effects.OfType<StatModAbilityEffectSO>().ToList();
    }
    public override void Execute(ExecutionContext context) {
        foreach (var effect in effects) {
            effect.Execute(context);
        }
    }
    public override void PrepareRecipe(float value, List<ItemData> resourcePool) {
        base.PrepareRecipe(value, resourcePool);
        requiredItems = CalculateItemQuantities(Mathf.RoundToInt(value), resourcePool,
            new QuantityCalculationOptions{MaxContributionPercentage = 0.50f });
    }
    
    public static List<ItemQuantity> CalculateItemQuantities(
        int targetValue,
        List<ItemData> resourcePool,
        QuantityCalculationOptions options = null
    ) {
        if (options == null)
            options = new QuantityCalculationOptions();

        float remaining = targetValue;
        var result = new List<ItemQuantity>();

        // Sort by descending itemValue
        var sorted = resourcePool
            .Where(item => item.itemValue > 0f)
            .OrderByDescending(item => item.itemValue);

        foreach (var item in sorted) {
            if (remaining <= 0f)
                break;

            float valuePerItem = item.itemValue;
            // Maximum needed purely by remaining points
            int maxNeeded = Mathf.FloorToInt(remaining / valuePerItem);

            int maxByPercentage;
            if(resourcePool.Count == 1) {
                maxByPercentage = Mathf.FloorToInt(targetValue / valuePerItem);

            } else {
                // Maximum allowed by percentage cap
                maxByPercentage = Mathf.FloorToInt((targetValue * options.MaxContributionPercentage) / valuePerItem);
            }

            // Final count is min of need, availability, and percentage cap
            int take = Mathf.Min(maxNeeded, maxByPercentage);

            if (take > 0) {
                result.Add(new ItemQuantity {
                    item = item,
                    quantity = take
                });
                remaining -= take * valuePerItem;
            }
        }
        return result;
    }

    public List<StatChangeStatus> GetStatStatuses() {
        var statuses = new List<StatChangeStatus>();
        foreach (var effect in effects) {
            statuses.Add(effect.GetChangeStatus());
        }
        return statuses;
    }
}
public class QuantityCalculationOptions {
    /// <summary>
    /// Maximum fraction (0 to 1) of the targetValue that any single item may contribute.
    /// For example, 0.5 means an item cannot cover more than 50% of the total points.
    /// </summary>
    public float MaxContributionPercentage { get; set; } = 1f;

    // Future optional parameters can go here...
}

public enum UpgradeType {
    // MINING Lazer
    MiningLazerRange,
    MiningLazerDamage,
    MiningLazerHandling,
    MiningLazerSpecialNoFalloff,
    MiningLazerSpecialNoKnockback,
    MiningLazerSpecialCombo,
    MiningLazerSpecialBrimstone,
    // PLAYER SPEED
    PlayerSpeedMax,
    PlayerSpecialHandling,
    PlayerSpecialOreEmit,
    PlayerSpecialBlockOxygen,
    PlayerSpecialDiver,
    SpeedAcceleration,
    // OXYGEN
    OxygenMax,
    // UTILITY  
    PlayerLightRange,
    UtilityLightIntensity,
}

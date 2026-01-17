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
    public override void PrepareRecipe(float value, List<ItemQuantity> resourcePool) {
        base.PrepareRecipe(value, resourcePool);
        requiredItems = CalculateItemQuantities(Mathf.RoundToInt(value), resourcePool,
            new QuantityCalculationOptions{ RespectAvailability = false, MaxContributionPercentage = 0.75f });
    }
    
    public static List<ItemQuantity> CalculateItemQuantities(
        int targetValue,
        List<ItemQuantity> resourcePool,
        QuantityCalculationOptions options = null
    ) {
        if (options == null)
            options = new QuantityCalculationOptions();

        float remaining = targetValue;
        var result = new List<ItemQuantity>();

        // Sort by descending itemValue
        var sorted = resourcePool
            .Where(iq => iq.item.itemValue > 0f)
            .OrderByDescending(iq => iq.item.itemValue);

        foreach (var iq in sorted) {
            if (remaining <= 0f)
                break;

            float valuePerItem = iq.item.itemValue;
            // Determine how many are "available" by option
            int availableLimit = options.RespectAvailability ? iq.quantity : int.MaxValue;

            // Maximum needed purely by remaining points
            int maxNeeded = Mathf.FloorToInt(remaining / valuePerItem);

            // Maximum allowed by percentage cap
            int maxByPercentage = Mathf.FloorToInt((targetValue * options.MaxContributionPercentage) / valuePerItem);

            // Final count is min of need, availability, and percentage cap
            int take = Mathf.Min(maxNeeded, availableLimit, maxByPercentage);

            if (take > 0) {
                result.Add(new ItemQuantity {
                    item = iq.item,
                    quantity = take
                });
                remaining -= take * valuePerItem;
            }
        }

        // TODO: handle leftover if remaining > 0, if desired
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
    /// If true, will cap usage of each item by its available quantity in the pool.
    /// If false, ignores availability and assumes infinite supply.
    /// </summary>
    public bool RespectAvailability { get; set; } = true;

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

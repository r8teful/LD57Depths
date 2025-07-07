using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[CreateAssetMenu(fileName = "UpgradeDataSO", menuName = "ScriptableObjects/UpgradeDataSO", order = 2)]
// Holds data for one specific upgrade node
public class UpgradeRecipeSO : RecipeBaseSO {

    public UpgradeType type;
    public IncreaseType increaseType;
    public float value; // how much to increase the attribute by

    public Dictionary<ushort, int> costData; // Array to hold costs for different resource

    public override void PrepareRecipe(float value, List<ItemQuantity> resourcePool) {
        base.PrepareRecipe(value, resourcePool);
        requiredItems = CalculateItemQuantities(Mathf.RoundToInt(value), resourcePool);
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
    public override bool ExecuteRecipe(RecipeExecutionContext context) {
        throw new System.NotImplementedException();
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
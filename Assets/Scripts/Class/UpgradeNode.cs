using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class UpgradeNode {
    public ushort NodeID;
    public int CurrentLevel;

    public List<ItemQuantity> requiredItems = new List<ItemQuantity>(); // set by the tree

    public bool IsMaxed(int maxStages) => CurrentLevel >= maxStages;

    // Constructor
    public UpgradeNode(ushort id) {
        NodeID = id;
        CurrentLevel = 0;
    }

    public UpgradeNode(ushort id, float cost,UpgradeTierSO tier) : this(id) {
        UpdateNodeCost(cost, tier.Items);
    }


    public void UpdateNodeCost(float value, List<ItemData> resourcePool) {
        requiredItems = CalculateItemQuantities(Mathf.RoundToInt(value), resourcePool,
            new QuantityCalculationOptions { MaxContributionPercentage = 0.50f });
    }

    internal void UpdateNodeCost(UpgradeNodeSO node, UpgradeTreeDataSO tree) {
        var cost = node.GetStageCost(CurrentLevel, tree);
        var tier = node.GetStageTier(CurrentLevel);
        if (tier == null) return; // final tier reached, no need to change costs
        UpdateNodeCost(cost, tier.Items);
    }
    public bool CanAfford(InventoryManager inv) {
        if (inv == null)
            return false;
        if (requiredItems.Count == 0) return false;
        foreach (var req in requiredItems) {
            if (inv.GetItemCount(req.item.ID) < req.quantity) {
                return false;
            }
        }
        return true;
    }

    /// <summary>
    /// Gets the status of each ingredient for UI display.
    /// </summary>
    public List<IngredientStatus> GetIngredientStatuses(InventoryManager clientInventory) {
        var statuses = new List<IngredientStatus>();
        if (clientInventory == null) {
            // If no inventory, assume player has none of the items
            foreach (var req in requiredItems) {
                statuses.Add(new IngredientStatus(req.item, req.quantity, 0));
            }
            return statuses;
        }

        foreach (var req in requiredItems) {
            statuses.Add(new IngredientStatus(
                req.item,
                req.quantity,
                clientInventory.GetItemCount(req.item.ID)
            ));
        }
        return statuses;
    }
    public int GetRecipeValue() {
        int total = 0;
        foreach (var req in requiredItems) {
            total += (req.item.itemValue * req.quantity);
        }
        return total;
    }
    public static List<ItemQuantity> CalculateItemQuantities(int targetValue,List<ItemData> resourcePool,QuantityCalculationOptions options = null) {
        var result = new List<ItemQuantity>();
        options ??= new QuantityCalculationOptions();
        float remaining = targetValue;
        if (resourcePool == null || resourcePool.Count == 0) return result;

        // Sort by descending itemValue
        var sorted = resourcePool.Where(item => item.itemValue > 0f).OrderByDescending(item => item.itemValue);

        foreach (var item in sorted) {
            if (remaining <= 0f)break;
            float valuePerItem = item.itemValue;
            // Maximum needed purely by remaining points
            int maxNeeded = Mathf.FloorToInt(remaining / valuePerItem);
            int maxByPercentage;
            if (resourcePool.Count == 1) {
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

}
public class QuantityCalculationOptions {
    /// <summary>
    /// Maximum fraction (0 to 1) of the targetValue that any single item may contribute.
    /// For example, 0.5 means an item cannot cover more than 50% of the total points.
    /// </summary>
    public float MaxContributionPercentage { get; set; } = 1f;

    // Future optional parameters can go here...
}
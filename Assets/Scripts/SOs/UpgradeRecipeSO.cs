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

    public override void PrepareRecipe(int tier, UpgradeTreeCosts costsValues) {
        base.PrepareRecipe(tier,costsValues);
        // Todo actually calculate the thing here
        var item = new ItemQuantity() { item = App.ResourceSystem.GetItemByID(0), quantity = 3 };
        requiredItems = new List<ItemQuantity>() {item };
    }
    // The node should handle the calculation of the costs itself
    public Dictionary<ushort, int> CalculateItemQuantities(
        int level,
        float baseValue,
        float linearIncrease,
        float expIncrease,
        Dictionary<ushort, int> resourcePool) {
        // Calculate total cost
        float cost = UpgradeCalculator.CalculateTotalPoints(level, baseValue, linearIncrease, expIncrease);
        float remaining = cost;

        // Prepare result dictionary
        var result = new Dictionary<ushort, int>();

        // Get item values and sort by descending value
        var sortedItems = resourcePool.Keys
            .Select(id => new {
                Id = id,
                Data = App.ResourceSystem.GetItemByID(id)
            })
            .OrderByDescending(x => x.Data.value)
            .ToList();

        // Iterate highest to lowest
        foreach (var entry in sortedItems) {
            if (remaining <= 0f)
                break;

            ushort itemId = entry.Id;
            float itemValue = entry.Data.value;
            int available = resourcePool[itemId];

            if (available <= 0 || itemValue <= 0f)
                continue;

            // Calculate max count of this item we can use
            int maxNeeded = (int)Mathf.Floor(remaining / itemValue);
            int count = Mathf.Min(available, maxNeeded);

            if (count > 0) {
                result[itemId] = count;
                remaining -= count * itemValue;
            }
        }

        // Optionally, we can handle leftover cost here if remaining > 0
        return result;
    }

    public override bool ExecuteRecipe(RecipeExecutionContext context) {
        throw new System.NotImplementedException();
    }
}
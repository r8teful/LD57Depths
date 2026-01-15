using System;
using System.Collections.Generic;
using UnityEngine;



[Serializable]
public struct UpgradeTreeCosts {
    public float baseValue;
    public float linearIncrease; // How much points are added each level
    public float expIncrease; // Procent of points added each level
}

// Defines how the upgrade tree should look
[CreateAssetMenu(fileName = "UpgradeTreeDataSO", menuName = "ScriptableObjects/Upgrades/UpgradeTreeDataSO")]
public class UpgradeTreeDataSO : ScriptableObject {
    public UpgradeTreeCosts costsValues; // How the costs of the upgrades increases
    public List<UpgradeTierSO> tiers;
    public List<StatType> statsToDisplay; // Used in the upgrade screen UI, shows what the values of the suplied stats are
    public string treeName; 
    public List<UpgradeNodeSO> nodes = new List<UpgradeNodeSO>();
    public UIUpgradeTree prefab; // The visual representation of this tree in a prefab with the approriate nodes already created

    // The prepared tree is a dictionary mapping the original SO to its runtime instance.
    // This is a robust pattern to avoid modifying the original assets.
    private Dictionary<UpgradeRecipeSO, UpgradeRecipeSO> preparedUpgradeTree;
  
    public Dictionary<UpgradeRecipeSO, UpgradeRecipeSO> PreparedUpgradeTree {
        get {
            if (preparedUpgradeTree == null) {
                GenerateUpgradeTree();
            }
            return preparedUpgradeTree;
        }
    }

    private void GenerateUpgradeTree() {
        if (nodes == null || nodes.Count == 0) return;

        // --- PASS 2: Prepare each recipe instance with the calculated cost ---
        preparedUpgradeTree = new Dictionary<UpgradeRecipeSO, UpgradeRecipeSO>();
        foreach (var node in nodes) {
            if (node.stages == null) continue;
            foreach (var stage in node.stages) {
                int upgradeTier = stage.tier;

                float baseCost = UpgradeCalculator.CalculateCostForLevel(upgradeTier, costsValues.baseValue, costsValues.linearIncrease, costsValues.expIncrease);
                float finalCost = baseCost * stage.costMultiplier;
                List<ItemQuantity> itemPool = GetItemPoolForTier(upgradeTier);

                UpgradeRecipeSO recipeInstance = Instantiate(stage.upgrade);
                recipeInstance.name = stage.upgrade.name + "_Instance";
                recipeInstance.PrepareRecipe(finalCost, itemPool);
                preparedUpgradeTree.Add(stage.upgrade, recipeInstance);
                // Prerequisites are in the NODEs not in the upgrade now
            }
        }
    }
   
    private List<ItemQuantity> GetItemPoolForTier(int tier) {
        var itemPool = new List<ItemQuantity>();
        foreach (var treeTier in tiers) {
            if(treeTier.Tier != tier) continue; // Only care about the their we are checking for
            foreach (var tierItem in treeTier.ItemsInTier) {
                itemPool.Add(new ItemQuantity(tierItem, 999)); // Assuming a large or "infinite" quantity for calculation
            }
        }
        if (itemPool.Count == 0)
            Debug.LogError("Tree does not have a definition for tier: " + tier);
        return itemPool;
    }

    /// <summary>
    /// Gets the prepared runtime instance of an upgrade from the original ScriptableObject asset.
    /// </summary>
    public UpgradeRecipeSO GetPreparedUpgrade(UpgradeRecipeSO originalRecipe) {
        PreparedUpgradeTree.TryGetValue(originalRecipe, out var preparedInstance);
        return preparedInstance;
    }
    
    /// <summary>
    /// Since we no longer generate instances, we need a way to get the final cost on-demand.
    /// </summary>
    /// <param name="stage">The stage whose cost we want.</param>
    /// <returns>The final prepared recipe with calculated costs.</returns>
    public UpgradeRecipeSO GetPreparedRecipeForStage(UpgradeStage stage) {
        int currentStageLevel = stage.tier;
        float baseCost = UpgradeCalculator.CalculateCostForLevel(currentStageLevel, costsValues.baseValue, costsValues.linearIncrease, costsValues.expIncrease);
        float finalCost = baseCost * stage.costMultiplier;
        List<ItemQuantity> itemPool = GetItemPoolForTier(currentStageLevel);

        UpgradeRecipeSO recipeInstance = Instantiate(stage.upgrade);
        recipeInstance.name = $"{stage.upgrade.name}_Preview"; // Use a temporary name
        recipeInstance.PrepareRecipe(finalCost, itemPool);

        return recipeInstance;
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
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

    
    /// <summary>
    /// Since we no longer generate instances, we need a way to get the final cost on-demand.
    /// </summary>
    /// <param name="stage">The stage whose cost we want.</param>
    /// <returns>The final prepared recipe with calculated costs.</returns>
    public UpgradeRecipeSO GetPreparedRecipeForStage(UpgradeStage stage) {
        int currentStageLevel = stage.costTier;
        float baseCost = UpgradeCalculator.CalculateCostForLevel(currentStageLevel, costsValues.baseValue, costsValues.linearIncrease, costsValues.expIncrease);
        float finalCost = baseCost * stage.costMultiplier;
        if (stage.upgradeItemPool == null) return null;
        List<ItemData> itemPool = stage.upgradeItemPool.Items;

        UpgradeRecipeSO recipeInstance = Instantiate(stage.upgrade);
        recipeInstance.name = $"{stage.upgrade.name}_Preview"; // Use a temporary name
        recipeInstance.PrepareRecipe(finalCost, itemPool);

        return recipeInstance;
    }
    public UpgradeRecipeSO GetUpgradeWithValue(int value, HashSet<ushort> pickedIDs) {
        // I hate this but we never store any actual upgrade data so we have to make it every time
        var u = NetworkedPlayer.LocalInstance.UpgradeManager.GetUnlockedUpgrades();
        int bestValueDiff = 99999;
        UpgradeRecipeSO bestMatch = null;
        foreach(var node in nodes) {
            if (!node.ArePrerequisitesMet(u)) continue;
            if (node.IsNodeMaxedOut(u)) continue;
            // Note that there are two IDs, upgrade NODES, and upgrade RECIPES, we have to check RECIPES ( we could check nodes, it would probably be better, but slighlt more complicated to check for maybe?!?)
            UpgradeRecipeSO recipe = node.GetUpgradeData(u,this);
            if(pickedIDs != null) {
                if (pickedIDs.Contains(recipe.ID)) continue; // Don't choose upgrades we already have picked before 
            }
            if (recipe == null) continue;
            var valDiff = Mathf.Abs(recipe.GetRecipeValue() - value);
            if (valDiff < bestValueDiff) {
                bestMatch = recipe;
                bestValueDiff = valDiff;
            }
        }
        return bestMatch;
    }
    public UpgradeRecipeSO GetRandomUpgrade() {
        // I hate this but we never store any actual upgrade data so we have to make it every time
        var u = NetworkedPlayer.LocalInstance.UpgradeManager.GetUnlockedUpgrades();
        System.Random rng = new System.Random();
        var randomNodes = nodes.OrderBy(s => rng.Next());
        foreach (var node in randomNodes) {
            if (!node.ArePrerequisitesMet(u)) continue;
            if (!node.IsNodeMaxedOut(u)) continue;
            return node.GetUpgradeData(u,this);
        }
        return null;
    }
}

using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[Serializable]
public class UpgradeStage {
    [Tooltip("The Scriptable Object defining the upgrade for this level.")]
    public UpgradeRecipeSO upgrade;

    [Range(0.1f, 5f)]
    public float costMultiplier = 1.0f;
    
    [Tooltip("Tier determines which items are required for purchase")] 
    public int tier;

    public string descriptionOverride;
}

[Serializable]
public class UpgradeNode {
    [Tooltip("Human friendly name, for editor & UI")]
    public string nodeName;
    private string guid;
    [ShowInInspector]
    public string GUID => guid;
    [Button("Generate New GUID")]
    private void GenerateGuid() {
        guid = System.Guid.NewGuid().ToString();
    }
    [Tooltip("ANY of these prerequisite nodes must be fully unlocked before this one can be started.")]
    public List<UpgradeNode> prerequisiteNodesAny; 
 
    [Tooltip("The sequence of upgrades for this node, from Level 1 to Max Level.")]
    public List<UpgradeRecipeSO> upgradeLevels;
  
    [Tooltip("Stages are in sequential order, can also just be one like normal")]
    public List<UpgradeStage> stages = new List<UpgradeStage>();
    public int MaxLevel => upgradeLevels.Count;
    public bool IsNodeMaxedOut(IReadOnlyCollection<ushort> unlockedUpgrades) {
        return GetCurrentLevel(unlockedUpgrades) >= MaxLevel;
    }
    /// <summary>
    /// Calculates the current level of a node based on the set of unlocked upgrades.
    /// </summary>
    /// <param name="node">The design-time node to check.</param>
    /// <param name="unlockedUpgrades">The player's set of unlocked upgrade IDs.</param>
    public int GetCurrentLevel(IReadOnlyCollection<ushort> unlockedUpgrades) {
        if (stages == null || stages.Count == 0 || unlockedUpgrades == null) return 0;

        int level = 0;
        foreach (var stage in stages) {
            if (stage.upgrade != null && unlockedUpgrades.Contains(stage.upgrade.ID)) {
                level++;
            } else {
                // Since levels are sequential, we stop at the first un-purchased one.
                break;
            }
        }
        return level;
    }
    /// <summary>
    /// Gets the next available stage for a specific node, if any.
    /// </summary>
    /// <returns>The UpgradeStage to be purchased next, or null if the node is maxed out.</returns>
    public UpgradeStage GetNextStageForNode(IReadOnlyCollection<ushort> unlockedUpgrades) {
        int currentLevel = GetCurrentLevel(unlockedUpgrades);
        if (currentLevel < MaxLevel) {
            return stages[currentLevel];
        }
        return null;
    }

    public bool ArePrerequisitesMet(IReadOnlyCollection<ushort> unlockedUpgrades) {
        if (prerequisiteNodesAny == null || prerequisiteNodesAny.Count == 0) {
            return true; // root nodes available by default
        }
        return prerequisiteNodesAny.Any(p => p != null && p.IsNodeMaxedOut(unlockedUpgrades));
    }
}
[Serializable]
public struct UpgradeTreeCosts {
    public float baseValue;
    public float linearIncrease; // How much points are added each level
    public float expIncrease; // Procent of points added each level
}
[Serializable]
public struct UpgradeTreeTiers {
    [PropertyTooltip("X inlcusive, Y Exlusive ")]
    public Vector2Int tierStartStop; // At what LEVEL does this tier start and stop? This could also be a list of start and stop if we want it to come back at a later level
    public UpgradeTierSO tier; // Which tier are we talking about here?
}

// Defines how the upgrade tree should look
[CreateAssetMenu(fileName = "UpgradeTreeDataSO", menuName = "ScriptableObjects/Upgrades/UpgradeTreeDataSO")]
public class UpgradeTreeDataSO : ScriptableObject {
    public UpgradeTreeCosts costsValues; // How the costs of the upgrades increases
    public List<UpgradeTreeTiers> tiers;
    public List<StatType> statsToDisplay; // Used in the upgrade screen UI, shows what the values of the suplied stats are
    public string treeName; 
    public List<UpgradeNode> nodes = new List<UpgradeNode>();
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
            if (tier >= treeTier.tierStartStop.x && tier < treeTier.tierStartStop.y) {
                foreach (var tierItem in treeTier.tier.ItemsInTier) {
                    itemPool.Add(new ItemQuantity(tierItem, 999)); // Assuming a large or "infinite" quantity for calculation
                }
            }
        }
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

        // Determine tier, calculate cost, etc. (This logic is moved from GenerateUpgradeTree)
        int tierToUse = 0; // Replace with your GetGlobalTierForLevel logic
        float baseCost = UpgradeCalculator.CalculateCostForLevel(currentStageLevel, costsValues.baseValue, costsValues.linearIncrease, costsValues.expIncrease);
        float finalCost = baseCost * stage.costMultiplier;
        List<ItemQuantity> itemPool = GetItemPoolForTier(tierToUse); // Your method for this

        UpgradeRecipeSO recipeInstance = Instantiate(stage.upgrade);
        recipeInstance.name = $"{stage.upgrade.name}_Preview"; // Use a temporary name
        recipeInstance.PrepareRecipe(finalCost, itemPool);

        return recipeInstance;
    }
}

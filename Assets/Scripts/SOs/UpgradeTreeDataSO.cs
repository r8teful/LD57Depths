using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[Serializable]
public class UpgradeNode {
    [Tooltip("The Scriptable Object defining the upgrade itself.")]
    public UpgradeRecipeBase upgrade;

    [Tooltip("The prerequisite upgrade that must be unlocked before this one. Leave empty for root nodes.")]
    public UpgradeRecipeBase prerequisite;

    [Range(0.1f, 5f)]
    public float costMultiplier = 1.0f;
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
    public string treeName; 
    public List<UpgradeNode> nodes = new List<UpgradeNode>();
    public UIUpgradeTree prefab; // The visual representation of this tree in a prefab with the approriate nodes already created

    // The prepared tree is now a dictionary mapping the original SO to its runtime instance.
    // This is a robust pattern to avoid modifying the original assets.
    private Dictionary<UpgradeRecipeBase, UpgradeRecipeBase> preparedUpgradeTree;

    // A helper dictionary to quickly look up node levels.
    private Dictionary<UpgradeRecipeBase, int> upgradeLevels;
  
    public Dictionary<UpgradeRecipeBase, UpgradeRecipeBase> PreparedUpgradeTree {
        get {
            if (preparedUpgradeTree == null) {
                GenerateUpgradeTree();
            }
            return preparedUpgradeTree;
        }
    }

    // Creates the actual data of the tree
    //private Dictionary<int, UpgradeRecipeBase> GenerateUpgradeTree() {
    //    // TODO, here you need to aquire the UpgradeRecipeSO's for the specific UpgradeTreeType we got.
    //    // We could for example do this by having a dictionary in the resourceSystem that contains all the upgradeRecipes for 
    //    // the specific type, then here we would get them. Then we would call UpgradeRecipeSO.PrepareRecipe on each
    //    var recipes = App.ResourceSystem.GetAllRecipeByType(type);
    //    var dictionaryOutput = new Dictionary<int, UpgradeRecipeBase>();
    //    var l = UpgradeCalculator.CalculatePointArray(length, costsValues.baseValue,costsValues.linearIncrease,costsValues.expIncrease);
    //    var d = GetItemPoolForAllTiers();
    //    for (int i = 0; i < l.Length; i++) {
    //        // Setup dictionary
    //        if (recipes[i] == null) Debug.LogError("Recipes likely out of bounds");
    //        dictionaryOutput.Add(i, recipes[i]);
    //        dictionaryOutput[i].PrepareRecipe(l[i], d[i]);
    //        dictionaryOutput[i].SetPrerequisites(i==0 ? null : recipes[i-1]); // Link to the previous node
    //    }
    //    return dictionaryOutput;
    //}
    private void GenerateUpgradeTree() {
        preparedUpgradeTree = new Dictionary<UpgradeRecipeBase, UpgradeRecipeBase>();
        upgradeLevels = new Dictionary<UpgradeRecipeBase, int>();

        // Find all root nodes (those without prerequisites) to start the generation process.
        var rootNodes = nodes.Where(n => n.prerequisite == null);

        foreach (var rootNode in rootNodes) {
            // Process each root and its descendants recursively.
            ProcessNode(rootNode, 0);
        }
    } 
    /// <summary>
    /// Recursively processes a node, calculates its level, prepares its recipe, and then processes its children.
    /// </summary>
    private void ProcessNode(UpgradeNode node, int level) {
        // Avoid processing the same node multiple times if it's referenced incorrectly.
        if (node.upgrade == null || preparedUpgradeTree.ContainsKey(node.upgrade)) {
            return;
        }
        var recipe = node.upgrade;
        // --- Calculate Cost & Item Pool for this specific node ---
        float costBase = UpgradeCalculator.CalculateCostForLevel(level, costsValues.baseValue, costsValues.linearIncrease, costsValues.expIncrease);
        float cost = costBase * node.costMultiplier;
        List<ItemQuantity> itemPool = GetItemPoolForLevel(level);

        // --- Instantiate and Prepare the Recipe ---
        // We instantiate it so we don't modify the base ScriptableObject asset.
        UpgradeRecipeBase recipeInstance = Instantiate(recipe);
        recipeInstance.name = recipe.name + "_Instance"; // For easier debugging
        recipeInstance.PrepareRecipe(cost, itemPool);

        // --- Store the prepared instance and its level ---
        preparedUpgradeTree.Add(recipe, recipeInstance);
        upgradeLevels.Add(recipe, level);

        // --- Find and process all children of this node ---
        var children = nodes.Where(n => n.prerequisite == recipe);
        foreach (var childNode in children) {
            // Pass the original prerequisite link to the instance
            recipeInstance.AddChild(childNode.upgrade); // Assumes you add an AddChild method
            childNode.upgrade.SetPrerequisites(recipeInstance); // Set prerequisite on the original for consistency? No, on instance

            // To ensure correct prerequisites are linked on the INSTANCES:
            UpgradeRecipeBase childInstance = GetPreparedUpgrade(childNode.upgrade);
            if (childInstance != null) { // If it's already processed, just link it
                childInstance.SetPrerequisites(recipeInstance);
            }

            // Process the next level
            ProcessNode(childNode, level + 1);
        }
    }
    private List<ItemQuantity> GetItemPoolForLevel(int level) {
        var itemPool = new List<ItemQuantity>();
        foreach (var tier in tiers) {
            if (level >= tier.tierStartStop.x && level < tier.tierStartStop.y) {
                foreach (var tierItem in tier.tier.ItemsInTier) {
                    itemPool.Add(new ItemQuantity(tierItem, 999)); // Assuming a large or "infinite" quantity for calculation
                }
            }
        }
        return itemPool;
    }

    #region Helper Functions

    /// <summary>
    /// Gets the prepared runtime instance of an upgrade from the original ScriptableObject asset.
    /// </summary>
    public UpgradeRecipeBase GetPreparedUpgrade(UpgradeRecipeBase originalRecipe) {
        PreparedUpgradeTree.TryGetValue(originalRecipe, out var preparedInstance);
        return preparedInstance;
    }

    /// <summary>
    /// Returns all root upgrades in this tree (those with no prerequisites).
    /// </summary>
    public IEnumerable<UpgradeRecipeBase> GetRootUpgrades() {
        return nodes.Where(n => n.prerequisite == null)
                    .Select(n => GetPreparedUpgrade(n.upgrade));
    }

    /// <summary>
    /// A very useful helper to find which upgrades can be unlocked next.
    /// </summary>
    /// <param name="unlockedUpgrades">A collection of original UpgradeRecipeBase SOs that the player has unlocked.</param>
    /// <returns>A list of prepared upgrades that are now available to be unlocked.</returns>
    public List<UpgradeRecipeBase> GetAvailableUpgrades(ICollection<UpgradeRecipeBase> unlockedUpgrades) {
        var available = new List<UpgradeRecipeBase>();
        foreach (var node in nodes) {
            // An upgrade is available if:
            // 1. It hasn't been unlocked yet.
            // 2. Its prerequisite is null (it's a root) OR its prerequisite has been unlocked.
            if (!unlockedUpgrades.Contains(node.upgrade) && (node.prerequisite == null || unlockedUpgrades.Contains(node.prerequisite))) {
                available.Add(GetPreparedUpgrade(node.upgrade));
            }
        }
        return available;
    }

    #endregion
}
public enum IncreaseType {
    Add,
    Multiply
}
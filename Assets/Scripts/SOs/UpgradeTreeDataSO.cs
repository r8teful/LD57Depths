using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[Serializable]
public class UpgradeNode {
    [Tooltip("The Scriptable Object defining the upgrade itself.")]
    public UpgradeRecipeSO upgrade;

    [Tooltip("The prerequisite upgrade that must be unlocked before this one. Leave empty for root nodes.")]
    public List<UpgradeRecipeSO> prerequisiteAny;

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
    public List<StatType> statsToDisplay; // Used in the upgrade screen UI, shows what the values of the suplied stats are
    public string treeName; 
    public List<UpgradeNode> nodes = new List<UpgradeNode>();
    public UIUpgradeTree prefab; // The visual representation of this tree in a prefab with the approriate nodes already created

    // The prepared tree is now a dictionary mapping the original SO to its runtime instance.
    // This is a robust pattern to avoid modifying the original assets.
    private Dictionary<UpgradeRecipeSO, UpgradeRecipeSO> preparedUpgradeTree;

    // A helper dictionary to quickly look up node levels.
    private Dictionary<UpgradeRecipeSO, int> upgradeLevels;
  
    public Dictionary<UpgradeRecipeSO, UpgradeRecipeSO> PreparedUpgradeTree {
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
        if (nodes == null || nodes.Count == 0) return;

        // --- PASS 1: Calculate the level (shortest path depth) for every node ---
        CalculateAllNodeLevels();

        // --- PASS 2: Prepare each recipe instance with the calculated cost ---
        preparedUpgradeTree = new Dictionary<UpgradeRecipeSO, UpgradeRecipeSO>();
        foreach (var node in nodes) {
            if (node.upgrade == null) continue;

            int level = upgradeLevels[node.upgrade];
            float baseCost = UpgradeCalculator.CalculateCostForLevel(level, costsValues.baseValue, costsValues.linearIncrease, costsValues.expIncrease);
            float finalCost = baseCost * node.costMultiplier;
            List<ItemQuantity> itemPool = GetItemPoolForLevel(level);

            UpgradeRecipeSO recipeInstance = Instantiate(node.upgrade);
            recipeInstance.name = node.upgrade.name + "_Instance";
            recipeInstance.PrepareRecipe(finalCost, itemPool);

            preparedUpgradeTree.Add(node.upgrade, recipeInstance);
        }

        // --- PASS 3: Link the prepared instances together ---
        // This is done last to ensure all instances exist before linking.
        foreach (var node in nodes) {
            if (node.upgrade == null || node.prerequisiteAny.Count == 0) continue;

            var preparedNode = GetPreparedUpgrade(node.upgrade);
            foreach (var prereq in node.prerequisiteAny) {
                var preparedPrereq = GetPreparedUpgrade(prereq);
                if (preparedPrereq != null) {
                    preparedNode.AddPrerequisite(preparedPrereq); // Assumes a new method in UpgradeRecipeBase
                }
            }
        }
    }
    private void CalculateAllNodeLevels() {
        upgradeLevels = new Dictionary<UpgradeRecipeSO, int>();
        var nodeLookup = nodes.ToDictionary(n => n.upgrade);
        var childrenLookup = new Dictionary<UpgradeRecipeSO, List<UpgradeRecipeSO>>();

        // Initialize levels and build child lookup for fast traversal
        foreach (var node in nodes) {
            if (node.upgrade == null) continue;
            upgradeLevels[node.upgrade] = int.MaxValue;
            if (!childrenLookup.ContainsKey(node.upgrade)) {
                childrenLookup[node.upgrade] = new List<UpgradeRecipeSO>();
            }

            foreach (var prereq in node.prerequisiteAny) {
                if (!childrenLookup.ContainsKey(prereq)) {
                    childrenLookup[prereq] = new List<UpgradeRecipeSO>();
                }
                childrenLookup[prereq].Add(node.upgrade);
            }
        }

        // Use a queue for breadth-first traversal (guarantees shortest path)
        var queue = new Queue<UpgradeRecipeSO>();

        // Find all root nodes (no prerequisites) and set their level to 0
        foreach (var node in nodes.Where(n => n.prerequisiteAny == null || n.prerequisiteAny.Count == 0)) {
            if (node.upgrade == null) continue;
            upgradeLevels[node.upgrade] = 0;
            queue.Enqueue(node.upgrade);
        }

        while (queue.Count > 0) {
            var current = queue.Dequeue();
            int nextLevel = upgradeLevels[current] + 1;

            if (!childrenLookup.ContainsKey(current)) continue;

            foreach (var child in childrenLookup[current]) {
                // If we found a shorter path to this child, update its level and re-evaluate its children
                if (nextLevel < upgradeLevels[child]) {
                    upgradeLevels[child] = nextLevel;
                    queue.Enqueue(child);
                }
            }
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
    public UpgradeRecipeSO GetPreparedUpgrade(UpgradeRecipeSO originalRecipe) {
        PreparedUpgradeTree.TryGetValue(originalRecipe, out var preparedInstance);
        return preparedInstance;
    }

    /// <summary>
    /// Returns all root upgrades in this tree (those with no prerequisites).
    /// </summary>
    public IEnumerable<UpgradeRecipeSO> GetRootUpgrades() {
        return nodes.Where(n => n.prerequisiteAny == null)
                    .Select(n => GetPreparedUpgrade(n.upgrade));
    }

    /// <summary>
    /// A very useful helper to find which upgrades can be unlocked next.
    /// </summary>
    /// <param name="unlockedUpgrades">A collection of original UpgradeRecipeBase SOs that the player has unlocked.</param>
    /// <returns>A list of prepared upgrades that are now available to be unlocked.</returns>
    public List<UpgradeRecipeSO> GetAvailableUpgrades(ICollection<UpgradeRecipeSO> unlockedUpgrades) {
        var available = new List<UpgradeRecipeSO>();
        if (nodes == null) return available;

        foreach (var node in nodes) {
            if (node.upgrade == null || unlockedUpgrades.Contains(node.upgrade)) {
                continue; // Skip if no upgrade is assigned or if it's already unlocked
            }

            // Handle root nodes (no prerequisites)
            if (node.prerequisiteAny.Count == 0) {
                available.Add(GetPreparedUpgrade(node.upgrade));
                continue;
            }

            bool isAvailable =  node.prerequisiteAny.Any(p => unlockedUpgrades.Contains(p));
            if (isAvailable) {
                available.Add(GetPreparedUpgrade(node.upgrade));
            }
        }
        return available;
    }

    #endregion
}

using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using UnityEngine;


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
[CreateAssetMenu(fileName = "UpgradeTreeDataSO", menuName = "ScriptableObjects/Upgades/UpgradeTreeDataSO")]
public class UpgradeTreeDataSO : ScriptableObject {
    public UpgradeTreeType type; // The main attribute this tree upgrades
    public int length; // Length of the tree, how many upgrades the tree has
    public UpgradeTreeCosts costsValues; // How the costs of the upgrades increases
    public List<UpgradeTreeTiers> tiers;
    // Dictionary<Level,Upgrade that level>
    private Dictionary<int,UpgradeRecipeBase> upgradeTree; // The actual nodes of the tree

    public Dictionary<int, UpgradeRecipeBase> UpgradeTree {
        get {
            upgradeTree ??= GenerateUpgradeTree(); // If we haven't made it yet create it
            return upgradeTree;
        }
    }

    // Creates the actual data of the tree
    private Dictionary<int, UpgradeRecipeBase> GenerateUpgradeTree() {
        // TODO, here you need to aquire the UpgradeRecipeSO's for the specific UpgradeTreeType we got.
        // We could for example do this by having a dictionary in the resourceSystem that contains all the upgradeRecipes for 
        // the specific type, then here we would get them. Then we would call UpgradeRecipeSO.PrepareRecipe on each
        var recipes = App.ResourceSystem.GetAllRecipeByType(type);
        var dictionaryOutput = new Dictionary<int, UpgradeRecipeBase>();
        var l = UpgradeCalculator.CalculatePointArray(length, costsValues.baseValue,costsValues.linearIncrease,costsValues.expIncrease);
        var d = GetItemPoolForAllTiers();
        for (int i = 0; i < l.Length; i++) {
            // Setup dictionary
            if (recipes[i] == null) Debug.LogError("Recipes likely out of bounds");
            dictionaryOutput.Add(i, recipes[i]);
            dictionaryOutput[i].PrepareRecipe(l[i], d[i]);
            dictionaryOutput[i].SetPrerequisites(i==0 ? null : recipes[i-1]); // Link to the previous node
        }
        return dictionaryOutput;
    }
    private List<ItemQuantity> GetItemPoolForTier(int i) {
        var d = new List<ItemQuantity>();
            // Get available items based on tiers
            foreach (var tier in tiers) {
                // Find all tiers in the range of start & stop
                if (i >= tier.tierStartStop.x && i < tier.tierStartStop.y) {
                    // valid!
                    foreach (var tierItem in tier.tier.ItemsInTier) {
                        d.Add(new ItemQuantity(tierItem)); // TODO this just adds 99 to the quantity!
                    }
                }
        }
        return d;
    }
    private Dictionary<int,List<ItemQuantity>> GetItemPoolForAllTiers() {
        var d = new Dictionary<int, List<ItemQuantity>>();
        for (int i = 0; i < length; i++) {
            d.Add(i,GetItemPoolForTier(i));
        }
        return d;
    }
}
public enum UpgradeTreeType {
    PlayerSpeed,
    Mining,
    Oxygen,
    Utility,
    TreeFarm,
    Lamp,
    Pollution,
}
public enum UpgradeType {
    MiningRange,
    MiningDamage,
    MaxSpeed,
    Acceleration,
    OxygenCapacity,
    ResourceCapacity,
    Light,
    Unlock
}
public enum IncreaseType {
    Add,
    Multiply
}
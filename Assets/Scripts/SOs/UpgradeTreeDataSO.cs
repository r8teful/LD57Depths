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
    public string treeName; 
    public List<UpgradeNodeSO> nodes = new List<UpgradeNodeSO>();
    public UIUpgradeTree prefab; // The visual representation of this tree in a prefab with the approriate nodes already created

    public UpgradeStage GetUpgradeWithValue(int value, HashSet<ushort> pickedIDs) {
        // I hate this but we never store any actual upgrade data so we have to make it every time
        // We can actually change this now but idk if we will need to get any upgrades anymore (yet)

        //var u = PlayerManager.LocalInstance.UpgradeManager.GetUnlockedUpgrades();
        //int bestValueDiff = 99999;
        //UpgradeStage bestMatch = null;
        //foreach(var node in nodes) {
        //    if (!node.ArePrerequisitesMet(u)) continue;
        //    if (node.IsNodeMaxedOut(u)) continue;
        //    // Note that there are two IDs, upgrade NODES, and upgrade RECIPES, we have to check RECIPES ( we could check nodes, it would probably be better, but slighlt more complicated to check for maybe?!?)
        //    UpgradeStage recipe = node.GetUpgradeData(u,this);
        //    if(pickedIDs != null) {
        //        if (pickedIDs.Contains(recipe.ID)) continue; // Don't choose upgrades we already have picked before 
        //    }
        //    if (recipe == null) continue;
        //    var valDiff = Mathf.Abs(recipe.GetRecipeValue() - value);
        //    if (valDiff < bestValueDiff) {
        //        bestMatch = recipe;
        //        bestValueDiff = valDiff;
        //    }
        //}
        return null;
        //return bestMatch;
    }
    public UpgradeStage GetRandomUpgrade() {
        // I hate this but we never store any actual upgrade data so we have to make it every time
        //var u = PlayerManager.LocalInstance.UpgradeManager.GetUnlockedUpgrades();
        //System.Random rng = new System.Random();
        //var randomNodes = nodes.OrderBy(s => rng.Next());
        //foreach (var node in randomNodes) {
        //    if (!node.ArePrerequisitesMet(u)) continue;
        //    if (!node.IsNodeMaxedOut(u)) continue;
        //    return node.GetUpgradeData(u,this);
        //}
        return null;
    }
}

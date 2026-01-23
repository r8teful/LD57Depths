using System.Collections.Generic;
using UnityEngine;

// Basically how this will work: 
// - At init, we set initial states etc, bind this visual data to its corresponding node
// - Within the UIUpgradeNode we subscribe to when we BUY an upgrade, this will then
// ask this script to update the next stage or state within the node. When we want to 
// Show the popup, we ask this script to get the ingredient status, because that will basically
// Change all the time, because we could be buying or removing or gaining items all the time
// That's it, this script just handles the logic of this mess of a code in one place
// Cleans it up, and lets UIUpgradeNode simply display whatever data needs to be displayed 
// without having to do any of the logic itself
public class UpgradeNodeVisualData {
    private UpgradeRecipeSO _currentRecipe;
    private readonly UpgradeTreeDataSO _cachedTree;
    private readonly InventoryManager _inventory;
    private readonly UpgradeNodeSO _node;
    public UpgradeNodeSO Node => _node;
    // Straight from SO
    public Sprite Icon;
    public string Title;
    public string Description;

    // Depends on game state
    public List<IngredientStatus> IngredientStatuses; // This comes from a RecipeBaseSO
    public List<StatChangeStatus> StatChangeStatuses; // This comes from a RecipeBaseSO
    public UpgradeNodeState State; // Depends on inventory and costs
    public int LevelMax;
    public int LevelCurrent;

    public UpgradeNodeVisualData(UpgradeNodeSO node, InventoryManager inventory,
        UpgradeTreeDataSO tree, HashSet<ushort> existingUpgrades) {
        // Need to get current node STAGE, and get the recipe from that state from here
        _currentRecipe = node.GetUpgradeData(existingUpgrades, tree);
        _cachedTree = tree; // this *SHOULD* never change, unless we do some crazy stuff 
        _inventory = inventory;
        _node = node;
        Icon = node.icon;
        Title = node.nodeName;
        if (_currentRecipe == null) {
            Description = node.description;
        } else {
            // Use title if we have node aswell?
            Description = _currentRecipe.description;
        }
        RefreshRecipeData(existingUpgrades);
        // Don't really need to get the popup info on Init because we dont show it untill we 
        // actually want to show the popup
    }


    private void RefreshRecipeData(HashSet<ushort> existingUpgrades) {
        _currentRecipe = _node.GetUpgradeData(existingUpgrades, _cachedTree);
        // Get new stat stanges
        if(_currentRecipe == null) {
            // Probably no stages. simply return
            return;
        }
        StatChangeStatuses = _currentRecipe.GetStatStatuses();
        var canAfford = _currentRecipe.CanAfford(_inventory);
        State = _node.GetState(existingUpgrades,canAfford);
        LevelMax = _node.MaxLevel;
        LevelCurrent = _node.GetCurrentLevel(existingUpgrades);
        UpdateForPopup(_inventory); // We're ontop of the upgrade when upgrading it so we should refresh the popup
    }
    public void UpdateForUpgradePurchase(HashSet<ushort> existingUpgrades) {
        RefreshRecipeData(existingUpgrades);
    }

    internal void UpdateForPopup(InventoryManager inv) {
        if(_currentRecipe == null) {
            return;
        }
        IngredientStatuses = _currentRecipe.GetIngredientStatuses(inv);
    }

    internal bool IsMaxLevel() {
        // As long as we call RefreshRecipeData before this these variables should be correct
        return LevelCurrent == LevelMax;
    }
}

public enum UpgradeNodeState { Purchased, Purchasable, Unlocked, Locked }
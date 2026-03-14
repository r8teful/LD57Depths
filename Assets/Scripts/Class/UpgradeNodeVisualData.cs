using Assets.SimpleLocalization.Scripts;
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
    private UpgradeStage _currentUpgradeStage;
    private UpgradeManagerPlayer _upgradeManager;
    private readonly UpgradeNodeSO _node;
    public UpgradeNodeSO Node => _node;
    // Straight from SO
    public Sprite Icon;
    public Sprite IconExtra; // Used for sub upgrades as a cool extra for the popup
    public string Title;
    public string Description;
    public bool IsCool; // for shader and particle effects

    // Depends on game state
    public List<IngredientStatus> IngredientStatuses; // This comes from a RecipeBaseSO
    public List<StatChangeStatus> StatChangeStatuses; // This comes from a RecipeBaseSO
    public UpgradeNodeState State; // Depends on inventory and costs
    public int LevelMax;
    public int LevelCurrent;

    public UpgradeNodeVisualData(UpgradeNodeSO node, UpgradeManagerPlayer upgradeManager) {
        // Need to get current node STAGE, and get the recipe from that state from here
         _upgradeManager = upgradeManager;
        _node = node;
        Icon = node.icon;
        IsCool = node.IsCool;
        RefreshRecipeData();
        OnLocalize();
        LocalizationManager.OnLocalizationChanged += OnLocalize;
    }
    internal void OnDestroy() {
        LocalizationManager.OnLocalizationChanged -= OnLocalize;
    }

    private void OnLocalize() {
        if (_node.nodeStageNum > 0) {
            Title = LocalizationManager.Localize(_node.nodeKey, _node.nodeStageNum);
        } else {
            Title = LocalizationManager.Localize(_node.nodeKey); // Normal without a number at the end
        }
        // Desc
        if (_currentUpgradeStage == null) {
            Description = LocalizationManager.Localize(_node.descriptionKey);
        }
    }

    private void RefreshRecipeData() {
        _currentUpgradeStage = _upgradeManager.GetUpgradeStage(_node);
        if (_currentUpgradeStage != null) {
            // Probably no stages. simply return
            bool isComplete = _upgradeManager.IsNodeCompleted(_node);
            StatChangeStatuses = _currentUpgradeStage.GetStatStatuses();
            if(StatChangeStatuses != null&& StatChangeStatuses.Count > 0) {
                if (isComplete) {
                    foreach (var stat in StatChangeStatuses) {
                        if (stat == null) continue;
                        stat.ValueNext = ""; // Just set next value to nonde
                    }
                }

            }
            if (_currentUpgradeStage.extraData != null) {
                // Take extra icon from it
                if (_currentUpgradeStage.extraData is UpgradeStageSubData s)
                    IconExtra = isComplete ? s.UpgradeIconComplete : s.UpgradeIcon;
            }
        }
        // Wow this is so much better almost like I know what I'm doing!!
        State = _upgradeManager.GetState(_node);
        LevelMax = _node.MaxLevel;
        LevelCurrent = _upgradeManager.GetCurrentLevel(_node);

       
        UpdateForPopup(); // We're ontop of the upgrade when upgrading it so we should refresh the popup
    }
    public void UpdateForUpgradePurchase() {
        RefreshRecipeData();
    }

    internal void UpdateForPopup() {
        if(_currentUpgradeStage == null) {
            return;
        }
        IngredientStatuses = _upgradeManager.GetIngredientStatuses(_node);
    }

    internal bool IsMaxLevel() {
        // As long as we call RefreshRecipeData before this these variables should be correct
        return LevelCurrent == LevelMax;
    }

   
}

public enum UpgradeNodeState { Purchased, Purchasable, Unlocked, Locked, LockedDemo}
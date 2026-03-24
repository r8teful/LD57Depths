using Assets.SimpleLocalization.Scripts;
using System;
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
    private Dictionary<UpgradeNodeState, StateVisualConfig> _normalConfigs;
    private Dictionary<UpgradeNodeState, StateVisualConfig> _coolConfigs;
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
    private Color _iconPurchasedColor;
    private Color _iconUnlockedColor;
    private Color _iconPurchasableColor;
    internal StateVisualConfig CurrentConfig => IsCool ? _coolConfigs.GetValueOrDefault(State) : _normalConfigs.GetValueOrDefault(State);
    private static readonly string ICON_PURCHASED_HEX = "#FFAA67";
    private static readonly string ICON_UNLOCKED_HEX = "#FFFFFF"; // slighly gray?
    private static readonly string ICON_PURCHASABLE_HEX = "#FFFFFF";

    public UpgradeNodeVisualData(UpgradeNodeSO node, UpgradeManagerPlayer upgradeManager) {
        // Need to get current node STAGE, and get the recipe from that state from here
        SetColours();
        BuildConfigs();
         _upgradeManager = upgradeManager;
        _node = node;
        Icon = node.icon;
        IsCool = node.IsCool;
        RefreshRecipeData();
        OnLocalize();
        LocalizationManager.OnLocalizationChanged += OnLocalize;
    }

    private void SetColours() {
        ColorUtility.TryParseHtmlString(ICON_PURCHASED_HEX, out _iconPurchasedColor);
        ColorUtility.TryParseHtmlString(ICON_UNLOCKED_HEX, out _iconUnlockedColor);
        ColorUtility.TryParseHtmlString(ICON_PURCHASABLE_HEX, out _iconPurchasableColor);
    }

    private void BuildConfigs() {
        _normalConfigs = new Dictionary<UpgradeNodeState, StateVisualConfig> {
            [UpgradeNodeState.Purchased] = new()    { alpha = 1f, iconColor = _iconPurchasedColor, interactable = true },
            [UpgradeNodeState.Unlocked] = new()     { alpha = 1f, iconColor = _iconUnlockedColor, interactable = true },
            [UpgradeNodeState.Purchasable] = new()  { alpha = 1f, iconColor = _iconPurchasableColor, interactable = true },
            [UpgradeNodeState.Locked] = new()       { alpha = 0f, iconColor = Color.white, interactable = false, clearSprite = true },
            [UpgradeNodeState.None] = new()       { alpha = 0f, iconColor = Color.white, interactable = false, clearSprite = true },
        };

        _coolConfigs = new Dictionary<UpgradeNodeState, StateVisualConfig> {
            [UpgradeNodeState.Purchased] = new()    { alpha = 1f, iconColor = _iconPurchasedColor, interactable = true, killCoolAnimation = true },
            [UpgradeNodeState.Unlocked] = new()     { alpha = 0.5f, iconColor = Color.white, interactable = true, useWhiteSprite = true, useCoolAnimation = true },
            [UpgradeNodeState.Purchasable] = new()  { alpha = 1f, iconColor = Color.white, interactable = true, useWhiteSprite = true, useCoolAnimation = true },
            [UpgradeNodeState.Locked] = new()       { alpha = 0f, iconColor = Color.white, interactable = false, clearSprite = true },
            [UpgradeNodeState.None] = new()       { alpha = 0f, iconColor = Color.white, interactable = false, clearSprite = true },
        };
    }
    internal void OnDestroy() {
        LocalizationManager.OnLocalizationChanged -= OnLocalize;
    }

    private void OnLocalize() {
        if (_node.nodeStageNum > 0) {
            LocalizationManager.TryLocalize(_node.nodeKey, out var title,_node.nodeStageNum);
            Title = title;
        } else {
            LocalizationManager.TryLocalize(_node.nodeKey, out var title); // Normal without a number at the end
            Title = title;
        }
        // Desc
        if (_currentUpgradeStage != null) {
            if (LocalizationManager.TryLocalize(_node.nodeKey + ".D", out var desc)) {
                Description = desc; 
            }
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

    [Serializable]
    public struct StateVisualConfig {
        public float alpha;
        public Color iconColor;
        public bool interactable;
        public bool clearSprite;       // True = set sprite to null
        public bool useWhiteSprite;    // True = override with _whiteSprite
        public bool useCoolAnimation;  // True = start/maintain gradient loop
        public bool killCoolAnimation; // True = stop animation and reset material
    }
}

public enum UpgradeNodeState { None, Purchased, Purchasable, Unlocked, Locked, LockedDemo}
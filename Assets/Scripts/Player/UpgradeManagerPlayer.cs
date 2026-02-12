using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// Has to hold upgrade info! . Also holds the entire upgrade tree state it feels like that should be its own script 
public class UpgradeManagerPlayer : MonoBehaviour, IPlayerModule {

    private PlayerManager _player;

    private Dictionary<ushort, UpgradeNode> _nodeStates = new Dictionary<ushort, UpgradeNode>();
    private UpgradeTreeDataSO _cachedTree;
    private readonly Dictionary<ValueKey, IValueModifiable> _valueModifierScipts = new Dictionary<ValueKey, IValueModifiable>();
    public static UpgradeManagerPlayer Instance { get; private set; }
    public event Action<UpgradeNodeSO> OnUpgradePurchased;
    private int _highestCostTierPurchased; // used for chest loot
    public UpgradeNode GetUpgradeNode(ushort id) {
        if(_nodeStates.TryGetValue(id, out var state)) {
            return state;
        }
        return null;
    }
    public UpgradeStage GetUpgradeStage(UpgradeNodeSO node) {
        if (IsNodeCompleted(node)) {
            return node.GetLastStage();
        }
        var lvl = GetCurrentLevel(node);
        return node.GetStage(lvl);
    }
    
    private void OnUpgradePurchase(UpgradeNodeSO recipe) {
        Debug.Log($"Successfully purchased upgrade: {recipe.name}");
        OnUpgradePurchased?.Invoke(recipe);
    }

    public int InitializationOrder => 10;

    public void InitializeOnOwner(PlayerManager playerParent) {
        Instance = this;
        _player = playerParent;
        InitUpgradeNodes();
    }
    private void InitUpgradeNodes() {
        // Basically will just just init all the nodes into its runtime data which will also be the recipes based on the tree we have
        _cachedTree = App.ResourceSystem.GetTreeByName(GameSetupManager.Instance.GetUpgradeTreeName());
        foreach(var node in _cachedTree.nodes) {
            // We're starting fresh so basically look at the first stage (if there is one)
            // and take that stages tier and upgrade pool, then 
            var cost = node.GetStageCost(0,_cachedTree);
            var tier = node.GetStageTier(0);
            _nodeStates.Add(node.ID, new(node.ID, cost,tier));
        }
    }
    
    // Call this lots when you're balancing
    public void UpdateAllNodeCosts() {
        foreach(var state in _nodeStates) {
            UpdateNodeCost(state.Key);
        }
    }
    private void UpdateNodeCost(UpgradeNodeSO node) {
        if(_nodeStates.TryGetValue(node.ID, out var data)){
            data.UpdateNodeCost(node, _cachedTree);
        } else {
            Debug.LogError($"{node.ID} was not found.");
        }
    }
    // Call this when that specific node is being upgraded
    private void UpdateNodeCost(ushort nodeID) {
        var node = App.ResourceSystem.GetUpgradeNodeByID(nodeID);
        if (node == null) {
            Debug.LogError("coudn't find node with ID!");
            return;
        }
        UpdateNodeCost(node);
    }
    public bool TryPurchaseUpgrade(UpgradeNodeSO node) {
        if (!CanAffordUpgrade(node)) {
            HandlePurchaseFail();
            return false;
        }
        var state = _nodeStates[node.ID];

        // This will succeed because CanAfford checks it
        SubmarineManager.Instance.SubInventory.RemoveItems(state.requiredItems);
        UpgradeStage stage = node.stages[state.CurrentLevel];
        foreach (var effect in stage.effects) {
            effect.Execute(new(_player)); 
        }
        state.CurrentLevel++;
        if(stage.costTier > _highestCostTierPurchased) {
            _highestCostTierPurchased = stage.costTier;
        }
        OnUpgradePurchased?.Invoke(node);
        UpdateNodeCost(node);
        return true;
    }

    private void HandlePurchaseFail() {
        if (PopupManager.Instance == null) return;
        if (PopupManager.Instance.CurrentPopup == null) return;
        PopupManager.Instance.CurrentPopup.HandleFailVisual();
    }
    /// <summary>
    /// Checks if the node is visible/purchasable based on prerequisites.
    /// </summary>
    public bool IsNodeUnlocked(UpgradeNodeSO node) {
        if (node.prerequisiteNodesAny == null || node.prerequisiteNodesAny.Count == 0)
            return true;
        foreach (var prereq in node.prerequisiteNodesAny) {
            int prereqLevel = GetCurrentLevel(prereq);
            if (node.UnlockedAtFirstPrereqStage && prereqLevel >= 1)
                return true;

            if (!node.UnlockedAtFirstPrereqStage && IsNodeCompleted(prereq))
                return true;
        }

        return false;
    }
    public bool IsStagePurchased(UpgradeNodeSO node, int stage) {
        if(stage > node.stages.Count) {
            Debug.LogWarning("Node doesn't have that many stages!");
            return false;
        }
        return GetCurrentLevel(node) >= stage;
    }

    internal UpgradeNodeState GetState(UpgradeNodeSO node) {
        int currentLevel = GetCurrentLevel(node);
        bool isMaxed = IsNodeCompleted(node);
        bool prereqsMet = IsNodeUnlocked(node);
        bool canAfford = CanAffordUpgrade(node);
        if (!prereqsMet && currentLevel == 0) {
            return UpgradeNodeState.Locked;
        } else if (isMaxed) {
            return UpgradeNodeState.Purchased;
        } else if (canAfford) {
            return UpgradeNodeState.Purchasable;
        } else { // prereqsMet and currentLevel is 0
            return UpgradeNodeState.Unlocked;
        }
    }
    public bool CanAffordUpgrade(UpgradeNodeSO node) {
        if (IsNodeCompleted(node)) return false;
        if (!IsNodeUnlocked(node)) return false;
        _nodeStates.TryGetValue(node.ID, out var state);
        if (SubmarineManager.Instance == null) return false;
        if (!state.CanAfford(SubmarineManager.Instance.SubInventory)) return false;
        return true;
    }
    public List<UpgradeNode> GetAvailableNodes() {
        var list = new List<UpgradeNode>();
        foreach (var node in _nodeStates) {
            var upgradeNode = App.ResourceSystem.GetUpgradeNodeByID(node.Key);
            if (upgradeNode == null) continue;
            if (IsNodeUnlocked(upgradeNode)) {
                list.Add(node.Value);
            }
        }
        return list;
    }
    // Basically non completed upgrade that are around the highest bought upgrade so far
    public List<UpgradeNode> GetUpgradesForChests() {
        // Used for chest loot
        const int maxRange = 4;
        const int amount = 4;
        int costStage = _highestCostTierPurchased;
        for (int range = 1; range <= maxRange; range++) {
            var list = new List<UpgradeNode>();
            foreach (var node in _nodeStates) {
                var upgradeNode = App.ResourceSystem.GetUpgradeNodeByID(node.Key);
                if (upgradeNode == null) continue;
                if (IsNodeCompleted(upgradeNode)) continue;
                var stage = GetUpgradeStage(upgradeNode);
                if (stage == null) continue;

                // if outside the current expanding range, skip
                if (stage.costTier > costStage + range) continue; // Too expensive
                if (stage.costTier < costStage - range) continue; // Too cheap

                list.Add(node.Value);
            }
            if (list.Count >= amount|| range == maxRange) {
                return list;
            }
            // increase range and try again
        }
        return new List<UpgradeNode>();
    }


    public int GetCurrentLevel(UpgradeNodeSO node) {
        return _nodeStates.TryGetValue(node.ID, out var state) ? state.CurrentLevel : 0;
    }

    public bool IsNodeCompleted(UpgradeNodeSO node) {
        if (!_nodeStates.TryGetValue(node.ID, out var state)) return false;
        return state.CurrentLevel >= node.MaxLevel;
    }


    public void RegisterValueModifierScript(ValueKey key, IValueModifiable modifiable) {
        if (_valueModifierScipts.ContainsKey(key)) {
            Debug.LogWarning($"SimpleValueManager: {modifiable} already registered, overwriting.");
        }
        _valueModifierScipts.Add(key,modifiable);
    }

    public T Get<T>(ValueKey key) where T : class, IValueModifiable {
        _valueModifierScipts.TryGetValue(key, out var value);
        return value as T;
    }

    internal void RemoveAllUpgrades() {
        _nodeStates.Clear();
        InitUpgradeNodes();
        _player.UiManager.UpgradeScreen.UpgradeTreeInstance.UpdateNodeVisualData();
        _player.PlayerAbilities.RemoveAllAbilityModifiers();
        _player.PlayerStats.RemoveAllModifiers();
        WorldTileManager.Instance.RemoveAllTileUpgrades();
    }

    internal List<IngredientStatus> GetIngredientStatuses(UpgradeNodeSO node) {
        return GetUpgradeNode(node.ID).
            GetIngredientStatuses(SubmarineManager.Instance.SubInventory);
    }
}
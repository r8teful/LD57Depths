using r8teful;
using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using UnityEngine;

// Has to hold upgrade info! . Also holds the entire upgrade tree state it feels like that should be its own script 
public class UpgradeManagerPlayer : MonoBehaviour, IPlayerModule, ISaveable {

    private PlayerManager _player;
    [ShowInInspector]
    private Dictionary<ushort, UpgradeNode> _nodeStates = new Dictionary<ushort, UpgradeNode>();
    [ShowInInspector]
    private Dictionary<ushort, int> _loadedNodeStates = new Dictionary<ushort, int>();
    private UpgradeTreeDataSO _cachedTree;
    private readonly Dictionary<ValueKey, IValueModifiable> _valueModifierScipts = new Dictionary<ValueKey, IValueModifiable>();
    public event Action<UpgradeNodeSO> OnUpgradePurchased;
    private int _highestCostTierPurchased; // used for chest loot
    private List<UpgradeNodeSO> _nodes = new List<UpgradeNodeSO>();
    public List<UpgradeNodeSO> Nodes => _nodes;

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

    public int InitializationOrder => 10; 

    public void InitializeOnOwner(PlayerManager playerParent) {
        // Init is called after OnLoad
        _player = playerParent;
        InitUpgradeNodes();
    }

    // Basically just init but we have to run it later because some effect require references (like abilities, stats, etc...)
    public void ExecuteStageEffects() {
        foreach (var node in _nodes) {
            int stage = 0;
            // Take the loaded state if we have any, otherwise just default to 0
            if (_loadedNodeStates != null && _loadedNodeStates.TryGetValue(node.ID, out var loadedStage)) {
                stage = loadedStage;
            }
            // Apply effects for all previously purchased stages
            for (int i = 0; i < stage; i++) {
                // Safety check in case the ScriptableObject was changed/shrunk after a save
                if (i < node.stages.Count) {
                    UpgradeStage purchasedStage = node.stages[i];
                    foreach (var effect in purchasedStage.effects) {
                        effect.Execute(new(_player));
                    }
                    // Restore highest cost tier purchased from the save data
                    if (purchasedStage.costTier > _highestCostTierPurchased) {
                        _highestCostTierPurchased = purchasedStage.costTier;
                    }
                } else {
                    Debug.LogWarning($"Save data says stage {stage} but node only has {node.stages.Count} stages!");
                }
            }
        }
    }
    private void InitUpgradeNodes() {
        _cachedTree = App.ResourceSystem.GetTreeByName(GameManager.Instance.GetUpgradeTreeName());
        var tempTree = Instantiate(_cachedTree.prefab);
        foreach (Transform child in tempTree.transform) {
            // So i don't have to put it in the inspector, just get all the nodes that are in the prefab
            if (!child.TryGetComponent<UIUpgradeNode>(out var node)) continue;
            var nodeData = App.ResourceSystem.GetUpgradeNodeByID(node.IDBoundNode);
            if (nodeData == null) continue;
            _nodes.Add(nodeData);
        }
        foreach (var node in _nodes) {
            int stage = 0;
            // Take the loaded state if we have any, otherwise just default to 0
            if (_loadedNodeStates != null && _loadedNodeStates.TryGetValue(node.ID, out var loadedStage)) {
                stage = loadedStage;
            }
            var cost = node.GetStageCost(stage, _cachedTree);
            var tier = node.GetStageTier(stage);
            if(tier == null) {
                Debug.Log("What is going on");
            }
            _nodeStates.TryAdd(node.ID, new(node.ID,stage, cost, tier));
        }
        Destroy(tempTree.gameObject);
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
        UpgradeStage stage = node.stages[state.CurrentStage];
        foreach (var effect in stage.effects) {
            effect.Execute(new(_player)); 
        }
        if(!node.InfinateStages)
            state.CurrentStage++; // its that easy lol 
        
        if(stage.costTier > _highestCostTierPurchased) {
            _highestCostTierPurchased = stage.costTier;
        }
        OnUpgradePurchased?.Invoke(node);
        UpdateNodeCost(node);
        return true;
    }

    public void PurchaseNodeDebug(UpgradeNodeSO node) {
        var state = _nodeStates[node.ID];
        UpgradeStage stage = node.stages[state.CurrentStage];
        foreach (var effect in stage.effects) {
            effect.Execute(new(_player));
        }
        OnUpgradePurchased?.Invoke(node);
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
        if (!_nodeStates.TryGetValue(node.ID, out var state)) return false;
        if (SubmarineManager.Instance == null || SubmarineManager.Instance.SubInventory == null) return false;
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
        return _nodeStates.TryGetValue(node.ID, out var state) ? state.CurrentStage : 0;
    }

    public bool IsNodeCompleted(UpgradeNodeSO node) {
        if (!_nodeStates.TryGetValue(node.ID, out var state)) return false;
        return state.CurrentStage >= node.MaxLevel;
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
        if(node == null) return null;
        return GetUpgradeNode(node.ID).
            GetIngredientStatuses(SubmarineManager.Instance.SubInventory);
    }

    public void OnSave(SaveData data) {
        Dictionary<ushort, int> nodeSaveData = new Dictionary<ushort, int>();
        foreach(var node in _nodeStates) {
            nodeSaveData.Add(node.Key, node.Value.CurrentStage);
        }
        data.bobData.nodeSaveData = nodeSaveData; // Will it be this easy?
    }
    // Called before init
    public void OnLoad(SaveData data) {
        if (data == null) return;
        if(data.bobData == null) return; 
        _loadedNodeStates = data.bobData.nodeSaveData;
    }

}
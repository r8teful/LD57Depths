using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class UIUpgradeTree : MonoBehaviour {
    [SerializeField] private Transform _resourceContainer; // For the first upgrade that is there

    private Dictionary<UpgradeRecipeBase, UIUpgradeNode> _nodeMap = new Dictionary<UpgradeRecipeBase, UIUpgradeNode>();

    internal void Init(UIUpgradeScreen uIUpgradeScreen, UpgradeTreeDataSO tree, HashSet<ushort> existingUpgrades) {
        _nodeMap.Clear();
        // Instead of instantiating the nodes, we "link" the existing node prefab to the data, so that it displays the right
        // upgrade. This is how we have to do it if we want more complex trees, instead of just instantiating the nodes
        // in a horizontal layout group. A bit more work to setup now but easier to code
        var uiLinkers = GetComponentsInChildren<UIUpgradeNode>();
        var dataToNodeLookup = new Dictionary<UpgradeRecipeBase, UIUpgradeNode>();
        foreach (var linker in uiLinkers) {
            if (linker != null && !dataToNodeLookup.ContainsKey(linker.ConnectedRecipeData)) {
                dataToNodeLookup.Add(linker.ConnectedRecipeData, linker);
            }
        }
        // Iterate through the ACTUAL upgrade data and initialize the corresponding UI nodes.
        // We use tree.nodes now, as it contains the original, unprepared assets.
        foreach (var nodeData in tree.nodes) {
            var originalUpgrade = nodeData.upgrade;
            if (originalUpgrade == null) continue;

            // Find the UI node in our prefab that has the matching GUID.
            if (dataToNodeLookup.TryGetValue(originalUpgrade, out UIUpgradeNode uiNode)) {
                // Get the PREPARED version of the upgrade, which has the calculated costs.
                UpgradeRecipeBase preparedUpgrade = tree.GetPreparedUpgrade(originalUpgrade);
                if (preparedUpgrade != null) {
                    // Initialize it!
                    uiNode.name = $"UI_Node_{preparedUpgrade.displayName}";
                    uiNode.Init(preparedUpgrade, this, false);
                    _nodeMap.Add(preparedUpgrade, uiNode);
                    
                    // Need to have this to make the popup work correctly
                    uIUpgradeScreen.GetUIManager().PopupManager.RegisterIPopupInfo(uiNode);
                }
            } else {
                Debug.LogWarning($"Found upgrade data '{originalUpgrade.name}' in SO but no matching UI node");
            }
        }
    }

    internal void SetNodeAvailable(UpgradeRecipeBase upgradeData) {
        if (_resourceContainer == null) return;
        foreach(Transform child in _resourceContainer) {
            Destroy(child.gameObject);
        }
        foreach (var item in upgradeData.requiredItems) {
            Instantiate(App.ResourceSystem.GetPrefab<UIUpgradeResourceDisplay>("UpgradeResourceDisplay"), _resourceContainer)
                .Init(item.item.icon, item.quantity);
        }
        _resourceContainer.gameObject.SetActive(true);
    }

    // Helper function to easily find a UI node later.
    public UIUpgradeNode GetNodeForUpgrade(UpgradeRecipeBase upgrade) {
        _nodeMap.TryGetValue(upgrade, out var uiNode);
        return uiNode;
    }
}
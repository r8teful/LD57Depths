using System.Collections.Generic;
using Unity.VisualScripting.Antlr3.Runtime.Tree;
using UnityEngine;
using UnityEngine.UI.Extensions;
using static UIUpgradeNode;

public class UIUpgradeTree : MonoBehaviour {

    // String is the GUID
    private Dictionary<ushort, UIUpgradeNode> _nodeMap = new Dictionary<ushort, UIUpgradeNode>();
    private Dictionary<ushort, List<UILineRenderer>> _lineMap = new Dictionary<ushort, List<UILineRenderer>>();
    private UIUpgradeScreen _uiParent;
    private UpgradeTreeDataSO _treeData;
    internal void Init(UIUpgradeScreen uIUpgradeScreen, UpgradeTreeDataSO tree, HashSet<ushort> existingUpgrades, InventoryManager inv) {
        _nodeMap.Clear();
        _uiParent = uIUpgradeScreen;
        _treeData = tree;
        // Instead of instantiating the nodes, we "link" the existing node prefab to the data, so that it displays the right
        // upgrade. This is how we have to do it if we want more complex trees, instead of just instantiating the nodes
        // in a horizontal layout group. A bit more work to setup now but easier to code
        var existingNodes = GetComponentsInChildren<UIUpgradeNode>(true);
        var dataToNodeLookup = new Dictionary<ushort, UIUpgradeNode>();

        foreach (var node in existingNodes) {

            if (node.IDBoundNode == ResourceSystem.InvalidID) {
                Debug.LogError($"A UIUpgradeNode in the prefab '{gameObject.name}' is missing its 'ConnectedNodeName'.", node);
                continue;
            }
            if (!dataToNodeLookup.ContainsKey(node.IDBoundNode)) {
                dataToNodeLookup.Add(node.IDBoundNode, node);
            }
        }
        // Iterate through the ACTUAL upgrade data and initialize the corresponding UI nodes.
        // We use tree.nodes now, as it contains the original, unprepared assets.
        foreach (var nodeData in tree.nodes) {
            // We just want to tell the existing node what prepared node it is connected to
            // Find the UI node in our prefab that has the matching GUID.
            if (!dataToNodeLookup.TryGetValue(nodeData.ID, out UIUpgradeNode uiNode)) {
                continue;
            }
            // Now have the node that is an EXISTING child
            // Calculate the node's current state from the player's progress.
            int currentLevel = nodeData.GetCurrentLevel(existingUpgrades);

            // Get the correct recipe data for display.
            UpgradeRecipeSO preparedRecipe = nodeData.GetUpgradeData(existingUpgrades, tree);
            bool canAfford = false;
            if (preparedRecipe != null) {
                canAfford = preparedRecipe.CanAfford(inv);
            }
            // Determine the node's visual status.
            UpgradeNodeState status = nodeData.GetState(existingUpgrades, canAfford);
            // Update the UI element with all the calculated information.
            uiNode.Init(this, inv, nodeData, currentLevel, preparedRecipe, status);
            _nodeMap.Add(nodeData.ID, uiNode);
        }
        UpdateConnectionLines(tree, existingUpgrades);
        RefreshNodes(existingUpgrades); // This makes lines update their state, which needs to be done because we just created them

        UIUpgradeScreen.OnSelectedNodeChanged += SelectedChange;
    }

 

    private void SelectedChange(UpgradeNodeSO node) {
        if(!_nodeMap.TryGetValue(node.ID, out var nodeUI)){
            Debug.LogError("Could not find nodeUI of " + node.ID);
            return;
        }
        nodeUI.SetSelected();
        // Tell previous selected node that it is no longer sellected
        //TODO
        
    }

    private void UpdateConnectionLines(UpgradeTreeDataSO treeData, IReadOnlyCollection<ushort> unlockedUpgrades) {
        foreach (var dataNode in treeData.nodes) {
            if (!_nodeMap.TryGetValue(dataNode.ID, out UIUpgradeNode childUiNode)) continue;

            foreach (var prereqNode in dataNode.prerequisiteNodesAny) {
                if (!_nodeMap.TryGetValue(prereqNode.ID, out UIUpgradeNode parentUiNode)) continue;
                var line = AddLine(prereqNode.ID, parentUiNode.transform,childUiNode.transform);
                // Now, set the line's color based on state.
                line.gameObject.AddComponent<UIUpgradeLine>().Init(parentUiNode,childUiNode, line);
                // line.color = parentIsMaxed ? Color.yellow : Color.gray;
            }
        }
    }
    private UILineRenderer AddLine(ushort sourceID, Transform from, Transform to) {
        
        var lineRenderer = Instantiate(App.ResourceSystem.GetPrefab<UILineRenderer>("UILine"),transform);
        lineRenderer.transform.SetAsFirstSibling();
        lineRenderer.name = $"Line {sourceID}";
        float offsetFromNodes = 1f;
        int linePointsCount = 2;

        RectTransform fromRT = from as RectTransform;
        RectTransform toRT = to as RectTransform;
        Vector2 fromPoint = fromRT.anchoredPosition +
                                (toRT.anchoredPosition - fromRT.anchoredPosition).normalized * offsetFromNodes;

        Vector2 toPoint = toRT.anchoredPosition +
                          (fromRT.anchoredPosition - toRT.anchoredPosition).normalized * offsetFromNodes;
        // drawing lines in local space:
        lineRenderer.transform.position = from.transform.position +
                                          (Vector3)(toRT.anchoredPosition - fromRT.anchoredPosition).normalized *
                                          offsetFromNodes;

        // line renderer with 2 points only does not handle transparency properly:
        List<Vector2> list = new List<Vector2>();
        for (int i = 0; i < linePointsCount; i++) {
            list.Add(Vector3.Lerp(Vector3.zero, toPoint - fromPoint +
                                                2 * (fromRT.anchoredPosition - toRT.anchoredPosition).normalized *
                                                offsetFromNodes, (float)i / (linePointsCount - 1)));
        }

        //Debug.Log("From: " + fromPoint + " to: " + toPoint + " last point: " + list[list.Count - 1]);

        lineRenderer.Points = list.ToArray();

        if (_lineMap.ContainsKey(sourceID)) {
            _lineMap[sourceID].Add(lineRenderer);
        } else {
            // new key
            _lineMap.Add(sourceID, new List<UILineRenderer>{ lineRenderer });
        }
            return lineRenderer;
    }
   
    // Helper function to easily find a UI node later.
    //public UIUpgradeNode GetNodeForUpgrade(UpgradeRecipeSO upgrade) {
    //    _nodeMap.TryGetValue(upgrade, out var uiNode);
    //    return uiNode;
    //}

    internal void OnUpgradeButtonClicked(UIUpgradeNode uIUpgradeNode, UpgradeNodeSO upgradeNode) {
        //_uiParent.OnUpgradeNodeClicked(upgradeData); // We where doing in it the upgradeScreen script but why not just do it here?
        //App.AudioController.PlaySound2D("ButtonClick");
        if (UpgradeManagerPlayer.LocalInstance.TryPurchaseUpgrade(upgradeNode)) {
            // Local code only

            // Unlocked upgrades list has the just newly purchased upgrade in it now, BUT, this wont work for non client host, because that would not have arrived yet
            var unlockedUpgrades = UpgradeManagerPlayer.LocalInstance.GetUnlockedUpgrades(); // Ugly but sometimes that is okay
            if (!upgradeNode.IsNodeMaxedOut(unlockedUpgrades)) {
                _nodeMap[upgradeNode.ID].DoPurchaseAnim(); // Purchase anim when not maxed, if we are maxed, setting to purchase state will play the animation
            }
            // Calculate the next upgrade cost for that node
            uIUpgradeNode.SetNewPreparedUpgrade(upgradeNode.GetUpgradeData(unlockedUpgrades, _treeData));
            RefreshNodes(unlockedUpgrades); // We could make it more performant by checking which nodes could have actually changed, but its not that performant heavy anyway.
        }
    }   
    public void RefreshNodes(IReadOnlyCollection<ushort> unlockedUpgrades) {
        foreach (var node in _treeData.nodes) {
            var uiNode = _nodeMap[node.ID];
            bool canAfford = false; // TODO
            UpgradeNodeState status = node.GetState(unlockedUpgrades, canAfford);
            if(status == UpgradeNodeState.Unlocked) {
                // Need to fetch new data if we're now active
                uiNode.SetNewPreparedUpgrade(node.GetUpgradeData(unlockedUpgrades, _treeData));

            }
            uiNode.UpdateVisual(status,node.GetCurrentLevel(unlockedUpgrades));
        }
    }
    public UpgradeRecipeSO GetUpgrade() {
        foreach(var node in _treeData.nodes) {
            //UpgradeRecipeSO preparedRecipe = node.GetNextUpgradeForNode();
            // todo 
        }
        return null;
    }
}
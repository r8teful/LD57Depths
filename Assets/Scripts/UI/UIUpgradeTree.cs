using System;
using System.Collections.Generic;
using System.Linq;
using TreeEditor;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI.Extensions;
using static UIUpgradeNode;

public class UIUpgradeTree : MonoBehaviour {

    // String is the GUID
    private Dictionary<ushort, UIUpgradeNode> _nodeMap = new Dictionary<ushort, UIUpgradeNode>();
    private Dictionary<ushort, List<UILineRenderer>> _lineMap = new Dictionary<ushort, List<UILineRenderer>>();
    private UIUpgradeScreen _uiParent;
    internal void Init(UIUpgradeScreen uIUpgradeScreen, UpgradeTreeDataSO tree, HashSet<ushort> existingUpgrades) {
        _nodeMap.Clear();
        _uiParent = uIUpgradeScreen;
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
            // 3. Calculate the node's current state from the player's progress.
            int currentLevel = nodeData.GetCurrentLevel(existingUpgrades);
            bool isMaxed = currentLevel >= nodeData.MaxLevel;
            bool prereqsMet = nodeData.ArePrerequisitesMet(existingUpgrades);

            // 4. Determine the node's visual status.
            UpgradeNodeState status;
            if (!prereqsMet && currentLevel == 0) {
                status = UpgradeNodeState.Inactive; 
            } else if (isMaxed) {
                status = UpgradeNodeState.Purchased;
            } else if (currentLevel > 0) {
                //status = UpgradeNodeState.InProgress;
                status = UpgradeNodeState.Active;
            } else { // prereqsMet and currentLevel is 0
                status = UpgradeNodeState.Active;
            }
            // 5. Get the correct recipe data for display.
            UpgradeRecipeSO baseRecipeForInfo = null;
            UpgradeRecipeSO preparedNextStage = null;

            // Determine which stage's info to show (the next one, or the last one if maxed). We don't need to loop through all stages here obviously
            int infoStageIndex = isMaxed ? nodeData.MaxLevel - 1 : currentLevel;
            if (infoStageIndex >= 0 && infoStageIndex < nodeData.stages.Count) {
                // Get the RAW recipe asset for displaying icon/description on ALL nodes.
                baseRecipeForInfo = nodeData.stages[infoStageIndex].upgrade;
            }
            if (!isMaxed && prereqsMet) {
                UpgradeStage nextStageToUnlock = nodeData.stages[currentLevel];
                preparedNextStage = tree.GetPreparedRecipeForStage(nextStageToUnlock);

                // In this case, the info recipe should also be from the next stage.
                baseRecipeForInfo = nextStageToUnlock.upgrade;
            }

            // 6. Update the UI element with all the calculated information.
            uiNode.Init(this, nodeData, currentLevel, baseRecipeForInfo, preparedNextStage, status);
            _nodeMap.Add(nodeData.ID, uiNode);
        }
        UpdateConnectionLines(tree, existingUpgrades);

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
                var line = AddLine(prereqNode.ID, childUiNode.transform, parentUiNode.transform);
                // Now, set the line's color based on state.
                //bool parentIsMaxed = treeData.IsNodeMaxedOut(prereqNode, unlockedUpgrades);
                
                //line.gameObject.AddComponent<UIUpgradeLine>().Init(dataToNodeLookup[p], uiNode, line);
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

    internal void OnUpgradeButtonClicked(UpgradeNodeSO upgradeData) {
        _uiParent.OnUpgradeNodeClicked(upgradeData);
    }
}
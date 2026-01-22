using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI.Extensions;

public class UIUpgradeTree : MonoBehaviour {

    // String is the GUID
    private Dictionary<ushort, UIUpgradeNode> _nodeMap = new Dictionary<ushort, UIUpgradeNode>();
    private Dictionary<ushort, List<UILineRenderer>> _lineMap = new Dictionary<ushort, List<UILineRenderer>>();
    private UpgradeTreeDataSO _treeData;
    private NetworkedPlayer _player;

    public List<UIUpgradeNode> GetAllCurrentNodes => _nodeMap.Values.ToList();
    internal void Init(UpgradeTreeDataSO tree, HashSet<ushort> existingUpgrades, NetworkedPlayer player) {
        _nodeMap.Clear();
        _treeData = tree;
        _player = player;
        var inv = player.GetInventory();
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
            uiNode.Init(this,_treeData,nodeData,inv,existingUpgrades);
            _nodeMap.Add(nodeData.ID, uiNode);
        }
        CreateConnectionLines(tree, existingUpgrades);
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

    private void CreateConnectionLines(UpgradeTreeDataSO treeData, IReadOnlyCollection<ushort> unlockedUpgrades) {
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

    internal void OnUpgradeButtonClicked(UIUpgradeNode uIUpgradeNode, UpgradeNodeSO upgradeNode) {
      if (UpgradeManagerPlayer.LocalInstance.TryPurchaseUpgrade(upgradeNode)) {
            var unlockedUpgrades = _player.UpgradeManager.GetUnlockedUpgrades();
            uIUpgradeNode.OnUpgraded(unlockedUpgrades);
        } 
    }

    internal void UpdateConnectedNodes(UpgradeNodeSO node) {
        // Instead of refreshing all nodes, we find nodes that have this node as prereqasite and now update their state
        foreach (var kvp in _nodeMap) {
            // its either all that crap or App.ResourceSystem.GetNodeByID()
            if(kvp.Value.GetVisualData.Node.prerequisiteNodesAny.Any(p => p.ID == node.ID)) {
                kvp.Value.UpdateVisualData(_player.UpgradeManager.GetUnlockedUpgrades());
            }
            
        }
    }
}
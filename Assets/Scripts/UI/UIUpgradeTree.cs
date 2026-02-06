using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI.Extensions;

public class UIUpgradeTree : MonoBehaviour {

    // String is the GUID
    private Dictionary<ushort, UIUpgradeNode> _nodeMap = new Dictionary<ushort, UIUpgradeNode>();
    private Dictionary<ushort, List<UILineRenderer>> _lineMap = new Dictionary<ushort, List<UILineRenderer>>();
    private UpgradeTreeDataSO _treeData;
    private PlayerManager _player;
    private Dictionary<ushort, List<ushort>> _adjacencyDict;
    private bool _closing;
    public bool IsClosing => _closing; // to fix a stupid bug where you get a popup because we squich the transform
    public static event Action OnUpgradeButtonPurchased; // this would break if we have several trees
    public Dictionary<ushort, UIUpgradeNode> GetNodeMap => _nodeMap;
    internal void Init(UpgradeTreeDataSO tree, HashSet<ushort> existingUpgrades, PlayerManager player) {
        _nodeMap.Clear();
        _treeData = tree;
        _player = player;
        var inv = SubmarineManager.Instance.SubInventory;
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
        _adjacencyDict = BuildUndirectedAdjacency(tree.nodes);
        UIUpgradeScreen.OnSelectedNodeChanged += SelectedChange;
        _player.UiManager.UpgradeScreen.OnPanelChanged += PanelChanged;

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
    public static Dictionary<ushort, List<ushort>> BuildUndirectedAdjacency(IEnumerable<UpgradeNodeSO> nodes) {
        var adj = new Dictionary<ushort, List<ushort>>();

        // first add all nodes (so adjacency contains an entry for each node id)
        foreach (var n in nodes) {
            if (!adj.ContainsKey(n.ID)) adj[n.ID] = new List<ushort>();
        }

        // add edges for prerequisites
        foreach (var n in nodes) {
            if (n.prerequisiteNodesAny == null) continue;
            foreach (var p in n.prerequisiteNodesAny) {
                // ensure both endpoints exist
                if (!adj.ContainsKey(p.ID)) adj[p.ID] = new List<ushort>();

                // add edge both ways if not present
                if (!adj[n.ID].Contains(p.ID)) adj[n.ID].Add(p.ID);
                if (!adj[p.ID].Contains(n.ID)) adj[p.ID].Add(n.ID);
            }
        }

        return adj;
    }
    internal void OnUpgradeButtonClicked(UIUpgradeNode uIUpgradeNode, UpgradeNodeSO upgradeNode) {
      if (UpgradeManagerPlayer.LocalInstance.TryPurchaseUpgrade(upgradeNode)) {
            // Purchased succefully!
            var unlockedUpgrades = _player.UpgradeManager.GetUnlockedUpgrades();
            uIUpgradeNode.OnUpgraded(unlockedUpgrades);
            OnUpgradeButtonPurchased.Invoke();
            StartSimpleRipple(upgradeNode.ID, _adjacencyDict, _nodeMap);
        } 
    }


    private void PanelChanged(bool isActive) {
        if (isActive) {
            UpdateNodeVisualData();
        }
    }

    public void UpdateNodeVisualData() {
        foreach (var kvp in _nodeMap) {
            kvp.Value.UpdateVisualData(_player.UpgradeManager.GetUnlockedUpgrades());
        }
        Debug.Log($"Update: {_nodeMap.Count} nodes");
    }

    internal IEnumerator OnPanSelect(UIUpgradeNode uIUpgradeNode) {
        yield return _player.UiManager.UpgradeScreen.PanAndZoom.FocusOnNode(uIUpgradeNode.Rect);
    }

    // Starts the simple serial ripple and returns the coroutine handle so caller can stop it.
    public Coroutine StartSimpleRipple(ushort startId,
                                      Dictionary<ushort, List<ushort>> adjacency,
                                      Dictionary<ushort, UIUpgradeNode> uiNodes,
                                      int maxDepth = int.MaxValue) {
        return StartCoroutine(NodeRipple(startId, adjacency, uiNodes, maxDepth));
    }
 
    private IEnumerator NodeRipple(ushort startId,
                                       Dictionary<ushort, List<ushort>> adjacency,
                                       Dictionary<ushort, UIUpgradeNode> uiNodes,
                                       int maxDepth) {
        if (adjacency == null) yield break;
        if (!adjacency.ContainsKey(startId)) {
            Debug.LogWarning($"Start id {startId} not found in adjacency map.");
            yield break;
        }

        // BFS to compute levels (distance) from startId
        var queue = new Queue<ushort>();
        var level = new Dictionary<ushort, int>(); // id -> distance
        queue.Enqueue(startId);
        level[startId] = 0;

        while (queue.Count > 0) {
            var id = queue.Dequeue();
            int currentLevel = level[id];
            if (currentLevel >= maxDepth) continue;

            if (!adjacency.TryGetValue(id, out var neighbors)) continue;
            foreach (var nb in neighbors) {
                if (level.ContainsKey(nb)) continue;
                level[nb] = currentLevel + 1;
                queue.Enqueue(nb);
            }
        }

        // Group by level so we can apply a consistent delay per level
        var levels = new SortedDictionary<int, List<ushort>>();
        foreach (var kv in level) {
            int lv = kv.Value;
            if (!levels.ContainsKey(lv)) levels[lv] = new List<ushort>();
            levels[lv].Add(kv.Key);
        }

        // For each level in order, trigger pulses. Within a level we optionally stagger each node a bit.
        foreach (var kv in levels) {
            int lv = kv.Key;
            var idsAtLevel = kv.Value;

            // Compute base delay for this level
            float baseDelay;
            if(lv == 0) {
                baseDelay = 0;
            } else {
                baseDelay = (lv-1) * 0.09f;
            }
            for (int i = 0; i < idsAtLevel.Count; i++) {
                ushort nodeId = idsAtLevel[i];
                StartCoroutine(InvokePulseAfterDelay(nodeId, baseDelay, uiNodes, lv));
            }
        }
    }
    private IEnumerator InvokePulseAfterDelay(ushort nodeId,
                                             float delay,
                                             Dictionary<ushort, UIUpgradeNode> uiNodes,
                                             int level) {
        if (delay > 0f) yield return new WaitForSeconds(delay);

        if (uiNodes != null && uiNodes.TryGetValue(nodeId, out var uiNode) && uiNode != null) {
            uiNode.DoPulseAnim(level);
        } 
    }

    internal void OnTreeCloseFinish() {
        _closing = false;
    }

    internal void OnTreeClose() {
        _closing = true;
    }
}
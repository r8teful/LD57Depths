using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI.Extensions;

public class UIUpgradeTree : MonoBehaviour {
    [SerializeField] private Transform _resourceContainer; // For the first upgrade that is there

    private Dictionary<UpgradeRecipeSO, UIUpgradeNode> _nodeMap = new Dictionary<UpgradeRecipeSO, UIUpgradeNode>();
    private Dictionary<UpgradeRecipeSO, List<UILineRenderer>> _lineMap = new Dictionary<UpgradeRecipeSO, List<UILineRenderer>>();
    private UIUpgradeScreen _uiParent;
    internal void Init(UIUpgradeScreen uIUpgradeScreen, UpgradeTreeDataSO tree, HashSet<ushort> existingUpgrades) {
        _nodeMap.Clear();
        _uiParent = uIUpgradeScreen;
        // Instead of instantiating the nodes, we "link" the existing node prefab to the data, so that it displays the right
        // upgrade. This is how we have to do it if we want more complex trees, instead of just instantiating the nodes
        // in a horizontal layout group. A bit more work to setup now but easier to code
        var uiLinkers = GetComponentsInChildren<UIUpgradeNode>();
        var dataToNodeLookup = new Dictionary<UpgradeRecipeSO, UIUpgradeNode>();
        foreach (var linker in uiLinkers) {
            if(linker.ConnectedRecipeData == null) {
                Debug.LogError("Tree nodes in prefab not setup properly!");
            }
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
                UpgradeRecipeSO preparedUpgrade = tree.GetPreparedUpgrade(originalUpgrade);
                if (preparedUpgrade != null) {
                    // Make line connections for the node
                    List<UILineRenderer> nodeLines = new();
                    foreach (var p in nodeData.prerequisiteAny) {
                        // Here the last upgrade will have 3 connections, meaning if we set its color to "availabe"
                        var line = AddLine(preparedUpgrade, uiNode.transform, dataToNodeLookup[p].transform);
                        nodeLines.Add(line);
                        //line.gameObject.AddComponent<UIUpgradeLine>().Init(uiNode, dataToNodeLookup[p], line);
                        line.gameObject.AddComponent<UIUpgradeLine>().Init(dataToNodeLookup[p],uiNode, line);
                    }
                    // Initialize it!
                    uiNode.name = $"UI_Node_{preparedUpgrade.displayName}";
                    uiNode.Init(preparedUpgrade, this, nodeLines);
                    _nodeMap.Add(preparedUpgrade, uiNode);


                }
            } else {
                Debug.LogWarning($"Found upgrade data '{originalUpgrade.name}' in SO but no matching UI node");
            }
        }
    }
    private UILineRenderer AddLine(UpgradeRecipeSO source, Transform from, Transform to) {
        
        var lineRenderer = Instantiate(App.ResourceSystem.GetPrefab<UILineRenderer>("UILine"),transform);
        lineRenderer.transform.SetAsFirstSibling();
        lineRenderer.name = $"Line {source}";
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

        if (_lineMap.ContainsKey(source)) {
            _lineMap[source].Add(lineRenderer);
        } else {
            // new key
            _lineMap.Add(source, new List<UILineRenderer>{ lineRenderer });
        }
            return lineRenderer;
    }

    internal void SetNodeAvailable(UpgradeRecipeSO upgradeData) {
        // Set line to right color
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
    public void SetLineColor(UpgradeRecipeSO upgrade, Color c) {
        // upgrade = LazerDamage1. It says prerequesate of LazerDamage0 is met. What we want it to do is set the 
        if(upgrade.GetPrerequisites().Count == 1) {
          
        }
        foreach(var met in UpgradeManagerPlayer.LocalInstance.GetAllPrerequisitesMet(upgrade)) {
            if (_lineMap.TryGetValue(met, out var lines)) {
                foreach (var line in lines) {
                    line.color = c;
                }
            }
            if (_lineMap.TryGetValue(upgrade, out var lines2)) {
                foreach (var line in lines2) {
                    line.color = c;
                }
            }
        }    
    }

    // Helper function to easily find a UI node later.
    public UIUpgradeNode GetNodeForUpgrade(UpgradeRecipeSO upgrade) {
        _nodeMap.TryGetValue(upgrade, out var uiNode);
        return uiNode;
    }

    internal void OnUpgradeButtonClicked(UpgradeRecipeSO upgradeData) {
        _uiParent.OnUpgradeNodeClicked(upgradeData);
    }
}
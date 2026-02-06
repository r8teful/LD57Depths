using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

public class UpgradeTreeController : MonoBehaviour {
    [Header("Settings")]
    [SerializeField] private float inputDelay = 0.25f; // Prevent cursor flying too fast
    [SerializeField] private float directionalThreshold = 60f; // The angle "cone" to search in

    [Header("Debug")]
    [SerializeField] private UIUpgradeNode currentSelection;

    private List<UIUpgradeNode> _allNodes;
    private InputManager _inputManager;
    private float _lastInputTime;
    private UIUpgradeNode startingNode;
    public void Init(PlayerManager client, UIUpgradeTree upgradeTreeInstance) {
        var nodes = upgradeTreeInstance.GetNodeMap;
        _allNodes = nodes.Values.ToList();
        nodes.TryGetValue(0, out var baseNode);
        _inputManager = client.InputManager;
        _inputManager.OnUIInteraction += HandleActionInput;
        startingNode = baseNode;

        client.UiManager.UpgradeScreen.OnPanelChanged += OnPanelChange;
        //if (_inputManager.IsUsingController) {
        //    if (startingNode != null) {
        //        SetSelection(startingNode);
        //    }
        //}
    }
    private void OnDestroy() {
        if(PlayerManager.Instance != null) {
            PlayerManager.Instance.UiManager.UpgradeScreen.OnPanelChanged -= OnPanelChange;
        }
    }

    private void OnPanelChange(bool isActive) {
        if (!isActive) {
            // Eh its done directly now
        }
    }

    internal void OnTreeOpen() {
        SetSelection(startingNode);
    }

    internal void OnTreeClose() {
        SetSelection(null);
    }
    
    private void Update() {
        HandleDirectionalInput();
    }

    private void HandleActionInput(InputAction.CallbackContext context) {
        if (currentSelection != null) {
            currentSelection.OnPurchaseInput();
        }
    }

    private void HandleDirectionalInput() {
        // Simple timer to prevent scrolling too fast
        if (Time.time < _lastInputTime + inputDelay) return;

        Vector2 inputDir = _inputManager.GetUINavigationInput();
        //Debug.Log(inputDir);
        if(inputDir.magnitude < 0.01f) {
            // Only act on "big" inputs
            return;
        }

        // Perform the "Raycast" search
        UIUpgradeNode bestCandidate = FindNextNode(inputDir);

        if (bestCandidate != null && bestCandidate != currentSelection) {
            StartCoroutine(SetSelectionRoutine(bestCandidate));
            _lastInputTime = Time.time;
        }
    }

    private void SetSelection(UIUpgradeNode newNode) {
        // Deselect previous
        if (currentSelection != null) {
            currentSelection.Deselect();
        }

        // Select new
        currentSelection = newNode;
        if (currentSelection == null) return;
        currentSelection.Select(usingPointer: false);
    }
    private IEnumerator SetSelectionRoutine(UIUpgradeNode newNode) {
        // Deselect previous
        if (currentSelection != null) {
            currentSelection.Deselect();
        }
        yield return null;
        // Select new
        currentSelection = newNode;
        currentSelection.Select(usingPointer: false);
    }

    /// <summary>
    /// This simulates a Raycast. It looks for the closest node 
    /// that aligns with the input direction.
    /// </summary>
    private UIUpgradeNode FindNextNode(Vector2 direction) {
        if (currentSelection == null) {
            // Select starting node, ideally, select a node within our current zoom
            return startingNode;
        }

        RectTransform startTransform = currentSelection.GetComponent<RectTransform>();
        Vector2 startPos = startTransform.position;
        UIUpgradeNode bestNode = null;
        float bestScore = float.MaxValue; // We are looking for the Lowest score
        foreach (var node in _allNodes) {
            // Skip locked nodes
            if (node.GetState == UpgradeNodeState.Locked) continue;
            // Skip self
            if (node == currentSelection) continue;
            if (!node.gameObject.activeInHierarchy) continue;
            Vector2 targetPos = node.GetComponent<RectTransform>().position;
            Vector2 dirToTarget = targetPos - startPos;

            float distance = dirToTarget.magnitude;
            float angle = Vector2.Angle(direction, dirToTarget);
            if (angle > 60) continue;

            float score = distance + (angle * 4);
            if (score < bestScore) {
                bestScore = score;
                bestNode = node;
            }
        }

        return bestNode;
    }

}
using System;
using System.Collections;
using System.Collections.Generic;
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


    public void Init(NetworkedPlayer client, UIUpgradeTree upgradeTreeInstance) {
        _allNodes = upgradeTreeInstance.GetAllCurrentNodes;
        _inputManager = client.InputManager;
        _inputManager.OnUIInteraction += HandleActionInput;
        UIUpgradeNode startingNode = null; // TODO 
        if (_inputManager.IsUsingController) {
            if (startingNode != null) {
                SetSelection(startingNode);
            }
        }
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

        Vector2 inputDir = _inputManager.GetAimScreenInput();
        if(inputDir.magnitude < 0.01f) {
            // Only act on "big" inputs
            return;
        }

        // Perform the "Raycast" search
        UIUpgradeNode bestCandidate = FindNextNode(inputDir);

        if (bestCandidate != null && bestCandidate != currentSelection) {
            SetSelection(bestCandidate);
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
        currentSelection.Select();
    }

    /// <summary>
    /// This simulates a Raycast. It looks for the closest node 
    /// that aligns with the input direction.
    /// </summary>
    private UIUpgradeNode FindNextNode(Vector2 direction) {
        if (currentSelection == null) return null;

        RectTransform startTransform = currentSelection.GetComponent<RectTransform>();
        Vector2 startPos = startTransform.position;

        UIUpgradeNode bestNode = null;
        float closestDistance = float.MaxValue;

        foreach (var node in _allNodes) {
            // Skip self
            if (node == currentSelection) continue;
            // Skip disabled nodes (if you hide parts of the tree)
            if (!node.gameObject.activeInHierarchy) continue;

            RectTransform targetTransform = node.GetComponent<RectTransform>();
            Vector2 targetPos = targetTransform.position;
            Vector2 dirToTarget = targetPos - startPos;

            // 1. Check Distance (Optimization: Check sqrMagnitude first if many nodes)
            float dist = dirToTarget.magnitude;

            // 2. Check Direction (Angle)
            // Get angle between Input Direction and Direction to this Node
            float angle = Vector2.Angle(direction, dirToTarget);

            // Logic: 
            // - Must be within the angle threshold (e.g., within 45 degrees of Stick direction)
            // - Must be the closest one found so far
            if (angle <= directionalThreshold && dist < closestDistance) {
                closestDistance = dist;
                bestNode = node;
            }
        }

        return bestNode;
    }
}
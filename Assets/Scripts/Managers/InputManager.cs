using FishNet.Object;
using UnityEngine.InputSystem;
using UnityEngine;
using System;
using UnityEditor.ShaderGraph;

// UI input handling is in inventoryUIManager
// This script sits on the player client
public class InputManager : NetworkBehaviour {
    private PlayerInput _playerInput;
    private InputAction _interact;
    private ShootMode _currentShootMode = ShootMode.Mining;
    [SerializeField] private LayerMask _interactableLayerMask;
    [SerializeField] private ToolController _toolController;
    private Vector2 movementInput;   // For character movement
    private Vector2 rawAimInput;     // Raw input for aiming (mouse position or joystick)
    private IInteractable currentInteractable;
    private IInteractable previousInteractable;

    public override void OnStartClient() {
        base.OnStartClient();
        if (!base.IsOwner) {
            enabled = false;
            return;
        }
        _playerInput = GetComponent<PlayerInput>();
        _interact = _playerInput.actions["Interact"];
    }

    private void Update() {
        // Detect nearby interactables using OverlapCircleAll
        Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, 1.5f, _interactableLayerMask);

        if (colliders.Length > 0) {
            // Find the closest interactable
            Collider2D closest = null;
            float minDistance = float.MaxValue;

            foreach (var collider in colliders) {
                float distance = Vector2.Distance(transform.position, collider.transform.position);
                if (distance < minDistance) {
                    minDistance = distance;
                    closest = collider;
                }
            }
            
            currentInteractable = closest.GetComponent<IInteractable>();
            if (currentInteractable != previousInteractable) {
                previousInteractable?.SetInteractable(false);
                previousInteractable = currentInteractable;

                string key = _interact.GetBindingDisplayString(options: InputBinding.DisplayStringOptions.DontOmitDevice);
                currentInteractable.SetInteractable(true,App.ResourceSystem.GetSprite(FormatBindingDisplayString(key)));
            }
        } else {
            currentInteractable?.SetInteractable(false);
            currentInteractable = null;
            previousInteractable = null;
            //promptText.gameObject.SetActive(false);
        }

        // Handle interaction input
        if (currentInteractable != null && _interact.WasPerformedThisFrame()) {
            currentInteractable.Interact(NetworkObject);
        }
    }

    // Called by Input System for movement input
    public void OnMove(InputAction.CallbackContext context) {
        if (!base.IsOwner)
            return; // Only process inputs for the owning client
        movementInput = context.ReadValue<Vector2>();
    }

    // Called by Input System for aim input
    public void OnAim(InputAction.CallbackContext context) {
        if (!base.IsOwner)
            return;
        rawAimInput = context.ReadValue<Vector2>();
    }

    public void OnSwitchTool(InputAction.CallbackContext context) {
        if (!base.IsOwner)
            return;
        if (context.performed) {
            // Just switch between for now
            if (_toolController != null)
                _toolController.StopCurrentTool();
            if(_currentShootMode == ShootMode.Mining) {
                _currentShootMode = ShootMode.Cleaning;
            } else {
                _currentShootMode = ShootMode.Mining;
            }
        }
    }
    // Get movement input (e.g., WASD, joystick)
    public Vector2 GetMovementInput() {
        return movementInput;
    }

    // Get aim input, processed based on control scheme
    public Vector2 GetAimInput() {
        if (_playerInput.currentControlScheme == "Keyboard&Mouse") {
            // Convert mouse screen position to world position
            return Camera.main.ScreenToWorldPoint(rawAimInput);
        } else // Controller
          {
            // Use joystick direction, normalized
            return rawAimInput.normalized;
        }
    }
    // Could either be mining or cleaning!
    public void OnShoot(InputAction.CallbackContext context) {
        if (context.performed) {
            if (_currentShootMode == ShootMode.Mining) {
                _toolController.PerformMining(this);
            } else {
                _toolController.PerformCleaning(this);
            }
        } else if (context.canceled) {
            if (_currentShootMode == ShootMode.Mining) {
                _toolController.StopMining();
            } else {
                _toolController.StopCleaning();
            }
        }
    }
    public void OnInteract(InputAction.CallbackContext context) {

    }
    public static string FormatBindingDisplayString(string input) {
        if (string.IsNullOrWhiteSpace(input))
            return input;

        int openBracket = input.IndexOf('[');
        int closeBracket = input.IndexOf(']');

        if (openBracket != -1 && closeBracket != -1 && closeBracket > openBracket) {
            string beforeBracket = input.Substring(0, openBracket).Trim(); // e.g., "E"
            string inBracket = input.Substring(openBracket + 1, closeBracket - openBracket - 1).Trim(); // e.g., "Keyboard"
            return inBracket + beforeBracket; // e.g., "KeyboardE"
        }

        // If format is not as expected, return input as fallback
        return input;
    }
}

public enum ShootMode {
    Mining,
    Cleaning
}
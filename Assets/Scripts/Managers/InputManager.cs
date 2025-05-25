using FishNet.Object;
using UnityEngine.InputSystem;
using UnityEngine;
using System;

// UI input handling is in inventoryUIManager
public class InputManager : NetworkBehaviour {
    private PlayerInput _playerInput;
    private Vector2 movementInput;   // For character movement
    private Vector2 rawAimInput;     // Raw input for aiming (mouse position or joystick)
    
    private void Awake() {
        _playerInput = GetComponent<PlayerInput>();
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
}
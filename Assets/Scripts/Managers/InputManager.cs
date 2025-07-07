using FishNet.Object;
using UnityEngine.InputSystem;
using UnityEngine;
using UnityEngine.EventSystems;
using Sirenix.OdinInspector;
using UnityEngine.Windows;
using System.Linq;
using Unity.VisualScripting;
public enum PlayerInteractionContext {
    None,                 // Default state, no specific interaction available
    InteractingWithUI,    // Highest priority: Mouse is over any UI element
    DraggingItem,         // High priority: Player is dragging an item from inventory
    WorldInteractable,    // Player is near an object they can interact with (e.g., "Press E to open")
    HotebarItemSelected,      // Lowest priority: The default game world interaction (mining, placing)
    UsingToolOnWorld      // Lowest priority: The default game world interaction (mining, placing)
}
// UI input handling is in inventoryUIManager
// This script sits on the player client
public class InputManager : NetworkBehaviour {
    private PlayerInput _playerInput;
    private InputAction _interactAction;
    private InputAction _playerClickAction;
    private InputAction _useItemAction;
    private InputAction _hotbarSelection;
    private UIManagerInventory _inventoryUIManager;
    private InputDevice lastUsedDevice;
    // UI
    private InputAction _UItoggleInventoryAction; // Assign your toggle input action asset
    private InputAction _uiInteractAction;   // e.g., Left Mouse / Gamepad A
    private InputAction _uiAltInteractAction; // e.g., Right Mouse / Gamepad X
    private InputAction _uiDropOneAction;     // e.g., Right Mouse (when holding) / Gamepad B
    private InputAction _uiNavigateAction;    // D-Pad / Arrow Keys
    private InputAction _uiPointAction;       // Mouse position for cursor icon
    private InputAction _uiCancelAction;       // Escape / Gamepad Start (to cancel holding)
    private InputAction _uiTabLeft;
    private InputAction _uiTabRight;
    
    
    private ShootMode _currentShootMode = ShootMode.Mining;
    [SerializeField] private LayerMask _interactableLayerMask;
    [SerializeField] private ToolController _toolController;
    [SerializeField] private float _interactionRadius;
    private Vector2 movementInput;   // For character movement
    private Vector2 rawAimInput;     // Raw input for aiming (mouse position or joystick)
    private IInteractable _currentInteractable;
    private IInteractable _previousInteractable;
    [ShowInInspector]
    private PlayerInteractionContext _currentContext;

    internal void SetUIManager(UIManagerInventory uiManager) {
        _inventoryUIManager  = uiManager;
    }
    public override void OnStartClient() {
        base.OnStartClient();
        if (!base.IsOwner) {
            enabled = false;
            return;
        }
        _playerInput = GetComponent<PlayerInput>();
        if (_playerInput != null) {
            _interactAction = _playerInput.actions["Interact"]; // E
            _playerClickAction = _playerInput.actions["Shoot"];
            _UItoggleInventoryAction = _playerInput.actions["UI_Toggle"]; // I
            _uiInteractAction = _playerInput.actions["UI_Interact"]; // LMB
            _uiAltInteractAction = _playerInput.actions["UI_AltInteract"]; // RMB
            _uiDropOneAction = _playerInput.actions["UI_DropOne"];
            _uiNavigateAction = _playerInput.actions["UI_Navigate"];
            _uiPointAction = _playerInput.actions["UI_Point"];
            _uiCancelAction = _playerInput.actions["UI_Cancel"]; 
            _uiTabLeft = _playerInput.actions["UI_TabLeft"]; // Opening containers
            _uiTabRight = _playerInput.actions["UI_TabRight"]; // Opening containers
            _hotbarSelection = _playerInput.actions["HotbarSelect"];
            _useItemAction = _playerInput.actions["Shoot"];
        } else {
            Debug.LogWarning("PlayerInput component not found on player. Mouse-only or manual input bindings needed.", gameObject);
        }
        SubscribeToEvents();
    }
    void OnDisable() { UnsubscribeFromEvents(); }
    private void SubscribeToEvents() {
        if (_UItoggleInventoryAction != null)
            _UItoggleInventoryAction.performed += UIOnToggleInventory;
        if (_uiInteractAction != null)
            _uiInteractAction.performed += UIOnPrimaryInteractionPerformed;
        if (_uiAltInteractAction != null)
            _uiAltInteractAction.performed += UIOnSecondaryInteractionPerformed;
        if (_uiCancelAction != null)
            _uiCancelAction.performed += UIHandleCloseAction;
        if (_uiTabLeft != null)
            _uiTabLeft.performed += l => UIScrollTabs(-1); // Not unsubscribing but what is the worst that could happen?
        if (_uiTabRight != null)
            _uiTabRight.performed += l => UIScrollTabs(1);
        _playerClickAction.performed += OnPrimaryInteractionPerformed;
        _playerClickAction.canceled += OnPrimaryInteractionPerformed;
        _useItemAction.performed += OnUseHotbarInput;
        _hotbarSelection.performed += OnHotbarSelection;
    }

  
    private void UnsubscribeFromEvents() {
        if (_UItoggleInventoryAction != null)
            _UItoggleInventoryAction.performed -= UIOnToggleInventory;
        if (_uiInteractAction != null)
            _uiInteractAction.performed -= UIOnPrimaryInteractionPerformed;
        if (_uiAltInteractAction != null)
            _uiAltInteractAction.performed -= UIOnSecondaryInteractionPerformed;
        if (_uiCancelAction != null)
            _uiCancelAction.performed -= UIHandleCloseAction;
        _playerClickAction.performed -= OnPrimaryInteractionPerformed;
        _playerClickAction.canceled -= OnPrimaryInteractionPerformed;
        _useItemAction.performed -= OnUseHotbarInput;
        _hotbarSelection.performed -= OnHotbarSelection;
    }
    private void Update() {
        if (_inventoryUIManager == null) return;
        UpdateInteractionContext();
        UpdateCursor();
        //UpdatePlayerFeedback(); // Optional but recommended: change cursor, etc.
        // Handle interaction input
        if (_currentInteractable != null && _interactAction.WasPerformedThisFrame()) {
            _currentInteractable.Interact(NetworkObject);
        }
    }

    private void UpdateCursor() {
        if(_currentContext == PlayerInteractionContext.InteractingWithUI 
            || _currentContext == PlayerInteractionContext.DraggingItem) {
            Cursor.SetCursor(Resources.Load<Texture2D>("cursorMenu"), new Vector2(3, 3), CursorMode.Auto);
        } else {
            Cursor.SetCursor(Resources.Load<Texture2D>("cursorCrossHair"), new Vector2(10.5f, 10.5f), CursorMode.Auto);
            // Crosshair
        }
    }

    private void UpdateInteractionContext() {
        // 1. HIGHEST PRIORITY: Check for UI interaction
        if (EventSystem.current.IsPointerOverGameObject() || _inventoryUIManager.IsOpen) {
            _currentContext = PlayerInteractionContext.InteractingWithUI;
            // TODO this should sometimes clear the interactable, but sometimes not. As the UI could be the interactable!
            
            //ClearInteractable(); // Can't interact with world objects if UI is in the way
            return;
        }

        // 2. SECOND PRIORITY: Check if we are dragging an item
        if (_inventoryUIManager.IsDraggingItem) {
            _currentContext = PlayerInteractionContext.DraggingItem;
            ClearInteractable();
            return;
        }

        // 3. THIRD PRIORITY: Check for nearby world interactables (your existing logic)
        CheckForNearbyInteractables(); // This method now just *finds* the interactable, doesn't handle input
        if (_currentInteractable != null) {
            _currentContext = PlayerInteractionContext.WorldInteractable;
            return;
        }
        if (_inventoryUIManager.ItemSelectionManager.CanUseSelectedSlotItem()) {
            _currentContext = PlayerInteractionContext.HotebarItemSelected;
            return;
        }

        // 4. LOWEST PRIORITY: Default to using a tool on the world
        _currentContext = PlayerInteractionContext.UsingToolOnWorld;
    }
    private void CheckForNearbyInteractables() {
        Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, _interactionRadius, _interactableLayerMask);
        IInteractable closestInteractable = FindClosestInteractable(colliders);

        if (closestInteractable != _currentInteractable) {
            _previousInteractable?.SetInteractable(false); // Hide prompt on old one
            Debug.Log("Found new interactable!: " + closestInteractable);
            _currentInteractable = closestInteractable;
            _previousInteractable = _currentInteractable;

            if (_currentInteractable != null) {
                // Show prompt on new one
                // You can get the binding string here as you did before
                string key = _interactAction.GetBindingDisplayString(InputBinding.DisplayStringOptions.DontOmitDevice);
                _currentInteractable.SetInteractable(true, App.ResourceSystem.GetSprite(FormatBindingDisplayString(key)));
            }
        }
    }

    private IInteractable FindClosestInteractable(Collider2D[] colliders) {
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
            if(closest.TryGetComponent<IInteractable>(out var i)) {
                if (i.CanInteract) {
                    return closest.GetComponent<IInteractable>();
                }
            }
        }
        return null;
    }
    private void ClearInteractable() {
        if (_currentInteractable != null) {
            _currentInteractable.SetInteractable(false);
            _currentInteractable = null;
            _previousInteractable = null;
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
        // Dissable movement if we are in a menu
        return _currentContext != PlayerInteractionContext.DraggingItem && _currentContext != PlayerInteractionContext.InteractingWithUI 
            ? movementInput : Vector2.zero;
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

    public void OnInteract(InputAction.CallbackContext context) {

    }
    private void OnHotbarSelection(InputAction.CallbackContext context) {
        // Just pass logic to the SelectionManager for now
        _inventoryUIManager.ItemSelectionManager.HandleHotbarSelection(context);
    }
    private void OnUseHotbarInput(InputAction.CallbackContext context) {
        if(_currentContext == PlayerInteractionContext.HotebarItemSelected) {
            _inventoryUIManager.ItemSelectionManager.HandleUseInput(context);
        }
    }
    public void OnPrimaryInteractionPerformed(InputAction.CallbackContext context) {
        lastUsedDevice = context.control.device;
        // Todo some kind of checks to see if this is allowed..
        // This is Left Mouse Click / Gamepad A
        // Switch on the context to decide what Left Click does
        switch (_currentContext) {
            case PlayerInteractionContext.UsingToolOnWorld:
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
                break;
            case PlayerInteractionContext.WorldInteractable:
            case PlayerInteractionContext.None:
            default:
                // Do nothing
                break;
        }
    }
    public void UIOnPrimaryInteractionPerformed(InputAction.CallbackContext context) {
        if (_currentContext == PlayerInteractionContext.DraggingItem) {
            if (context.performed)
                _inventoryUIManager.ProcessInteraction(PointerEventData.InputButton.Left, _uiPointAction, _playerInput.currentControlScheme == "Gamepad");

        } else if (_currentContext == PlayerInteractionContext.InteractingWithUI) {
            if (context.performed)
                _inventoryUIManager.ProcessInteraction(PointerEventData.InputButton.Left, _uiPointAction, _playerInput.currentControlScheme == "Gamepad");
        }
    }
    public void UIOnSecondaryInteractionPerformed(InputAction.CallbackContext context) {
        // This is Right Mouse Click / Gamepad X
        _inventoryUIManager.ProcessInteraction(PointerEventData.InputButton.Right, _uiPointAction, _playerInput.currentControlScheme == "Gamepad");
    }
    #region INVENTORY

    // direction should be either -1 or 1 idealy
    public void UIScrollTabs(int direction) {
        _inventoryUIManager.HandleScrollTabs(direction);
    }
    private void UIOnToggleInventory(InputAction.CallbackContext context) {
        if (Console.IsConsoleOpen())
            return;
        _inventoryUIManager.HandleToggleInventory(context);
    }

    private void UIHandleCloseAction(InputAction.CallbackContext context) {
        // E.g., Escape key or Gamepad B/Start (if configured to cancel)
        _inventoryUIManager.HandleCloseAction(context);
     
    }
    #endregion
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
    void OnDrawGizmosSelected() {
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(new(transform.position.x,transform.position.y+_interactionRadius,0), _interactionRadius);
    }
}


public enum ShootMode {
    Mining,
    Cleaning
}
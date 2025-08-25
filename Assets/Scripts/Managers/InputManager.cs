using FishNet.Object;
using Sirenix.OdinInspector;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
public enum PlayerInteractionContext {
    None,                 // Default state, no specific interaction available
    Building,             // Player is trying to build an entity
    InteractingWithUI,    // Mouse is over any UI element
    DraggingItem,         // Player is dragging an item from inventory
    WorldInteractable,    // Player is near an object they can interact with (e.g., "Press E to open")
    UsingToolOnWorld      // Lowest priority: The default game world interaction (mining, placing)
}
// UI input handling is in inventoryUIManager
// This script sits on the player client
public class InputManager : MonoBehaviour, INetworkedPlayerModule {
    private PlayerInput _playerInput;
    private InputAction _interactAction;
    private InputAction _playerClickAction;
    private InputAction _playerMoveAction;
    private InputAction _playerAimAction;
    private InputAction _playerDashAction;
    private InputAction _useItemAction;
    private InputAction _hotbarSelection;
    private UIManagerInventory _inventoryUIManager;
    private NetworkObject _clientObject;
    private InputDevice lastUsedDevice;
    // UI
    private InputAction _UItoggleInventoryAction; // Assign your toggle input action asset
    private InputAction _uiInteractAction;   // e.g., Left Mouse / Gamepad A
    private InputAction _uiAltInteractAction; // e.g., Right Mouse / Gamepad X
    private InputAction _uiDropOneAction;     // e.g., Right Mouse (when holding) / Gamepad B
    private InputAction _uiNavigateAction;    // D-Pad / Arrow Keys
    private InputAction _uiPointAction;       // Mouse position for cursor icon
    private InputAction _cancelAction;       // Escape / Gamepad Start (to cancel holding)
    private InputAction _uiTabLeft;
    private InputAction _uiTabRight;
    
    
    private LayerMask _interactableLayerMask;
    private ToolController _toolController;
    private PlayerMovement _playerMovement;
    private float _interactionRadius = 2f;
    private bool _dashPefromed;
    private Vector2 movementInput;   // For character movement
    private Vector2 rawAimInput;     // Raw input for aiming (mouse position or joystick)
    private IInteractable _currentInteractable;
    private IInteractable _previousInteractable;
    [ShowInInspector]
    private PlayerInteractionContext _currentContext;

    public int InitializationOrder => 101;

    public void InitializeOnOwner(NetworkedPlayer playerParent) {
        _inventoryUIManager = playerParent.UiManager.UIManagerInventory;
        _clientObject = playerParent.PlayerNetworkedObject;
        _toolController = playerParent.ToolController;
        _playerMovement = playerParent.PlayerMovement;
        _interactableLayerMask = 1 << LayerMask.NameToLayer("Interactables"); // Don't ask me why its in the unity documentation
        SetupInputs();
        SubscribeToEvents();
    }
    private void SetupInputs() {
        _playerInput = GetComponent<PlayerInput>();
        if (_playerInput != null) {
            _interactAction = _playerInput.actions.FindAction("Interact",true); // E
            _playerClickAction = _playerInput.actions.FindAction("Shoot",true);
            _playerMoveAction = _playerInput.actions.FindAction("Move",true);
            _playerAimAction = _playerInput.actions.FindAction("Aim",true);
            //_playerSwitchAction = _playerInput.actions.FindAction("SwitchTool",true);
            _playerDashAction = _playerInput.actions.FindAction("Dash",true);
            _cancelAction = _playerInput.actions.FindAction("Cancel",true);
            _UItoggleInventoryAction = _playerInput.actions.FindAction("UI_Toggle",true); // I
            _uiInteractAction = _playerInput.actions.FindAction("UI_Interact",true); // LMB
            _uiAltInteractAction = _playerInput.actions.FindAction("UI_AltInteract",true); // RMB
            _uiDropOneAction = _playerInput.actions.FindAction("UI_DropOne",true);
            _uiNavigateAction = _playerInput.actions.FindAction("UI_Navigate",true);
            _uiPointAction = _playerInput.actions.FindAction("UI_Point",true);
            _uiTabLeft = _playerInput.actions.FindAction("UI_TabLeft",true); // Opening containers
            _uiTabRight = _playerInput.actions.FindAction("UI_TabRight",true); // Opening containers
            _hotbarSelection = _playerInput.actions.FindAction("HotbarSelect",true);
            _useItemAction = _playerInput.actions.FindAction("Shoot",true);
        } else {
            Debug.LogWarning("PlayerInput component not found on player. Mouse-only or manual input bindings needed.", gameObject);
        }
    }
    void OnDisable() { if(_playerInput !=null) UnsubscribeFromEvents(); }
    private void SubscribeToEvents() {
        if (_UItoggleInventoryAction != null)
            _UItoggleInventoryAction.performed += UIOnToggleInventory;
        if (_cancelAction != null)
            _cancelAction.performed += UIHandleCloseAction;
            _cancelAction.performed += HandleCancelAction;
        _playerClickAction.performed += OnPrimaryInteractionPerformed;
        _playerClickAction.canceled += OnPrimaryInteractionPerformed;
        _playerDashAction.performed += OnDashPerformed;
        _playerDashAction.canceled += OnDashPerformed;
        _playerMoveAction.performed += OnMove;
        _playerMoveAction.canceled += OnMove;
        _playerAimAction.performed += OnAim;
        _playerAimAction.canceled += OnAim;
    }

 

    private void UnsubscribeFromEvents() {
        if (_UItoggleInventoryAction != null)
            _UItoggleInventoryAction.performed -= UIOnToggleInventory;
        if (_cancelAction != null)
            _cancelAction.performed -= UIHandleCloseAction;
            _cancelAction.performed -= HandleCancelAction;
        if (_playerClickAction != null) {
            _playerClickAction.performed -= OnPrimaryInteractionPerformed;
            _playerClickAction.canceled -= OnPrimaryInteractionPerformed;
        }
        if (_playerMoveAction != null) { 
            _playerMoveAction.performed -= OnMove;
        }
        if (_playerAimAction != null) {
            _playerAimAction.performed -= OnAim;
        }
    }
    private void Update() {
        if (_inventoryUIManager == null) return;
        UpdateInteractionContext();
        UpdateCursor();
        //UpdatePlayerFeedback();
        // Handle interaction input
        if (_currentInteractable != null && _interactAction.WasPerformedThisFrame()) {
            _currentInteractable.Interact(_clientObject);
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
        // We want to prioritize user initiated states
        if (BuildingManager.Instance != null) {
            if (BuildingManager.Instance.IsBuilding) {
                _currentContext = PlayerInteractionContext.Building;
                //Debug.Log("we are building!");
                return;
            }
        }
        // Check for UI interaction
        if (EventSystem.current.IsPointerOverGameObject() || _inventoryUIManager.IsOpen) {
            _currentContext = PlayerInteractionContext.InteractingWithUI;
            // TODO this should sometimes clear the interactable, but sometimes not. As the UI could be the interactable!
            
            //ClearInteractable(); // Can't interact with world objects if UI is in the way
            return;
        }

        // Check for nearby world interactables (your existing logic)
        CheckForNearbyInteractables(); // This method now just *finds* the interactable, doesn't handle input
        if (_currentInteractable != null) {
            _currentContext = PlayerInteractionContext.WorldInteractable;
            return;
        }
        // Not having hotbar selections anymore, for now 
        //if (_inventoryUIManager.ItemSelectionManager.CanUseSelectedSlotItem()) {
        //    _currentContext = PlayerInteractionContext.HotebarItemSelected;
        //    return;
        //}

        // Default to using a tool on the world
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
        if (colliders == null || colliders.Length == 0)
            return null;

        // 1) Sort colliders by distance to this object
        var sorted = colliders
            .OrderBy(c => Vector2.SqrMagnitude((Vector2)transform.position - (Vector2)c.transform.position));

        // 2) For each collider, check all its IInteractable components
        foreach (var col in sorted) {
            // 3) GetComponents returns every IInteractable on that GameObject
            var interactables = col.GetComponents<IInteractable>();
            foreach (var interactable in interactables) {
                // 4) Return the first one we can actually interact with
                if (interactable.CanInteract)
                    return interactable;
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
        movementInput = context.ReadValue<Vector2>();
    }

    // Called by Input System for aim input
    public void OnAim(InputAction.CallbackContext context) {
        rawAimInput = context.ReadValue<Vector2>();
    }
    public bool GetDashInput() => _dashPefromed;

    // Get movement input (e.g., WASD, joystick)
    public Vector2 GetMovementInput() {
        // Dissable movement if we are in a menu
        return _currentContext != PlayerInteractionContext.DraggingItem && _currentContext != PlayerInteractionContext.InteractingWithUI 
            ? movementInput : Vector2.zero;
    }
  
    // Get aim input, processed based on control scheme
    public Vector2 GetAimWorldInput() {
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
   
    //private void OnUseHotbarInput(InputAction.CallbackContext context) {
    //    if(_currentContext == PlayerInteractionContext.HotebarItemSelected) {
    //        _inventoryUIManager.ItemSelectionManager.HandleUseInput(context);
    //    }
    //}
    private void OnDashPerformed(InputAction.CallbackContext context) {
        if (context.performed) {
            _dashPefromed = true;
        } else if(context.canceled) {
            _dashPefromed = false;
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
                   _toolController.PerformMining(this);
                } else if (context.canceled) {
                     _toolController.StopMining();
                }
                break;
            case PlayerInteractionContext.Building:
                if (context.performed) {
                    BuildingManager.Instance.UserPlacedClicked(_clientObject);
                }
                break;
            case PlayerInteractionContext.WorldInteractable:
            case PlayerInteractionContext.None:
            default:
                // Do nothing
                break;
        }
    }
    #region INVENTORY

    private void UIOnToggleInventory(InputAction.CallbackContext context) {
        if (Console.IsConsoleOpen())
            return;
        _inventoryUIManager.HandleToggleInventory();
    }

    private void UIHandleCloseAction(InputAction.CallbackContext context) {
        // E.g., Escape key or Gamepad B/Start 
        _inventoryUIManager.HandleCloseAction(context); // For UI related
        ClearInteractable(); // Also clear interactable
    }
    private void HandleCancelAction(InputAction.CallbackContext context) {
        if(_currentContext == PlayerInteractionContext.Building)
            BuildingManager.Instance.HandlePlaceFailOrCancel();
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

    internal bool TryEnterBuildMode() {
        if (!_playerMovement.CanBuild()) {
            return false;
        }

        return true;
    }
}
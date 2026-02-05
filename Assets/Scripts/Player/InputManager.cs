using Sirenix.OdinInspector;
using System;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
public enum PlayerInteractionContext {
    None,                 // Default state, no specific interaction available
    InteractingWithUI,    // Mouse is over any UI element
    WorldInteractable,    // Player is near an object they can interact with (e.g., "Press E to open")
    UsingToolOnWorld      // Lowest priority: The default game world interaction (mining, placing)
}
// UI input handling is in inventoryUIManager
// This script sits on the player client
public class InputManager : MonoBehaviour, IPlayerModule {
    private PlayerInput _playerInput;
    private InputAction _interactAction;
    private InputAction _playerShootAction;
    private InputAction _playerMoveAction;
    private InputAction _playerAbilityAction;
    private InputAction _playerAimAction;
    private InputAction _playerDashAction;
    private InputAction _useItemAction;
    private InputAction _hotbarSelection;
    private UIManagerInventory _inventoryUIManager;
    private UIManager _UIManager;
    private PlayerManager _player;
    private PlayerAbilities _playerAbilities;
    private InputDevice lastUsedDevice;
    // UI
    private InputAction _UItoggleInventoryAction; // Assign your toggle input action asset
    private InputAction _uiInteractAction;   // e.g., Left Mouse / Gamepad A
    private InputAction _uiAltInteractAction; // e.g., Right Mouse / Gamepad X
    private InputAction _uiNavigateAction;    // D-Pad / Arrow Keys
    private InputAction _uiPointAction;       // Mouse position
    private InputAction _cancelAction;       // Escape / Gamepad Start (to cancel holding)
    private InputAction _uiTabLeft;
    private InputAction _uiTabRight;
    private InputAction _uiPan;
    private InputAction _uiZoom;
    
    
    private LayerMask _interactableLayerMask;
    private PlayerMovement _playerMovement;
    private float _interactionRadius = 2f;
    private bool _dashPefromed;
    private Vector2 movementInput;   // For character movement
    private Vector2 rawAimInput;     // Raw input for aiming (mouse position or joystick). Mouse pos is in screen pixels. 0,0 bottom left, screenrez top right
    private IInteractable _currentInteractable;
    private IInteractable _previousInteractable;
    [ShowInInspector]
    private PlayerInteractionContext _currentContext;
    private Vector2 _mousePos;
    private bool _primaryInputToggle;
    private Vector2 _uiNavigationVector;

    public int InitializationOrder => 101;

    public bool IsUsingController { get; internal set; }
    public event Action<InputAction.CallbackContext> OnUIInteraction;
    public void InitializeOnOwner(PlayerManager playerParent) {
        _UIManager = playerParent.UiManager;
        _inventoryUIManager = _UIManager.UIManagerInventory;
        _player = playerParent;
        _playerAbilities = playerParent.PlayerAbilities;
        _playerMovement = playerParent.PlayerMovement;
        _interactableLayerMask = 1 << LayerMask.NameToLayer("Interactables"); // Don't ask me why, its in the unity documentation
        SetupInputs();
        SubscribeToEvents();
    }
    private void SetupInputs() {
        _playerInput = GetComponent<PlayerInput>();
        if (_playerInput != null) {
            _interactAction = _playerInput.actions.FindAction("Interact",true); // E
            _playerShootAction = _playerInput.actions.FindAction("Shoot",true);
            _playerMoveAction = _playerInput.actions.FindAction("Move",true);
            _playerAbilityAction = _playerInput.actions.FindAction("Ability",true);
            _playerAimAction = _playerInput.actions.FindAction("Aim",true);
            //_playerSwitchAction = _playerInput.actions.FindAction("SwitchTool",true);
            _playerDashAction = _playerInput.actions.FindAction("Dash",true);
            _cancelAction = _playerInput.actions.FindAction("Cancel",true);
            _UItoggleInventoryAction = _playerInput.actions.FindAction("UI_Toggle",true); // I
            _uiInteractAction = _playerInput.actions.FindAction("UI_Interact",true); // LMB
            _uiAltInteractAction = _playerInput.actions.FindAction("UI_AltInteract",true); // RMB
            _uiNavigateAction = _playerInput.actions.FindAction("UI_Navigate",true);
            _uiPointAction = _playerInput.actions.FindAction("UI_Point",true);
            _uiTabLeft = _playerInput.actions.FindAction("UI_TabLeft",true); // Opening containers
            _uiTabRight = _playerInput.actions.FindAction("UI_TabRight",true); // Opening containers

            _uiPan = _playerInput.actions.FindAction("UI_PanAction",true); // Start Moving upgrade view
            _uiZoom = _playerInput.actions.FindAction("UI_Scroll",true); // Zooming upgrade view
            _hotbarSelection = _playerInput.actions.FindAction("HotbarSelect",true);
            _useItemAction = _playerInput.actions.FindAction("Shoot",true);
        } else {
            Debug.LogWarning("PlayerInput component not found on player. Mouse-only or manual input bindings needed.", gameObject);
        }
    }
    void OnDisable() { if(_playerInput !=null) UnsubscribeFromEvents(); }
    private void SubscribeToEvents() {
        _UItoggleInventoryAction.performed += UIOnToggleInventory;
        _cancelAction.performed += UIHandleCloseAction;
        _cancelAction.performed += HandleCancelAction;
        _playerShootAction.performed += OnPrimaryInteraction;
        _playerShootAction.canceled += OnPrimaryInteraction;
        _uiInteractAction.performed += OnPrimaryUIInteraction;
        _playerDashAction.performed += OnDashPerformed;
        _playerDashAction.canceled += OnDashPerformed;
        _playerMoveAction.performed += OnMove;
        _playerMoveAction.canceled += OnMove;
        _playerAimAction.performed += OnAim;
        _playerAimAction.canceled += OnAim;
        _uiPan.performed += OnPanStart;
        _uiPan.canceled += OnPanStop;
        _uiPointAction.performed += OnMousePosChange;
        _uiZoom.performed += OnZoom;
        _uiZoom.canceled += OnZoom;
        _uiNavigateAction.performed += OnUINavigation;
        _uiNavigateAction.canceled += OnUINavigation;

        _playerAbilityAction.performed+= OnAbilityPerformed;
    }

    private void OnPanStop(InputAction.CallbackContext context) {
        _UIManager.UpgradeScreen.PanAndZoom.OnPanStop();
    }

    private void OnPanStart(InputAction.CallbackContext obj) {
        _UIManager.UpgradeScreen.PanAndZoom.OnPanStart();
     }

    private void OnZoom(InputAction.CallbackContext context) {
        _UIManager.UpgradeScreen.PanAndZoom.OnZoom(context.ReadValue<Vector2>().y);
    }

    private void OnMousePosChange(InputAction.CallbackContext context) {
        _mousePos = context.ReadValue<Vector2>();
    }
    private void OnUINavigation(InputAction.CallbackContext context) {
        _uiNavigationVector = context.ReadValue<Vector2>();
        if (context.canceled) {
            _uiNavigationVector = Vector2.zero;
        }
    }


    private void UnsubscribeFromEvents() {
        if (_UItoggleInventoryAction != null)
            _UItoggleInventoryAction.performed -= UIOnToggleInventory;
        if (_cancelAction != null)
            _cancelAction.performed -= UIHandleCloseAction;
            _cancelAction.performed -= HandleCancelAction;
        if (_playerShootAction != null) {
            _playerShootAction.performed -= OnPrimaryInteraction;
            _playerShootAction.canceled -= OnPrimaryInteraction;
        }
        if (_playerMoveAction != null) { 
            _playerMoveAction.performed -= OnMove;
        }
        if (_playerAimAction != null) {
            _playerAimAction.performed -= OnAim;
        }
        if (_uiInteractAction != null) {
            _uiInteractAction.performed -= OnPrimaryUIInteraction;
        }

        _uiNavigateAction.performed -= OnUINavigation;
    }
    private void Update() {
        if (_inventoryUIManager == null) return;
        UpdateInteractionContext();
        UpdateCursor();
        //UpdatePlayerFeedback();
        // Handle interaction input
        if (_currentInteractable != null && _interactAction.WasPerformedThisFrame()) {
            _currentInteractable.Interact(_player);
        }
    }

    private void UpdateCursor() {
        if(_currentContext == PlayerInteractionContext.InteractingWithUI) {
            Cursor.SetCursor(Resources.Load<Texture2D>("cursorMenu"), new Vector2(3, 3), CursorMode.Auto);
        } else {
            Cursor.SetCursor(Resources.Load<Texture2D>("cursorCrossHair"), new Vector2(10.5f, 10.5f), CursorMode.Auto);
            // Crosshair
        }
    }

    private void UpdateInteractionContext() {
        if(_playerMovement.GetState == PlayerMovement.PlayerState.None) {
            _currentContext = PlayerInteractionContext.None;
        }

        // Check for UI interaction
        if (_UIManager.IsAnyUIOpen()) {
            _currentContext = PlayerInteractionContext.InteractingWithUI;
            // TODO this should sometimes clear the interactable, but sometimes not. As the UI could be the interactable!            
            return;
        }
        
        CheckForNearbyInteractables();
        
        if (_currentInteractable != null) {
            _currentContext = PlayerInteractionContext.WorldInteractable;
            return;
        }
        // Default to using a tool on the world
        _currentContext = PlayerInteractionContext.UsingToolOnWorld;
    }
    private void CheckForNearbyInteractables() {
        Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, _interactionRadius, _interactableLayerMask);
        IInteractable closestInteractable = FindClosestInteractable(colliders);

        if (closestInteractable != _currentInteractable) {
            _previousInteractable?.SetInteractable(false); // Hide prompt on old one
            //Debug.Log("Found new interactable!: " + closestInteractable);
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
        return _currentContext == PlayerInteractionContext.UsingToolOnWorld || _currentContext == PlayerInteractionContext.WorldInteractable
            ? movementInput : Vector2.zero;
    }
  
    // Get aim input, processed based on control scheme
    public Vector2 GetAimWorldInput(Transform reference = null) {
        var cam = Camera.main;
        //Debug.Log($"rawAimInput: {rawAimInput}, cam: {(cam == null ? "null" : cam.name)}, camPos: {(cam == null ? "n/a" : cam.transform.position.ToString())}");
        if (cam != null) {
            if (reference != null) { 
                float z = cam.WorldToScreenPoint(reference.position).z; // replace referenceTransform with player/tool transform
                //Debug.Log($"screenZForReference: {z}");
                Vector3 screen = new Vector3(rawAimInput.x, rawAimInput.y, z);
                Vector3 world = cam.ScreenToWorldPoint(screen);
                //Debug.Log($"screen: {screen} -> world: {world}");
                return world;
            }
        }
        if (_playerInput.currentControlScheme == "Keyboard&Mouse") {
            // Convert mouse screen position to world position
            float zDistanceToPlane = -Camera.main.transform.position.z;
            //Debug.Log($"Cam {Camera.main.name} has z pos: {Camera.main.transform.position.z}");
            Vector3 screenPos = new Vector3(rawAimInput.x, rawAimInput.y, zDistanceToPlane);
            //return Camera.main.ScreenToWorldPoint(rawAimInput);
            return Camera.main.ScreenToWorldPoint(screenPos);
        } else // Controller
          {
            // Use joystick direction, normalized
            return rawAimInput.normalized;
        }
    }
    public Vector2 GetDirFromPos(Vector2 worldPos) {
        Vector2 screenPos = rawAimInput;
        // Convert to world coordinates using main camera
        Vector3 worldMouse = Camera.main.ScreenToWorldPoint(screenPos);
        Vector2 dir = (Vector2)(worldMouse - (Vector3)worldPos);
        return dir.normalized;
    }
    public Vector2 GetAimScreenInput() {
        if (_playerInput.currentControlScheme == "Keyboard&Mouse") {
            // Convert mouse screen position to world position
            return rawAimInput;
        } else // Controller
          {
            return rawAimInput; // Also raw?!
        }
    }
    public Vector2 GetUINavigationInput() {
        return _uiNavigationVector;
    }
    public bool IsHoldingDownPrimaryInput() {
        // Could add some more checks here later idk
        return _primaryInputToggle;
    }
    public bool IsShooting() {
        // Could add some more checks here later idk
        return IsHoldingDownPrimaryInput() && _playerMovement.CanUseTool();
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
            _playerAbilities.UseActive(ResourceSystem.PlayerDashID);

            _dashPefromed = true;
        } else if(context.canceled) {
            _dashPefromed = false;
        }
    }

    private void OnAbilityPerformed(InputAction.CallbackContext context) {
        if (!context.performed) return;

        // This should either use all the active, or it should depend on what we have equipped
        // Or it should depend on what button we pressed, so many options! Just always do ability for now
        _playerAbilities.UseActive(ResourceSystem.BrimstoneBuffID);
        /* Old toolcontroller code, we're not using toolcontroller anymore
        var behaviour = _toolController.CurrentToolBehaviour;
        if (behaviour == null) return;

        // local handler so we can remove it easily
        void OnAbilityStateChanged(bool isUsing) {
            IsUsingAbility = isUsing;
            if (!isUsing) behaviour.AbilityStateChanged -= OnAbilityStateChanged; // auto-unsubscribe once ended
        }

        // avoid double-subscribe
        behaviour.AbilityStateChanged -= OnAbilityStateChanged;
        behaviour.AbilityStateChanged += OnAbilityStateChanged;

        // now start — safe because we are already subscribed
        behaviour.ToolAbilityStart(_toolController);
         */
    }

    public void OnPrimaryInteraction(InputAction.CallbackContext context) {
        lastUsedDevice = context.control.device;
        // Todo some kind of checks to see if this is allowed..
        // This is Left Mouse Click / Gamepad A
        // Switch on the context to decide what Left Click does
        if(_currentContext == PlayerInteractionContext.UsingToolOnWorld || _currentContext == PlayerInteractionContext.WorldInteractable) {
            if (context.performed) {
                _primaryInputToggle = true;
                //_toolController.ToolStart(this);
            } else if (context.canceled) {
                _primaryInputToggle = false;
                //_toolController.ToolStop();
            }
        } 
        // old building code
        //        if (context.performed) {
        //            BuildingManager.Instance.UserPlacedClicked(_clientObject);
        //        }
        
    }
    private void OnPrimaryUIInteraction(InputAction.CallbackContext context) {
        // Just invoke an event? Right??
        // BTW this is not MB1, its enter, for when handling ui with keyboard only
        OnUIInteraction.Invoke(context);
    }


    #region INVENTORY

    private void UIOnToggleInventory(InputAction.CallbackContext context) {
        if (Console.IsConsoleOpen())
            return;
       // _inventoryUIManager.HandleToggleInventory();
    }

    private void UIHandleCloseAction(InputAction.CallbackContext context) {
        // E.g., Escape key or Gamepad B/Start 
        //_inventoryUIManager.HandleCloseAction(context); // For UI related
        ClearInteractable(); // Also clear interactable
    }
    private void HandleCancelAction(InputAction.CallbackContext context) {
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
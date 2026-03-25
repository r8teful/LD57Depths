using Anarkila.DeveloperConsole;
using Sirenix.OdinInspector;
using System;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
public enum PlayerInteractionContext {
    None,                 // Default state, no specific interaction available
    Console,              // Console is open
    InteractingWithUI,    // Mouse is over any UI element
    WorldInteractable,    // Player is near an object they can interact with (e.g., "Press E to open")
    UsingToolOnWorld      // Lowest priority: The default game world interaction (mining, placing)
}
// UI input handling is in inventoryUIManager
// This script sits on the player client
public class InputManager : MonoBehaviour, IPlayerModule {
    private PlayerInput _playerInput;
    private InputActionMap _playerActionMap;
    private InputActionMap _uiActionMap;
    // Player
    private InputAction _playerMoveAction;
    private InputAction _playerAimAction;
    private InputAction _playerShootAction;
    private InputAction _playerInteractAction;
    private InputAction _playerAbilityAction;
    private InputAction _playerPauseAction;


    // UI 
    private InputAction _uiNavigateAction;    // D-Pad / Arrow Keys
    private InputAction _uiPanMoveAction;
    private InputAction _uiSubmitAction;   // e.g., Left Mouse / Gamepad A
    private InputAction _uiCancelAction;
    private InputAction _uiZoomAction;
    private InputAction _uiZoomInAction;
    private InputAction _uiZoomOutAction;
    private InputAction _uiPanHoldAction; 
    private InputAction _uiPointAction;       // Mouse position
    private InputAction _uiTabLeft;
    private InputAction _uiTabRight;
    private InputAction _uiInteractExit;


    private UIManagerInventory _inventoryUIManager;
    private UIManager _UIManager;
    private PlayerManager _player;
    private PlayerAbilities _playerAbilities;
    private InputDevice lastUsedDevice;

    public enum DeviceType { KeyboardMouse, Gamepad }
    public static event Action<DeviceType> OnDeviceChanged;
    public static DeviceType CurrentDevice { get; private set; } = DeviceType.KeyboardMouse;

    private LayerMask _interactableLayerMask;
    private PlayerMovement _playerMovement;
    private float _interactionRadius = 2f;
    private Vector2 movementInput;   // For character movement
    private Vector2 rawAimInput;     // Raw input for aiming (mouse position or joystick). Mouse pos is in screen pixels. 0,0 bottom left, screenrez top right
    private IInteractable _currentInteractable;
    private IInteractable _previousInteractable;
    [ShowInInspector]
    private PlayerInteractionContext _currentContext;
    private bool _primaryInputToggle;
    private Vector2 _uiNavigationVector;
    private Vector2 _panVector;

    public int InitializationOrder => 1001; // after ui 

    public bool IsUsingController { get; internal set; }
    public event Action<InputAction.CallbackContext> OnUIInteraction;
    public void InitializeOnOwner(PlayerManager playerParent) {
        Debug.Log("InputManagerInit..");
        _UIManager = playerParent.UiManager;
        _inventoryUIManager = _UIManager.UIManagerInventory;
        _player = playerParent;
        _playerAbilities = playerParent.PlayerAbilities;
        _playerMovement = playerParent.PlayerMovement;
        _interactableLayerMask = (1 << LayerMask.NameToLayer("InteractablesInterior")) |
                                 (1 << LayerMask.NameToLayer("InteractablesExterior")); // Don't ask me why, its in the unity documentation
        SetupInputs();
        SubscribeToEvents();

    }
    private void SetupInputs() {
        _playerInput = GetComponent<PlayerInput>();
        if (_playerInput == null) {
            Debug.LogError("PlayerInput component not found on player. Mouse-only or manual input bindings needed.", gameObject);
        } 
        if( _playerInput.actions == null) {
            Debug.LogError("Actions not found on player!!");
        }

        _playerActionMap = _playerInput.actions.FindActionMap("Player");
        _playerMoveAction = _playerInput.actions.FindAction("Player/Move",true);
        _playerAimAction = _playerInput.actions.FindAction("Player/Aim",true);
        _playerShootAction = _playerInput.actions.FindAction("Player/Shoot",true);
        _playerInteractAction = _playerInput.actions.FindAction("Player/Interact",true); // E
        _playerAbilityAction = _playerInput.actions.FindAction("Player/Ability",true);
        _playerPauseAction = _playerInput.actions.FindAction("Player/Pause",true);


        _uiActionMap = _playerInput.actions.FindActionMap("UI");
        _uiNavigateAction = _playerInput.actions.FindAction("UI/Navigate",true);
        _uiPanMoveAction = _playerInput.actions.FindAction("UI/Pan",true); // Start Moving upgrade view
        _uiSubmitAction = _playerInput.actions.FindAction("UI/Submit",true); // LMB, X
        _uiCancelAction = _playerInput.actions.FindAction("UI/Cancel",true);
        _uiZoomAction = _playerInput.actions.FindAction("UI/Zoom",true); // Zooming with mouse
        _uiZoomInAction = _playerInput.actions.FindAction("UI/ZoomOut",true); // Zooming with controller
        _uiZoomOutAction = _playerInput.actions.FindAction("UI/ZoomIn", true); // Zooming with controller
        _uiPanHoldAction = _playerInput.actions.FindAction("UI/PanHold", true); 
        _uiPointAction = _playerInput.actions.FindAction("UI/Point",true);
        _uiTabLeft = _playerInput.actions.FindAction("UI/TabLeft",true); 
        _uiTabRight = _playerInput.actions.FindAction("UI/TabRight",true);
        _uiInteractExit = _playerInput.actions.FindAction("UI/InteractExit", true); // Same as E 
        Debug.Log("_playerMoveAction: " + _playerMoveAction.GetBindingDisplayString(InputBinding.DisplayStringOptions.DontOmitDevice));
        Debug.Log("_playerAimAction : " + _playerAimAction .GetBindingDisplayString(InputBinding.DisplayStringOptions.DontOmitDevice));
        Debug.Log("_playerShootAction : " + _playerShootAction .GetBindingDisplayString(InputBinding.DisplayStringOptions.DontOmitDevice));
        Debug.Log("_playerInteractAction : " + _playerInteractAction .GetBindingDisplayString(InputBinding.DisplayStringOptions.DontOmitDevice));
        Debug.Log("_playerAbilityAction : " + _playerAbilityAction .GetBindingDisplayString(InputBinding.DisplayStringOptions.DontOmitDevice));
        Debug.Log("_playerPauseAction : " + _playerPauseAction.GetBindingDisplayString(InputBinding.DisplayStringOptions.DontOmitDevice));
    }


    void OnDisable() { 
        if(_playerInput !=null) UnsubscribeFromEvents(); 
    }
    private void SubscribeToEvents() {
        _uiCancelAction.performed += OnCancel;
        _playerShootAction.performed += OnPrimaryInteraction;
        _playerShootAction.canceled += OnPrimaryInteraction;
        _uiSubmitAction.performed += OnPrimaryUIInteraction;
        _playerMoveAction.performed += OnMove;
        _playerMoveAction.canceled += OnMove;
        _playerAimAction.performed += OnAim;
        _playerAimAction.canceled += OnAim;
        _playerPauseAction.performed += OnPause;
        _uiPanHoldAction.performed += OnPanStart;
        _uiPanHoldAction.canceled += OnPanStop;
        _uiPanMoveAction.performed += OnPanContol;
        _uiPanMoveAction.canceled += OnPanContol;
        _uiPointAction.performed += OnMousePosChange;
        _uiZoomAction.performed += OnZoom;
        _uiZoomAction.canceled += OnZoom;
        _uiZoomInAction.performed += OnZoomIn;
        _uiZoomInAction.canceled += OnZoomIn;
        _uiZoomOutAction.performed += OnZoomOut;
        _uiZoomOutAction.canceled += OnZoomOut;
        _uiNavigateAction.performed += OnUINavigation;
        _uiNavigateAction.canceled += OnUINavigation;
        _playerInput.onControlsChanged += OnControlsChanged;

        _playerAbilityAction.performed+= OnAbilityPerformed;

        InputSystem.onActionChange += HandleActionChange;
        UIManager.OnUIOpenChange += HandleActionMaps;
    }
    private void UnsubscribeFromEvents() {
        _uiCancelAction.performed -= OnCancel;
        _playerShootAction.performed -= OnPrimaryInteraction;
        _playerShootAction.canceled -= OnPrimaryInteraction;
        _uiSubmitAction.performed -= OnPrimaryUIInteraction;
        _playerMoveAction.performed -= OnMove;
        _playerMoveAction.canceled -= OnMove;
        _playerPauseAction.performed -= OnPause;
        _playerAimAction.performed -= OnAim;
        _playerAimAction.canceled -= OnAim;
        _uiPanMoveAction.performed -= OnPanStart;
        _uiPanMoveAction.canceled -= OnPanStop;
        _uiPointAction.performed -= OnMousePosChange;
        _uiZoomAction.performed -= OnZoom;
        _uiZoomAction.canceled -= OnZoom;
        _uiZoomInAction.performed -= OnZoomIn;
        _uiZoomInAction.canceled -= OnZoomIn;
        _uiZoomOutAction.performed -= OnZoomOut;
        _uiZoomOutAction.canceled -= OnZoomOut;
        _uiNavigateAction.performed -= OnUINavigation;
        _uiNavigateAction.canceled -= OnUINavigation;
        _playerInput.onControlsChanged -= OnControlsChanged;
        _playerAbilityAction.performed -= OnAbilityPerformed;

        InputSystem.onActionChange -= HandleActionChange;
    }
    private void HandleActionMaps(bool isUIOpen) {
        if (isUIOpen) {
            _uiActionMap.Enable();
            _playerActionMap.Disable();
        } else {
            _uiActionMap.Disable();
            _playerActionMap.Enable();
        }
    }
    private void OnPause(InputAction.CallbackContext context) {
        if (_UIManager.IsAnyUIOpen()) {
            return; // Don't do anything if UI is open
        }
        _UIManager.Pause();
    }

    private void OnCancel(InputAction.CallbackContext context) {
        // If any ui open, close it 
        if (_UIManager.TryCloseAnyOpenUI()) {
            return;
        }
        ClearInteractable();
    }

    private void OnPanStop(InputAction.CallbackContext context) {
        //_UIManager.UpgradeScreen.PanAndZoom.OnPanStop();
        _UIManager.UpgradeScreen.PanAndZoom.OnPanEnd();
    }

    private void OnPanStart(InputAction.CallbackContext obj) {
        //_UIManager.UpgradeScreen.PanAndZoom.OnPanStart();
        _UIManager.UpgradeScreen.PanAndZoom.OnPanStart();
    }

    // Movement vector for controller panning
    private void OnPanContol(InputAction.CallbackContext context) {
        if (context.canceled) {
            _panVector = Vector2.zero;
        }else {
            _panVector = context.ReadValue<Vector2>();
        }
    }

    private void OnZoom(InputAction.CallbackContext context) {
       // _UIManager.UpgradeScreen.PanAndZoom.OnZoom(context.ReadValue<Vector2>().y);
        _UIManager.UpgradeScreen.PanAndZoom.OnZoom(context.ReadValue<Vector2>().y);
    }
    private void OnZoomOut(InputAction.CallbackContext context) {
        if (context.canceled) { 
            _UIManager.UpgradeScreen.PanAndZoom.OnPanEnd();
        }else {
            _UIManager.UpgradeScreen.PanAndZoom.OnZoomStart(isZoomIn: false);
        }
    }

    private void OnZoomIn(InputAction.CallbackContext context) {
        if (context.canceled) {
            _UIManager.UpgradeScreen.PanAndZoom.OnPanEnd();
        } else {
            _UIManager.UpgradeScreen.PanAndZoom.OnZoomStart(isZoomIn: true);
        }
    
    }

    private void OnMousePosChange(InputAction.CallbackContext context) {
        rawAimInput = context.ReadValue<Vector2>(); // rawAimInput also used for player, we could make a different variable here but eh
    }
    private void OnUINavigation(InputAction.CallbackContext context) {
        _uiNavigationVector = context.ReadValue<Vector2>();
        if (context.canceled) {
            _uiNavigationVector = Vector2.zero;
        }
    }
    private void HandleActionChange(object obj, InputActionChange change) {
        // We only care about actions that were actually performed
        if (change != InputActionChange.ActionPerformed)
            return;

        if (obj is not InputAction action)
            return;

        var device = action.activeControl?.device;
        if (device == null)
            return;

        DeviceType detected = device is Gamepad ? DeviceType.Gamepad : DeviceType.KeyboardMouse;

        if (detected != CurrentDevice) {
            CurrentDevice = detected;
            Debug.Log("Device change to: " + detected);
            OnDeviceChanged?.Invoke(CurrentDevice);
        }
    }


    private void Update() {
        if (_inventoryUIManager == null) return;
        UpdateInteractionContext();
        UpdateCursor();
        //UpdatePlayerFeedback();
        // Handle interaction input
        if (_currentContext == PlayerInteractionContext.Console) 
            return;
        if (_currentInteractable != null && _playerInteractAction.WasPerformedThisFrame()) {
            _currentInteractable.Interact(_player);
        }
    }

    private void UpdateCursor() {
        if(_currentContext == PlayerInteractionContext.InteractingWithUI) {
            App.CursorManager.SetCursor(CursorType.Menu);
        } else {
            App.CursorManager.SetCursor(CursorType.Crosshair);
        }
    }

    private void UpdateInteractionContext() {
        if (ConsoleManager.IsConsoleOpen()) {
            _currentContext = PlayerInteractionContext.Console;
            return;
        }
        if(_playerMovement.GetState == PlayerMovement.PlayerState.None) {
            _currentContext = PlayerInteractionContext.None;
        }

        // Check for UI interaction
        if (_UIManager.IsAnyUIOpen()) { // I've tried to add || Console.IsConsoleOpen() but it doesn't really help because we need to also clear the interactable but then we coudn't close ui's because that is tied to the interactable thing
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
        Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, _interactionRadius*4f, _interactableLayerMask);
        IInteractable closestInteractable = FindClosestInteractable(colliders);

        if (closestInteractable != _currentInteractable) {
            _previousInteractable?.SetInteractable(false); // Hide prompt on old one
            //Debug.Log("Found new interactable!: " + closestInteractable);
            _currentInteractable = closestInteractable;
            _previousInteractable = _currentInteractable;

            _currentInteractable?.SetInteractable(true);
        }
    }

    private IInteractable FindClosestInteractable(Collider2D[] colliders) {
        if (colliders == null || colliders.Length == 0)
            return null;
        // Remove too big colliders

        // Sort colliders by distance to this object
        var sorted = colliders
            .OrderBy(c => Vector2.SqrMagnitude((Vector2)transform.position - (Vector2)c.transform.position));

        // For each collider, check all its IInteractable components
        foreach (var col in sorted) {
            // GetComponents returns every IInteractable on that GameObject
            var interactables = col.GetComponents<IInteractable>();
            foreach (var interactable in interactables) {
                float effectiveRange = interactable.InteractionRangeOverride < 0 ?
                                       _interactionRadius : interactable.InteractionRangeOverride;
                float distance = Vector2.Distance(transform.position, col.transform.position);

                if (distance <= effectiveRange) {
                    // Return the first one we can actually interact with
                    if (interactable.CanInteract)
                        return interactable;
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
    
    public void OnMove(InputAction.CallbackContext context) {
        movementInput = context.ReadValue<Vector2>();
    }


    public void OnAim(InputAction.CallbackContext context) {
        rawAimInput = context.ReadValue<Vector2>();
        //Debug.Log(rawAimInput);
    }


    // Get movement input (e.g., WASD, joystick)
    public Vector2 GetMovementInput() {
        // Dissable movement if we are in a menu
        return _currentContext == PlayerInteractionContext.UsingToolOnWorld || _currentContext == PlayerInteractionContext.WorldInteractable
            ? movementInput : Vector2.zero;
    }
    internal Vector2 GetPanVector() {
        return _panVector;
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

    public Vector2 GetPointerPivotInViewport(RectTransform viewport) {
        //Vector2 screenPos = Pointer.current.position.ReadValue();
         Vector2 screenPos = rawAimInput;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            viewport,
            screenPos,
            null,
            out Vector2 localPoint
        );

        return localPoint;
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

    private void OnControlsChanged(PlayerInput input) {
        IsUsingController = input.currentControlScheme == "Gamepad";
    }
    //private void OnUseHotbarInput(InputAction.CallbackContext context) {
    //    if(_currentContext == PlayerInteractionContext.HotebarItemSelected) {
    //        _inventoryUIManager.ItemSelectionManager.HandleUseInput(context);
    //    }
    //}
 
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
        // This is Left Mouse Click / Gamepad A
        if(_currentContext == PlayerInteractionContext.UsingToolOnWorld || _currentContext == PlayerInteractionContext.WorldInteractable) {
            if (context.performed) {
                _primaryInputToggle = true;
            } else if (context.canceled) {
                _primaryInputToggle = false;
            }
        } 
        
    }
    private void OnPrimaryUIInteraction(InputAction.CallbackContext context) {
        // Just invoke an event? Right??
        // BTW this is not MB1, its enter, for when handling ui with keyboard only
        OnUIInteraction?.Invoke(context);
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
    void OnDrawGizmosSelected() {
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(new(transform.position.x,transform.position.y+_interactionRadius,0), _interactionRadius);
    }

}
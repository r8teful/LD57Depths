using System;
using System.Collections.Generic;
using UnityEngine;
public class PlayerMovement : MonoBehaviour, IPlayerModule {

    private Rigidbody2D rb;
    private Camera MainCam;

    public float walkSpeed = 5f;
    
    private float decelerationForce = 3f;

    public enum PlayerState {None, Swimming, Grounded}
    private PlayerState _currentState;
    private PlayerStatsManager _playerStats;
    private InputManager _inputManager;
    private PlayerVisualHandler _visualHandler;

    private Vector2 _currentInput;
    private bool _isDashing;
    private bool DEBUGIsGOD;

    List<ContactPoint2D> _contactsMostRecent = new List<ContactPoint2D>(); // Store the contacts we hit on collision enter
    private bool _dashUnlocked = false;
    private float _cachedSwimSpeed;

    public List<ContactPoint2D> ContactsMostRecent { get => _contactsMostRecent; set => _contactsMostRecent = value; }
    public int InitializationOrder => 998;


    internal bool CanUseTool() => _currentState == PlayerState.Swimming;
    internal PlayerState GetState => _currentState;
    internal bool CanBuild() => _currentState == PlayerState.Swimming;
    public event Action<PlayerState> OnPlayerStateChanged;
    public Rigidbody2D GetRigidbody() => rb;

    public void InitializeOnOwner(PlayerManager playerParent) {
        MainCam = Camera.main;
        MainCam.transform.SetParent(transform);
        MainCam.transform.localPosition = new Vector3(0, 0, -10);
        _playerStats = playerParent.PlayerStats;
        _inputManager = playerParent.InputManager;
        _visualHandler = playerParent.PlayerVisuals;
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0;
        ChangeState(PlayerState.Swimming);
        SubscribeToEvents();
    }
    private void SubscribeToEvents() {
        // Subscribe to the event to recalculate stats when a NEW upgrade is bought
        //MiningLazer.OnPlayerKnockbackRequested += OnMiningKnockback;
        PlayerLayerController.OnPlayerVisibilityChanged += PlayerVisibilityLayerChanged;
    }

    // Called every frame from lazer
    public void ApplyMiningKnockback(Vector2 force) {
        rb.AddForce(force);
    }


    private void PlayerVisibilityLayerChanged(VisibilityLayerType type) {
        switch (type) {
            case VisibilityLayerType.Exterior:
                ChangeState(PlayerState.Swimming);
                break;
            case VisibilityLayerType.Interior:
                ChangeState(PlayerState.Grounded);
                break;
            default:
                break;
        }
    }

    private void OnDisable() {
        PlayerLayerController.OnPlayerVisibilityChanged -= PlayerVisibilityLayerChanged;
        //NetworkedPlayer.LocalInstance.PlayerStats.OnStatChanged -= OnStatChanged; // This will give null exception when a remote client disables the script
    }

    void Update() {
        if (_inputManager == null)
            return;
        // Get input for movement
        _currentInput = _inputManager.GetMovementInput();
        // Handle state-specific logic in Update (mostly non-physics like animations, input processing)
        switch (_currentState) {
            case PlayerState.Swimming:
                HandleSwimmingUpdate();
                break;
            case PlayerState.Grounded:
                break;
        }
        _visualHandler.HandleVisualUpdate(_currentState, _currentInput);
    }

    void OnCollisionEnter2D(Collision2D col) {
        if (rb.linearVelocity.magnitude > _cachedSwimSpeed)
            rb.linearVelocity = Vector2.ClampMagnitude(rb.linearVelocity, _cachedSwimSpeed);
        _contactsMostRecent.Clear();
        _contactsMostRecent.AddRange(col.contacts);
    }

    void FixedUpdate() {
        if (_inputManager == null)
            return;
        // Handle state-specific physics logic in FixedUpdate
        switch (_currentState) {
            case PlayerState.Swimming:
                HandleSwimmingPhysics();
                break;
            case PlayerState.Grounded:
                HandleGroundedPhysics();
                break;
        }
    }

    
    #region SWIMMING
    private void HandleSwimmingUpdate() {
        // Dash logic was here but its nothing now
    }

    // --- State Handlers for FixedUpdate (Physics) ---
    private void HandleSwimmingPhysics() {
        var accelerationForce = _playerStats.GetStat(StatType.PlayerAcceleration);
        _cachedSwimSpeed = _playerStats.GetStat(StatType.PlayerSpeedMax);
#if UNITY_EDITOR
        if (DEBUGIsGOD) {
            _cachedSwimSpeed *= 5;
            accelerationForce *= 5;
        }
#endif
        Vector2 moveDirection = _currentInput.normalized;

        if (moveDirection != Vector2.zero) {
            rb.AddForce(moveDirection * accelerationForce);
        } else {
            if (rb.linearVelocity.magnitude > 0.01f) {
                Vector2 oppositeDirection = -rb.linearVelocity.normalized;
                rb.AddForce(oppositeDirection * decelerationForce);
            } else {
                rb.linearVelocity = Vector2.zero;
            }
        }
        if (!_isDashing) {
            float speed = rb.linearVelocity.magnitude;
            if (speed > _cachedSwimSpeed) {
                // lerp the magnitude down toward swimSpeed each FixedUpdate
                float newSpeed = Mathf.Lerp(speed, _cachedSwimSpeed, 5 * Time.fixedDeltaTime);
                rb.linearVelocity = rb.linearVelocity.normalized * newSpeed;
            }
        }
    }
    #endregion

    private void HandleGroundedPhysics() {
        // Horizontal movement
        rb.linearVelocity = new Vector2(_currentInput.x * walkSpeed, rb.linearVelocity.y);
    }

  

    // --- State Management ---
    public void ChangeState(PlayerState newState) {
        if (_currentState == newState)
            return;

        // Exit current state logic
        OnStateExit(_currentState, newState);

        _currentState = newState;
         Debug.Log("Changed state to: " + _currentState);
        // Enter new state logic
        OnStateEnter(_currentState, newState);
        OnPlayerStateChanged?.Invoke(newState);
    }

    void OnStateEnter(PlayerState state, PlayerState oldState) {
        _visualHandler.OnStateEnter(state);
        switch (state) {
            case PlayerState.Swimming:
                rb.gravityScale = 0; // No gravity when swimming
                break;
            case PlayerState.Grounded:
                rb.gravityScale = 2;
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0); // Reset vertical velocity when entering from climb
                break;
        }
    }


    void OnStateExit(PlayerState state, PlayerState newState) {
        
    }


   
   
   
    internal void DEBUGToggleHitbox() {
        _visualHandler.DEBUGToggleHitbox(_currentState);
    }

    internal void DEBUGToggleGodMove() {
        if (!DEBUGIsGOD) {
            DEBUGIsGOD = true;
            DEBUGToggleHitbox();
        } else {
            DEBUGIsGOD = false;
            DEBUGExitGodMove();
        }
    }
    private void DEBUGExitGodMove() {
        DEBUGToggleHitbox();
    }
}
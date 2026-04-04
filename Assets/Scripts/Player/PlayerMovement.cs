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
    private bool DEBUGIsGOD;

    List<ContactPoint2D> _contactsMostRecent = new List<ContactPoint2D>(); // Store the contacts we hit on collision enter
    private float _swimSpeed;
    private float _waterDifficulty;

    public List<ContactPoint2D> ContactsMostRecent { get => _contactsMostRecent; set => _contactsMostRecent = value; }
    public int InitializationOrder => 998;


    internal bool CanUseTool() => _currentState == PlayerState.Swimming;
    internal PlayerState GetState => _currentState;
    internal bool CanPickup() => _currentState == PlayerState.Swimming;
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
        PlayerWorldLayerController.OnPlayerWorldLayerChange += PlayerLayerChanged;
    }

    private void PlayerLayerChanged(int index) {
        if(index == 0) {
            _waterDifficulty = 1; // treat index as a multiplier
        } else if(index == 1) {
            _waterDifficulty = 0.3f;
        } else if (index ==2) {
            _waterDifficulty = 0.2f;
        } else if (index ==3) {
            _waterDifficulty = 0.1f;
        } else {
            _waterDifficulty = 1;
        }
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
        _visualHandler.HandleVisualUpdate(_currentState, _currentInput);
    }

    void OnCollisionEnter2D(Collision2D col) {
        if (rb.linearVelocity.magnitude > _swimSpeed)
            rb.linearVelocity = Vector2.ClampMagnitude(rb.linearVelocity, _swimSpeed);
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

    

    // --- State Handlers for FixedUpdate (Physics) ---
    private void HandleSwimmingPhysics() {
        _swimSpeed = _playerStats.GetStat(StatType.PlayerSpeedMax);
#if UNITY_EDITOR
        if (DEBUGIsGOD) {
            rb.MovePosition(rb.position + 40 * Time.fixedDeltaTime * _currentInput.normalized);
            return;
        }
#endif
        //Debug.Log(_waterDifficulty);
        _swimSpeed  *= _waterDifficulty;
        Vector2 input = Vector2.ClampMagnitude(_currentInput, 1f); // still lets player go slower if on controller
        Vector2 desiredVelocity = input * _swimSpeed;
        float accelerationTime = 1f;
        float decelerationTime = 0.5f;
        float time = input != Vector2.zero ? accelerationTime : decelerationTime;

        // How much velocity can change this frame
        float maxDelta = _swimSpeed / time * Time.fixedDeltaTime;

        rb.linearVelocity = Vector2.MoveTowards(
            rb.linearVelocity,
            desiredVelocity,
            maxDelta
        );

        // Hard safety cap
        rb.linearVelocity = Vector2.ClampMagnitude(rb.linearVelocity, Mathf.Max(_swimSpeed,15));
    }

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

    internal void DEBUGToggleGodMove() {
        if (!DEBUGIsGOD) {
            DEBUGIsGOD = true;
            _visualHandler.DEBUGSetGodMode(true);
        } else {
            DEBUGIsGOD = false;
            _visualHandler.DEBUGSetGodMode(false);
        }
    }
}
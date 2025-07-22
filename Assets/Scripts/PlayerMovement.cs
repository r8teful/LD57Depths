using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour, INetworkedPlayerModule {

    private Rigidbody2D rb;
    private Camera MainCam;
    public Transform insideSubTransform;
    public Transform groundCheck;
    //public CanvasGroup blackout;
    public static event Action<float,float> OnOxygenChanged;
    #region Movement Parameters

    [Header("Movement Parameters")]
    [Tooltip("Maximum speed the swimmer can reach.")]
    public float swimSpeed = 5f;
    public float walkSpeed = 5f;
    public float climbSpeed = 5f;

    [Tooltip("Acceleration force applied when moving.")]
    public float accelerationForce = 20f;
    public float dashForce = 8f;
    [SerializeField] private float dashDuration = 0.25f;          // How long the dash lasts (seconds)
    [SerializeField] private float cooldownDuration = 2f;        // Cooldown period after dash (seconds)
    [Tooltip("Deceleration force applied when no input is given, simulating water resistance.")]
    public float decelerationForce = 15f;

    [Tooltip("How quickly the swimmer can change direction.")]
    [Range(0f, 1f)] public float directionalChangeSpeed = 0.5f;

    public Vector2 ladderSensorSize;
    public float groundCheckRadius;
    public LayerMask ladderLayer;
    public LayerMask ladderExitLayer;
    public LayerMask groundLayer;

    #endregion
    public enum PlayerState {None, Swimming, Grounded, Cutscene, ClimbingLadder}
    private PlayerState _currentState;
    private InputManager _inputManager;
    private PlayerVisualHandler _visualHandler;
    [Header("Oxygen")]
    public float maxOxygen = 250f;
    public float oxygenDepletionRate = 1f;   // Oxygen loss per second underwater
    public float currentOxygen;
    private float maxHealth = 15; // amount in seconds the player can survive with 0 oxygen 
    private float playerHealth;
    public GameObject OxygenWarning;
    private bool peepPlayed;
    private bool _isFlashing;
    private Coroutine _flashCoroutine;
    private InputAction _playerMoveInput;
    private Collider2D _currentLadder;
    private bool _isOnLadder;
    private bool _isGrounded;
    private Vector2 _currentInput;
    private bool _isDashing;
    private float dashTimer = 0f;
    private float cooldownTimer = 0f;
    private Collider2D _topTrigger;
    private bool _dashUnlocked;
    private bool _isInsideOxygenZone;

    public int InitializationOrder => 80;
    internal bool CanUseTool() => _currentState == PlayerState.Swimming;

    public void InitializeOnOwner(NetworkedPlayer playerParent) {
        MainCam = Camera.main;
        MainCam.transform.SetParent(transform);
        MainCam.transform.localPosition = new Vector3(0, 0, -10);
        _inputManager = GetComponent<InputManager>();
        Debug.Log("Init input manager: " + _inputManager);
        _visualHandler = GetComponent<PlayerVisualHandler>();
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0;
        currentOxygen = maxOxygen;
        playerHealth = maxHealth;
        ChangeState(PlayerState.Swimming);
    }
    private void OnEnable() {
        // Subscribe to the event to recalculate stats when a NEW upgrade is bought
        UpgradeManager.OnUpgradePurchased += HandleUpgradePurchased;
        WorldVisibilityManager.OnLocalPlayerVisibilityChanged += PlayerVisibilityLayerChanged;
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
        WorldVisibilityManager.OnLocalPlayerVisibilityChanged -= PlayerVisibilityLayerChanged;
        UpgradeManager.OnUpgradePurchased -= HandleUpgradePurchased;
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
                HandleGroundedUpdate();
                break;
            case PlayerState.ClimbingLadder:
                HandleClimbingLadderUpdate();
                break;
        }
        _visualHandler.HandleVisualUpdate(_currentState, _currentInput);
        if (ShouldDepleteOxygen()) {
            DepleteOxygen();
        } else {
            ReplenishOxygen();
        }
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
                CheckEnvironment(); // For ladder checks
                break;
            case PlayerState.ClimbingLadder:
                HandleClimbingLadderPhysics();
                CheckEnvironment(); // For ladder checks
                break;
        }
        // Always update dash cooldown
        if (cooldownTimer > 0f) {
            cooldownTimer -= Time.fixedDeltaTime;
        }
    }

    private void HandleUpgradePurchased(UpgradeRecipeBase data) {
        if (data.type == UpgradeType.SpeedMax) {
            swimSpeed = UpgradeCalculator.CalculateUpgradeIncrease(swimSpeed, data as UpgradeRecipeValue);
            Debug.Log("Increase swimSpeed to " + swimSpeed);
        } else if (data.type == UpgradeType.SpeedAcceleration) {
            accelerationForce = UpgradeCalculator.CalculateUpgradeIncrease(accelerationForce, data as UpgradeRecipeValue);
            Debug.Log("Increase accelerationForce to " + accelerationForce);
        } else if (data.type == UpgradeType.OxygenMax) {
            maxOxygen = UpgradeCalculator.CalculateUpgradeIncrease(maxOxygen, data as UpgradeRecipeValue);
            Debug.Log("Increase maxOxygen to " + maxOxygen);
        } else if (data.type == UpgradeType.SpeedDashUnlock) {
            _dashUnlocked = true;
        }
    }
    #region SWIMMING
    private void HandleSwimmingUpdate() {
        if (_dashUnlocked && _inputManager.GetDashInput() && !_isDashing && cooldownTimer <= 0f && _currentInput != Vector2.zero) {
            // Start dashing
            _isDashing = true;
            dashTimer = dashDuration; // Reset dash timer
        }
        DepleteOxygen();
    }

    // --- State Handlers for FixedUpdate (Physics) ---
    private void HandleSwimmingPhysics() {
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
        if (_isDashing) {
            // Set velocity directly to dash speed in the direction of current input
            rb.linearVelocity = moveDirection * dashForce;

            // Decrease dash timer and check if dash should end
            dashTimer -= Time.fixedDeltaTime;
            if (dashTimer <= 0f) {
                _isDashing = false;
                cooldownTimer = cooldownDuration;
            }
        } else {
            float speed = rb.linearVelocity.magnitude;
            if (speed > swimSpeed) {
                // lerp the magnitude down toward swimSpeed each FixedUpdate
                float newSpeed = Mathf.Lerp(speed, swimSpeed, 5 * Time.fixedDeltaTime);
                rb.linearVelocity = rb.linearVelocity.normalized * newSpeed;
            }
        }
    }
    #endregion

    #region GROUNDED
    private void HandleGroundedUpdate() {
    ReplenishOxygen();
        if(_isOnLadder) {
            if(_topTrigger != null && _currentInput.y < -0.01f) {
                ChangeState(PlayerState.ClimbingLadder);
                return;
            } else if (_topTrigger == null && _currentInput.y > 0.01f) {
                if (_currentState != PlayerState.ClimbingLadder) {
                    ChangeState(PlayerState.ClimbingLadder);
                    return; // Important: Exit after changing state
                }

            }
        }
    }
    private void HandleGroundedPhysics() {
        // Horizontal movement
        rb.linearVelocity = new Vector2(_currentInput.x * walkSpeed, rb.linearVelocity.y);
    }
    #endregion

    #region FALLING

    #endregion

    #region LADDER
    private void HandleClimbingLadderUpdate() {
        ReplenishOxygen();
        if(_currentLadder == null) 
            return;
        if (!_isOnLadder) {
            // Exit ladder state
            ChangeState(_isGrounded ? PlayerState.Grounded : PlayerState.Grounded); // Could add falling state later
            return;
        }
        float feetY = groundCheck.position.y - groundCheckRadius;
        float ladderTopY = _currentLadder.bounds.max.y;
        float ladderBottomY = _currentLadder.bounds.min.y;
        // Exit conditions
        if (feetY > ladderTopY && _currentInput.y > 0) {
            ChangeState(PlayerState.Grounded); // Assumes platform at top
        } else if (feetY < ladderBottomY && _currentInput.y < 0) {
            ChangeState(PlayerState.Grounded); // Or falling
        }
    }
    private void HandleClimbingLadderPhysics() {
        // Use the Y component of the "Move" action for climbing ladders
        rb.linearVelocity = new Vector2(_currentInput.x, _currentInput.y) * climbSpeed;
    }
    private Collider2D[] LadderCheck() {
        Vector2 detectionCenter = (Vector2)transform.position;
        var ladders = Physics2D.OverlapBoxAll(detectionCenter, ladderSensorSize, 0, ladderLayer);
        SubLadder ladderComponent = null;
        if (ladders.Length > 0) {
            // Try and find the subladder component in the hitbox
            foreach (var collider in ladders) {
                ladderComponent = collider.GetComponentInParent<SubLadder>();
                if(ladderComponent != null) {
                    break; // Found it!
                }
            }
        }
        if (ladderComponent != null) {
            if(!ladderComponent.CanUse) 
                return null;  // if the found one is broken, we can't climb the ladder, so just say we didn't find anything
        }
        return ladders; // Passed checks and just return the ladder hitboxes we found
    }
    #endregion

    private void CheckEnvironment() {
        _isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);
        var ladderColliders = LadderCheck();
        // Check if we got any colliders back
        
        if (ladderColliders == null || ladderColliders.Length == 0) {
            _isOnLadder = false;
            _topTrigger = null;
            return;
        }
        // Loop through all colliders and assign appropriately
        foreach (var collider in ladderColliders) {
            if (collider != null) {
                if (collider.name == "TopTrigger") {
                    _topTrigger = collider;
                } else if (collider.name == "MainTrigger") {
                    _currentLadder = collider;
                }
            }
        }
        _isOnLadder = true;
        //Debug.Log("_isOnLadder: " + _currentLadder);
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
    }

    void OnStateEnter(PlayerState state, PlayerState oldState) {
        _visualHandler.OnStateEnter(state);
        switch (state) {
            case PlayerState.Swimming:
                rb.gravityScale = 0; // No gravity when swimming
                break;
            case PlayerState.Grounded:
                rb.gravityScale = 2;
                //MainCam.transform.SetParent(insideSubTransform);
                //MainCam.transform.localPosition = new Vector3(0, 0, -10);
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0); // Reset vertical velocity when entering from climb
                break;
            case PlayerState.ClimbingLadder:
                rb.gravityScale = 0;
                //rb.linearVelocity = Vector2.zero; // Stop movement when grabbing ladder
                if(_topTrigger != null) {
                    _topTrigger.GetComponentInParent<SubLadder>().SetPlatform(false);
                }
                if (_currentLadder != null) {
                    _currentLadder.GetComponentInParent<SubLadder>().SetPlatform(false);
                    // Align player with ladder if outside minXDistance
                    float ladderX = _currentLadder.bounds.center.x;
                    float playerX = transform.position.x;
                    float distance = Mathf.Abs(playerX - ladderX);
                    if (distance > _currentLadder.bounds.max.x) {
                        Vector3 newPosition = transform.position;
                        newPosition.x = ladderX;
                        transform.position = newPosition;
                    }
                }
                break;
        }
    }


    void OnStateExit(PlayerState state, PlayerState newState) {
        // Clean up from the state we are leaving
        switch (state) {
            case PlayerState.ClimbingLadder:
                _currentLadder.GetComponentInParent<SubLadder>().SetPlatform(true);
                rb.gravityScale = 2; // Restore gravity when leaving ladder
                if (newState == PlayerState.None || newState == PlayerState.Grounded) {
                    // If we were at the top and are now grounded, make sure we don't "pop" up
                    if (_isGrounded) {
                        rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0);
                    }
                }
                _currentLadder = null; // de-reference
                break;
            case PlayerState.Swimming:
               // rb.gravityScale = _originalGravityScale; // Restore gravity
                break;
                // No specific exit logic for Interior needed for now
        }
    }

    // --- Gizmos for Debugging ---
    void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }

        Gizmos.color = Color.yellow;
        // For OverlapBox, Gizmos.DrawWireCube needs center and full size
        Gizmos.DrawWireCube(transform.position, new Vector3(ladderSensorSize.x, ladderSensorSize.y, 0));
    }
    // --- Helper Functions ---


    private bool ShouldDepleteOxygen() {
        if( _currentState == PlayerState.Swimming ) {
            if (_isInsideOxygenZone) {
                return false; 
            } else {
                return true; 
            }
        } else if (_currentState == PlayerState.Grounded || _currentState == PlayerState.ClimbingLadder) {
            return false; // Replenish oxygen when grounded
        }
        return true;
    }
    void DepleteOxygen() {
        currentOxygen -= oxygenDepletionRate * Time.deltaTime;
        currentOxygen = Mathf.Clamp(currentOxygen, 0, maxOxygen);
        UpdateSlider();
        if(currentOxygen <= 10 && !peepPlayed) {

            if(AudioController.Instance != null) AudioController.Instance.PlaySound2D("PeepPeep", 1f);
            peepPlayed = true;
            //SliderFlash(true);
        }
        if (currentOxygen <= 0) {
            // Slowly fade out and then teleport player back to base?
            playerHealth -= 1 * Time.deltaTime;
            if(playerHealth <= 0) {
                // Todo remove resources?
                Debug.LogWarning("No logic for resource removement");
                Resurect();
            }
            UpdateFadeOutVisual();
        }
    }
    void ReplenishOxygen() {
        peepPlayed = false;
        //SliderFlash(false);
        currentOxygen += oxygenDepletionRate * 50 * Time.deltaTime;
        currentOxygen = Mathf.Clamp(currentOxygen, 0, maxOxygen);
        playerHealth = maxHealth;
        UpdateSlider();
        UpdateFadeOutVisual();
    }
    // Call this function to start or stop the flashing
    public void SliderFlash(bool shouldFlash) {
        if (OxygenWarning == null) {
            Debug.LogWarning("SliderFlash: sliderToFlash GameObject is not assigned! Please assign it in the Inspector.");
            return;
        }

        if (shouldFlash) {
            if (!_isFlashing) // Don't start a new coroutine if already flashing
            {
                _isFlashing = true;
                _flashCoroutine = StartCoroutine(FlashCoroutine());
            }
        } else {
            if (_isFlashing) // Only stop if currently flashing
            {
                _isFlashing = false;
                StopCoroutine(_flashCoroutine);
                OxygenWarning.SetActive(false); // Ensure it's visible when stopping the flash
            }
        }
    }

    private IEnumerator FlashCoroutine() {
        while (_isFlashing) {
            // Toggle the active state of the GameObject
            OxygenWarning.SetActive(!OxygenWarning.activeSelf);

            // Wait for the flashSpeed duration
            yield return new WaitForSeconds(0.2f);
        }
    }

    public void DEBUGSet0Oxygen() {
        currentOxygen = 0;
    }
    public void DEBUGPlayerPassOut() {
        currentOxygen = 1;
        playerHealth = 1;
    }
    public void DEBUGinfOx() {
        maxOxygen= 9999999;
        currentOxygen = 999999;
    }
    private void Resurect() {
        playerHealth = maxHealth;
        currentOxygen = maxOxygen;
        UpdateFadeOutVisual();
    }

    private void UpdateFadeOutVisual() {
        float healthRatio = playerHealth / maxHealth;
        float easedValue = 1 - Mathf.Pow(healthRatio, 2); // Quadratic ease-out
        
        //blackout.alpha = easedValue;
    }
    private void UpdateSlider() {
        OnOxygenChanged?.Invoke(currentOxygen,maxOxygen);
    }

    internal void SetOxygenZone(bool v) {
        _isInsideOxygenZone = v;
    }
}
using FishNet.Object;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;

public class PlayerMovement : NetworkBehaviour {
    public static PlayerMovement LocalInstance { get; private set; } // Singleton for local player
    private Animator animator;
    private string currentAnimation = "";
    private Rigidbody2D rb;
    private SpriteRenderer sprite;
    private Camera MainCam;
    public Transform insideSubTransform;
    public Transform groundCheck;
    public Collider2D playerSwimCollider;
    public Collider2D playerWalkCollider;
    //public CanvasGroup blackout;
    public Light2D lightSpot;
    public static event Action<float,float> OnOxygenChanged;
    #region Movement Parameters

    [Header("Movement Parameters")]
    [Tooltip("Maximum speed the swimmer can reach.")]
    public float swimSpeed = 5f;
    public float walkSpeed = 5f;
    public float climbSpeed = 5f;

    [Tooltip("Acceleration force applied when moving.")]
    public float accelerationForce = 20f;

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
    [Header("Oxygen")]
    public float maxOxygen = 250f;
    public float oxygenDepletionRate = 1f;   // Oxygen loss per second underwater
    private float lightStartIntensity;
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
    private Collider2D _topTrigger;

    public override void OnStartClient() {
        base.OnStartClient();
        if (base.IsOwner) // Check if this NetworkObject is owned by the local client
        {
            Debug.Log("We are the owner!");
            LocalInstance = this;
            MainCam = Camera.main;
            MainCam.transform.SetParent(transform);
            MainCam.transform.localPosition = new Vector3(0,0,-10);
            ChangeState(PlayerState.Swimming);
            _inputManager = GetComponent<InputManager>();
            // Enable input, camera controls ONLY for the local player
            // Example: GetComponent<PlayerInputHandler>().enabled = true;
            // Example: playerCamera.SetActive(true);
        } else {
            // Disable controls for remote players on this client
            Debug.Log("We are NOT the owner!");
            GetComponent<PlayerMovement>().enabled = false;
            // Example: GetComponent<PlayerInputHandler>().enabled = false;
        }

        // Try to find the WorldGenerator - might need adjustment based on your scene setup
       // worldGenerator = FindObjectOfType<WorldGenerator>();
       // if (worldGenerator == null) {
       //     Debug.LogError("PlayerController could not find WorldGenerator!");
       // }
    }
    private void OnEnable() {
        // Subscribe to the event to recalculate stats when a NEW upgrade is bought
        UpgradeManager.OnUpgradePurchased += HandleUpgradePurchased;
    }


    private void OnDisable() {
        UpgradeManager.OnUpgradePurchased -= HandleUpgradePurchased;
    }

    private void Start() {
        //LocalInstance = this;
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        sprite = GetComponent<SpriteRenderer>();
        rb.gravityScale = 0; // Disable default gravity
        if (lightSpot != null)
            lightStartIntensity = lightSpot.intensity;
        // oxygen and slider
        currentOxygen = maxOxygen;
        playerHealth = maxHealth;
    }

    private void OnUpgraded(UpgradeType type) {
        if(type == UpgradeType.MaxSpeed) {
            swimSpeed = UpgradeManager.Instance.GetUpgradeValue(type);
            // Also increase acceleration slightly
            accelerationForce += 0.1f;
        } else if (type == UpgradeType.OxygenCapacity) {
            maxOxygen = UpgradeManager.Instance.GetUpgradeValue(type);
            currentOxygen = maxOxygen;
            UpdateSlider();
        } else if(type == UpgradeType.Light) {
            lightSpot.pointLightOuterRadius = UpgradeManager.Instance.GetUpgradeValue(type);
        }
        
    }

    void Update() {
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
    }

    void FixedUpdate() {
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
    }

    private void HandleUpgradePurchased(UpgradeRecipeBase data) {
        if (data.type == UpgradeType.MaxSpeed) {
            swimSpeed = UpgradeCalculator.CalculateUpgradeIncrease(swimSpeed, data as UpgradeRecipeValue);
            Debug.Log("Increase swimSpeed to " + swimSpeed);
        } else if (data.type == UpgradeType.Acceleration) {
            accelerationForce = UpgradeCalculator.CalculateUpgradeIncrease(accelerationForce, data as UpgradeRecipeValue);
            Debug.Log("Increase accelerationForce to " + accelerationForce);
        } else if (data.type == UpgradeType.OxygenCapacity) {
            maxOxygen = UpgradeCalculator.CalculateUpgradeIncrease(maxOxygen, data as UpgradeRecipeValue);
            Debug.Log("Increase maxOxygen to " + maxOxygen);
        }
    }
    #region SWIMMING
    private void HandleSwimmingUpdate() {
        if (_currentInput.magnitude != 0) {
            ChangeAnimation("Swim");
        } else {
            ChangeAnimation("SwimIdle");
        }
        FlipSprite(_currentInput.x);
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
        // Limit maximum speed
        rb.linearVelocity = Vector2.ClampMagnitude(rb.linearVelocity, swimSpeed);
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
        FlipSprite(_currentInput.x);
        ChangeAnimation(Mathf.Abs(_currentInput.x) > 0.01f ? "Walk" : "Idle");
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

        // Climbing animation logic
        if (Mathf.Abs(_currentInput.y) > 0.01f) {
            ChangeAnimation("Climb");
        } else {
            ChangeAnimation("ClimbIdle"); // Or just stop animator speed for Climb animation
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
        SetHitbox(state);
        switch (state) {
            case PlayerState.Swimming:
                rb.gravityScale = 0; // No gravity when swimming
                SetLights(true);
                break;
            case PlayerState.Grounded:
                rb.gravityScale = 2;
                SetLights(false);
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

    private void SetHitbox(PlayerState state) {
        switch (state) {
            case PlayerState.None:
                break;
            case PlayerState.Swimming:
                // Horizontal
                playerSwimCollider.enabled = true;
                playerWalkCollider.enabled = false;
                break;
            case PlayerState.Grounded:
                // Vertical
                playerSwimCollider.enabled = false;
                playerWalkCollider.enabled = true;
                break;
            case PlayerState.Cutscene:
                break;
            case PlayerState.ClimbingLadder:
                break;
            default:
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
    void FlipSprite(float horizontalInput) {
        if (horizontalInput > 0.01f) {
            sprite.flipX = false;
        } else if (horizontalInput < -0.01f) {
            sprite.flipX = true;
        }
    }
    void ChangeAnimation(string animationName) {
        if (animator == null)
            return;
        if (currentAnimation != animationName) {
            currentAnimation = animationName;
            animator.CrossFade(animationName, 0.2f, 0);
            // animator.Play(animationName); // Or use this one instead of crossfade
        }
    }
    private void SetLights(bool setOn) {
        if (setOn) {
            lightSpot.intensity = lightStartIntensity;
        } else {
            lightSpot.intensity = 0;
        }
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
    void ReplenishOxygen() {
        peepPlayed = false;
        //SliderFlash(false);
        currentOxygen += oxygenDepletionRate*50 * Time.deltaTime;
        currentOxygen = Mathf.Clamp(currentOxygen, 0, maxOxygen);
        playerHealth = maxHealth;
        UpdateSlider();
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
}
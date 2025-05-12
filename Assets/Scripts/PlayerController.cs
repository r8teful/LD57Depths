using FishNet.Object;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;

public class PlayerController : NetworkBehaviour {
    // public float moveSpeed = 5f; // Base speed of swimming
    // public float acceleration = 2f; // How quickly the character reaches full speed
    // public float deceleration = 2f; // How quickly the character slows down
    // public float buoyancy = 1f; // Simulates slight downwards force
    // public float smoothRotation = 5f; // How smoothly the character rotates
    // public float collisionRadius = 0.2f; // Size of the collision check
    public static PlayerController LocalInstance { get; private set; } // Singleton for local player
    private Animator animator;
    private string currentAnimation = "";
    private Rigidbody2D rb;
    private CapsuleCollider2D colliderPlayer;
    private SpriteRenderer sprite;
    private MiningGun miningGun;
    private Camera MainCam;
    public Transform insideSubTransform;
    public Transform groundCheck;
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
    public Transform ladderTopExitCheckPoint;
    public Vector3 ladderSensorOffset = Vector3.zero;         // Offset for the ladder sensor from player pivot
    public float groundCheckRadius;
    public float ladderTopExitCheckRadius;
    public LayerMask ladderLayer;
    public LayerMask ladderExitLayer;
    public LayerMask groundLayer;

    #endregion



    private Vector2 _currentInput;
    public enum PlayerState {None, Swimming, Interior, Cutscene, ClimbingLadder}
    private PlayerState _currentState = PlayerState.Swimming;

    [Header("Oxygen")]
    public float maxOxygen = 100f;
    public float oxygenDepletionRate = 1f;   // Oxygen loss per second underwater
    public float currentOxygen;
    private float maxHealth = 15; // amount in seconds the player can survive with 0 oxygen 
    private float playerHealth;
    public GameObject OxygenWarning;
    private bool peepPlayed;
    private bool _isFlashing;
    private Coroutine _flashCoroutine;
    private PlayerInput _playerInput;
    private InputAction _playerMoveInput;
    private bool _isOnLadder;
    private GameObject _currentLadder;
    private bool _isGrounded;
    private bool _canExitAtLadderTop;

    public override void OnStartClient() {
        base.OnStartClient();
        if (base.IsOwner) // Check if this NetworkObject is owned by the local client
        {
            Debug.Log("We are the owner!");
            LocalInstance = this;
            MainCam = Camera.main;
            MainCam.transform.SetParent(transform);
            MainCam.transform.localPosition = new Vector3(0,0,-10);
            _playerInput = GetComponent<PlayerInput>();
            _playerMoveInput = _playerInput.actions["Move"];
            _playerMoveInput.performed += OnInteractPerformed;
            // Enable input, camera controls ONLY for the local player
            // Example: GetComponent<PlayerInputHandler>().enabled = true;
            // Example: playerCamera.SetActive(true);
        } else {
            // Disable controls for remote players on this client
            Debug.Log("We are NOT the owner!");
            GetComponent<PlayerController>().enabled = false;
            // Example: GetComponent<PlayerInputHandler>().enabled = false;
        }

        // Try to find the WorldGenerator - might need adjustment based on your scene setup
       // worldGenerator = FindObjectOfType<WorldGenerator>();
       // if (worldGenerator == null) {
       //     Debug.LogError("PlayerController could not find WorldGenerator!");
       // }
    }
    public override void OnStopClient() {
        base.OnStopClient();
        _playerMoveInput.performed -= OnInteractPerformed;
    }

    private void Start() {
        //LocalInstance = this;
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        sprite = GetComponent<SpriteRenderer>();
        miningGun = GetComponentInChildren<MiningGun>();
        colliderPlayer = GetComponent<CapsuleCollider2D>();
        rb.gravityScale = 0; // Disable default gravity
        
        // oxygen and slider
        currentOxygen = maxOxygen;
        playerHealth = maxHealth;
    }



    private void Awake() {
        UpgradeManager.UpgradeBought += OnUpgraded;
    }
    private void OnDestroy() {
        UpgradeManager.UpgradeBought -= OnUpgraded;
    }

    private void OnUpgraded(UpgradeType type) {
        if(type == UpgradeType.MovementSpeed) {
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
        _currentInput = _playerMoveInput.ReadValue<Vector2>();
        // Handle state-specific logic in Update (mostly non-physics like animations, input processing)
        switch (_currentState) {
            case PlayerState.Swimming:
                HandleSwimmingUpdate();
                break;
            case PlayerState.Interior:
                HandleInteriorUpdate();
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
            case PlayerState.Interior:
                HandleInteriorPhysics();
                CheckEnvironment(); // For ladder checks
                break;
            case PlayerState.ClimbingLadder:
                HandleClimbingLadderPhysics();
                CheckEnvironment(); // For ladder checks
                break;
        }
    }

    // --- State Handlers for Update ---
    private void HandleSwimmingUpdate() {
        if (_currentInput.magnitude != 0) {
            ChangeAnimation("Swim");
        } else {
            ChangeAnimation("SwimIdle");
        }
        FlipSprite(_currentInput.x);
        DepleteOxygen();
    }

    private void HandleInteriorUpdate() {
        ReplenishOxygen();
        if(_isOnLadder && _currentInput.y > 0.01f) {
            // Enter ladder state
            // Check if already climbing to prevent re-entry issues
            if (_currentState != PlayerState.ClimbingLadder) {
                // Small check: Don't allow initiating climb if standing on top of a ladder and pressing up
                // This needs a more robust "is at top of ladder" check, but for now, this simple condition helps
                // Or just allow it and let them "climb" into the air slightly if ladder collider is short.
                // For now, simpler is better.
                ChangeState(PlayerState.ClimbingLadder);
                return; // Important: Exit after changing state
            }
        }
        FlipSprite(_currentInput.x);
        ChangeAnimation(Mathf.Abs(_currentInput.x) > 0.01f ? "Walk" : "Idle");
    }

    private void HandleClimbingLadderUpdate() {
        ReplenishOxygen();
        if (!_isOnLadder) {
            ChangeState(_isGrounded ? PlayerState.Interior : PlayerState.Interior); // Or Falling state
            return;
        }
        // Condition 2: At a ladder top exit point and moving to dismount
        if (_canExitAtLadderTop) {
            // If pressing UP at the top OR moving horizontally significantly
            if (_currentInput.y > 0.1f || Mathf.Abs(_currentInput.x) > 0.1f) {
                // Optional: Small positional adjustment to ensure clearing the ladder top
                // This depends heavily on your ladder top platform colliders
                // Example: transform.position += Vector3.up * 0.1f;
                ChangeState(_isGrounded ? PlayerState.Interior : PlayerState.Interior); // Or Walking/Falling
                return;
            }
        }
        // Condition 3: Reached bottom of ladder and pressing down while grounded
        if (_isGrounded && _currentInput.y < -0.1f && _isOnLadder && !_canExitAtLadderTop) // Ensure not at top
        {
            // Check if player's feet are roughly at or below ladder bottom
            // This is a simplified check; a more robust one would compare player Y to ladder bottom Y
            ChangeState(PlayerState.Interior);
            return;
        }

        // Climbing animation logic
        if (Mathf.Abs(_currentInput.y) > 0.01f) {
            ChangeAnimation("Climb");
        } else {
            ChangeAnimation("ClimbIdle"); // Or just stop animator speed for Climb animation
        }
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

    private void HandleInteriorPhysics() {
        // Horizontal movement
        rb.linearVelocity = new Vector2(_currentInput.x * walkSpeed, rb.linearVelocity.y);
    }

    private void HandleClimbingLadderPhysics() {
        // Use the Y component of the "Move" action for climbing ladders
        rb.linearVelocity = new Vector2(_currentInput.x, _currentInput.y) * climbSpeed;
    }

    private void CheckEnvironment() {
        _isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);

        Collider2D ladderCollider = Physics2D.OverlapBox((Vector2)transform.position + ladderSensorSize, ladderSensorSize, 0, ladderLayer);
        //Collider2D ladderCollider = Physics2D.OverlapCollider(ladderCollider,ladderLayer,results);
        _isOnLadder = ladderCollider != null;
        Debug.Log("is On Ladder: " + _isOnLadder);
        if (_isOnLadder) {
            _currentLadder = ladderCollider.gameObject; 
        } else {
            _currentLadder = null;
        }
        _canExitAtLadderTop = Physics2D.OverlapCircle(ladderTopExitCheckPoint.position, ladderTopExitCheckRadius, ladderExitLayer);
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
        switch (state) {
            case PlayerState.Swimming:
                rb.gravityScale = 0; // No gravity when swimming
                SetLights(true);
                // Potentially change physics material for water drag if needed
                break;
            case PlayerState.Interior:
                rb.gravityScale = 2;
                SetLights(false);
                if (miningGun != null)
                    miningGun.CanShoot = false;
                //MainCam.transform.SetParent(insideSubTransform);
                //MainCam.transform.localPosition = new Vector3(0, 0, -10);
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0); // Reset vertical velocity when entering from climb
                break;
            case PlayerState.ClimbingLadder:
                rb.gravityScale = 0;
                //rb.linearVelocity = Vector2.zero; // Stop movement when grabbing ladder
                if (_currentLadder != null) {
                    // Snap to ladder's X position for better feel
                    //transform.position = new Vector2(_currentLadder.transform.position.x, transform.position.y);
                }
                break;
        }
    }

    void OnStateExit(PlayerState state, PlayerState newState) {
        // Clean up from the state we are leaving
        switch (state) {
            case PlayerState.ClimbingLadder:
                 rb.gravityScale = 2; // Restore gravity when leaving ladder
                if (newState == PlayerState.None || newState == PlayerState.Interior) {
                    // If we were at the top and are now grounded, make sure we don't "pop" up
                    if (_canExitAtLadderTop && _isGrounded) {
                        rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0);
                    }
                }
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
        // Ladder Top Exit Check Gizmo
        if (ladderTopExitCheckPoint != null) {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(ladderTopExitCheckPoint.position, ladderTopExitCheckRadius);
        }
    }
    // --- Helper Functions ---
    void FlipSprite(float horizontalInput) {
        if (horizontalInput > 0.01f) {
            sprite.flipX = false;
            if (miningGun != null)
                miningGun.Flip(false);
        } else if (horizontalInput < -0.01f) {
            sprite.flipX = true;
            if (miningGun != null)
                miningGun.Flip(true);
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
            lightSpot.gameObject.SetActive(true);
        } else {
            lightSpot.gameObject.SetActive(false);
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
                // Lose some resources and go back to base
                if (UpgradeManager.Instance != null)
                    UpgradeManager.Instance.RemoveAllResources(0.5f);
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
    private void OnInteractPerformed(InputAction.CallbackContext context) {
        if (_currentState == PlayerState.Interior && _isOnLadder && _currentLadder != null) {
            //ChangeState(PlayerState.ClimbingLadder); // We could make this an actuall button later
        } else if (_currentState == PlayerState.ClimbingLadder) {
            // Option 1: Exit ladder with interact button
            //ChangeState(PlayerState.Interior); // Same here!!
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
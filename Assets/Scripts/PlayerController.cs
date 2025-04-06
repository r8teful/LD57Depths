using System;
using UnityEngine;

public class PlayerController : StaticInstance<PlayerController> {
   // public float moveSpeed = 5f; // Base speed of swimming
   // public float acceleration = 2f; // How quickly the character reaches full speed
   // public float deceleration = 2f; // How quickly the character slows down
   // public float buoyancy = 1f; // Simulates slight downwards force
   // public float smoothRotation = 5f; // How smoothly the character rotates
   // public float collisionRadius = 0.2f; // Size of the collision check

    private Animator animator;
    private string currentAnimation = "";
    private Rigidbody2D rb;
    private Vector2 velocity;
    private SpriteRenderer sprite;
    public Camera MainCam;
    public Transform insideSubTransform;
    #region Movement Parameters

    [Header("Movement Parameters")]
    [Tooltip("Maximum speed the swimmer can reach.")]
    public float swimSpeed = 5f;

    [Tooltip("Acceleration force applied when moving.")]
    public float accelerationForce = 20f;

    [Tooltip("Deceleration force applied when no input is given, simulating water resistance.")]
    public float decelerationForce = 15f;

    [Tooltip("How quickly the swimmer can change direction.")]
    [Range(0f, 1f)] public float directionalChangeSpeed = 0.5f;

#endregion

    #region Buoyancy Parameters (Optional)

    [Header("Buoyancy (Optional)")]
    [Tooltip("If enabled, adds a slight upward force to simulate natural buoyancy.")]
    public bool useBuoyancy = true;

    [Tooltip("Upward force applied to simulate buoyancy.")]
    public float buoyancyForce = 5f;

    #endregion

    #region Bobbing Effect (Optional Visual)

    [Header("Bobbing Effect (Optional Visual)")]
    [Tooltip("If enabled, adds a gentle bobbing motion for visual feedback.")]
    public bool useBobbingEffect = true;

    [Tooltip("Speed of the bobbing motion.")]
    public float bobbingSpeed = 2f;

    [Tooltip("Amplitude of the bobbing motion.")]
    public float bobbingAmplitude = 0.1f;

    private float bobbingTimer = 0f;
    private Vector3 originalLocalPosition;

    #endregion

    private Vector2 currentInput;
    public enum PlayerState { Swimming, Outside }
    public PlayerState CurrentState = PlayerState.Swimming;

    [Header("Outside")]
    // Outside state curve control points (set these via inspector or during initialization)
    public Transform outsideStart;
    public Transform outsideTurning;
    public Transform outsideEnd;
    public float outsideSpeed = 1f;  // How fast the player moves along the curve
    private float outsideT = 0f;     // Parameter between 0 and 1 for the curve

    void Start() {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        sprite = GetComponent<SpriteRenderer>();
        rb.gravityScale = 0; // Disable default gravity
        originalLocalPosition = transform.localPosition; // Store initial position for bobbing
    }
    void ChangeAnimation(string animation) {
        if(currentAnimation != animation) {
            currentAnimation = animation;
            animator.CrossFade(animation,0.2f,0);
        }
    }


    protected override void Awake() {
        base.Awake();
        UpgradeManager.UpgradeBought += OnUpgraded;
    }
    private void OnDestroy() {
        UpgradeManager.UpgradeBought -= OnUpgraded;
    }

    private void OnUpgraded() {
        swimSpeed = UpgradeManager.Instance.GetUpgradeValue(UpgradeType.MovementSpeed);
    }


    void Update() {
        // Get input for movement
        currentInput = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));

        if (CurrentState == PlayerState.Swimming) {
            //if (!rb.simulated) rb.simulated = true; 
            // Swimming animations and sprite flipping
            if (currentInput.magnitude != 0) {
                ChangeAnimation("Swim");
            } else {
                ChangeAnimation("SwimIdle");
            }
            if (currentInput.x > 0) {
                sprite.flipX = false;
            } else if (currentInput.x < 0) {
                sprite.flipX = true;
            }
            // Optional Bobbing Effect
            if (useBobbingEffect) {
                HandleBobbing();
            }
        } else if (CurrentState == PlayerState.Outside) {
            // Update the parameter along the curve based on horizontal input.
            outsideT += currentInput.x * outsideSpeed * Time.deltaTime;
            outsideT = Mathf.Clamp01(outsideT);

            // Update sprite direction and animation as before.
            if (currentInput.x > 0) {
                sprite.flipX = false;
            } else if (currentInput.x < 0) {
                sprite.flipX = true;
            }
            ChangeAnimation(Mathf.Abs(currentInput.x) > 0 ? "Walk" : "Idle");
        }
    }

    void FixedUpdate() {
        if (CurrentState == PlayerState.Swimming) {
            HandleMovement();
            // Optional Buoyancy
            if (useBuoyancy) {
                HandleBuoyancy();
            }
        } else if (CurrentState == PlayerState.Outside) {
            // Snap player to the curve position
            Vector3 newPos = EvaluateBezier(outsideStart.position, outsideTurning.position, outsideEnd.position, outsideT);
            rb.MovePosition(newPos);
        }
    }
    // A helper function to evaluate a quadratic Bézier curve
    Vector3 EvaluateBezier(Vector3 p0, Vector3 p1, Vector3 p2, float t) {
        float oneMinusT = 1 - t;
        return oneMinusT * oneMinusT * p0 + 2 * oneMinusT * t * p1 + t * t * p2;
    }
    void HandleMovement() {
        // Normalize input to prevent faster diagonal movement
        Vector2 moveDirection = currentInput.normalized;

        if (moveDirection != Vector2.zero) {
            // Apply acceleration force in the input direction
            rb.AddForce(moveDirection * accelerationForce);
        } else {
            // Apply deceleration (water resistance) when no input
            if (rb.linearVelocity.magnitude > 0.01f) // Avoid tiny movements when nearly stopped
            {
                Vector2 oppositeDirection = -rb.linearVelocity.normalized;
                rb.AddForce(oppositeDirection * decelerationForce);
            } else {
                rb.linearVelocity = Vector2.zero; // Stop completely if very slow to avoid drift
            }
        }

        // Limit maximum speed
        rb.linearVelocity = Vector2.ClampMagnitude(rb.linearVelocity, swimSpeed);
    }

    void HandleBuoyancy() {
        // Apply a constant upward force
        rb.AddForce(Vector2.up * buoyancyForce);
    }

    void HandleBobbing() {
        bobbingTimer += Time.deltaTime * bobbingSpeed;
        float bobbingOffset = Mathf.Sin(bobbingTimer) * bobbingAmplitude;
        transform.position = Vector3.up * bobbingOffset;
    }

    internal void SetState(PlayerState state) {
        CurrentState = state;
        if(state == PlayerState.Outside) {

           MainCam.transform.SetParent(insideSubTransform);
            MainCam.transform.localPosition = new Vector3(0, 0, -10);
        } else {
            MainCam.transform.SetParent(transform);
            MainCam.transform.localPosition = new Vector3(0, 0, -10);

        }
    }
}
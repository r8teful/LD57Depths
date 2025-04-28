using FishNet.Object;
using System;
using System.Collections;
using UnityEngine;
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
    private Vector2 velocity;
    private SpriteRenderer sprite;
    private MiningGun miningGun;
    private Camera MainCam;
    public Transform insideSubTransform;
    //public CanvasGroup blackout;
    public Light2D lightSpot;
    public static event Action<float,float> OnOxygenChanged;
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
    public enum PlayerState { Swimming, Ship, Cutscene, None}
    private PlayerState _currentState = PlayerState.Swimming;

    [Header("Outside")]
    // Outside state curve control points (set these via inspector or during initialization)
    public Transform outsideStart;
    public Transform outsideTurning;
    public Transform outsideEnd;
    public float outsideSpeed = 1f;  // How fast the player moves along the curve
    private float outsideT = 0f;     // Parameter between 0 and 1 for the curve
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
    
    public override void OnStartClient() {
        base.OnStartClient();
        if (base.IsOwner) // Check if this NetworkObject is owned by the local client
        {
            Debug.Log("We are the owner!");
            LocalInstance = this;
            MainCam = Camera.main;
            MainCam.transform.SetParent(transform);
            MainCam.transform.localPosition = new Vector3(0,0,-10);
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
    
    private void Start() {
        //LocalInstance = this;
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        sprite = GetComponent<SpriteRenderer>();
        miningGun = GetComponentInChildren<MiningGun>();
        colliderPlayer = GetComponent<CapsuleCollider2D>();
        rb.gravityScale = 0; // Disable default gravity
        originalLocalPosition = transform.localPosition; // Store initial position for bobbing
        
        // oxygen and slider
        currentOxygen = maxOxygen;
        playerHealth = maxHealth;
    }
    void ChangeAnimation(string animation) {
        if(currentAnimation != animation) {
            currentAnimation = animation;
            animator.CrossFade(animation,0.2f,0);
        }
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
        currentInput = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));

        if (_currentState == PlayerState.Swimming) {
            //if (!rb.simulated) rb.simulated = true; 
            // Swimming animations and sprite flipping
            if (currentInput.magnitude != 0) {
                ChangeAnimation("Swim");
            } else {
                ChangeAnimation("SwimIdle");
            }
            if (currentInput.x > 0) {
                sprite.flipX = false;
                if(miningGun!=null) miningGun.Flip(false);
            } else if (currentInput.x < 0) {
                sprite.flipX = true;
                if (miningGun != null) miningGun.Flip(true);
            }
            // Optional Bobbing Effect
            if (useBobbingEffect) {
                HandleBobbing();
            }
            DepleteOxygen();
        } else if (_currentState == PlayerState.Ship) {
            // Update the parameter along the curve based on horizontal input.
            outsideT += currentInput.x * outsideSpeed * Time.deltaTime;
            outsideT = Mathf.Clamp01(outsideT);

            ReplenishOxygen();
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
        if (_currentState == PlayerState.Swimming) {
            HandleMovement();
            // Optional Buoyancy
            if (useBuoyancy) {
                HandleBuoyancy();
            }
        } else if (_currentState == PlayerState.Ship) {
            // Snap player to the curve position
            Vector3 newPos = EvaluateBezier(outsideStart.position, outsideTurning.position, outsideEnd.position, outsideT);
            rb.MovePosition(newPos);
        } else if (_currentState == PlayerState.Cutscene) {
            // Follow sub position;
            rb.MovePosition(Submarine.Instance.transform.position);
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
        Debug.Log("Setting state to:" + state);
        _currentState = state;
        if(state == PlayerState.Ship) {
            SetLights(false);
            if (miningGun != null) miningGun.CanShoot = false;
            outsideT = 0.5f;
            rb.linearVelocity = Vector2.zero;
            MainCam.transform.SetParent(insideSubTransform);
            MainCam.transform.localPosition = new Vector3(0, 0, -10);
        } else { // Camera should also follow player during the cutscene
            if (state != PlayerState.Cutscene) {
                if (miningGun != null) miningGun.CanShoot = true;
            } 
            SetLights(true);
            MainCam.transform.SetParent(transform);
            MainCam.transform.localPosition = new Vector3(0, 0, -10);
            rb.linearVelocity = Vector2.zero;
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
            SliderFlash(true);
        }
        if (currentOxygen <= 0) {
            // Slowly fade out and then teleport player back to base?
            playerHealth -= 1 * Time.deltaTime;
            if(playerHealth <= 0) {
                // Lose some resources and go back to base
                if (UpgradeManager.Instance != null)
                    UpgradeManager.Instance.RemoveAllResources(0.5f);
                if(Submarine.Instance != null)
                    Submarine.Instance.EnterSub(); // Will also set player state
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
        SliderFlash(false);
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
    internal void CutsceneEnd() {
        colliderPlayer.enabled = true;
        SetState(PlayerState.Swimming);
    }

    internal void CutsceneStart() {
        SetState(PlayerState.Cutscene);
        colliderPlayer.enabled = false;
    }
}
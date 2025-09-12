using System;
using UnityEngine;
using Random = UnityEngine.Random;

public class MiningLazer : MiningBase {
    [Header("Raycast Gun Settings")]
    public float innerSpotAngle = 5f;
    public float outerSpotAngle = 30f;

    [SerializeField] private float maxRotationSpeed = 180f;

    [Tooltip("If you start firing again within this time, the lazer continues from its last angle.")]
    [SerializeField] private float directionMemoryTime = 1.5f;

    [Tooltip("Strength of the knockback applied to the player when firing")]
    [SerializeField] private float knockbackStrength = 2.5f;
    public override float Range { get; set; } = 10f;
    public override float DamagePerHit { get; set; } = 10f;
    public override GameObject GO => gameObject;

    public float falloffStrength = 0.1f; // Higher values = faster falloff
    public bool CanMine { get; set; } = true;
     private IToolVisual _toolVisual;
    public override IToolVisual toolVisual => _toolVisual;

    public override ToolType toolType => ToolType.Lazer;

    public static event Action<Vector2> OnPlayerKnockbackRequested;

    // --- State Variables ---
    private Vector2 _currentDirection;
    private Vector2 _lastKnownDirection;
    private float _timeToolStopped;
    private bool _isFirstShot; // Flag to handle initial direction logic

    private void Awake() {
        Debug.Log("Start called on: " + toolType);
        if (gameObject.TryGetComponent<IToolVisual>(out var c)){
            _toolVisual = c;
        } else {
            Debug.LogError("Could not find minglazerVisual on gameobject!");
        }
    }
    protected override void Update() {
        if (!_isMining) {
            return;
        }
        // --- Smooth Rotation Logic (runs every frame) ---
        Vector2 toolPosition = transform.position;
        Vector2 targetPos = _inputManager.GetAimWorldInput();
        Vector2 targetDirection = (targetPos - toolPosition).normalized;

        // Handle the "first shot" logic to either snap or use memory
        if (_isFirstShot) {
            if (Time.time - _timeToolStopped < directionMemoryTime && _lastKnownDirection.sqrMagnitude > 0) {
                _currentDirection = _lastKnownDirection;
            } else {
                _currentDirection = targetDirection; // Snap instantly on the first frame
            }
            _isFirstShot = false;
        } else {
            // Smoothly rotate towards the target direction over time
            float maxAngleDelta = maxRotationSpeed * Time.deltaTime; // Use Time.deltaTime for per-frame smoothness
            _currentDirection = Vector3.RotateTowards(_currentDirection, targetDirection, maxAngleDelta * Mathf.Deg2Rad, 0.0f).normalized;
        }

        toolVisual.HandleVisualUpdate(_currentDirection, base._inputManager); // using new "lagging" direction now

        // Knockback
        if (knockbackStrength > 0) {
            // Apply knockback in the opposite direction of the lazer
            Vector2 knockbackForce = -_currentDirection.normalized * knockbackStrength * Time.deltaTime;

            // Invoke the event, sending the force vector. Any listeners will react.
            OnPlayerKnockbackRequested?.Invoke(knockbackForce);
        }
    }
    Vector2 GetConeRayDirection(Vector2 baseDirection) {
        float randomAngle = Random.Range(-outerSpotAngle / 2f, outerSpotAngle / 2f); // Angle variation within outer cone
        float innerAngleThreshold = innerSpotAngle / 2f;

        // Reduce spread near center for inner cone effect
        if (Mathf.Abs(randomAngle) < innerAngleThreshold) {
            randomAngle *= (Mathf.Abs(randomAngle) / innerAngleThreshold); // Scale angle closer to zero near center
        }

        Quaternion rotation = Quaternion.AngleAxis(randomAngle, Vector3.forward);
        return rotation * baseDirection;
    }

    internal void Flip(bool facingLeft) {
        Vector3 position = transform.localPosition;
        if (facingLeft) {
            position.x = -Mathf.Abs(position.x); // Ensure it moves to the left
        } else {
            position.x = Mathf.Abs(position.x); // Ensure it moves to the right
        }
        transform.localPosition = position;
    }

    public override void ToolStart(InputManager input, ToolController controller) {
        // Set the flag to handle our special "first shot" logic in CastRays
        _isFirstShot = true;
        base.ToolStart(input, controller); // This will start the MiningRoutine
    }

    public override void ToolStop(ToolController controller) {
        // Before stopping, store the current state for our "memory" feature
        _lastKnownDirection = _currentDirection;
        _timeToolStopped = Time.time;

        base.ToolStop(controller); // This will stop the MiningRoutine
    }

    public override void CastRays(Vector2 targetPos, ToolController controller, bool isFlipped) {
        Vector2 toolPosition = transform.position;
        Vector2 targetDirection = (targetPos - toolPosition).normalized;

        // Use the (potentially smoothed) _currentDirection for the raycast
        RaycastHit2D hit = Physics2D.Raycast(toolPosition, _currentDirection, Range, LayerMask.GetMask("MiningHit"));

        if (hit.collider != null) {
            Vector2 nudgedPoint = hit.point + _currentDirection * 0.1f; // Nudged point logic seems reversed, correcting it.

            // Damage Calculation
            float distance = hit.distance;
            float falloffFactor = Mathf.Clamp01(1f - (distance / Range) * falloffStrength);
            float finalDamage = DamagePerHit * falloffFactor;
            controller.CmdRequestDamageTile(new Vector3(nudgedPoint.x, nudgedPoint.y, 0), (short)finalDamage);
        }
    }
}
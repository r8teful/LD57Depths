using System.Collections;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Fish projectile that has three states:
/// - FindingTarget: periodically cone-raycasts to find a mining target
/// - SwimmingToTarget: swims toward the acquired target and continues checking validity
/// - MiningTarget: periodically requests damage at the hit position and keeps checking if target still exists
/// 
/// Hook up listeners to OnDamageRequest to perform tile damage logic (e.g. Tilemap manager).
/// Make sure the MiningHit layer is assigned to the tiles/objects that can be targeted.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class FishProjectile : MonoBehaviour {
    public enum State {
        FindingTarget,
        SwimmingToTarget,
        MiningTarget
    }
    public State currentState;

    [Header("General")]
    [SerializeField] float moveSpeed = 6f;
    [SerializeField] float rotateSpeed = 720f; // degrees/sec smoothing
    [SerializeField] State startState = State.FindingTarget;
    [SerializeField] LayerMask miningLayerMask; // assign to layer(s) for mining hits (e.g. "MiningHit")

    [Header("FindingTarget (cone check)")]
    [SerializeField] float viewDistance = 8f;
    [SerializeField] float coneAngle = 60f; // full cone angle in degrees
    [SerializeField] int raysPerCheck = 9; // how many rays inside the cone
    [SerializeField] float findCheckInterval = 0.15f; // periodic checks

    [Header("SwimmingToTarget")]
    [SerializeField] float swimCheckInterval = 0.12f;
    [SerializeField] float arriveDistance = 0.5f; // distance considered "arrived" at target
    [SerializeField] float minDistanceDecreaseThreshold = 0.01f; // optional slow-check for stuck targets

    [Header("MiningTarget")]
    [SerializeField] float miningRequestInterval = 0.6f; // how often to request damage to the tile
    [SerializeField] float miningValidityCheckInterval = 0.25f; // how often to check target validity while mining

    [Header("Events (hook externally)")]
    public UnityEvent<Vector2> OnDamageRequest; // called with world position of the mining hit point
    public UnityEvent<Vector2> OnTargetAcquired;

    // internal
    Rigidbody2D rb;

    // target info
    Vector2 targetPoint;
    Collider2D targetCollider; // collider that was hit when target was acquired (optional)
    float lastDistanceToTarget = float.MaxValue;
    private NetworkedPlayer _player;

    // coroutine refs
    Coroutine stateRoutine;

    void Awake() {
        rb = GetComponent<Rigidbody2D>();
        if (miningLayerMask == 0) {
            // default to a layer named "MiningHit" if it exists (helpful convenience)
            int mask = LayerMask.GetMask("MiningHit");
            if (mask != 0) miningLayerMask = mask;
        }

        if (OnDamageRequest == null) OnDamageRequest = new UnityEvent<Vector2>();
        if (OnTargetAcquired == null) OnTargetAcquired = new UnityEvent<Vector2>();
    }

    public void Init(NetworkedPlayer player, Vector2 dir) {
        rb.AddForce(dir,ForceMode2D.Impulse);
        _player = player;
        stateRoutine = StartCoroutine(FindingRoutine());
        SetState(startState);
    }

    void OnDisable() {
        StopStateRoutine();
    }

    #region State machine
    void SetState(State newState) {
        if (currentState == newState) return;
        StopStateRoutine();
        currentState = newState;

        switch (currentState) {
            case State.FindingTarget:
                stateRoutine = StartCoroutine(FindingRoutine());
                break;
            case State.SwimmingToTarget:
                stateRoutine = StartCoroutine(SwimmingRoutine());
                break;
            case State.MiningTarget:
                stateRoutine = StartCoroutine(MiningRoutine());
                break;
        }
    }

    void StopStateRoutine() {
        if (stateRoutine != null) {
            StopCoroutine(stateRoutine);
            stateRoutine = null;
        }
    }
    #endregion

    #region FindingTarget
    IEnumerator FindingRoutine() {
        // Periodically perform cone raycasts looking for mining targets.
        while (true) {
            TryFindTargetInCone();
            yield return new WaitForSeconds(findCheckInterval);
        }
    }

    void TryFindTargetInCone() {
        Vector2 origin = transform.position;
        // forward direction: local right is typical forward for 2D facing right; change if needed
        Vector2 forward = transform.right;
        rb.linearVelocity = forward * moveSpeed * 0.2f;
        float halfAngle = coneAngle * 0.5f;
        RaycastHit2D closestHit = new RaycastHit2D();
        float closestDist = float.MaxValue;
        bool foundAny = false;
        // rays are evenly spaced across cone
        for (int i = 0; i < raysPerCheck; i++) {
            float t = (raysPerCheck == 1) ? 0.5f : (float)i / (raysPerCheck - 1);
            float angle = Mathf.Lerp(-halfAngle, halfAngle, t);
            Vector2 dir = RotateVector(forward, angle);

            RaycastHit2D hit = Physics2D.Raycast(origin, dir, viewDistance, miningLayerMask);
            DebugDrawRay(origin, dir * viewDistance, Color.cyan, 0.1f);

            if (hit.collider != null) {
                float dist = hit.distance;

                // Keep closest
                if (dist < closestDist) {
                    closestDist = dist;
                    closestHit = hit;
                    foundAny = true;
                }
            }
        }

        if (foundAny) {
            AcquireTarget(closestHit);
        }
    }

    void AcquireTarget(RaycastHit2D hit) {
        Vector2 dir = (hit.point - (Vector2)transform.position).normalized;
        Vector2 nudgedPoint = hit.point + dir * 0.1f;
        targetPoint = nudgedPoint;
        targetCollider = hit.collider;
        lastDistanceToTarget = Vector2.Distance(transform.position, targetPoint);
        OnTargetAcquired?.Invoke(targetPoint);
        SetState(State.SwimmingToTarget);
    }
    #endregion

    #region SwimmingToTarget
    IEnumerator SwimmingRoutine() {
        // Move toward target every frame, but do periodic checks for ray/arrival/validity.
        // We'll keep moving in Update physics, but perform checks here.
        float checkTimer = 0f;
        while (true) {
            // Move towards target
            SeekTargetPerFrame();

            // Periodic checks
            checkTimer += Time.deltaTime;
            if (checkTimer >= swimCheckInterval) {
                checkTimer = 0f;
                bool stillValid = ValidateTargetExists();

                if (!stillValid) {
                    // target vanished (tile broken or missing)
                    ClearTargetAndReturnToFind();
                    yield break;
                }

                float currentDistance = Vector2.Distance(transform.position, targetPoint);
                // Optional: check that we're actually getting closer (not strictly required)
                // If required, you could add logic to handle stuck movement; here we simply update lastDistance.
                lastDistanceToTarget = currentDistance;

                // Check arrival
                if (currentDistance <= arriveDistance) {
                    // reached target -> begin mining
                    SetState(State.MiningTarget);
                    yield break;
                }

                // Also cast a forward ray each check to see if there's a direct forward hit in front of fish (optional)
                Vector2 forward = transform.right;
                RaycastHit2D hitForward = Physics2D.Raycast(transform.position, forward, viewDistance, miningLayerMask);
                DebugDrawRay(transform.position, forward * viewDistance, Color.yellow, 0.08f);
                // (we don't require anything from hitForward except maybe visual debug)
            }

            yield return null;
        }
    }

    void SeekTargetPerFrame() {
        if (targetPoint == null) return;
        // Direction to target
        Vector2 dir = (targetPoint - (Vector2)transform.position).normalized;

        // rotate smoothly toward target direction
        float targetAngle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        float smoothed = Mathf.MoveTowardsAngle(transform.eulerAngles.z, targetAngle, rotateSpeed * Time.deltaTime);
        transform.rotation = Quaternion.Euler(0, 0, smoothed);
        DebugDrawCircle(targetPoint, 1f, Color.yellow,1f);
        // Move using Rigidbody2D velocity for reliable physics interactions
        rb.linearVelocity = dir * moveSpeed;
    }
    #endregion

    #region MiningTarget
    IEnumerator MiningRoutine() {
        // While mining, we periodically request damage at targetPoint, and also check if target still exists
        float damageTimer = 0f;
        float validityTimer = 0f;

        // Stop rigidbody velocity while mining (optional). You might want a small wobble instead.
        rb.linearVelocity = Vector2.zero;

        while (true) {
            damageTimer += Time.deltaTime;
            validityTimer += Time.deltaTime;

            if (damageTimer >= miningRequestInterval) {
                damageTimer = 0f;
                // Request damage at the target point (hook external tile/manager to perform actual damage)
                DebugDrawCircle(targetPoint, 0.01f, Color.green, 1f);

                OnDamageRequest?.Invoke(targetPoint);
                Debug.Log("DAMAING");
                _player.CmdRequestDamageNearestSolidTile(targetPoint, 2);
            }

            if (validityTimer >= miningValidityCheckInterval) {
                validityTimer = 0f;
                bool stillValid = ValidateTargetExists();
                if (!stillValid) {
                    ClearTargetAndReturnToFind();
                    yield break;
                }
            }

            yield return null;
        }
    }
    #endregion

    #region Utilities
    void ClearTargetAndReturnToFind() {
        targetCollider = null;
        targetPoint = Vector2.zero;
        lastDistanceToTarget = float.MaxValue;
        rb.linearVelocity = Vector2.zero;
        SetState(State.FindingTarget);
    }

    bool ValidateTargetExists() {
        // We'll perform a short overlap/raycast check at the target point to see whether the MiningHit layer still exists there.
        // Use a small radius circle overlap to detect colliders in miningLayerMask near the target point.
        float checkRadius = 0.05f;
        Collider2D col = Physics2D.OverlapCircle(targetPoint, checkRadius, miningLayerMask);
        DebugDrawCircle(targetPoint, checkRadius, Color.magenta, 0.06f);

        // If the original collider still exists, that's a strong signal; otherwise fallback to raycast
        if (targetCollider != null) {
            if (col == null) return false;
            // optionally compare colliders
            if (col == targetCollider) return true;
            // otherwise the original collider might have been replaced, still treat as valid if there is any mining collider at that point
            return true;
        } else {
            return col != null;
        }
    }

    static Vector2 RotateVector(Vector2 v, float degrees) {
        float rad = degrees * Mathf.Deg2Rad;
        float cs = Mathf.Cos(rad);
        float sn = Mathf.Sin(rad);
        return new Vector2(v.x * cs - v.y * sn, v.x * sn + v.y * cs);
    }

    // Debug helpers (draw debug rays/circles, visible only in-editor or development builds)
    void DebugDrawRay(Vector2 start, Vector2 dir, Color col, float duration = 0.05f) {
#if UNITY_EDITOR
        Debug.DrawRay(start, dir, col, duration);
#endif
    }

    void DebugDrawCircle(Vector2 center, float radius, Color col, float duration = 0.05f) {
#if UNITY_EDITOR
        const int segments = 20;
        Vector3 prev = center + new Vector2(radius, 0f);
        for (int i = 1; i <= segments; i++) {
            float ang = (i / (float)segments) * Mathf.PI * 2f;
            Vector3 next = center + new Vector2(Mathf.Cos(ang) * radius, Mathf.Sin(ang) * radius);
            Debug.DrawLine(prev, next, col, duration);
            prev = next;
        }
#endif
    }

    private void OnDrawGizmosSelected() {
        return;
        // Draw distance circles
        Gizmos.color = Color.yellow; // Idle
        Gizmos.DrawWireSphere(transform.position, viewDistance);
        
        // This is VITAL for debugging your avoidance behavior!
        Vector2 forwardDirection = transform.right;
        Vector2 leftFeelerDir = Quaternion.Euler(0, 0, coneAngle) * forwardDirection;
        Vector2 rightFeelerDir = Quaternion.Euler(0, 0, -coneAngle) * forwardDirection;

        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(transform.position, transform.position + (Vector3)forwardDirection * viewDistance);
        Gizmos.DrawLine(transform.position, transform.position + (Vector3)leftFeelerDir * viewDistance);
        Gizmos.DrawLine(transform.position, transform.position + (Vector3)rightFeelerDir * viewDistance);
        Gizmos.color = Color.magenta;
        //Gizmos.DrawLine(transform.position, transform.position + (Vector3)_avoidanceDir * avoidanceRayDistance);
       // Gizmos.DrawLine(transform.position, transform.position + (Vector3)_finalMoveDir * avoidanceRayDistance);
    }
    #endregion
}

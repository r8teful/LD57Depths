using DG.Tweening;
using FishNet;
using FishNet.Object;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;
public class FishEntity : NetworkBehaviour, IPlayerAwareness {
    private enum FishState {
        Idle,
        Roaming,
        Fleeing
    }
    [SerializeField] private Transform _spriteTransform;

    [Header("State Control")]
    [SerializeField]
    private FishState currentState = FishState.Roaming;
    private Transform closestClient = null;

    [Header("Distance Parameters")]
    [Tooltip("The distance at which the fish will try to flee from a player.")]
    [SerializeField] private float fleeDistance = 5f;
    [Tooltip("The ideal distance the fish wants to keep from a player.")]
    [SerializeField] private float desiredDistance = 10f;
    [Tooltip("If further than this from a player, the fish will actively roam towards them.")]
    [SerializeField] private float roamDistance = 15f;

    [Header("Movement Speeds")]
    [SerializeField] private float maxSpeed = 10f;
    [Header("Trust Intervals")]
    [SerializeField] private float thrustIntervalIdle = 0.5f; 
    [SerializeField] private float thrustIntervalRoam = 0.25f; 
    [SerializeField] private float thrustIntervalFlee = 0.1f; 
    [Header("Physics & Steering")]
    [Tooltip("How powerfull of movements the fish can do. Higher is quier.")]
    [SerializeField] private float fishPower = 5f;
    [Tooltip("How quickly the fish turns. Higher is more agile.")]
    [SerializeField] private float turnTorque = 200f;

    private float thrustTimer = 0f;
    private float impulseMagnitude; // Calculated impulse strength
    // --- IMPROVED: Obstacle Avoidance ---
    [Header("Environment Awareness")]
    [Tooltip("The layer mask for walls and other obstacles.")]
    [SerializeField] private LayerMask obstacleLayer;
    [Tooltip("How far the 'feeler' raycasts check for walls.")]
    [SerializeField] private float avoidanceRayDistance = 3f;
    [Tooltip("The angle of the side 'feelers'. 30-45 degrees is a good start.")]
    [SerializeField] private float avoidanceFeelerAngle = 30f;
    [Tooltip("How strongly the fish reacts to avoid walls. 0-1 range.")]
    [SerializeField][Range(0f, 1f)] private float avoidanceWeight = 0.8f;

    [Header("Components")]
    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private List<NetworkObject> _nearbyPlayers;

    private Vector2 _finalMoveDir;
    public override void OnStartServer() {
        base.OnStartServer();
        _nearbyPlayers = new List<NetworkObject>();
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponentInChildren<SpriteRenderer>(); // Assumes sprite is a child or on the same object

        if (rb == null)
            Debug.LogError("FishAI requires a Rigidbody2D component.", this);
    }

    private void FixedUpdate() {
        // AI logic should only run on the server. FishNet handles synchronization.
        if (!IsServerInitialized)
            return;

        UpdateClosestClient();
        UpdateState();
        PerformStateAction();
    }
    private void Start() {
        //StartCoroutine(FishWiggle());
    }

    // --- Core AI Logic ---
    /// <summary>
    /// Finds the closest active player from the PlayerManager list.
    /// </summary>
    private void UpdateClosestClient() {
        closestClient = null;
        float minDistance = float.MaxValue;
        _nearbyPlayers.Clear();
        // Filter out any null or inactive clients before checking distances
        foreach (var conn in InstanceFinder.ServerManager.Clients.Values) {
            if (conn.FirstObject == null)
                continue; // Player object not spawned/found?
            _nearbyPlayers.Add(conn.FirstObject);
        }
        var activePlayers = _nearbyPlayers;
        if (activePlayers == null)
            return;
        if (!activePlayers.Any())
            return;

        foreach (var player in activePlayers) {
            float distance = Vector2.Distance(transform.position, player.transform.position);
            if (distance < minDistance) {
                minDistance = distance;
                closestClient = player.transform;
            }
        }
    }

    /// <summary>
    /// Determines the fish's state based on the distance to the closest client.
    /// </summary>
    private void UpdateState() {
        if (closestClient == null) {
            // If no clients are around, just roam aimlessly
            currentState = FishState.Roaming;
            return;
        }

        float distanceToClient = Vector2.Distance(transform.position, closestClient.position);

        if (distanceToClient < fleeDistance) {
            currentState = FishState.Fleeing;
        } else if (distanceToClient > roamDistance) {
            currentState = FishState.Roaming;
        } else {
            currentState = FishState.Idle;
        }
    }

    private void PerformStateAction() {
        Vector2 primaryTargetDirection = CalculatePrimaryTargetDirection();

        // 1. Get an avoidance direction if we are about to hit a wall
        Vector2 avoidanceDirection = CalculateAvoidanceDirection();

        // 2. Blend the two directions. If avoidance is needed, it heavily influences the final direction.
        Vector2 finalDirection = Vector2.Lerp(primaryTargetDirection, avoidanceDirection, avoidanceWeight).normalized;
        // If avoidance direction is zero (no obstacles), Lerp will just return the primary direction.
        if (avoidanceDirection == Vector2.zero) {
            finalDirection = primaryTargetDirection;
        }
        _finalMoveDir = avoidanceDirection;
        // 3. Move the fish using torque and force for smooth, physical movement
        MoveWithPhysics(finalDirection, maxSpeed);

        // 4. Flip the sprite
        FlipSprite(); 
    }

    // --- State-Specific Helper Methods ---

    private Vector2 CalculatePrimaryTargetDirection() {
        switch (currentState) {
            case FishState.Fleeing:
                return (transform.position - closestClient.position).normalized;

            case FishState.Idle:
                Vector2 vectorToClient = closestClient.position - transform.position;
                float distanceError = vectorToClient.magnitude - desiredDistance;
                Vector2 correction = vectorToClient.normalized * Mathf.Clamp(distanceError, -1, 1);
                Vector2 perpendicular = new Vector2(-vectorToClient.y, vectorToClient.x).normalized * 0.3f;
                return (correction + perpendicular).normalized;

            case FishState.Roaming:
                //if (closestClient != null) {
                //    return (closestClient.position - transform.position).normalized;
                //} else {
                    // If no clients, just wander. To prevent spinning, only change direction periodically.
                    if (rb.linearVelocity.magnitude < 0.1f && Random.Range(0, 200) < 1) {
                    Debug.Log("random target");
                        return Random.insideUnitCircle.normalized;
                    }
                    return rb.linearVelocity.normalized; // Continue in the same direction
               // }
        }
        return transform.up; // Default case
    }


    // --- IMPROVED: Movement and Avoidance ---

    /// <summary>
    /// Calculates a direction to steer to avoid obstacles using three "feeler" raycasts.
    /// </summary>
    private Vector2 CalculateAvoidanceDirection() {
        Vector2 forwardDirection = transform.right; // Fish's forward direction

        Vector2 avoidanceDir = Vector2.zero;

        // Define feeler directions
        Vector2 leftFeelerDir = Quaternion.Euler(0, 0, avoidanceFeelerAngle) * forwardDirection;
        Vector2 rightFeelerDir = Quaternion.Euler(0, 0, -avoidanceFeelerAngle) * forwardDirection;

        // Cast rays from fish position
        RaycastHit2D hitForward = Physics2D.Raycast(transform.position, forwardDirection, avoidanceRayDistance, obstacleLayer);
        RaycastHit2D hitLeft = Physics2D.Raycast(transform.position, leftFeelerDir, avoidanceRayDistance, obstacleLayer);
        RaycastHit2D hitRight = Physics2D.Raycast(transform.position, rightFeelerDir, avoidanceRayDistance, obstacleLayer);

        // Add repulsion vector for each hit
        if (hitForward.collider != null) {
            Vector2 repulsion = ((Vector2)transform.position - hitForward.point).normalized / (hitForward.distance + 0.001f);
            avoidanceDir += repulsion;
        }
        if (hitLeft.collider != null) {
            Vector2 repulsion = ((Vector2)transform.position - hitLeft.point).normalized / (hitLeft.distance + 0.001f);
            avoidanceDir += repulsion;
        }
        if (hitRight.collider != null) {
            Vector2 repulsion = ((Vector2)transform.position - hitRight.point).normalized / (hitRight.distance + 0.001f);
            avoidanceDir += repulsion;
        }

        // Return normalized direction, or zero if no avoidance needed
        return avoidanceDir != Vector2.zero ? avoidanceDir.normalized : Vector2.zero;
    }


    /// <summary>
    /// Moves the Rigidbody using torque for rotation and force for translation. This creates smooth, natural movement.
    /// </summary>
    private void MoveWithPhysics(Vector2 moveDirection, float maxSpeed) {
        // --- ROTATION ---
        float angleDifference = Vector2.SignedAngle(transform.right, moveDirection);
        float rotationAmount = angleDifference * (turnTorque / 100f) * Time.fixedDeltaTime;
        rb.AddTorque(rotationAmount);
        // Dampen angular velocity to prevent overshooting
        rb.angularVelocity *= 0.9f;

        // --- THRUST ---
        var thrustInterval = GetTrustIntervalForCurrentState();
        float averageForce = rb.mass * fishPower;
        impulseMagnitude = averageForce;// * thrustInterval; // Don't really want to scale it because it will just result in the same quanitity of movement

        thrustTimer -= Time.fixedDeltaTime;
        if (thrustTimer <= 0f) {
            Vector2 impulse = (Vector2)transform.right * impulseMagnitude;
            rb.AddForce(impulse, ForceMode2D.Impulse);
            DoFishWiggle();
            thrustTimer += thrustInterval;
        }

        // --- SPEED CONTROL ---
        if (rb.linearVelocity.magnitude > maxSpeed) {
            rb.linearVelocity = rb.linearVelocity.normalized * maxSpeed;
        }
    }

    private float GetTrustIntervalForCurrentState() {
        switch (currentState) {
            case FishState.Idle:
                return thrustIntervalIdle;
            case FishState.Roaming:
                return thrustIntervalRoam;
            case FishState.Fleeing:
                return thrustIntervalFlee;
            default:
                return thrustIntervalIdle;
        }
    }

    private void FlipSprite() {
        if (spriteRenderer == null)
            return;
        float zRotation = transform.localEulerAngles.z;

        // Normalize rotation to [0, 360)
        zRotation = zRotation % 360;

        if (zRotation > 90f && zRotation < 270f) {
            spriteRenderer.flipY = true;
        } else {
            spriteRenderer.flipY = false;
        }
    }


    /// <summary>
    /// Draw debug gizmos in the editor for easier tuning.
    /// </summary>
    private void OnDrawGizmosSelected() {
        // Draw distance circles
        Gizmos.color = Color.yellow; // Idle
        Gizmos.DrawWireSphere(transform.position, desiredDistance);
        Gizmos.color = Color.red; // Flee
        Gizmos.DrawWireSphere(transform.position, fleeDistance);
        Gizmos.color = Color.green; // Roam
        Gizmos.DrawWireSphere(transform.position, roamDistance);


        // This is VITAL for debugging your avoidance behavior!
        Vector2 forwardDirection = transform.right;
        Vector2 leftFeelerDir = Quaternion.Euler(0, 0, avoidanceFeelerAngle) * forwardDirection;
        Vector2 rightFeelerDir = Quaternion.Euler(0, 0, -avoidanceFeelerAngle) * forwardDirection;

        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(transform.position, transform.position + (Vector3)forwardDirection * avoidanceRayDistance);
        Gizmos.DrawLine(transform.position, transform.position + (Vector3)leftFeelerDir * avoidanceRayDistance);
        Gizmos.DrawLine(transform.position, transform.position + (Vector3)rightFeelerDir * avoidanceRayDistance);
        Gizmos.color = Color.magenta;
        //Gizmos.DrawLine(transform.position, transform.position + (Vector3)_avoidanceDir * avoidanceRayDistance);
        Gizmos.DrawLine(transform.position, transform.position + (Vector3)_finalMoveDir * avoidanceRayDistance);
        if (closestClient != null) {
            Gizmos.color = Color.magenta;
            Gizmos.DrawLine(transform.position, closestClient.position);
        }
    }
    private IEnumerator FishWiggle() {
        yield return DoFishWiggle().WaitForCompletion();
        yield return new WaitForSeconds(2);
        StartCoroutine(FishWiggle());
    }
    private Sequence DoFishWiggle() {

        Sequence moveSeq = DOTween.Sequence();
        moveSeq.Append(transform.DOScaleX(0.8f, 0.2f));
        moveSeq.Insert(0.05f, transform.DOScaleY(1.2f, 0.2f));
        moveSeq.Append(transform.DOScaleX(1, 0.2f).SetEase(Ease.OutBounce));
        moveSeq.Join(transform.DOScaleY(1, 0.2f).SetEase(Ease.OutBounce));
        return moveSeq;
    }
    public void UpdateNearbyPlayers(List<NetworkObject> nearbyPlayerNobs) {
        _nearbyPlayers = nearbyPlayerNobs;
    }
}
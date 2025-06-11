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
    [SerializeField] private float idleSpeed = 1f;
    [SerializeField] private float roamSpeed = 3f;
    [SerializeField] private float fleeSpeed = 6f;
    [SerializeField] private float turnSpeed = 2f;

    [Header("Physics & Steering")]
    [Tooltip("How quickly the fish accelerates. Higher is more responsive.")]
    [SerializeField] private float acceleration = 5f;
    [Tooltip("How quickly the fish turns. Higher is more agile.")]
    [SerializeField] private float turnTorque = 200f;
    [Tooltip("How much the fish slows down when not accelerating.")]
    [SerializeField] private float velocityDampening = 0.5f;

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

    private Vector2 _avoidanceDir;
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
        StartCoroutine(FishWiggle());
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
        float targetSpeed = GetTargetSpeedForCurrentState();

        // 1. Get an avoidance direction if we are about to hit a wall
        Vector2 avoidanceDirection = CalculateAvoidanceDirection();

        // 2. Blend the two directions. If avoidance is needed, it heavily influences the final direction.
        Vector2 finalDirection = Vector2.Lerp(primaryTargetDirection, avoidanceDirection, avoidanceWeight).normalized;
        // If avoidance direction is zero (no obstacles), Lerp will just return the primary direction.
        if (avoidanceDirection == Vector2.zero) {
            finalDirection = primaryTargetDirection;
        }
        _finalMoveDir = finalDirection;
        // 3. Move the fish using torque and force for smooth, physical movement
        MoveWithPhysics(finalDirection, targetSpeed);

        // 4. Flip the sprite
        //FlipSprite(rb.velocity); // Flip based on actual velocity now
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
                if (closestClient != null) {
                    return (closestClient.position - transform.position).normalized;
                } else {
                    // If no clients, just wander. To prevent spinning, only change direction periodically.
                    if (rb.linearVelocity.magnitude < 0.1f || Random.Range(0, 200) < 1) {
                        return Random.insideUnitCircle.normalized;
                    }
                    return rb.linearVelocity.normalized; // Continue in the same direction
                }
        }
        return transform.up; // Default case
    }

    private float GetTargetSpeedForCurrentState() {
        switch (currentState) {
            case FishState.Fleeing:
                return fleeSpeed;
            case FishState.Idle:
                return idleSpeed;
            case FishState.Roaming:
                return roamSpeed;
            default:
                return idleSpeed;
        }
    }

    // --- IMPROVED: Movement and Avoidance ---

    /// <summary>
    /// Calculates a direction to steer to avoid obstacles using three "feeler" raycasts.
    /// </summary>
    private Vector2 CalculateAvoidanceDirection() {
        // We cast rays in the direction the fish is currently FACING (transform.up)
        Vector2 forwardDirection = transform.up;
        Vector2 avoidanceDir = Vector2.zero;

        // Create feeler directions
        Vector2 leftFeelerDir = Quaternion.Euler(0, 0, avoidanceFeelerAngle) * forwardDirection;
        Vector2 rightFeelerDir = Quaternion.Euler(0, 0, -avoidanceFeelerAngle) * forwardDirection;

        // Cast the rays
        RaycastHit2D hitForward = Physics2D.Raycast(transform.position, forwardDirection, avoidanceRayDistance, obstacleLayer);
        RaycastHit2D hitLeft = Physics2D.Raycast(transform.position, leftFeelerDir, avoidanceRayDistance, obstacleLayer);
        RaycastHit2D hitRight = Physics2D.Raycast(transform.position, rightFeelerDir, avoidanceRayDistance, obstacleLayer);

        // If a feeler hits a wall, we want to steer away from the wall's normal
        // By adding the normals together, we get a composite direction away from all nearby obstacles
        if (hitForward.collider != null) {
            avoidanceDir += (Vector2)hitForward.normal;
        }
        if (hitLeft.collider != null) {
            avoidanceDir += (Vector2)hitLeft.normal;
        }
        if (hitRight.collider != null) {
            avoidanceDir += (Vector2)hitRight.normal;
        }
        _avoidanceDir = avoidanceDir.normalized;
        // Return the normalized direction, or Vector2.zero if no obstacles were hit
        return avoidanceDir.normalized;
    }


    /// <summary>
    /// Moves the Rigidbody using torque for rotation and force for translation. This creates smooth, natural movement.
    /// </summary>
    private void MoveWithPhysics(Vector2 moveDirection, float targetSpeed) {
        // --- ROTATION ---
        // Calculate the angle difference between where we are facing and where we want to go
        float angleDifference = Vector2.SignedAngle(transform.up, moveDirection);
        // Apply torque to close this angle. The amount of torque is proportional to the angle difference.
        float rotationAmount = angleDifference * (turnTorque / 100f) * Time.fixedDeltaTime; // Scaled torque
        rb.AddTorque(rotationAmount);

        // --- FORWARD MOVEMENT ---
        // Apply force in the direction the fish is currently facing
        Vector2 forwardForce = (Vector2)transform.up * acceleration;
        rb.AddForce(forwardForce * Time.fixedDeltaTime);

        // --- SPEED & DRAG CONTROL ---
        // Clamp the velocity to the maximum speed for the current state
        if (rb.linearVelocity.magnitude > targetSpeed) {
            rb.linearVelocity = rb.linearVelocity.normalized * targetSpeed;
        }

        // Apply a dampening force if we are not trying to accelerate (or are already fast enough)
        // This acts like water resistance and prevents the fish from sliding forever.
        if (forwardForce.magnitude == 0 || rb.linearVelocity.magnitude > targetSpeed) {
            rb.linearVelocity = Vector2.Lerp(rb.linearVelocity, Vector2.zero, velocityDampening * Time.fixedDeltaTime);
        }
    }


    /// <summary>
    /// Flips the sprite horizontally based on movement direction (optional but looks good).
    /// </summary>
    private void FlipSprite(Vector2 direction) {
        if (spriteRenderer == null)
            return;

        // Note: This assumes your sprite faces right by default.
        // If it moves left (x < 0), flip it. If it moves right (x > 0), unflip it.
        if (direction.x < -0.1f) {
            spriteRenderer.flipX = true;
        } else if (direction.x > 0.1f) {
            spriteRenderer.flipX = false;
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
        Vector2 forwardDirection = transform.up;
        Vector2 leftFeelerDir = Quaternion.Euler(0, 0, avoidanceFeelerAngle) * forwardDirection;
        Vector2 rightFeelerDir = Quaternion.Euler(0, 0, -avoidanceFeelerAngle) * forwardDirection;

        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(transform.position, transform.position + (Vector3)forwardDirection * avoidanceRayDistance);
        Gizmos.DrawLine(transform.position, transform.position + (Vector3)leftFeelerDir * avoidanceRayDistance);
        Gizmos.DrawLine(transform.position, transform.position + (Vector3)rightFeelerDir * avoidanceRayDistance);
        Gizmos.color = Color.green;
        //Gizmos.DrawLine(transform.position, transform.position + (Vector3)_avoidanceDir * avoidanceRayDistance);
        Gizmos.DrawLine(transform.position, transform.position + (Vector3)_finalMoveDir * avoidanceRayDistance);
        if (closestClient != null) {
            Gizmos.color = Color.magenta;
            Gizmos.DrawLine(transform.position, closestClient.position);
        }
    }
    private IEnumerator FishWiggle() {
        Sequence moveSeq = DOTween.Sequence();
        moveSeq.Append(transform.DOScaleX(0.8f, 0.2f));
        moveSeq.Insert(0.05f, transform.DOScaleY(1.2f, 0.2f));
        moveSeq.Append(transform.DOScaleX(1,0.2f).SetEase(Ease.OutBounce));
        moveSeq.Join(transform.DOScaleY(1,0.2f).SetEase(Ease.OutBounce));
        yield return moveSeq.WaitForCompletion();
        yield return new WaitForSeconds(2);
        StartCoroutine(FishWiggle());
    }

    public void UpdateNearbyPlayers(List<NetworkObject> nearbyPlayerNobs) {
        _nearbyPlayers = nearbyPlayerNobs;
    }
}
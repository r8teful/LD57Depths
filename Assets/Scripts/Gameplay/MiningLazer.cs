using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MiningLazer : MiningBase {

    [Tooltip("If you start firing again within this time, the lazer continues from its last angle.")]
    [SerializeField] private float directionMemoryTime = 1.5f;
    public bool CanMine { get; set; } = true;
    public override ToolType ToolType => ToolType.Lazer; 
    public override BuffSO AbilityData => Ability;

    public BuffSO Ability;
    public override object VisualData => _currentDirection;
    public static event Action<Vector2> OnPlayerKnockbackRequested;

    // --- State Variables ---
    private Vector2 _currentDirection;
    private Vector2 _lastKnownDirection;
    private float _timeToolStopped;
    private bool _isFirstShot; // Flag to handle initial direction logic


    public override void OwnerUpdate() {
        //Debug.Log(_isMining);
        if (!_isMining) {
            return;
        }
        // --- Smooth Rotation Logic (runs every frame) ---
        Vector2 toolPosition = transform.position;
        Vector2 targetPos = _inputManager.GetAimWorldInput(transform);
        Vector2 targetDirection = (targetPos - toolPosition).normalized;
        //Debug.Log($"ToolPos: {toolPosition} targetPos: {targetPos} dir: {targetDirection}");
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
            float maxAngleDelta = RotationSpeed * Time.deltaTime; // Use Time.deltaTime for per-frame smoothness
            _currentDirection = Vector3.RotateTowards(_currentDirection, targetDirection, maxAngleDelta * Mathf.Deg2Rad, 0.0f).normalized;
        }
        //Debug.Log($"Current dir: {_currentDirection}");
        //ToolVisual.HandleVisualUpdate(_currentDirection, base._inputManager,_isUsingAbility); // using new "lagging" direction now

        // Knockback
        if (KnockbackStrength > 0) {
            // Apply knockback in the opposite direction of the lazer
            Vector2 knockbackForce = -_currentDirection.normalized * KnockbackStrength * Time.deltaTime;

            // Invoke the event, sending the force vector. Any listeners will react.
            OnPlayerKnockbackRequested?.Invoke(knockbackForce);
        }
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
        //Debug.Log($"Range: {Range} Dir: {_currentDirection}");
        if (hit.collider != null) {
            Vector2 nudgedPoint = hit.point + _currentDirection * 0.1f; // Nudged point logic seems reversed, correcting it.

            // Damage Calculation
            float distance = hit.distance;
            float falloffFactor = Mathf.Clamp01(1f - (distance / Range) * FalloffStrength);
            float finalDamage = DamagePerHit * falloffFactor;
            controller.CmdRequestDamageTile(new Vector3(nudgedPoint.x, nudgedPoint.y, 0), finalDamage);
        }
    }

    public override IEnumerator MiningRoutineAbility(ToolController controller) {
        while (true) {
            yield return new WaitForSeconds(0.02f); // 0.02 is also fixed update btw
            if (!_isMining) yield break;

            var pos = _inputManager.GetAimWorldInput();
            Debug.Log("MiningAbilityRoutine!");
            var isFlipped = false;
            var horizontalInput = _inputManager.GetMovementInput().x;

            CastRaysAbility(pos, controller, isFlipped); // Todo determine freq here
        }
    }
    public void CastRaysAbility(Vector2 targetPos, ToolController controller, bool isFlipped) {
        HashSet<Vector3Int> processedCells = new HashSet<Vector3Int>(); // To avoid duplicate tiles

        Vector2 origin = transform.position;
        Vector2 dir = _currentDirection.normalized;
        float range = Range;

        // Tunables — adjust these to change accuracy/performance/thickness
        float stepAlong = 0.2f;   // how far we move along the ray per sample (keeps your original style)
        float thickness = 2.0f;   // total width (world units) of the "thick" ray
        float stepAcross = 0.25f; // sampling step across the perpendicular (smaller = more coverage)

        // Perpendicular to direction (points to the "side" of the ray)
        Vector2 perp = new Vector2(-dir.y, dir.x);
        float halfWidth = thickness * 0.5f;

        // Walk along the ray and at each step sample across the perpendicular
        for (float distance = 0f; distance <= range; distance += stepAlong) {
            Vector2 alongPoint = origin + dir * distance;

            // sample across the thickness
            for (float offset = -halfWidth; offset <= halfWidth + 1e-6f; offset += stepAcross) {
                Vector2 samplePoint = alongPoint + perp * offset;

                // Convert world position to the tilemap cell and process once
                Vector3Int cellPosition = WorldManager.Instance.WorldToCell((Vector3)samplePoint);
                if (!processedCells.Contains(cellPosition)) {
                    processedCells.Add(cellPosition);
                    controller.CmdRequestDamageTile(cellPosition, (short)DamagePerHit);
                }
            }
        }
    }
    public void CastRaysAbilityOld(Vector2 targetPos, ToolController controller, bool isFlipped) {
        HashSet<Vector3Int> processedCells = new HashSet<Vector3Int>(); // To avoid duplicate tiles

        Vector2 origin = transform.position;

        Vector2 dir = _currentDirection.normalized;
        float range = Range;

        // RaycastAll will return hits sorted by distance (closest first)
        RaycastHit2D[] hits = Physics2D.RaycastAll(origin, dir, range, LayerMask.GetMask("MiningHit"));

        if (hits == null || hits.Length == 0) return;
        // We've hit the tilemap, now check where there are tiles along the line
        float distance = 0f;

        while (distance <= range) {
            // Calculate the current point along the ray
            Vector2 point = origin + dir * distance;
          
            // Convert the point to a cell position in the Tilemap
            Vector3Int cellPosition = WorldManager.Instance.WorldToCell(point);

            // Avoid processing the same cell multiple times
            if (!processedCells.Contains(cellPosition)) {
                processedCells.Add(cellPosition);

                // Just request lol, not performant but I don't really care
                controller.CmdRequestDamageTile(cellPosition,(short)DamagePerHit); 
            }

            // Increment the distance
            distance += 0.2f;
        }
    }
    private void OnDrawGizmos() {
        Gizmos.color = Color.yellow;
        Gizmos.DrawRay(transform.position,_currentDirection);
    }
}
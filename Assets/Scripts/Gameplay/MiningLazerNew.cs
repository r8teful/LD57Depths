using System.Collections.Generic;
using UnityEngine;

// Making a totally clean and new class because I don't want to mess up the other stuff, we'll just delete that old code afterwards
public class MiningLazerNew : MonoBehaviour, IInitializableAbility {
    private AbilityInstance _abilityInstance;
    private NetworkedPlayer _player;
    private const float MINING_COOLDOWN = 0.02f;
    private float _cooldownRemaining;
    private Vector2 _currentDirection;
    private Vector2 _lastKnownDirection;
    private float _timeToolStopped;
    private float directionMemoryTime;
    private bool _isShooting;
    private bool _wasShootingLastFrame;
    private MiningLazerVisualNew _visual;
    private bool _firstShot;
    public Vector2 CurrentDir => _currentDirection;
    public void Init(AbilityInstance instance, NetworkedPlayer player) {
        _abilityInstance = instance;
        _player = player;
        _visual = GetComponent<MiningLazerVisualNew>();
        _visual.Init(player,instance,this);
    }
    private void Update() {
        UpdateCurDir();
        ApplyKnockback();
        HandleShootStateTransition();
        _isShooting = _player.InputManager.IsAllowedMiningUse();
        if (!_isShooting) return;
        if (MineDelayCheck()) {
            if (_player.PlayerAbilities.IsBrimstoneAbilityActive()) {
                MineAbility();
            } else {
                Mine();
            }
            _visual.HandleVisualUpdate();
        }
    }

    private void HandleShootStateTransition() {
        // I hate bools
        if (_isShooting && !_wasShootingLastFrame) {
            OnStartShoot();
        }
        if(!_isShooting && _wasShootingLastFrame) {
            OnEndShoot();
        }
        _wasShootingLastFrame = _isShooting;
    }

    private void OnEndShoot() {
        _visual.EndVisual();
    }

    private void OnStartShoot() {
        _firstShot = true;
        _visual.StartVisual();

    }

    private bool MineDelayCheck() {
        _cooldownRemaining -= Time.deltaTime;
        if (_cooldownRemaining <= 0) {
            _cooldownRemaining = MINING_COOLDOWN; // Reset cooldown
            return true;
        }
        return false;
    }

    private void UpdateCurDir() {
        // --- Smooth Rotation Logic (runs every frame) ---
        Vector2 toolPosition = transform.position;
        Vector2 targetPos = _player.InputManager.GetAimWorldInput(transform);
        Vector2 targetDirection = (targetPos - toolPosition).normalized;
        //Debug.Log($"ToolPos: {toolPosition} targetPos: {targetPos} dir: {targetDirection}");
        // Handle the "first shot" logic to either snap or use memory
        
        
        if (_firstShot) {
            if (Time.time - _timeToolStopped < directionMemoryTime && _lastKnownDirection.sqrMagnitude > 0) {
                _currentDirection = _lastKnownDirection;
            } else {
                _currentDirection = targetDirection; // Snap instantly on the first frame
            }
            _firstShot = false;
        } else {
            // Smoothly rotate towards the target direction over time
            float maxAngleDelta = _abilityInstance.GetEffectiveStat(StatType.MiningRotationSpeed) * Time.deltaTime; // Use Time.deltaTime for per-frame smoothness
            _currentDirection = Vector3.RotateTowards(_currentDirection, targetDirection, maxAngleDelta * Mathf.Deg2Rad, 0.0f).normalized;
        }
    }

    private void ApplyKnockback() {
        if (_abilityInstance.GetEffectiveStat(StatType.Knockback) > 0) {
            // Apply knockback in the opposite direction of the lazer
            Vector2 knockbackForce = -_currentDirection.normalized * _abilityInstance.GetEffectiveStat(StatType.Knockback) * Time.deltaTime;

            // Invoke the event, sending the force vector. Any listeners will react.
            
            //TODO
            //OnPlayerKnockbackRequested?.Invoke(knockbackForce);
        }
    }

    private void Mine() {
        Vector2 toolPosition = transform.position;
        var range = _abilityInstance.GetEffectiveStat(StatType.MiningRange);
        var falloff = _abilityInstance.GetEffectiveStat(StatType.MiningFalloff);
        var damage = _abilityInstance.GetEffectiveStat(StatType.MiningDamage);
        // Use the (potentially smoothed) _currentDirection for the raycast
        RaycastHit2D hit = Physics2D.Raycast(toolPosition, _currentDirection, range, LayerMask.GetMask("MiningHit"));
        //Debug.Log($"damage: {damage}");
        if (hit.collider != null) {

            //Debug.Log($"MINING HIT!!");
            Vector2 nudgedPoint = hit.point + _currentDirection * 0.1f; 
            // Damage Calculation
            float distance = hit.distance;
            float falloffFactor = Mathf.Clamp01(1f - (distance / range) * falloff);
            float finalDamage = damage * falloffFactor;
            
            _player.CmdRequestDamageTile(new Vector3(nudgedPoint.x, nudgedPoint.y, 0), finalDamage);
        }
    }
    public void MineAbility() {
        HashSet<Vector3Int> processedCells = new HashSet<Vector3Int>(); // To avoid duplicate tiles

        var range = _abilityInstance.GetEffectiveStat(StatType.MiningRange);
        var falloff = _abilityInstance.GetEffectiveStat(StatType.MiningFalloff);
        var damage = _abilityInstance.GetEffectiveStat(StatType.MiningDamage);
        Vector2 origin = transform.position;
        Vector2 dir = _currentDirection.normalized;
        //Debug.Log($"damage ability: {damage}");
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
                    _player.CmdRequestDamageTile(cellPosition, damage);
                }
            }
        }
    }
}
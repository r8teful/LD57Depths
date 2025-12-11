using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Windows;

// Making a totally clean and new class because I don't want to mess up the other stuff, we'll just delete that old code afterwards
public class MiningLazerNew : MonoBehaviour {
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
    public void Init(AbilityInstance instance, NetworkedPlayer player) {
        _abilityInstance = instance;
        _player = player;
        _visual = GetComponent<MiningLazerVisualNew>();
        _visual.Init(player);
    }
    private void Update() {
        UpdateCurDir();
        ApplyKnockback();
        HandleShootStateTransition();
        _isShooting = _player.InputManager.IsAllowedMiningUse();
        if (!_isShooting) return;
        if (MineDelayCheck()) {
            Mine();
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
        
        
        // TODO obviously!!
        var _isFirstShot = true; // TODO obviously!!
        if (_isFirstShot) {
            if (Time.time - _timeToolStopped < directionMemoryTime && _lastKnownDirection.sqrMagnitude > 0) {
                _currentDirection = _lastKnownDirection;
            } else {
                _currentDirection = targetDirection; // Snap instantly on the first frame
            }
            _isFirstShot = false;
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
        Vector2 input = _player.InputManager.GetAimWorldInput();
        Vector2 toolPosition = transform.position;
        Vector2 targetDirection = (input - toolPosition).normalized;
        var range = _abilityInstance.GetEffectiveStat(StatType.MiningRange);
        var falloff = _abilityInstance.GetEffectiveStat(StatType.MiningFalloff);
        var damage = _abilityInstance.GetEffectiveStat(StatType.MiningDamage);
        // Use the (potentially smoothed) _currentDirection for the raycast
        RaycastHit2D hit = Physics2D.Raycast(toolPosition, _currentDirection, range, LayerMask.GetMask("MiningHit"));
        //Debug.Log($"Range: {Range} Dir: {_currentDirection}");
        if (hit.collider != null) {

            Debug.Log($"MINING HIT!!");
            Vector2 nudgedPoint = hit.point + _currentDirection * 0.1f; // Nudged point logic seems reversed, correcting it.

            // Damage Calculation
            float distance = hit.distance;
            float falloffFactor = Mathf.Clamp01(1f - (distance / range) * falloff);
            float finalDamage = damage * falloffFactor;
            
            _player.CmdRequestDamageTile(new Vector3(nudgedPoint.x, nudgedPoint.y, 0), finalDamage);
        }
    }
}
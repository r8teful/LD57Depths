using r8teful;
using System;
using System.Collections.Generic;
using UnityEngine;

public class MiningLazerNew : MonoBehaviour, IInitializableAbility {
    private AbilityInstance _abilityInstance;
    private PlayerManager _player;
    private const float MINING_COOLDOWN = 0.02f;
    private float _cooldownRemaining;
    private Vector2 _currentDirection;
    private Vector2 _lastKnownDirection;
    private float _timeToolStopped;
    private float directionMemoryTime = 5;
    private bool _isShooting;
    private bool _wasShootingLastFrame;
    private MiningLazerVisualNew _visual;
    private bool _firstShot;
    private DamageContainer _damageContainer;

    public Vector2 CurrentDir => _currentDirection;
    public void Init(AbilityInstance instance, PlayerManager player) {
        _abilityInstance = instance;
        _player = player;
        _visual = GetComponent<MiningLazerVisualNew>();
        _visual.Init(player,instance,this);
        _damageContainer = new DamageContainer();
        _abilityInstance.OnBuffExpired += OnBuffExpire;
    }

    private void OnBuffExpire(BuffInstance buff) {
        if(buff.buffID == ResourceSystem.BrimstoneBuffID) {
            OnEndShoot();
        }
    }

    private void Update() {
        UpdateCurDir();
        HandleShootStateTransition();
        if (!MineDelayCheck()) return;
        _isShooting = _player.InputManager.IsShooting();
        if (_player.PlayerAbilities.IsBrimstoneAbilityActive()) {
            MineAbility(); // Brimstone doesn't require shooting
            _visual.HandleVisualUpdate();
        } else if(_isShooting){
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
        _timeToolStopped = Time.time;
        _chainTarget = Vector3Int.zero; // no chain target anymore
        Debug.Log("endshoot");
    }

    private void OnStartShoot() {
        _firstShot = true;
        _visual.StartVisual();
        _visual.StartChain();
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
        float knockback = _abilityInstance.GetEffectiveStat(StatType.Knockback);
        if (knockback > 0) {
            // Apply knockback in the opposite direction of the lazer
            Vector2 knockbackForce = -_currentDirection.normalized * knockback * Time.deltaTime;
            _player.PlayerMovement.ApplyMiningKnockback(knockbackForce);
        }
    }
    Vector2 debugVectorDir;
    Vector2 debugVectorStart;
    private float _createdRand;
    private Vector3Int _chainTarget;
    private Vector3Int pointBlue;
    private Vector3Int hitPointPrevious;
    private Vector3Int hitPointCurrent;

    private void Mine() {
        Vector2 toolPosition = transform.position;
        var range = _abilityInstance.GetEffectiveStat(StatType.MiningRange);
        var falloff = _abilityInstance.GetEffectiveStat(StatType.MiningFalloff);
        var damage = _abilityInstance.GetEffectiveStat(StatType.MiningDamage);
        
        if(RandomnessHelpers.TryGetCritDamage(_abilityInstance, out var critMult)) {
            // Crit!
            damage *= critMult;
            _damageContainer.crit = true;
        }
        _damageContainer.damage = damage;
        _lastKnownDirection = _currentDirection;
        RaycastHit2D hit = Physics2D.Raycast(toolPosition, _currentDirection, range, LayerMask.GetMask("MiningHit"));
        //Debug.Log($"damage: {damage}");
        
        if (hit.collider != null) {
            // move forward slighly so we get the right world block
            Vector2 nudgedPoint = hit.point + _currentDirection * 0.1f;
            HandleChainLogic(nudgedPoint);
            debugVectorStart = hit.point;
            var tiles = MineHelper.GetCircle(WorldManager.Instance.MainTileMap, hit.point,0.7f);
            foreach (var tile in tiles) {
                _damageContainer.tile = tile.CellPos;
                _player.RequestDamageTile(_damageContainer);
            }
            //_damageContainer.damage *= 0.5f;
            
            /* todo if you want falloff
            float distance = hit.distance;
            float falloffFactor = Mathf.Clamp01(1f - (distance / range) * falloff);
            float finalDamage = damage * falloffFactor;
            dmg.damage = finalDamage;    
             */
        } else {
            _visual.StopChain();
        } 
    }

    private void HandleChainLogic(Vector2 hit) {
        _visual.StartChain();
        float chainRange = 3f;
        hitPointCurrent = WorldManager.Instance.MainTileMap.WorldToCell(hit);
        if (_chainTarget == Vector3Int.zero) {
            // No target, find one
            var t = MineHelper.GetClosestSolidTile(WorldManager.Instance.MainTileMap, hit, chainRange, hitPointCurrent);
            if(t!=null) _chainTarget = t.Value.CellPos;
        }
        if(Vector3Int.Distance(hitPointCurrent,_chainTarget) > chainRange){
            //Chain target to far, get new chain target
            var t = MineHelper.GetClosestSolidTile(WorldManager.Instance.MainTileMap, hit, chainRange, hitPointCurrent);
            if(t!=null) _chainTarget = t.Value.CellPos;
        }
        var tile = WorldManager.Instance.MainTileMap.GetTile<TileSO>(_chainTarget);
        if (tile != null && !tile.IsSolid) {
            // tile broke, find new 
            var t = MineHelper.GetClosestSolidTile(WorldManager.Instance.MainTileMap, hit, 2, hitPointCurrent);
            if(t!=null) _chainTarget = t.Value.CellPos;
        }
        var chainVisualTarget = new Vector2(_chainTarget.x +0.5f, _chainTarget.y + 0.5f);// offset by 0.5 to get into center of block 

        _visual.DrawChain(hit, chainVisualTarget);
        _damageContainer.tile = _chainTarget;
        _player.RequestDamageTile(_damageContainer);
    }

    public void MineAbility() {
        HashSet<Vector3Int> processedCells = new HashSet<Vector3Int>(); // To avoid duplicate tiles
        ApplyKnockback();
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
                    _player.RequestDamageTile(cellPosition, damage);
                }
            }
        }
    }

    private void FindChainTarget() {

    }

    private void OnDrawGizmos() {
        Gizmos.color = Color.green;
        Gizmos.DrawRay(debugVectorStart, debugVectorDir);
        Gizmos.color = Color.red;
        Gizmos.DrawLine(debugVectorStart, debugVectorDir);
        Gizmos.DrawSphere(debugVectorDir,0.1f);
        Gizmos.color = Color.blue;
        Gizmos.DrawSphere(pointBlue, 0.1f);
    }
}
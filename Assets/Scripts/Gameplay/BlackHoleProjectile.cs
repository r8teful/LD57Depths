using System.Collections;
using UnityEngine;

public class BlackHoleProjectile : MonoBehaviour {
    private Rigidbody2D _rb;
    private PlayerManager _player;
    private AbilityInstance _ability;
    private float _startMagnitude;
    private float aliveTime;
    private void Awake() {
        _rb = GetComponent<Rigidbody2D>();
    }
    internal void Init(Vector2 shootForce,PlayerManager player, AbilityInstance abilityInstance) {
        _player = player;
        _ability = abilityInstance;
        _startMagnitude = shootForce.magnitude;
        _rb.AddForce(shootForce, ForceMode2D.Impulse);
        StartCoroutine(DamageRoutine());
    }
    private IEnumerator DamageRoutine() {
        var checkInterval = 0.025f;
        var size = _ability.GetEffectiveStat(StatType.Size);
        var baseDamage = _ability.GetEffectiveStat(StatType.MiningDamage);
        var minDamage = baseDamage * 0.5f;
        var timeAlive = 1.5f;
        while (aliveTime < timeAlive) {
            // Damage is smaller when moving
            var speed = _rb.linearVelocity.magnitude;
            var damage = Mathf.Max(minDamage, baseDamage*(1- (0.05f * speed)));
            var variableSize = Mathf.Max(size*0.2f, size * (1 - (0.05f * speed)));
            // Possibly make range smaller here so its like the trail of the blackhole is doing the damage
            var tiles = MineHelper.GetCircle(WorldManager.Instance.MainTileMap, transform.position, variableSize);
            foreach (var tile in tiles) {
                _player.RequestDamageTile(tile.CellPos, damage);
            }
            aliveTime += checkInterval;
            yield return new WaitForSeconds(checkInterval);
        }
        Destroy(gameObject);
    }
}
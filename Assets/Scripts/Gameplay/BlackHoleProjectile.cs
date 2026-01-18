using System;
using System.Collections;
using UnityEngine;

public class BlackHoleProjectile : MonoBehaviour {
    private Rigidbody2D _rb;
    private NetworkedPlayer _player;
    private float aliveTime;
    private void Awake() {
        _rb = GetComponent<Rigidbody2D>();
    }
    internal void Init(Vector2 shootForce,NetworkedPlayer player) {
        _player = player;
        _rb.AddForce(shootForce, ForceMode2D.Impulse);
        StartCoroutine(DamageRoutine());
    }
    private IEnumerator DamageRoutine() {
        var checkInterval = 0.1f;
        var size = 5f;
        var maxDamage = 5;
        var baseDamage = 2;
        var timeAlive = 3;
        while (aliveTime < timeAlive) {
            // Damage is smaller when moving
            var damage = Mathf.Min(maxDamage, baseDamage / _rb.linearVelocity.magnitude);
            // Possibly make range smaller here so its like the trail of the blackhole is doing the damage
            var tiles = MineHelper.GetCircle(WorldManager.Instance.MainTileMap, transform.position, size);
            foreach (var tile in tiles) {
                _player.CmdRequestDamageTile(tile.CellPos, damage);
            }
            aliveTime += checkInterval;
            yield return new WaitForSeconds(checkInterval);
        }
        Destroy(gameObject);
    }
}
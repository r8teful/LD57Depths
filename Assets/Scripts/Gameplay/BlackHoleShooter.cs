using UnityEngine;

public class BlackHoleShooter : ShootableAbilityBase {
    [SerializeField] private BlackHoleProjectile _prefab;

    public override void Shoot() {
        int count = Mathf.FloorToInt(_abilityInstance.GetEffectiveStat(StatType.ProjectileCount));
        for (int i = 0; i < count; i++) { 
            Vector2 shootForce = Random.Range(12, 20) * Random.insideUnitCircle;
            shootForce += _player.PlayerMovement.GetRigidbody().linearVelocity;
            Instantiate(_prefab, transform.position, Quaternion.identity)
                .Init(shootForce, _player, _abilityInstance);
        }
    }
}
using UnityEngine;

public class BouncingBallShooter : ShootableAbilityBase {
    [SerializeField] private BouncingBall _prefab;
    public override void Shoot() {
        int count = Mathf.FloorToInt(_abilityInstance.GetEffectiveStat(StatType.ProjectileCount));
        for (int i = 0; i < count; i++) { 
            var dir = Random.insideUnitCircle.normalized;
            Instantiate(_prefab, transform.position, Quaternion.identity)
                .Init(dir, _player, _abilityInstance);
        }
    }
}

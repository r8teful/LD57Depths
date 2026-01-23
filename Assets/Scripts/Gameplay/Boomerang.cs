using UnityEngine;

public class Boomerang : ShootableAbilityBase {
    [SerializeField] private BoomerangProjectile _boomerangPrefab;
    public override void Shoot() {
        int count = Mathf.FloorToInt(_abilityInstance.GetEffectiveStat(StatType.ProjectileCount));
        for (int i = 0; i < count; i++) {
            // random z rotation
            var z = Random.Range(0, 360);
            Quaternion rotation = Quaternion.Euler(0, 0, z);
            Instantiate(_boomerangPrefab, transform.position,rotation,transform)
                .Init(_player, _abilityInstance);
        }
    }
}
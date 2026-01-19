using System.Collections;
using UnityEngine;

public class ExtraFishShooter : ShootableAbilityBase {
    [SerializeField] private FishProjectile _prefab;
    public override void Shoot() {
        // Find the shoot direction if we have one 
        int count = Mathf.FloorToInt(_abilityInstance.GetEffectiveStat(StatType.ProjectileCount));
        for (int i = 0; i < count; i++) { 
            var input = _player.InputManager;
            Vector2 dir;
            if (input.IsShooting()) {
                dir = input.GetDirFromPos(transform.position);
            } else {
                // random dir
                dir = Random.insideUnitCircle.normalized;
            }
            // Calculate the angle in degrees from the target direction
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            // Create a quaternion for the rotation (rotate around Z-axis for 2D)
            Quaternion rotation = Quaternion.Euler(0f, 0f, angle);
            Instantiate(_prefab, transform.position, rotation).Init(_player, _abilityInstance, dir);
        }
    }
}
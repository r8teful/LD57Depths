using System.Collections;
using UnityEngine;

public class BlackHoleShooter : ShootableAbilityBase {
    [SerializeField] private BlackHoleProjectile _prefab;

    public override void Shoot() {
        Vector2 shootForce = Random.Range(12, 20) * Random.insideUnitCircle;
        shootForce += _player.PlayerMovement.GetRigidbody().linearVelocity; 
        Instantiate(_prefab,transform.position,Quaternion.identity).Init(shootForce,_player);
    }
}
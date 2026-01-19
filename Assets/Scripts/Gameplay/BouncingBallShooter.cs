using UnityEngine;

public class BouncingBallShooter : ShootableAbilityBase {
    [SerializeField] private BouncingBall _prefab;
    public override void Shoot() {
        var dir = Random.insideUnitCircle.normalized * 4;
        Instantiate(_prefab, transform.position, Quaternion.identity).Init(dir,_player);
    }
}

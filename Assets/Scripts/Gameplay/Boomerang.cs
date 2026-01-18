using System.Collections;
using UnityEngine;

public class Boomerang : ShootableAbilityBase {
    [SerializeField] private BoomerangProjectile _boomerangPrefab;
    public override void Shoot() {
        // random z rotation
        var z = Random.Range(0, 360);
        Quaternion rotation = Quaternion.Euler(0, 0, z);
        Instantiate(_boomerangPrefab, transform.position,rotation,transform).Init(_player);
    }
}
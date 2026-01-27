using UnityEngine;

public class MiningRPG : MonoBehaviour {
    [SerializeField] private Transform spawnPos;
    
    public float ExplosionVelocity;
    
    private Vector2 _visualDirection;

    

    public BuffSO Ability;
    public void CastRays(Vector2 pos, bool isFlipped) {
        Vector2 toolPosition = transform.position;
        Vector2 targetDirection = (pos - toolPosition).normalized;

        // Calculate the angle in degrees from the target direction
        float angle = Mathf.Atan2(targetDirection.y, targetDirection.x) * Mathf.Rad2Deg;
        // Create a quaternion for the rotation (rotate around Z-axis for 2D)
        Quaternion rotation = Quaternion.Euler(0f, 0f, angle);
        // Instantiate the projectile with the calculated rotation
        AudioController.Instance.PlaySound2D("RPGShoot",1);
      //  Instantiate(App.ResourceSystem.GetPrefab<RPGProjectile>("RPGProjectile"), spawnPos.position, rotation).Init(targetDirection * ExplosionVelocity);
    }
}
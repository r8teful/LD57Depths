using UnityEngine;

public class RPGProjectile : MonoBehaviour {
    [SerializeField] private Rigidbody2D _rb;
    [SerializeField] private float explosionRadius = 2f; // Radius of the explosion
    [SerializeField] private float hitBoxRadius = 2f; // Hitbox
    [SerializeField] private short damageAmount = 10; // Damage per tile
    [SerializeField] private float checkInterval = 0.1f; // How often to check for collisions
    [SerializeField] private GameObject explosionParticlePrefab;

    private float checkTimer = 0f;
    private bool hasExploded = false;

    public void Init(Vector2 force) {
        _rb.AddForce(force, ForceMode2D.Impulse);
    }


    private void FixedUpdate() {
        if (hasExploded) return;

        // Increment timer for periodic collision check
        checkTimer += Time.fixedDeltaTime;
        if (checkTimer >= checkInterval) {
            checkTimer = 0f; // Reset timer
            CheckForCollision();
        }
    }

    private void CheckForCollision() {
        // Check for colliders in the explosion radius on the MiningHit layer
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, hitBoxRadius, LayerMask.GetMask("MiningHit"));
        if (hits.Length > 0) {
            Explode();
        }
    }

    private void Explode() {
        if (hasExploded) return;
        hasExploded = true;

        // Get the center of the explosion
        Vector3 explosionCenter = transform.position;

        var tiles = MineHelper.GetCircle(
            WorldManager.Instance.MainTileMap, transform.position, explosionRadius);
        foreach (var tile in tiles) {
            // todo set this to player also I don't know if the circle thing will work but eh
           // toolController.CmdRequestDamageTile(tile.CellPos, damageAmount * tile.DamageRatio);
        }

        Instantiate(explosionParticlePrefab, transform.position,Quaternion.identity);
        AudioController.Instance.PlaySound2D("RPGExplode", 1);
        // Destroy the projectile after exploding
        Destroy(gameObject);
    }

    // Visualize the explosion radius in the editor
    private void OnDrawGizmosSelected() {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, explosionRadius);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, hitBoxRadius);
    }
}
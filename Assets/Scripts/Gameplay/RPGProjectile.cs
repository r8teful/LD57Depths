using System.Collections;
using UnityEngine;

public class RPGProjectile : MonoBehaviour {
    [SerializeField] private Rigidbody2D _rb;
    [SerializeField] private float explosionRadius = 2f; // Radius of the explosion
    [SerializeField] private float hitBoxRadius = 2f; // Hitbox
    [SerializeField] private short damageAmount = 10; // Damage per tile
    [SerializeField] private float checkInterval = 0.1f; // How often to check for collisions

    private ToolController toolController; // Reference to ToolController
    private float checkTimer = 0f;
    private bool hasExploded = false;

    public void Init(Vector2 force, ToolController tc) {
        _rb.AddForce(force, ForceMode2D.Impulse);
        toolController = tc;
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

        // Calculate grid positions within the explosion radius
        // Assuming tiles are on a 1x1 grid (adjust grid size if different)
        int gridRadius = Mathf.CeilToInt(explosionRadius);
        for (int x = -gridRadius; x <= gridRadius; x++) {
            for (int y = -gridRadius; y <= gridRadius; y++) {
                Vector3 tilePos = explosionCenter + new Vector3(x, y, 0);
                // Check if the tile is within the explosion radius
                if (Vector3.Distance(explosionCenter, tilePos) <= explosionRadius) {
                    toolController.CmdRequestDamageTile(tilePos, damageAmount);
                }
            }
        }

        // Optional: Add visual/audio effects here (e.g., spawn explosion particle system)

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
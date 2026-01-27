using UnityEngine;

public class CactusProjectile : MonoBehaviour {
    private PlayerManager _player;
    Vector2 direction;
    float speed;
    float lifetime;
    float aliveTime;
    private float checkTimer = 0f;
    private bool hasExploded = false;

    [SerializeField] private float hitBoxRadius = 2f; // Hitbox
    [SerializeField] private float checkInterval = 0.1f; // How often to check for collisions
    /// <summary>
    /// Call this to initialize/reuse the bullet.
    /// </summary>
    public void Init(PlayerManager player, Vector2 dir, float speed, float lifetime) {
        _player = player;
        this.direction = dir.normalized;
        this.speed = speed;
        this.lifetime = lifetime;
        this.aliveTime = 0f;
    }

    void OnEnable() {
        aliveTime = 0f;
    }

    void Update() {
        // Move bullet in a straight line
        transform.position += (Vector3)(direction * speed * Time.deltaTime);

        // Lifetime handling
        aliveTime += Time.deltaTime;
        if (aliveTime >= lifetime) {
            Recycle();
        }
    }

    void Recycle() {
        Destroy(gameObject);
    }
    void OnTriggerEnter2D(Collider2D other) {
        Recycle();
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
            TriggerHit();
        }
    }

    private void TriggerHit() {
        if (hasExploded) return;
        hasExploded = true;
        _player.RequestDamageNearestSolidTile(transform.position, 2);
        Recycle();
    }
    private void OnDrawGizmosSelected() {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, hitBoxRadius);
    }
}
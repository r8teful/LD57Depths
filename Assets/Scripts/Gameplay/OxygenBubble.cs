using UnityEngine;

public class OxygenBubble : MonoBehaviour {
    private Rigidbody2D _rb;
    private OxygenManager _oxygenManager;
    private float replenishPerBubble = 10;
    private void Awake() {
        _rb = GetComponent<Rigidbody2D>();
    }
    public void Init(Vector2 dirToPlayer,OxygenManager oxygen) {
        _rb.AddForce(dirToPlayer,ForceMode2D.Impulse);
        _oxygenManager = oxygen;
    }
    private void OnTriggerEnter2D(Collider2D collision) {
        if (collision.CompareTag("Player")) {
            // Gain oxygen
            _oxygenManager.GainOxygen(replenishPerBubble);
            Destroy(gameObject);
        }
    }
}
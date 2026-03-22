using UnityEngine;

public class OxygenBubble : MonoBehaviour {
    private Rigidbody2D _rb;
    private OxygenManager _oxygenManager;
    private float _replenishPerBubble = 0;
    private void Awake() {
        _rb = GetComponent<Rigidbody2D>();
    }
    public void Init(Vector2 dirToPlayer,OxygenManager oxygen, float oxygenIncrease) {
        _replenishPerBubble = oxygenIncrease;
        _rb.AddForce(dirToPlayer,ForceMode2D.Impulse);
        _oxygenManager = oxygen;
    }
    private void OnTriggerEnter2D(Collider2D collision) {
        if (collision.CompareTag("Player")) {
            // Gain oxygen
            _oxygenManager.GainOxygen(_replenishPerBubble);
            Destroy(gameObject);
        }
    }
}
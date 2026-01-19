using UnityEngine;

public class BouncingBall : MonoBehaviour {
    private Rigidbody2D _rb;
    private NetworkedPlayer _player;
    private int _bounceAmount;
    private int _maxBounces;


    private void Awake() {
        _rb = GetComponent<Rigidbody2D>();
        _rb.gravityScale = 0;
        _rb.linearDamping = 0;
        _rb.freezeRotation = true;
        PhysicsMaterial2D material = new PhysicsMaterial2D();
        material.bounciness = 1;
        material.friction = 0;
        _rb.sharedMaterial = material;
    }
    public void Init(Vector2 dir,NetworkedPlayer player) {
        _rb.AddForce(dir, ForceMode2D.Impulse);
        _player = player;
        _maxBounces = 3; // should be upgradable
    }
    private void OnCollisionEnter2D(Collision2D collision) {
        _player.CmdRequestDamageNearestSolidTile(transform.position,5);
        _bounceAmount++;
        if (_bounceAmount >= _maxBounces) {
            Destroy(gameObject);
        }
    }
}
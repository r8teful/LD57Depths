using FishNet.Demo.AdditiveScenes;
using UnityEngine;

public class FishProjectileDumb : MonoBehaviour {
    [Header("Movement")]
    public float forwardForce = 5f;
    public float checkDistance = 0.5f;

    [Header("Tile Mining")]
    public float mineInterval = 0.15f; // every X seconds

    private Rigidbody2D _rb;
    private float _mineTimer;
    private NetworkedPlayer _player;
    [SerializeField] private Transform _mouthPos;

    public void Init(NetworkedPlayer player,Vector2 dir) {
        _player = player;
        _rb.AddForce(dir, ForceMode2D.Impulse);
    }

    private void Awake() {
        _rb = GetComponent<Rigidbody2D>();
    }

    private void FixedUpdate() {
        // Constant forward push
        _rb.AddForce(transform.right * forwardForce, ForceMode2D.Force);

        // Mine timer
        _mineTimer -= Time.fixedDeltaTime;
        if (_mineTimer <= 0f) {
            _mineTimer = mineInterval;
            TryMineAhead();
        }
    }

    private void TryMineAhead() {
        // Look ahead in the direction the fish is facing
        Vector2 direction = transform.right;
        float mouthradius =0.5f;
        //RaycastHit2D hit = Physics2D.Raycast(origin, direction, checkDistance, LayerMask.GetMask("MiningHit"));
        RaycastHit2D hit = Physics2D.CircleCast(_mouthPos.position, mouthradius, direction,checkDistance, LayerMask.GetMask("MiningHit"));

        if (hit.collider != null) {
            //Vector2 nudgedPoint = hit.point + direction * 0.1f; // Nudged point logic seems reversed, correcting it.
            _player.CmdRequestDamageNearestSolidTile(new Vector3(hit.point.x, hit.point.y, 0), 2,2);
            //_player.CmdRequestDamageNearestSolidTile(x, y);
        }
    }
}
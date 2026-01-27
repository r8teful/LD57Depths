using UnityEngine;

public class FishProjectile : MonoBehaviour {
    [Header("Movement")]
    public float forwardForce = 5f;
    [Header("Tile Mining")]
    public float mineInterval = 0.15f; // every X seconds

    private Rigidbody2D _rb;
    private float _mineTimer;
    private PlayerManager _player;
    private AbilityInstance _ability;
    [SerializeField] private Transform _mouthPos;

    public void Init(PlayerManager player, AbilityInstance abilityInstance, Vector2 dir) {
        _player = player;
        _ability = abilityInstance;
        var speedMult = _ability.GetEffectiveStat(StatType.ProjectileSpeed);
        dir *= speedMult;
        _rb.AddForce(dir, ForceMode2D.Impulse);
        Destroy(gameObject, 5);
    }

    private void Awake() {
        _rb = GetComponent<Rigidbody2D>();
    }

    private void FixedUpdate() {
        // Constant forward push
        _rb.AddForce(forwardForce * transform.right, ForceMode2D.Force);

        // Mine timer
        _mineTimer -= Time.fixedDeltaTime;
        if (_mineTimer <= 0f) {
            _mineTimer = mineInterval;
            TryMineAhead();
        }
    }

    private void TryMineAhead() {
        float mouthradius = _ability.GetEffectiveStat(StatType.Size);
        float damage  = _ability.GetEffectiveStat(StatType.MiningDamage);
        var hits = MineHelper.GetCircle(WorldManager.Instance.MainTileMap, _mouthPos.position, mouthradius);
        foreach (var hit in hits) {
            _player.RequestDamageTile(hit.CellPos,damage);
            
        }
        
    }
}
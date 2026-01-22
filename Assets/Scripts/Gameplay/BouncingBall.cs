using DG.Tweening;
using UnityEngine;

public class BouncingBall : MonoBehaviour {
    private Rigidbody2D _rb;
    private NetworkedPlayer _player;
    private int _bounceAmount;
    private int _maxBounces;
    private AbilityInstance _ability;
    public Transform _visualTransform;

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
    public void Init(Vector2 dir,NetworkedPlayer player, AbilityInstance abilityInstance) {
        _ability = abilityInstance;
        _player = player;
        _maxBounces = Mathf.FloorToInt(_ability.GetEffectiveStat(StatType.ProjectileBounces));
        _rb.AddForce(dir * _ability.GetEffectiveStat(StatType.ProjectileSpeed), ForceMode2D.Impulse);
    }
    private void OnCollisionEnter2D(Collision2D collision) {
        _player.CmdRequestDamageNearestSolidTile(transform.position,5);
        _bounceAmount++;
        if (_bounceAmount >= _maxBounces) {
            Destroy(gameObject);
        }
        // animation
        var squach = _rb.linearVelocity.normalized;
        _visualTransform.DOKill();

        Vector2 relVel = collision.relativeVelocity;
        float speed = relVel.magnitude;
        if (speed <= 0.01f) return;

        _visualTransform.DOPunchScale(new(-0.2f,-0.2f), 0.2f);
    }
}
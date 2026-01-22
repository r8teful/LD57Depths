using System.Collections;
using UnityEditor;
using UnityEngine;

public class BoomerangProjectile : MonoBehaviour {
    private bool _isReturning;

    [SerializeField] private float duration = 0.6f;
    [SerializeField] private float distance = 5f;
    [SerializeField] private float curveAmount = 2f;

    private Transform playerTransform;
    private NetworkedPlayer _player;
    private AbilityInstance _ability;

    public void Init(NetworkedPlayer player, AbilityInstance abilityInstance) {
        playerTransform = player.transform;
        _player = player;
        _ability = abilityInstance;
        StartCoroutine(BoomerangRoutine());
        StartCoroutine(DamageRoutine());
    }

    private IEnumerator DamageRoutine() {
        var checkInterval = 0.05f;
        var size = _ability.GetEffectiveStat(StatType.Size);
        var damage = _ability.GetEffectiveStat(StatType.MiningDamage);
        while (true) {
            var tiles = MineHelper.GetCircle(WorldManager.Instance.MainTileMap, transform.position, size);
            foreach (var tile in tiles) {
                _player.CmdRequestDamageTile(tile.CellPos, damage);
            }
            yield return new WaitForSeconds(checkInterval);
        }
    }

    private IEnumerator BoomerangRoutine() {
        float elapsed = 0f;
        float total = Mathf.Max(0.01f, duration); // avoid zero duration
        Vector3 facingWorld = transform.right;
        Vector3 facingLocal3 = playerTransform.InverseTransformDirection(facingWorld);
        Vector2 facingLocal = new Vector2(facingLocal3.x, facingLocal3.y).normalized;

        // perpendicular to facing (local) used to offset the circle center sideways
        Vector2 perpLocal = new Vector2(-facingLocal.y, facingLocal.x).normalized;
        Vector2 centerLocal = facingLocal * distance + perpLocal * curveAmount;
        // circle radius is distance from the center to the player (so start point is exactly at player)
        float radius = centerLocal.magnitude;
        if (radius < 0.001f) radius = 0.001f;
        // start angle so that center + radius*(cos, sin) = player local pos (which is zero)
        float startAngle = Mathf.Atan2(-centerLocal.y, -centerLocal.x); // radians
        int dir = (curveAmount >= 0f) ? 1 : -1;
        int loopCount = Mathf.Max(1, 1); // Increasing this looks funny 
        while (elapsed < total) {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / total);
            float angle = startAngle + dir * Mathf.PI * 2f * (t * loopCount);
            Vector2 localPos = centerLocal + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
            transform.position = playerTransform.TransformPoint(new Vector3(localPos.x, localPos.y, 0f));
            yield return null;
        }
        transform.position = playerTransform.position;
        Destroy(gameObject);
    }

}
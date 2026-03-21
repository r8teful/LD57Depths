using UnityEngine;

public class ExplosiveCrits : MonoBehaviour, IInitializableAbility {
    private ITileDamageable _target;
    [Tooltip("Set for each prefab. The ability that we listen to when it fires")]
    [SerializeField] private AbilitySO abilityTarget;
    [SerializeField] private ValueModifiableComponent _values;
    private PlayerManager _player;
    private DamageContainer _damageContainer;
    public void Init(AbilityInstance instance, PlayerManager player) {
        _player = player;
        var target = player.PlayerAbilities.GetAbilityInstance(abilityTarget.ID);
        if (target != null && target.Object != null && target.Object.TryGetComponent<ITileDamageable>(out var lazerScript)) {
            Debug.Log("Found lazer script! subscribing...");
            _target = lazerScript;
            _target.OnTileDamaged += TileDamaged; 
        }
        _damageContainer = new();
        _values.Register();
    }

    private void TileDamaged(DamageContainer container) {
        var c = _values.GetValueNow(ValueKey.ExplosiveCritChance);
        if (Random.value > c) return; // fail
        var damage = _values.GetValueNow(ValueKey.ExplosiveCritDamage);
        var range = _values.GetValueNow(ValueKey.ExplosiveCritRange);

        var tiles = MineHelper.GetCircle( WorldManager.Instance.MainTileMap, transform.position, range);
        foreach (var tile in tiles) {
            _damageContainer.tile = tile.CellPos;
            _damageContainer.damage = damage * tile.DamageRatio;
            _damageContainer.exactHitPoint = container.exactHitPoint;

            _player.RequestDamageTile(_damageContainer);
        }
        WorldJuiceCreator.Instance.SpawnExplosion(_damageContainer.exactHitPoint);
    }

}
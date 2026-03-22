using r8teful;
using UnityEngine;

public class BlockOxygen : MonoBehaviour, IInitializableAbility {
    private AbilityInstance _ability;
    private PlayerManager _player;
    [SerializeField] private OxygenBubble _bubblePrefab;
    [SerializeField] private ValueModifiableComponent _values;
    public void Init(AbilityInstance instance, PlayerManager player) {
        _ability = instance;
        _player = player;
        _values.Register();
        ChunkManager.OnTileChanged += TileChanged;
    }
    private void OnDestroy() {
        ChunkManager.OnTileChanged -= TileChanged;
    }

    private void TileChanged(Vector3Int pos, ushort tileID) { 
        if (tileID != ResourceSystem.AirID) return; // Only care for breaking
        // Todo check for interval, limits, etc?
        var luck = _player.PlayerStats.GetStat(StatType.Luck);
        float chance = _values.GetValueNow(ValueKey.BlockOxygenChance);
        if (!RandomnessHelpers.GetBoolRoll(luck, chance))
            return;
        float oxygenIncrease = _values.GetValueNow(ValueKey.BlockOxygenAmount);
        // Spawn bubble and shoot it towards the player?
        Vector2 playerPos = _player.GetWorldPosition;
        Vector2 tilePos = new(pos.x, pos.y);
        Vector2 spawnPos = tilePos + Random.insideUnitCircle * 0.3f;
        Vector2 dirToPlayer = (playerPos - spawnPos).normalized;
        var b = Instantiate(_bubblePrefab, spawnPos, Quaternion.identity);
        b.Init(dirToPlayer, _player.OxygenManager, oxygenIncrease);
    }
}
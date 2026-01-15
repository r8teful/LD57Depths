using System.Collections;
using UnityEngine;

public class BlockOxygen : MonoBehaviour, IInitializableAbility {
    private AbilityInstance _ability;
    private NetworkedPlayer _player;
    [SerializeField] private OxygenBubble _bubblePrefab;
    public void Init(AbilityInstance instance, NetworkedPlayer player) {
        _ability = instance;
        _player = player;
        ChunkManager.OnTileChanged += TileChanged;
    }

    private void TileChanged(Vector3Int pos, ushort tileID) {
        if (tileID != ResourceSystem.AirID) return; // Only care for breaking
        // Todo check for interval, limits, etc?

        // Spawn bubble and shoot it towards the player?
        Vector2 playerPos = _player.GetWorldPosition;
        Vector2 tilePos = new(pos.x, pos.y);
        Vector2 spawnPos = tilePos + Random.insideUnitCircle * 0.3f;
        Vector2 dirToPlayer = (playerPos - spawnPos).normalized;
        var b = Instantiate(_bubblePrefab, spawnPos, Quaternion.identity);
        b.Init(dirToPlayer,_player.OxygenManager);
    }
}
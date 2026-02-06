using r8teful;
using System.Collections.Generic;
using UnityEngine;

public class TileUpgradeData {
    public int DropIncrease;

    public TileUpgradeData(int increase) {
        DropIncrease = increase;
    }

    // We might want to add more here like extra items to drop, etc
}

// We send this to the chunkmanager which uses it
public struct DropInfo {
    public ItemData ItemData;
    public int Amount;

    public DropInfo(ItemData item, int amount) : this() {
        ItemData = item;
        Amount = amount;    
    }

}

// Also holds upgrades related to drops like how many drop from a tile
public class WorldDropManager : StaticInstance<WorldDropManager> {
    [SerializeField] private DropPooled _dropPrefab;
    [SerializeField] private int _initialPoolSize = 200;

    // Queue is generally faster than List for First-In-First-Out pooling
    private Queue<DropPooled> _poolQueue = new Queue<DropPooled>();

    private Dictionary<ushort, TileUpgradeData> _tileUpgradeData = new();
    public void NewTileUpgrade(ushort tile, int increase) {
        if(_tileUpgradeData.TryGetValue(tile, out TileUpgradeData tileUpgradeData)) {
            tileUpgradeData.DropIncrease += increase;        
        } else {
            _tileUpgradeData.Add(tile, new(increase));
        }
    }

    public int GetTileDropAmount(TileSO tile, int increase = 0) {
        if (_tileUpgradeData.TryGetValue(tile.ID, out TileUpgradeData tileUpgradeData)) {
            return tileUpgradeData.DropIncrease + increase;
        } else {
            // If we later have several drops at start get the tileSO drop from here
            return 1 + increase;
        }
    }
    // its a list incase we want different drops from the same tile, right now we just add one 
    public List<DropInfo> GetDropData(TileSO tile) {
        var dropData = new List<DropInfo>();
        var drop = tile.drop;
        int maxDropAmount = 1;
        int dropAmount = 1;
        if (_tileUpgradeData.TryGetValue(tile.ID, out TileUpgradeData tileUpgradeData)) {
            maxDropAmount += tileUpgradeData.DropIncrease;
        }
        if(maxDropAmount > 1) {
            // We can possibly drop more than one, get the random drop amount based on luck
            var luck = PlayerManager.LocalInstance.PlayerStats.GetStat(StatType.Luck);
            dropAmount = RandomnessHelpers.GetDropScewed(maxDropAmount, luck);
        }
        dropData.Add(new(drop,dropAmount));
        return dropData;
    }

    protected override void Awake() {
        base.Awake();
        InitializePool();
    }

    private void InitializePool() {
        for (int i = 0; i < _initialPoolSize; i++) {
            CreateNewPoolObject();
        }
    }

    private DropPooled CreateNewPoolObject() {
        DropPooled drop = Instantiate(_dropPrefab, transform);
        drop.gameObject.SetActive(false);
        _poolQueue.Enqueue(drop);
        return drop;
    }

    public DropPooled SpawnDrop(Vector3 position, int amount, ItemData item) {
        if (_poolQueue.Count == 0) {
            // Auto-expand pool if we run out
            CreateNewPoolObject();
        }
        DropPooled drop = _poolQueue.Dequeue();
        drop.transform.SetPositionAndRotation(position, Quaternion.identity);
        drop.Init(item, amount);

        // Activate
        drop.gameObject.SetActive(true);

        return drop;
    }

    /// <summary>
    /// Call this from your PlayerPickupManager when the item is collected.
    /// </summary>
    public void ReturnToPool(DropPooled drop) {
        if (drop == null) return;

        drop.gameObject.SetActive(false);
        _poolQueue.Enqueue(drop);
    }
}
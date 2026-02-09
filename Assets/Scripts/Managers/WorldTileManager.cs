using r8teful;
using System.Collections.Generic;
using UnityEngine;

public class TileUpgradeData {
    public int DropIncrease;
    public float DurabilityIncrease; // as multiplier 

    public TileUpgradeData(int dropincrease) {
        DropIncrease = dropincrease;
        DurabilityIncrease = 1;
    }
    public TileUpgradeData(int dropincrease,float durIncrease) {
        DropIncrease = dropincrease;
        DurabilityIncrease = durIncrease;
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
public class WorldTileManager : StaticInstance<WorldTileManager> {
    [SerializeField] private DropPooled _dropPrefab;
    [SerializeField] private int _initialPoolSize = 200;

    // Queue is generally faster than List for First-In-First-Out pooling
    private Queue<DropPooled> _poolQueue = new Queue<DropPooled>();
    private Dictionary<ushort, TileUpgradeData> _tileUpgradeData = new();
    protected override void Awake() {
        base.Awake();
        InitializePool();
    }
    public void NewTileUpgrade(ushort tile, int increaseDrop,float increaseDur) {
        if(_tileUpgradeData.TryGetValue(tile, out TileUpgradeData tileUpgradeData)) {
            tileUpgradeData.DropIncrease += increaseDrop;
            tileUpgradeData.DurabilityIncrease += increaseDur;
        } else {
            _tileUpgradeData.Add(tile, new(increaseDrop,increaseDur + 1)); // +1 because we're basically summing the values afterwards and if we have already values like 1.4 we'd be adding way to much
        }
    }

    public int GetExtraTileDropAmount(TileSO tile, int increase = 0) {
        if (_tileUpgradeData.TryGetValue(tile.ID, out TileUpgradeData tileUpgradeData)) {
            return tileUpgradeData.DropIncrease + increase;
        } else {
            // If we later have several drops at start get the tileSO drop from here
            return 0 + increase;
        }
    }
    public float GetDurabilityIncrease(ushort tile) {
        if(_tileUpgradeData.TryGetValue(tile, out var data)) {
            return data.DurabilityIncrease;
        }
        return 1; // no increase, so just 1 (because its mult)
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

    public DropPooled SpawnDropOne(Vector3 position, int amount, ItemData item) {
        if (_poolQueue.Count == 0) {
            // Auto-expand pool if we run out
            CreateNewPoolObject();
        }
        // Slightly randomize drop position
        Vector3 spawnPos = position + (Vector3)Random.insideUnitCircle * 0.3f;
        DropPooled drop = _poolQueue.Dequeue();
        drop.transform.SetPositionAndRotation(spawnPos, Quaternion.identity);
        drop.Init(item, amount);

        // Activate
        drop.gameObject.SetActive(true);
        return drop;
    }
    public void SpawnDrop(Vector3 position, int amount, ItemData item) {
        int maxFromOne = 5;
        if(amount <= maxFromOne) {
            // One instance each
            for (int i = 0; i < amount; i++) {
                SpawnDropOne(position, 1, item);
            }
        } else {
            // Divide amount as evenly as possible
            int baseCount = amount / maxFromOne;
            int remainder = amount % maxFromOne;

            for (int i = 0; i < maxFromOne; i++) {
                int bucketAmount = baseCount + (i < remainder ? 1 : 0);
                if (bucketAmount <= 0) continue; // defensive
                SpawnDropOne(position, bucketAmount, item);
            }
        }
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
using r8teful;
using System.Collections;
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
    public GameObject prefabToDrop;
    public ushort itemID;
    public int amount;

    public DropInfo(GameObject prefab, int v, ushort id) : this() {
        prefabToDrop = prefab;
        amount = v;
        itemID = id;
    }

}

public class TileDropManager : StaticInstance<TileDropManager> {
    private Dictionary<ushort, TileUpgradeData> _tileUpgradeData = new();

    public void NewTileUpgrade(ushort tile, int increase) {
        if(_tileUpgradeData.TryGetValue(tile, out TileUpgradeData tileUpgradeData)) {
            tileUpgradeData.DropIncrease += increase;        
        } else {
            _tileUpgradeData.Add(tile, new(increase));
        }
    }

    // its a list incase we want different drops from the same tile
    public List<DropInfo> GetDropData(TileSO tile) {
        var dropData = new List<DropInfo>();
        var prefab = tile.drop.droppedPrefab;
        int maxDropAmount = 1; 
        if (_tileUpgradeData.TryGetValue(tile.ID, out TileUpgradeData tileUpgradeData)) {
            maxDropAmount += tileUpgradeData.DropIncrease;
        }
        if(maxDropAmount == 1) {
            dropData.Add(new(prefab, 1, tile.drop.ID));
            return dropData;
        }
        // Get the random drop amount based on luck
        var luck = NetworkedPlayer.LocalInstance.PlayerStats.GetStat(StatType.Luck);
        var dropAmount = RandomnessHelpers.GetDropScewed(maxDropAmount, luck);
        dropData.Add(new(prefab, dropAmount, tile.drop.ID));
        return dropData;
    } 
}
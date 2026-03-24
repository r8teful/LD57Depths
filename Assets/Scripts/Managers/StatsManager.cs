using r8teful;
using System.Collections.Generic;


// Tracks stats
public class StatsManager : Singleton<StatsManager>, ISaveable {
    private Dictionary<ushort, ulong> itemsGained;
    private Dictionary<ushort, ulong> plantsDestroyed;
    private Dictionary<ushort, ulong> blocksDestroyed;
    private Dictionary<string, int> characterWins;
    public ulong GetTotalItemsGained() {
        ulong total = 0;
        foreach (var value in itemsGained.Values)
            total += value;
        return total;
    }
    public ulong GetTotalPlantsDestroyed() {
        ulong total = 0;
        foreach (var value in plantsDestroyed.Values)
            total += value;
        return total;
    }
    public ulong GetTotalBlocksDestroyed() {
        ulong total = 0;
        foreach (var value in blocksDestroyed.Values)
            total += value;
        return total;
    }
    public int GetWinsForCharacter(string characterKey) {
        if(characterWins.TryGetValue(characterKey,out var wins)) {
            return wins;
        } else {
            return 0;
        }
    }
    public void GainItem(ushort id) {
        if (itemsGained.ContainsKey(id)) {
            itemsGained[id]++;
        } else {
            itemsGained.Add(id, 1);
        }
    }
    public void PlantDestroyed(ushort id) {
        if (plantsDestroyed.ContainsKey(id)) {
            plantsDestroyed[id]++;
        } else {
            plantsDestroyed.Add(id, 1);
        }
    }
    public void BlockDestroyed(ushort id) {
        if (blocksDestroyed.ContainsKey(id)) {
            blocksDestroyed[id]++;
        } else {
            blocksDestroyed.Add(id, 1);
        }
    }

    public void OnCharacterWin(string characterKey) {
        if(characterWins.ContainsKey(characterKey)){
            characterWins[characterKey]++;
        } else {
            characterWins.Add(characterKey,1);
        }
    }

    public void OnLoad(SaveData data) {
        if (data == null) return;
        if(data.statisticsData == null) return;
        itemsGained = data.statisticsData.itemsGained;
        plantsDestroyed = data.statisticsData.plantsDestroyed;
        blocksDestroyed = data.statisticsData.blocksDestroyed;
        characterWins = data.statisticsData.characterWins;
    }

    public void OnSave(SaveData data) {
        data.statisticsData.itemsGained = itemsGained;
        data.statisticsData.plantsDestroyed = plantsDestroyed;
        data.statisticsData.blocksDestroyed = blocksDestroyed;
        data.statisticsData.characterWins = characterWins;
    }
}
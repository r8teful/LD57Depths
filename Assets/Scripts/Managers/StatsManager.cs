using System.Collections.Generic;


// Tracks stats
public class StatsManager : Singleton<StatsManager> {
    private Dictionary<ushort, ulong> itemsGained;
    private Dictionary<ushort, ulong> plantsDestroyed;
    private Dictionary<ushort, ulong> blocksDestroyed;
    
    public void GainItem(ushort id) {
        // add entry, increment if it exists 
    }
}
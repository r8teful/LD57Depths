using System.Collections.Generic;

namespace r8teful {
    public class SaveData {
        // Meta
        public int schemaVersion = 1;   // increment when structure changes
        public string buildType = "";  // "demo" or "full"
        public string gameVersion = "";  // e.g. "0.4.1"
        public long lastSavedUtc = 0;

        // Persistant progression
        public float playTimeHours = 0f;
        //public SettingsSave settings = new();
        public StatData statisticsData = new();
        // Run specific (wiped when new run is started)
        public BobSaveData bobData = new();
        public WorldSaveData worldData = new();

        public bool HasRunData {
            get {
                if (worldData.savedChunks == null || worldData.savedChunks.Count == 0) return false;
                if (bobData.nodeSaveData == null || bobData.nodeSaveData.Count == 0) return false;
                return true;
            } 
        }
    }
    [System.Serializable]
    public class StatData {
        //public Dictionary<string, CharacterData> characterData;
        public Dictionary<string, int> characterWins;
        public Dictionary<ushort, ulong> itemsGained;
        public Dictionary<ushort, ulong> plantsDestroyed;
        public Dictionary<ushort, ulong> blocksDestroyed;
        public StatData() {
            characterWins = new Dictionary<string, int>();
            itemsGained = new Dictionary<ushort, ulong>();
            plantsDestroyed = new Dictionary<ushort, ulong>();
            blocksDestroyed = new Dictionary<ushort, ulong>();
        }
    }
    //[System.Serializable]
    //public class CharacterData {
    //    public int characterWins;
    //    public Dictionary<ushort, ulong> itemsGained;
    //    public Dictionary<ushort, ulong> plantsDestroyed;
    //    public Dictionary<ushort, ulong> blocksDestroyed;
    //    public CharacterData() {
    //        characterWins = 0;
    //        itemsGained = new Dictionary<ushort, ulong>();
    //        plantsDestroyed = new Dictionary<ushort, ulong>();
    //        blocksDestroyed = new Dictionary<ushort, ulong>();
    //    }
    //
    //}
    [System.Serializable]
    public class  BobSaveData {
        public Dictionary<ushort, int> nodeSaveData;
        public Dictionary<ushort, int> inventorySaveData;
        public BobSaveData() {
            nodeSaveData = new Dictionary<ushort, int>();
            inventorySaveData = new Dictionary<ushort, int>();
        }
    }
    [System.Serializable]
    public class ChunkSaveData {
        public List<ushort> tileIds; // Flattened list of Tile IDs
        public List<ushort> oreIds; 
        public List<byte> biomeId; 

        public ChunkSaveData() { 
            tileIds = new List<ushort>();
            oreIds = new List<ushort>();
            biomeId = new List<byte>(); 
        }

        public ChunkSaveData(int capacity) { 
            tileIds = new List<ushort>(capacity);
            oreIds = new List<ushort>(capacity);
            biomeId = new List<byte>(capacity); 
        }
        // Todo add entities
    }

    // Top-level container for the entire world save data
    [System.Serializable]
    public class WorldSaveData {
        // Use string keys for JSON compatibility across different serializers more easily
        public int Seed = 1;
        public Dictionary<string, ChunkSaveData> savedChunks; // string is chunk ID
        public Dictionary<ulong, PersistentEntityData> savedEntities; // ulong is persistantEntityID 
        public ulong nextPersistentEntityId; 
        
        public WorldSaveData() {
            savedChunks = new Dictionary<string, ChunkSaveData>();
            savedEntities = new Dictionary<ulong, PersistentEntityData>();
            nextPersistentEntityId = 0;
        }
    }
}
using System.Collections;
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

        // Run specific (wiped when new run is started)
        public BobSaveData bobData = new();
        public WorldSaveData worldData = new();

        public bool HasRunData {
            get {
                //if (worldData.savedChunks == null) return false;
                if (bobData.nodeSaveData == null) return false;
                //if (worldData.savedChunks.Count == 0 ) return false;
                if (bobData.nodeSaveData.Count  == 0) return false;
                return true;
            } 
        }
    }
    [System.Serializable]
    public class  BobSaveData {
        public Dictionary<ushort, int> nodeSaveData;
    }
    [System.Serializable]
    public class ChunkSaveData {
        public List<ushort> tileIds; // Flattened list of Tile IDs
        public List<float> tileDurabilities; // Save durability state
        public ChunkSaveData() { tileIds = new List<ushort>(); }
        public ChunkSaveData(int capacity) { tileIds = new List<ushort>(capacity); }
        // Todo add entities
    }

    // Top-level container for the entire world save data
    [System.Serializable]
    public class WorldSaveData {
        // Use string keys for JSON compatibility across different serializers more easily
        public int Seed = 1;
        public Dictionary<string, ChunkSaveData> savedChunks;
        public Dictionary<string, PersistentEntityData> savedInteriorEntities;
        
        public WorldSaveData() {
            savedChunks = new Dictionary<string, ChunkSaveData>();
        }
    }
}
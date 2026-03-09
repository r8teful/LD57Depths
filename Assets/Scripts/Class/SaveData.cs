using System.Collections;
using System.Collections.Generic;

namespace r8teful {
    public class SaveData {
        // Meta
        public int schemaVersion = 1;   // increment when structure changes
        public string buildType = "";  // "demo" or "full"
        public string gameVersion = "";  // e.g. "0.4.1"
        public long lastSavedUtc = 0;

        // Stats 
        public float playTimeHours = 0f;

        // State
        public PlayerSaveData player = new();
        public WorldSaveData world = new();
        //public SettingsSave settings = new();


    }
    [System.Serializable]
    public class  PlayerSaveData {
        // upgrades, et    
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
        public Dictionary<string, ChunkSaveData> savedChunks;
        public Dictionary<string, PersistentEntityData> savedInteriorEntities;
        
        public WorldSaveData() {
            savedChunks = new Dictionary<string, ChunkSaveData>();
        }
    }
}
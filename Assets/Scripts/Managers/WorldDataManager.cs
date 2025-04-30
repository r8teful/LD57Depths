using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Tilemaps;
[System.Serializable]
public class ChunkSaveData {
    public List<int> tileIds; // Flattened list of Tile IDs
    public List<int> tileDurabilities; // Save durability state
    public ChunkSaveData() { tileIds = new List<int>(); }
    public ChunkSaveData(int capacity) { tileIds = new List<int>(capacity); }
    // Todo add entities
}

// Top-level container for the entire world save data
[System.Serializable]
public class WorldSaveData {
    // Use string keys for JSON compatibility across different serializers more easily
    public Dictionary<string, ChunkSaveData> savedChunks;
    public Vector3 playerPosition; // Save player position too

    public WorldSaveData() {
        savedChunks = new Dictionary<string, ChunkSaveData>();
    }
}
public class WorldDataManager {
    private ChunkManager _chunkManager;
    private WorldManager _worldManager;
    [SerializeField] private string saveFileName = "world.json"; // Name of the save file    


    public void SaveWorld(Dictionary<Vector2Int, ChunkData> worldChunks, Vector3 playerPos) {
        WorldSaveData saveData = new WorldSaveData();
        var tileAssetToIdMap = _worldManager.GetTileToID();
        var chunkSize = _chunkManager.GetChunkSize();
        // --- Save Chunks ---
        foreach (KeyValuePair<Vector2Int, ChunkData> chunkPair in worldChunks) {
            // Only save chunks that have been generated/loaded AND potentially modified
            if (chunkPair.Value.hasBeenGenerated) // Or save all in worldChunks if memory isn't a concern
            {
                Vector2Int chunkCoord = chunkPair.Key;
                ChunkData chunkData = chunkPair.Value;

                ChunkSaveData chunkSave = new ChunkSaveData(chunkSize * chunkSize);
                for (int y = 0; y < chunkSize; y++) {
                    for (int x = 0; x < chunkSize; x++) {
                        TileBase tile = chunkData.tiles[x, y];
                        if (tileAssetToIdMap.TryGetValue(tile, out int tileId)) {
                            chunkSave.tileIds.Add(tileId);
                            chunkSave.tileDurabilities.Add(chunkData.tileDurability[x, y]);
                        } else {
                            Debug.LogWarning($"Tile '{tile?.name ?? "NULL"}' at [{x},{y}] in chunk {chunkCoord} has no ID mapping! Saving as air (ID 0).");
                            chunkSave.tileIds.Add(0); // Save as air/null ID
                        }
                    }
                }
                // Use a string key for better compatibility e.g. "x,y"
                string chunkKey = $"{chunkCoord.x},{chunkCoord.y}";
                saveData.savedChunks.Add(chunkKey, chunkSave);
            }
        }

        // --- Save Player Position ---
        if (playerPos != null) {
            saveData.playerPosition = playerPos;
        }


        // --- Serialize and Write to File ---
        try {
            string filePath = GetSaveFilePath();
            string json = JsonConvert.SerializeObject(saveData, Formatting.Indented, new JsonSerializerSettings() {
                ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore
            }); // Use Newtonsoft
            //string json = JsonUtility.ToJson(saveData, true); // Use Unity's (needs Dictionary workaround)
            File.WriteAllText(filePath, json);
            Debug.Log($"World saved to {filePath}");
        } catch (System.Exception e) {
            Debug.LogError($"Failed to save world: {e.Message}\n{e.StackTrace}");
        }
    }


    public void LoadWorld() {
        string filePath = GetSaveFilePath();

        if (File.Exists(filePath)) {
            try {
                string json = File.ReadAllText(filePath);
                WorldSaveData loadData = JsonConvert.DeserializeObject<WorldSaveData>(json); // Use Newtonsoft
                // WorldSaveData loadData = JsonUtility.FromJson<WorldSaveData>(json); // Use Unity's (needs Dictionary workaround)

                if (loadData != null && loadData.savedChunks != null) {
                    _worldManager.ClearAllData();

                    var idToTileAssetMap = _worldManager.GetIDToTile();
                    var chunkSize = _chunkManager.GetChunkSize();
                    foreach (KeyValuePair<string, ChunkSaveData> savedChunkPair in loadData.savedChunks) {
                        // Parse the string key back to Vector2Int
                        string[] keyParts = savedChunkPair.Key.Split(',');
                        if (keyParts.Length == 2 && int.TryParse(keyParts[0], out int x) && int.TryParse(keyParts[1], out int y)) {
                            Vector2Int chunkCoord = new Vector2Int(x, y);
                            ChunkSaveData chunkSave = savedChunkPair.Value;

                            if (chunkSave.tileIds.Count == chunkSize * chunkSize &&
                                 chunkSave.tileDurabilities.Count == chunkSize * chunkSize) 
                             {
                                ChunkData newChunk = new ChunkData(chunkSize, chunkSize);
                                int tileIndex = 0;
                                for (int localY = 0; localY < chunkSize; localY++) {
                                    for (int localX = 0; localX < chunkSize; localX++) {
                                        int tileId = chunkSave.tileIds[tileIndex];
                                        if (idToTileAssetMap.TryGetValue(tileId, out TileBase tileAsset)) {
                                            newChunk.tiles[localX, localY] = tileAsset;
                                            newChunk.tileDurability[localX, localY] = chunkSave.tileDurabilities[tileIndex];
                                            tileIndex++;
                                        } else {
                                            Debug.LogWarning($"Unknown Tile ID {tileId} found in chunk {chunkCoord} during load. Setting to null/air.");
                                            newChunk.tiles[localX, localY] = null; // Or airTile if not null
                                        }
                                    }
                                }
                                newChunk.hasBeenGenerated = true; // Mark as loaded/existing
                                newChunk.isModified = false; // Reset modified flag on load
                                _chunkManager.AddChunkData(chunkCoord, newChunk);
                            } else {
                                Debug.LogWarning($"Chunk {chunkCoord} has incorrect tile count ({chunkSave.tileIds.Count}) in save file. Skipping load for this chunk.");
                            }
                        } else {
                            Debug.LogWarning($"Invalid chunk key format '{savedChunkPair.Key}' in save file. Skipping.");
                        }

                    }

                    // --- Load Player Position ---
                   // if (playerTransform != null) {
                        // Ensure player doesn't fall through floor on load - may need adjustments
                       //playerTransform.position = loadData.playerPosition;
                        // Force physics update or short disable/enable of Rigidbody could be needed
                        // e.g. playerTransform.GetComponent<Rigidbody2D>()?.Sleep();
                        // e.g. playerTransform.GetComponent<Rigidbody2D>()?.WakeUp();

                    //}
                    // Update current chunk coord AFTER potentially moving player
                   
                    //currentPlayerChunkCoord = WorldToChunkCoord(playerTransform != null ? playerTransform.position : Vector3.zero);


                    Debug.Log($"World loaded successfully from {filePath}");
                    // Note: Initial chunk activation around player happens in Start() -> ChunkLoadingRoutine -> UpdateChunks
                } else {
                    Debug.LogError("Failed to deserialize world data or data was empty.");
                    // Optionally trigger initial generation if load fails catastrophically
                }
            } catch (System.Exception e) {
                Debug.LogError($"Failed to load world: {e.Message}\n{e.StackTrace}");
                // Optionally trigger initial generation if load fails
            }
        } else {
            Debug.Log("No save file found. Starting new world.");
            // No need to do anything else, world will generate as player moves
        }
    }
    private string GetSaveFilePath() {
        return Path.Combine(Application.persistentDataPath, saveFileName);
        // persistentDataPath is a good place for save files on most platforms
    }
}

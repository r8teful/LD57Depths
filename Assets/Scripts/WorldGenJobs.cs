using Unity.Collections;
using Unity.Jobs;
using Unity.Burst; // Optional, for Burst compilation
using UnityEngine;

// --- Job for Ore Generation ---
[BurstCompile] // Optional: Apply Burst for significant speedups
public struct GenerateOresJob : IJob {
    [ReadOnly] public NativeArray<ushort> baseTileIDs; // Input from GPU generation
    public NativeArray<ushort> processedOreIDs;       // Output: tile IDs with ores
    public Vector2Int chunkCoord;                      // For world-position-dependent logic
    public int chunkSize;
    public uint seed; // Seed for procedural generation

    private const ushort STONE_ID = 1; // Example ID for stone
    private const ushort IRON_ORE_ID = 3;
    private const ushort GOLD_ORE_ID = 11;
    private const ushort INVALID_ID = ushort.MaxValue;

    public void Execute() {
        // Simple Perlin noise based ore placement example
        // Note: Unity.Mathematics.Random can also be used within jobs for deterministic randomness
        Unity.Mathematics.Random random = new Unity.Mathematics.Random(seed + (uint)(chunkCoord.x * 1000 + chunkCoord.y));


        for (int y = 0; y < chunkSize; y++) {
            for (int x = 0; x < chunkSize; x++) {
                int index = y * chunkSize + x;
                ushort currentTile = baseTileIDs[index];

                if (currentTile == STONE_ID) // Only place ore in stone
                {
                    // Calculate world position for noise sampling (or use local chunk + seed)
                    float worldX = chunkCoord.x * chunkSize + x;
                    float worldY = chunkCoord.y * chunkSize + y;

                    // Example: Iron ore placement
                    // Using Unity.Mathematics.noise
                    float ironNoiseValue = Unity.Mathematics.noise.snoise(new Unity.Mathematics.float2(worldX * 0.1f, worldY * 0.1f));
                    if (ironNoiseValue > 0.6f && random.NextFloat() < 0.3f) // Adjust thresholds
                    {
                        processedOreIDs[index] = IRON_ORE_ID;
                        continue; // Don't place other ores if iron is placed
                    }

                    // Example: Gold ore placement (rarer)
                    float goldNoiseValue = Unity.Mathematics.noise.snoise(new Unity.Mathematics.float2(worldX * 0.2f + 100.0f, worldY * 0.2f + 100.0f)); // Different offset
                    if (goldNoiseValue > 0.75f && random.NextFloat() < 0.1f) {
                        processedOreIDs[index] = IRON_ORE_ID;
                    }
                } 
            }
        }
    }
}

// --- Job for Identifying Entity Spawn Locations (Example) ---
// This job might not modify tileIDs directly but output a list of potential spawn points.
[BurstCompile]
public struct FindEntitySpawnPointsJob : IJob {
    [ReadOnly] public NativeArray<ushort> tileIDs; // Input from previous stage (e.g., after ore gen)
    public NativeList<Vector3Int> potentialSpawnPoints; // Output: list of (worldX, worldY, entityTypeID)
    public Vector2Int chunkCoord;
    public int chunkSize;

    // Example entity type IDs
    private const int SPAWN_POINT_TYPE_ENEMY_CAVE = 1;
    // Example tile IDs
    private const ushort CAVE_FLOOR_ID = 5;

    public void Execute() {
        for (int y = 0; y < chunkSize; y++) {
            for (int x = 0; x < chunkSize; x++) {
                int index = y * chunkSize + x;
                ushort currentTile = tileIDs[index];

                if (currentTile == CAVE_FLOOR_ID) {
                    // Check surrounding tiles or other conditions if needed
                    // For simplicity, just add if it's a cave floor tile
                    int worldX = chunkCoord.x * chunkSize + x;
                    int worldY = chunkCoord.y * chunkSize + y;
                    potentialSpawnPoints.Add(new Vector3Int(worldX, worldY, SPAWN_POINT_TYPE_ENEMY_CAVE));
                }
            }
        }
    }
}
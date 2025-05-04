using Sirenix.OdinInspector;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Tilemaps;


[System.Serializable]
public struct DeterministicStructure {
    public string name;
    public int yLevel; // Exact Y level where this structure element appears
    public int minX;   // Min X range relative to trench center (0)
    public int maxX;   // Max X range relative to trench center (0)
    public TileBase structureTile;
    // Optional: Add pattern information if it's not just a single line
}

public static class WorldGen {
    private static Unity.Mathematics.Random noiseRandomGen;
    private static float seedOffsetX;
    private static float seedOffsetY;
    private static Dictionary<BiomeType, BiomeLayerSO> biomeLookup = new Dictionary<BiomeType, BiomeLayerSO>();
    private static WorldGenSettingSO _settings;
    private static float maxDepth;
    private static WorldManager worldmanager;
    private static List<WorldSpawnEntitySO> worldSpawnEntities;
    public static float GetDepth() => maxDepth;
    public static void Init(WorldGenSettingSO worldGenSettings, WorldManager wm) {
        _settings = worldGenSettings;
        worldmanager = wm;
        InitializeNoise();
        worldSpawnEntities = _settings.worldSpawnEntities;
        var maxD = -_settings.trenchBaseWidth / _settings.trenchWidenFactor;
        maxDepth = Mathf.Abs(maxD) * 0.90f; // 90% of the max theoretical depth
        biomeLookup.Clear();
        foreach (var biome in _settings.biomeLayers) {
            if (biome.biomeType != BiomeType.None && !biomeLookup.ContainsKey(biome.biomeType)) {
                biomeLookup.Add(biome.biomeType, biome);
                Debug.LogWarning($"Added biome: {biome.biomeType} to lookup");
            } else {
                Debug.LogWarning($"Duplicate or invalid biome name found: {biome.biomeType}");
            }
        }
        //_settings.biomeLayers.Sort((a, b) => a.startY.CompareTo(b.startY));
    }
    // Call this if you change the seed at runtime
    public static void InitializeNoise() {
        // Use the seed to initialize the random generator for noise offsets
        noiseRandomGen = new Unity.Mathematics.Random((uint)_settings.seed);
        // Generate large offsets based on the seed to shift noise patterns
        seedOffsetX = noiseRandomGen.NextFloat(-10000f, 10000f);
        seedOffsetY = noiseRandomGen.NextFloat(-10000f, 10000f);
        // Note: Unity.Mathematics.noise doesn't *directly* use this Random object for per-call randomness,
        // but we use it here to get deterministic offsets for the noise input coordinates.
    }



    internal static ChunkData GenerateChunk(int chunkSize, Vector3Int chunkOriginCell, out List<EntitySpawnInfo> entitySpawns) {
        //Debug.Log("Generating new chunk: " + chunkOriginCell);
        ChunkData chunkData = new ChunkData(chunkSize, chunkSize);
        entitySpawns = new List<EntitySpawnInfo>();
        // --- Pass 1: Base Terrain & Biome Assignment ---
        for (int y = 0; y < chunkSize; y++) {
            for (int x = 0; x < chunkSize; x++) {
                int worldX = chunkOriginCell.x + x;
                int worldY = chunkOriginCell.y + y;

                TileBase tile = DetermineBaseTerrainAndBiome(worldX, worldY, out BiomeType biomeType);
                chunkData.tiles[x, y] = tile; // Assign base tile

                // Store biome info if needed later (not doing yet)
                chunkData.biomeID[(byte)x, (byte)y] = (byte)biomeType;
            }
        }
        // --- Pass 2: Cave Generation ---
        GenerateNoiseCavesForChunk(chunkData, chunkOriginCell,chunkSize); // New function call

        // --- Pass 3: Ore Generation ---
        // Iterate again, placing ores only on non-cave, non-water tiles
        SpawnOresInChunk(chunkData, chunkOriginCell, chunkSize); // Encapsulate ore logic similar to structures/entities

        // --- Pass 4: Structure Placement ---
        // This needs careful design. It checks potential anchor points within the chunk.
        //PlaceStructuresInChunk(chunkData, chunkOriginCell,chunkSize);

        // --- Pass 5: Decorative Entity Spawning ---
        // Determines WHERE entities should be placed, adds them to chunkData.entitiesToSpawn
        SpawnEntitiesInChunk(chunkData, chunkOriginCell, chunkSize, entitySpawns);

        return chunkData;
    }
    // 0 Air, 1 Stone, 
    // --- Pass 1 Helper: Determine Base Terrain & Primary Biome ---
    private static TileBase DetermineBaseTerrainAndBiome(int worldX, int worldY, out BiomeType primaryBiome) {
        primaryBiome = BiomeType.None; // Default to no specific biome

        // Surface
        if (worldY > _settings.surfaceCenterY - _settings.surfaceMaxDepth - _settings.surfaceNoiseAmplitude) {
            // Calculate the actual noisy surface boundary Y value at this X coordinate
            // Noise value [0, 1] maps to depth [0, surfaceMaxDepth]
            float surfaceDepthAtX = GetNoise(worldX, worldY, _settings.surfaceNoiseFrequency) * _settings.surfaceMaxDepth;
            // Add additional wiggle noise
            surfaceDepthAtX += (GetNoise(worldX + 1000, worldY + 1000, _settings.surfaceNoiseFrequency * 2f) - 0.5f) * 2f * _settings.surfaceNoiseAmplitude;
            surfaceDepthAtX = Mathf.Clamp(surfaceDepthAtX, 0, _settings.surfaceMaxDepth * 1.5f); // Clamp variation a bit

            float boundaryY = _settings.surfaceCenterY - surfaceDepthAtX;

            if (worldY >= boundaryY) {
                // Above or at the noisy surface level - it's water (or air if you prefer)
                primaryBiome = BiomeType.Surface;
                return worldmanager.GetTileFromID(0);//_settings.surfaceWaterTile ?? _settings.mainWaterTile; // Use specified surface water or fallback
            }
            // If below the noisy surface level, proceed to trench/biome checks
        }
        // --- 2. Trench Definition ---
        // (Same trench logic as before - calculate noisyHalfWidth)
        float halfTrenchWidth = (_settings.trenchBaseWidth + Mathf.Abs(worldY) * _settings.trenchWidenFactor) / 2f;
        float edgeNoise = (GetNoise(worldX, worldY + 5000, _settings.trenchEdgeNoiseFrequency) - 0.5f) * 2f; // Use offset Y noise sample
        float noisyHalfWidth = halfTrenchWidth + edgeNoise * _settings.trenchEdgeNoiseAmplitude;
        noisyHalfWidth = Mathf.Max(0, noisyHalfWidth);

        if (Mathf.Abs(worldX) < noisyHalfWidth && Mathf.Abs(worldY) < maxDepth) {
            primaryBiome = BiomeType.Trench;
            return worldmanager.GetTileFromID(0); // Inside main trench
        }

        // --- 3. Biome Check (Priority Based - Uses Sorted List) ---
        // Iterate through biomes (sorted bottom-up by StartY in Awake)
        foreach (var biome in _settings.biomeLayers) {
            // --- Check Horizontal Range (with Noise) ---
            float maxDist = biome.maxHorizontalDistanceFromTrenchCenter;
            float horizontalNoiseShift = (GetNoise(worldX + 2000, worldY, biome.horizontalEdgeNoiseFrequency) - 0.5f) * 2f * biome.horizontalEdgeNoiseAmplitude;
            float noisyMaxHorizDist = Mathf.Max(0, maxDist + horizontalNoiseShift); // Don't let max dist go below 0

            if (Mathf.Abs(worldX) >= noisyMaxHorizDist) {
                continue; // Outside this biome's noisy horizontal range
            }


            // --- Check Vertical Range (with Noise) ---
            // Use different noise samples for start/end for independent boundaries
            float startNoiseShift = (GetNoise(worldX, worldY + 3000, biome.verticalEdgeNoiseFrequency) - 0.5f) * 2f * biome.verticalEdgeNoiseAmplitude;
            float endNoiseShift = (GetNoise(worldX, worldY + 4000, biome.verticalEdgeNoiseFrequency) - 0.5f) * 2f * biome.verticalEdgeNoiseAmplitude;

            float noisyStartY = biome.startY + startNoiseShift;
            float noisyEndY = biome.endY + endNoiseShift;

            // Ensure EndY is generally above StartY even with noise, adjust if necessary based on desired overlap behaviour
            // Example simple clamp: if (noisyEndY < noisyStartY + 1) noisyEndY = noisyStartY + 1; // Ensure min 1 unit height

            if (worldY >= noisyStartY && worldY < noisyEndY) {
                // --- Match Found! ---
                // This is the highest priority biome (lowest StartY checked first) that contains this point.
                primaryBiome = biome.biomeType;
                return biome.defaultGroundTile;
            }
        }
        // Fallback if outside all biome influences
        return worldmanager.GetTileFromID(1);
    }



    // --- New Cave Generation Function (Pass 2) ---
    private static void GenerateNoiseCavesForChunk(ChunkData chunkData, Vector3Int chunkOriginCell, int chunkSize) {
        for (int y = 0; y < chunkSize; y++) {
            for (int x = 0; x < chunkSize; x++) {

                // Only carve caves into existing rock/ground tiles
                if (!IsRock(chunkData.tiles[x, y])) {
                    continue;
                }

                int worldX = chunkOriginCell.x + x;
                int worldY = chunkOriginCell.y + y;

                // --- Determine Cave Settings for this tile ---
                BiomeType biomeName = (BiomeType)chunkData.biomeID[x, y];
                BiomeCaveSettings settingsToUse = _settings.globalCaveSettings;

                if (biomeLookup.TryGetValue(biomeName, out BiomeLayerSO biome)) {
                    if (biome.caveSettings.overrideGlobalCaveSettings) {
                        settingsToUse = biome.caveSettings;
                    }
                }

                // --- Check if caves are enabled for this context ---
                if (!settingsToUse.generateCavesInBiome) {
                    continue;
                }

                // --- Calculate Warped Cave Noise Value ---
                // 1. Calculate Warp Offsets (using different noise samples for X and Y offset)
                // Using slightly offset coordinates/seeds for the warp noises ensures they aren't identical.
                float warpOffsetX = GetNoise(worldX + 100.5f, worldY + 200.7f, settingsToUse.warpFrequency);
                float warpOffsetY = GetNoise(worldX - 300.2f, worldY - 400.9f, settingsToUse.warpFrequency);

                // Map noise [0,1] to offset range [-amp, +amp]
                warpOffsetX = (warpOffsetX - 0.5f) * 2f * settingsToUse.warpAmplitude;
                warpOffsetY = (warpOffsetY - 0.5f) * 2f * settingsToUse.warpAmplitude;

                // 2. Apply Warp and Get Base Cave Value
                float warpedX = worldX + warpOffsetX;
                float warpedY = worldY + warpOffsetY;
                float caveValue = GetNoise(warpedX, warpedY, settingsToUse.baseCaveFrequency);

                if (settingsToUse.useDetailNoise) {
                   float detailNoise = GetNoise(worldX, worldY, settingsToUse.detailFrequency);
                   // Modify caveValue based on detail noise, e.g., lerp towards 0.5 based on detail
                   caveValue = Mathf.Lerp(caveValue, 0.5f, (detailNoise - 0.5f) * settingsToUse.detailInfluence);
                }

                // --- Apply Threshold ---
                if (caveValue < settingsToUse.caveThreshold) {
                    // Replace rock with cave water
                    TileBase caveTile = worldmanager.GetTileFromID(0); 
                    chunkData.tiles[x, y] = caveTile;
                }
            }
        }
    }

    // --- Pass 3 Helper 
    private static void SpawnOresInChunk(ChunkData chunkData, Vector3Int chunkOriginCell, int chunkSize) {
        for (int y = 0; y < chunkSize; y++) {
            for (int x = 0; x < chunkSize; x++) {
                TileBase currentTile = chunkData.tiles[x, y];
                if (IsRock(currentTile)) // Check if it's a valid tile for ore placement
                {
                    int worldX = chunkOriginCell.x + x;
                    int worldY = chunkOriginCell.y + y;
                    string biomeName = null;//GetBiomeNameAt(worldX, worldY);

                    TileBase oreTile = DetermineOre(worldX, worldY, biomeName);
                    if (oreTile != null) {
                        //Debug.Log($"Generating ore at: X: {worldX} Y: {worldY}");
                        chunkData.oreID[x, y] = worldmanager.GetIDFromOre(oreTile as TileSO);
                    }
                }
            }
        }
    }
    private static TileBase DetermineOre(int worldX, int worldY, string biomeName) {
        TileBase foundOre = null;
        // Check Ores (consider priority/order)
        foreach (var ore in _settings.oreTypes) {
            //if (biomeName == null || !ore.allowedBiomeNames.Contains(biomeName)) continue;
            
            float clusterNoise = 1.0f; // Assume cluster passes if not required
            if (ore.requireCluster) {
                clusterNoise = GetNoise(worldX, worldY, ore.clusterFrequency);
            }

            if (clusterNoise > ore.clusterThreshold) {
                float oreNoise = GetNoise(worldX, worldY, ore.frequency);
                if (oreNoise > ore.threshold) {
                    foundOre = ore.tile; // Last ore checked wins - adjust list order for priority
                }
            }
        }
        return foundOre;
    }

    /*
    // --- Pass 4: Structure Placement ---
    private static void PlaceStructuresInChunk(ChunkData chunkData, Vector3Int chunkOriginCell, int chunkSize) {
        // Check potential anchor points *within* this chunk.
        // Structures can extend *outside* the chunk, tiles outside will be placed when neighbour chunk is generated.
        for (int localY = 0; localY < chunkSize; ++localY) {
            for (int localX = 0; localX < chunkSize; ++localX) {
                int worldX = chunkOriginCell.x + localX;
                int worldY = chunkOriginCell.y + localY;

                // Check each structure definition to see if it *could* spawn here
                foreach (var structureDef in worldGenerator.structureDefinitions) {
                    // Basic filtering
                    if (worldY < structureDef.minY || worldY > structureDef.maxY) continue;
                    int distFromTrench = Mathf.Abs(worldX);
                    if (distFromTrench < structureDef.minDistanceFromTrench || distFromTrench > structureDef.maxDistanceFromTrench) continue;

                    // Use Hashing/Noise for Sparse Placement
                    // Using hash on world coords ensures structure appears at same world spot regardless of chunk borders
                    float placementValue = worldGenerator.GetHash(worldX, worldY); // Or GetNoise with structureDef.placementFrequency
                    if (placementValue < structureDef.placementThreshold) continue; // Only spawn if value passes check

                    // Optional: Biome Check
                    if (structureDef.requiredBiomeNames != null && structureDef.requiredBiomeNames.Count > 0) {
                        string biomeName = GetBiomeNameAt(worldX, worldY);
                        if (biomeName == null || !structureDef.requiredBiomeNames.Contains(biomeName)) continue;
                    }

                    // --- Potential Spawn Point Found - Now Overlay the Structure ---
                    Vector2Int patternSize = structureDef.size;
                    Vector2Int anchorOffset = structureDef.anchor;

                    for (int py = 0; py < patternSize.y; ++py) {
                        for (int px = 0; px < patternSize.x; ++px) {
                            TileBase tileToPlace = structureDef.pattern[py * patternSize.x + px];
                            if (tileToPlace == null) continue; // Skip empty spots in pattern

                            // Calculate world coordinates for this part of the structure pattern
                            int targetWorldX = worldX + px - anchorOffset.x;
                            int targetWorldY = worldY + py - anchorOffset.y;

                            // Calculate local coords within the *current* chunk being generated
                            int targetLocalX = targetWorldX - chunkOriginCell.x;
                            int targetLocalY = targetWorldY - chunkOriginCell.y;

                            // *** IMPORTANT: Only place tile if it falls within THIS chunk's boundaries ***
                            if (targetLocalX >= 0 && targetLocalX < chunkSize &&
                                targetLocalY >= 0 && targetLocalY < chunkSize) {
                                // Check placement rules (overwriting)
                                TileBase existingTile = chunkData.tiles[targetLocalX, targetLocalY];
                                bool canPlace = true;
                                if (!structureDef.placeOverWater && (existingTile == worldGenerator.mainWaterTile || existingTile == worldGenerator.caveWaterTile)) {
                                    canPlace = false;
                                }
                                // Crude check, assumes IsRock=true for ores
                                if (!structureDef.placeOverOres && IsRock(existingTile) && DetermineOre(targetWorldX, targetWorldY, GetBiomeNameAt(targetWorldX, targetWorldY)) != null) {
                                    canPlace = false;
                                }


                                if (canPlace) {
                                    chunkData.tiles[targetLocalX, targetLocalY] = tileToPlace;
                                }
                            }
                            // Tiles for this structure that fall into neighbour chunks
                            // will be placed when those neighbours are generated.
                        }
                    }
                    // Optional: If only one structure per anchor point is desired, add 'break;' here.
                    // Otherwise multiple structures could potentially try to spawn from the same point.
                }
            }
        }
    }*/

    // --- Pass 5 Implementation ---
    private static void SpawnEntitiesInChunk(ChunkData chunkData, Vector3Int chunkOriginCell, int chunkSize, List<EntitySpawnInfo> entitySpawns) {
        if (worldSpawnEntities == null || worldSpawnEntities.Count == 0) return;
        // Use a temporary set for basic overlap check within this chunk pass
        HashSet<Vector2Int> occupiedAnchors = new HashSet<Vector2Int>();

        // Iterate through potential ANCHOR points in the chunk
        // An anchor is typically a ground tile surface
        for (int y = 0; y < chunkSize; y++) // Iterate y from 0 upwards
        {
            for (int x = 0; x < chunkSize; x++) {
                // --- Identify potential anchor tile ---
                TileBase anchorTile = chunkData.tiles[x, y];
                if (anchorTile == null || anchorTile == worldmanager.GetTileFromID(0)) {
                    continue; // Not a valid ground anchor tile type
                }
                // Optimization: If this anchor is already used by another entity in this pass, skip
                if (occupiedAnchors.Contains(new Vector2Int(x, y))) {
                    continue;
                }

                int worldX = chunkOriginCell.x + x;
                int worldY = chunkOriginCell.y + y;

                // --- Check conditions applicable to the anchor point itself ---
                bool isWaterSpawn = !IsRock(anchorTile); 
                bool isGroundBelow = IsRock(anchorTile); // 

                // Quick check using first entity's height needs, real check below
                bool clearAbove = true;
                for (int h = 1; h <= Mathf.Max(1, worldSpawnEntities.Count > 0 ? worldSpawnEntities[0].minCeilingHeight : 1); ++h) {
                    if (y + h < chunkSize) {
                        TileBase tileAbove = chunkData.tiles[x, y + h];
                        if (IsRock(tileAbove)) { // Is there solid ground above?
                            clearAbove = false;
                            break;
                        }
                    } else {
                        // Reached top of chunk, assume not clear unless world boundary says otherwise
                        clearAbove = false;
                        break;
                    }
                }
                if (!clearAbove) continue; // Trying to spawn in a solid block, skip this block
                //Debug.Log($"Valid placement found at x: {worldX} and y: {worldY}");

                bool adjacentToWater = false;
                if (worldSpawnEntities.Exists(def => def.requireWaterAdjacent)) // Optimization: only check if any entity needs it
                {
                    adjacentToWater = IsAdjacentWater(chunkData, x, y);
                }


                // --- Iterate through Entity Definitions ---
                foreach (var entityDef in worldSpawnEntities) {
                    if (entityDef.entityPrefab == null) continue; // Skip definitions without prefabs

                    // 1. Basic Filters (Position, Biome)
                    if (worldY < entityDef.minY || worldY > entityDef.maxY) continue;
                    int distFromTrench = Mathf.Abs(worldX);

                    if (entityDef.requiredBiomeNames != null && entityDef.requiredBiomeNames.Count > 0) {
                        string biomeName = null;//GetBiomeNameAt(worldX, worldY);
                        if (biomeName == null || !entityDef.requiredBiomeNames.Contains(biomeName)) continue;
                    }

                    // 2. Condition Checks
                    if (entityDef.requireSolidGroundBelow && !isGroundBelow) continue;
                    if (entityDef.requireWaterAdjacent && !adjacentToWater && !isWaterSpawn) continue;

                    // Check specific ceiling height required by *this* entity definition
                    if (entityDef.requireCeilingSpace) {
                        bool specificCeilingClear = true;
                        for (int h = 1; h <= entityDef.minCeilingHeight; ++h) {
                            if (y + h < chunkSize) {
                                TileBase tileAbove = chunkData.tiles[x, y + h];
                                if (IsRock(tileAbove)) {
                                    specificCeilingClear = false;
                                    break;
                                }
                            } // else: Assume clear above chunk
                        }
                        if (!specificCeilingClear) continue;
                    }


                    // 3. Stochastic Check (Noise/Hashing)
                    float placementValue = GetNoise(worldX, worldY, entityDef.placementFrequency);
                    // Could also use GetHash(worldX, worldY) for non-frequency based sparsity
                    if (placementValue < entityDef.placementThreshold) continue;


                    // --- ALL CHECKS PASSED --- Spawn this entity ---
                    Vector3Int spawnPos = new(worldX, worldY);

                    // Calculate rotation
                    Quaternion spawnRot = entityDef.randomYRotation ?
                                           Quaternion.Euler(0, noiseRandomGen.NextFloat(0f, 360f), 0) : // Use the seeded random gen
                                           Quaternion.identity;

                    // Add to the spawn list
                    entitySpawns.Add(new EntitySpawnInfo(entityDef.entityPrefab,entityDef.entityID, spawnPos, spawnRot,Vector3.one)); // todo add scale

                    // Mark anchor as occupied for this pass to prevent overlap *at the anchor*
                    occupiedAnchors.Add(new Vector2Int(x, y));

                    // Optional: Implement more robust clearance checking here if needed, potentially
                    // marking a larger area than just the anchor in occupiedAnchors or a separate grid.

                    // If only one entity type should spawn per valid anchor point, uncomment break:
                    break;
                }
            }
        }
    }

    private static bool IsRock(TileBase tile) {
        return tile != null && (tile != worldmanager.GetTileFromID(0) && tile != worldmanager.GetTileFromID(2));
        // Add checks for air tiles if you have them
    }
    // Helper for adjacent water check
    private static bool IsAdjacentWater(ChunkData chunkData, int x, int y) {
        int width = chunkData.tiles.GetLength(0);
        int height = chunkData.tiles.GetLength(0);


        int[,] neighbors = { { 0, 1 }, { 0, -1 }, { 1, 0 }, { -1, 0 } }; // N, S, E, W

        for (int i = 0; i < neighbors.GetLength(0); ++i) {
            int nx = x + neighbors[i, 0];
            int ny = y + neighbors[i, 1];

            // Check bounds (simple version, doesn't check neighbour chunks)
            if (nx >= 0 && nx < width && ny >= 0 && ny < height) {
                TileBase neighborTile = chunkData.tiles[nx, ny];
                if (neighborTile == worldmanager.GetTileFromID(0)){//|| neighborTile == worldGenerator.caveWaterTile) {
                    return true;
                }
            }
            // Else: Tile is outside this chunk's data. For perfect checks,
            // you'd need access to neighbor chunk data here. Often ignored for performance.
        }
        return false;
    }
    private static float GetNoise(float x, float y, float frequency) {
        // Apply seed offsets and frequency
        float sampleX = (x + seedOffsetX) * frequency;
        float sampleY = (y + seedOffsetY) * frequency;
        // noise.snoise returns value in range [-1, 1], remap to [0, 1]
        return (noise.snoise(new float2(sampleX, sampleY)) + 1f) * 0.5f;
    }
    // Helper for deterministic hashing (useful for structure placement)
    private static float GetHash(int x, int y) {
        // Simple hash combining seed, x, y. Replace with a better one if needed.
        uint hash = (uint)_settings.seed;
        hash ^= (uint)x * 73856093;
        hash ^= (uint)y * 19349663;
        hash ^= (uint)(x * y) * 83492791;
        return (hash & 0x0FFFFFFF) / (float)0x0FFFFFFF; // Convert to [0, 1] float
    }

}


using Sirenix.OdinInspector;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Tilemaps;

[System.Serializable]
public struct BiomeLayer {
    public string name;
    public TileBase defaultGroundTile;
    public int startY; // Center Y of the biome layer
    public int maxHeight; // Max vertical distance from center Y this biome extends
    public int maxHorizontalDistanceFromTrenchCenter; // Max horizontal extent
    [Range(0.0f, 1.0f)] public float verticalBlendPercentage; // How much of maxHeight is used for blending?
    [Range(0.0f, 1.0f)] public float horizontalBlendPercentage; // How much of maxHorizontalDist is used for blending?
}

[System.Serializable]
public struct OreType {
    public string name;
    public TileBase tile;
    public List<string> allowedBiomeNames; // Names of biomes where this ore can spawn
    public float frequency;     // Noise frequency for this ore
    [Range(0f, 1f)] public float threshold; // Noise value above which ore spawns (higher = rarer)
    public float clusterFrequency; // Lower frequency noise for controlling large clusters
    [Range(0f, 1f)] public float clusterThreshold; // Threshold for cluster noise
    public bool requireCluster; // Must the cluster noise also be above threshold?
}

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
    private static Dictionary<string, BiomeLayer> biomeLookup = new Dictionary<string, BiomeLayer>();
    private static Dictionary<int, TileBase> idToTileAssetMap;
    private static WorldGenSettingSO _settings;
    private static float maxDepth;
    public static float GetDepth() => maxDepth;
    public static void Init(WorldGenSettingSO worldGenSettings, Dictionary<int, TileBase> assetMap) {
        _settings = worldGenSettings;
        idToTileAssetMap = assetMap;
        InitializeNoise();

        var maxD = -_settings.trenchBaseWidth / _settings.trenchWidenFactor;
        maxDepth = Mathf.Abs(maxD) * 0.90f; // 90% of the max theoretical depth
        biomeLookup.Clear();
        foreach (var biome in _settings.biomeLayers) {
            if (!string.IsNullOrEmpty(biome.name) && !biomeLookup.ContainsKey(biome.name)) {
                biomeLookup.Add(biome.name, biome);
            } else {
                Debug.LogWarning($"Duplicate or invalid biome name found: {biome.name}");
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



    internal static ChunkData GenerateChunk(int chunkSize, Vector3Int chunkOriginCell) {
        Debug.Log("Generating new chunk: " + chunkOriginCell);
        ChunkData chunkData = new ChunkData(chunkSize, chunkSize);

        // Intermediate data for CA (bool could be byte for different initial states)
        bool[,] isPotentialCave = _settings.generateCaves ? new bool[chunkSize, chunkSize] : null;

        // --- Pass 1: Base Terrain & Biome Assignment ---
        // (And marking potential caves)
        for (int y = 0; y < chunkSize; y++) {
            for (int x = 0; x < chunkSize; x++) {
                int worldX = chunkOriginCell.x + x;
                int worldY = chunkOriginCell.y + y;

                TileBase tile = DetermineBaseTerrainAndBiome(worldX, worldY, out string biomeName);
                chunkData.tiles[x, y] = tile; // Assign base tile

                // Store biome info if needed later (optional)
                // chunkData.biomeNames[x,y] = biomeName;

                // Mark potential caves using noise (only if the base tile is rock)
                if (_settings.generateCaves && IsRock(tile)) // Need IsRock helper function
                {
                    float caveNoise = GetNoise(worldX, worldY, _settings.initialCaveNoiseFrequency);
                    if (caveNoise < _settings.initialCaveNoiseThreshold) // Use '<' for noise floor as caves
                    {
                        isPotentialCave[x, y] = true;
                    }
                }
            }
        }
        // --- Pass 2: Cellular Automata Caves ---
        if (_settings.generateCaves && isPotentialCave != null) {
            // IMPORTANT: For CA, you often need neighbor info from adjacent chunks.
            // This basic version only runs CA within the chunk's bounds.
            // A more robust solution would require reading border tiles from neighbour chunks.
            RunCellularAutomata(chunkData, isPotentialCave, _settings.caveCASteps, chunkSize);
        }

        // --- Pass 3: Ore Generation ---
        // Iterate again, placing ores only on non-cave, non-water tiles
        for (int y = 0; y < chunkSize; y++) {
            for (int x = 0; x < chunkSize; x++) {
                TileBase currentTile = chunkData.tiles[x, y];
                if (IsRock(currentTile)) // Only place ores in solid rock
                {
                    int worldX = chunkOriginCell.x + x;
                    int worldY = chunkOriginCell.y + y;
                    string biomeName = GetBiomeNameAt(worldX, worldY); // Need this helper

                    TileBase oreTile = DetermineOre(worldX, worldY, biomeName);
                    if (oreTile != null) {
                        chunkData.tiles[x, y] = oreTile;
                    }
                }
            }
        }


        // --- Pass 4: Structure Placement ---
        // This needs careful design. It checks potential anchor points within the chunk.
        //PlaceStructuresInChunk(chunkData, chunkOriginCell,chunkSize);
        
        return chunkData;
    }
    // 0 Air, 1 Stone, 
    // --- Pass 1 Helper: Determine Base Terrain & Primary Biome ---
    private static TileBase DetermineBaseTerrainAndBiome(int worldX, int worldY, out string primaryBiomeName) {
        primaryBiomeName = null; // Default to no specific biome

        // 1. Trench Shape
        float halfTrenchWidth = (_settings.trenchBaseWidth + Mathf.Abs(worldY) * _settings.trenchWidenFactor) / 2f;
        float edgeNoise = (GetNoise(worldX, worldY, _settings.trenchEdgeNoiseFrequency) - 0.5f) * 2f;
        float noisyHalfWidth = halfTrenchWidth + edgeNoise * _settings.trenchEdgeNoiseAmplitude;
        noisyHalfWidth = Mathf.Max(0, noisyHalfWidth);
        // Calculate the theoretical worldY where halfTrenchWidth would be 0
        if (Mathf.Abs(worldX) < noisyHalfWidth && Mathf.Abs(worldY) < maxDepth) {
            return idToTileAssetMap[0]; // Main trench
        } else if (worldY > 0) {
            return idToTileAssetMap[0]; // Surface
        }

        // 2. Biome Influence Calculation (Handles multiple overlapping potentials and blending)
        float totalWeight = 0f;
        Dictionary<string, float> biomeWeights = new Dictionary<string, float>();

        foreach (var biome in _settings.biomeLayers) {
            float weight = CalculateBiomeInfluence(worldX, worldY, biome);
            if (weight > 0.001f) // Use a small threshold
            {
                biomeWeights.Add(biome.name, weight);
                totalWeight += weight;
            }
        }

        // 3. Determine Dominant Biome and Tile
        if (totalWeight > 0f) {
            float maxWeight = 0f;
            string dominantBiome = null;

            // Normalize weights and find dominant biome
            foreach (var pair in biomeWeights) {
                float normalizedWeight = pair.Value / totalWeight; // Can be used for blending later if needed
                if (normalizedWeight > maxWeight) {
                    maxWeight = normalizedWeight;
                    dominantBiome = pair.Key;
                }
                // Optional: store normalizedWeight for later blending steps if needed
            }
            if (dominantBiome != null && biomeLookup.TryGetValue(dominantBiome, out BiomeLayer chosenBiome)) {
                primaryBiomeName = dominantBiome;
                // TODO: Implement actual blending here if desired (e.g., using RuleTiles based on neighbours, or lerping colors/properties)
                // For now, just return the dominant biome's default tile.
                return chosenBiome.defaultGroundTile;
            }
        }

        // Fallback if outside all biome influences
        return idToTileAssetMap[1];
    }


    // --- Helper: Calculate Influence of a Single Biome ---
    private static float CalculateBiomeInfluence(int worldX, int worldY, BiomeLayer biome) {
        // Vertical Check
        float dy = Mathf.Abs(worldY - biome.startY);
        float maxVertDist = biome.maxHeight / 2.0f;
        if (dy > maxVertDist) return 0f; // Outside max vertical range

        // Horizontal Check
        float dx = Mathf.Abs(worldX);
        if (dx > biome.maxHorizontalDistanceFromTrenchCenter) return 0f; // Outside max horizontal range

        // --- Calculate Blend Weights (using smoothstep for nice transitions) ---
        // Vertical Blend
        float vertBlendZone = maxVertDist * biome.verticalBlendPercentage;
        float vertCoreZone = maxVertDist - vertBlendZone;
        float vertWeight = 1.0f;
        if (dy > vertCoreZone && vertBlendZone > 0) {
            vertWeight = 1.0f - Mathf.SmoothStep(0.0f, 1.0f, (dy - vertCoreZone) / vertBlendZone);
        }

        // Horizontal Blend
        float horizBlendZone = biome.maxHorizontalDistanceFromTrenchCenter * biome.horizontalBlendPercentage;
        float horizCoreZone = biome.maxHorizontalDistanceFromTrenchCenter - horizBlendZone;
        float horizWeight = 1.0f;
        if (dx > horizCoreZone && horizBlendZone > 0) {
            horizWeight = 1.0f - Mathf.SmoothStep(0.0f, 1.0f, (dx - horizCoreZone) / horizBlendZone);
        }

        // Combine weights (multiplication means influence drops off in both blend zones)
        return vertWeight * horizWeight;
    }

    // --- Pass 2: Cellular Automata ---
    private static void RunCellularAutomata(ChunkData chunkData, bool[,] isWall, int steps, int chunkSize) {
        int width = chunkSize;
        int height = chunkSize;
        bool[,] currentWalls = (bool[,])isWall.Clone(); // Work on a copy

        for (int step = 0; step < steps; step++) {
            bool[,] nextWalls = new bool[width, height];
            for (int y = 0; y < height; y++) {
                for (int x = 0; x < width; x++) {
                    int neighborWallCount = CountAliveNeighbors(currentWalls, x, y);

                    if (currentWalls[x, y]) // If it's currently a wall
                    {
                        nextWalls[x, y] = (neighborWallCount >= _settings.caveSurvivalThreshold);
                    } else // If it's currently empty space
                      {
                        nextWalls[x, y] = (neighborWallCount >= _settings.caveBirthThreshold);
                    }
                }
            }
            currentWalls = nextWalls; // Update for next iteration
        }

        // Apply the final CA result to the chunk data
        for (int y = 0; y < height; y++) {
            for (int x = 0; x < width; x++) {
                if (currentWalls[x, y] && IsRock(chunkData.tiles[x, y])) {
                    // Keep it as the rock tile it was
                } else if (!currentWalls[x, y] && IsRock(chunkData.tiles[x, y])) {
                    // Turn rock into cave water if CA removed the wall
                    chunkData.tiles[x, y] = idToTileAssetMap[0]; // Empty water tile
                }
                // Else: Don't overwrite main water or already existing cave water
            }
        }
    }

    // --- Helper for CA: Count Neighbours ---
    // Includes diagonals, treats edge as wall
    private static int CountAliveNeighbors(bool[,] map, int x, int y) {
        int count = 0;
        int width = map.GetLength(0);
        int height = map.GetLength(1);

        for (int j = y - 1; j <= y + 1; j++) {
            for (int i = x - 1; i <= x + 1; i++) {
                if (i == x && j == y) continue; // Skip self

                if (i < 0 || i >= width || j < 0 || j >= height) {
                    count++; // Treat out of bounds as a wall
                } else if (map[i, j]) {
                    count++;
                }
            }
        }
        return count;
    }


    // --- Helper: Get Biome Name at coordinates (after base pass) ---
    // Recalculates influence just like DetermineBaseTerrainAndBiome but only returns the name
    private static string GetBiomeNameAt(int worldX, int worldY) {
        float totalWeight = 0f;
        Dictionary<string, float> biomeWeights = new Dictionary<string, float>();
        foreach (var biome in _settings.biomeLayers) {
            float weight = CalculateBiomeInfluence(worldX, worldY, biome);
            if (weight > 0.001f) {
                biomeWeights.Add(biome.name, weight);
                totalWeight += weight;
            }
        }

        if (totalWeight > 0f) {
            float maxWeight = 0f;
            string dominantBiome = null;
            foreach (var pair in biomeWeights) {
                float normalizedWeight = pair.Value / totalWeight;
                if (normalizedWeight > maxWeight) {
                    maxWeight = normalizedWeight;
                    dominantBiome = pair.Key;
                }
            }
            return dominantBiome;
        }
        return null;
    }


    // --- Pass 3 Helper: Determine Ore ---
    private static TileBase DetermineOre(int worldX, int worldY, string biomeName) {
        TileBase foundOre = null;
        // Check Ores (consider priority/order)
        foreach (var ore in _settings.oreTypes) {
            if (biomeName == null || !ore.allowedBiomeNames.Contains(biomeName)) continue;

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
    private static bool IsRock(TileBase tile) {
        return tile != null && tile != idToTileAssetMap[0];
        // Add checks for air tiles if you have them
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
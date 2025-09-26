﻿using Unity.Burst; // Optional, for Burst compilation
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

// --- Job for Ore Generation ---
[BurstCompile]
public struct GenerateOresJob : IJob {
    [ReadOnly] public NativeArray<ushort> baseTileIDs; // Input from GPU generation
    [ReadOnly] public NativeArray<OreDefinition> oreDefinitions;
    public NativeArray<ushort> processedOreIDs;       // Output: tile IDs with ores
    public Vector2Int chunkCoord;                      // For world-position-dependent logic
    public int chunkSize;
    public uint seed; // Seed for procedural generation

    public void Execute() {
        var random = new Unity.Mathematics.Random(seed + (uint)(chunkCoord.x * 1000 + chunkCoord.y));

        for (int y = 0; y < chunkSize; y++) {
            for (int x = 0; x < chunkSize; x++) {
                int index = y * chunkSize + x;
                ushort currentTile = baseTileIDs[index];

                // Calculate world position once per tile
                float worldX = chunkCoord.x * chunkSize + x;
                float worldY = chunkCoord.y * chunkSize + y;

                // Loop through all possible ores for this tile
                for (int i = 0; i < oreDefinitions.Length; i++) {
                    OreDefinition ore = oreDefinitions[i];

                    // 1. Check if we can even place this ore here
                    if (currentTile != ore.replaceableTileID) {
                        continue; // This ore can't replace the current tile, try the next ore
                    }
                    // 1. Quick check: Are we even within this ore's vertical band?
                    // (Remember, deeper means smaller Y values)
                    if (worldY < ore.startDepth || worldY > ore.stopDepth) {
                        //Debug.Log("ORE: " + ore.tileID  + " worldY: " + worldY + " startDepth: " + ore.startDepth + " stopDepth: " + ore.stopDepth);
                        continue;
                    }
                    float currentChance;
                    // 2. Determine if we are in the 'ramp-up' or 'ramp-down' section of the band.
                    if (worldY < ore.mostDepth) {
                        // We're in the upper part of the band (between spawn and max rarity)
                        float depthT = math.remap(worldY, ore.startDepth, ore.mostDepth, 0, 1);
                        currentChance = math.lerp(ore.minChance, ore.maxChance, depthT);
                    } else {
                        // We're in the lower part of the band (between max rarity and stop)
                        float depthT = math.remap(worldY, ore.mostDepth, ore.stopDepth, 0, 1);
                        currentChance = math.lerp(ore.maxChance, ore.minChance, depthT); // HERE you could change minChange to be a different number if you want the ore to show more freq higher up, or something
                    }

                    // 3. Quick check: if random chance fails, don't bother with expensive noise calculation
                    if (random.NextFloat() >= currentChance) {
                        continue; // Failed random roll, try next ore
                    }

                    // 4. Check against Perlin noise for vein clustering
                    float noiseValue = noise.snoise(new float2(worldX * ore.noiseScale, worldY * ore.noiseScale) + ore.noiseOffset);
                    if (noiseValue > ore.noiseThreshold) {
                        // Success! Place the ore.
                        processedOreIDs[index] = ore.tileID;

                        // IMPORTANT: Break the inner loop to stop checking for other ores on this tile.
                        break;
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
    public NativeList<Vector2Int> potentialSpawnPoints; // Output: list of (worldX, worldY)
    public NativeList<Vector2Int> entityDetails; // Output: list of (EntityID, orientation (0,1,2,3))
    public Vector2Int chunkCoord;
    public int chunkSize;

    // Example entity type IDs
    private const int SPAWN_POINT_TYPE_ENEMY_CAVE = 1;
    // Example tile IDs
    private const ushort SOLID_ID = 1;

    public void Execute() {
       /* HashSet<Vector2Int> occupiedAnchors = new HashSet<Vector2Int>();
        for (int y = 0; y < chunkSize; y++) {
            for (int x = 0; x < chunkSize; x++) {
                int index = y * chunkSize + x;
                ushort currentTile = tileIDs[index];

                if (currentTile == SOLID_ID) {
                    // Check surrounding tiles or other conditions if needed
                    // For simplicity, just add if it's a cave floor tile
                    int worldX = chunkCoord.x * chunkSize + x;
                    int worldY = chunkCoord.y * chunkSize + y;
                    potentialSpawnPoints.Add(new Vector2Int(worldX, worldY));
                    entityDetails.Add(new Vector2Int(SPAWN_POINT_TYPE_ENEMY_CAVE, 0));
                }

                ushort anchorTileID = tileIDs[index];
                if (anchorTileID != SOLID_ID) {
                    // We're doing a more extensive anchor check later
                    continue;
                }
                if (occupiedAnchors.Contains(new Vector2Int(x, y))) {
                    continue; // Could also check the whole anchor is overlapping here but EH, a bit dence will look nice
                }
                int worldX = chunkCoord.x * chunkSize + x;
                int worldY = chunkCoord.y * chunkSize + y;

                // --- Iterate through Entity Definitions ---
                foreach (var entityDef in worldSpawnEntities) {
                    if (entityDef.entityPrefab == null)
                        continue;

                    // 1. Stochastic Check
                    float placementValue = GetNoise(worldX, worldY, entityDef.placementFrequency);
                    if (placementValue < entityDef.placementThreshold)
                        continue;

                    // 2. Basic Filters
                    if (worldY < entityDef.minY || worldY > entityDef.maxY)
                        continue;

                    // Biome Check (ensure GetBiomeNameAt is implemented)
                    // if (entityDef.requiredBiomeNames != null && entityDef.requiredBiomeNames.Count > 0) {
                    //     string biomeName = GetBiomeNameAt(worldX, worldY); // Implement this
                    //     if (biomeName == null || !entityDef.requiredBiomeNames.Contains(biomeName)) continue;
                    // }


                    // --- 3. Attachment and Clearance Checks ---
                    bool canSpawn = true;
                    Quaternion spawnRot = Quaternion.identity; // Default rotation
                    var occopied = new List<Vector2Int>();
                    var bounds = entityDef.GetBoundingOffset();
                    int canonicalAnchorLocalY = bounds.Item1.y;
                    Debug.Log("Canon:" + canonicalAnchorLocalY);
                    foreach (var attachment in entityDef.allowedAttachmentTypes) {
                        for (int local_xx = bounds.Item1.x; local_xx <= bounds.Item2.x; local_xx++) {
                            for (int local_yy = bounds.Item1.y; local_yy <= bounds.Item2.y; local_yy++) {
                                int areaMatrixX = local_xx + 4;
                                int areaMatrixY = -local_yy + 8;

                                // Safety check
                                if (areaMatrixX < 0 || areaMatrixX >= entityDef.areaMatrix.GetLength(0) ||
                                    areaMatrixY < 0 || areaMatrixY >= entityDef.areaMatrix.GetLength(1)) {
                                    Debug.LogWarning($"areaMatrix access out of bounds for entity {entityDef.name}: local({local_xx},{local_yy}) -> matrix({areaMatrixX},{areaMatrixY}). Treating as not required.");
                                    continue;
                                }
                                if (!entityDef.areaMatrix[areaMatrixX, areaMatrixY]) {
                                    // This cell in areaMatrix is 'false', so no requirement here. Skip
                                    continue;
                                }
                                // GLOBAL coordinates of the tile to check based on AttachmentType
                                int checkGlobalX = 0;
                                int checkGlobalY = 0;

                                switch (attachment) {
                                    case AttachmentType.Ground:
                                        // Entity is upright. Canonical (local_xx, local_yy) maps directly to world offset.
                                        checkGlobalX = x + local_xx;
                                        checkGlobalY = y + local_yy;
                                        spawnRot = Quaternion.Euler(0, 0, 0);
                                        break;
                                    case AttachmentType.Ceiling:
                                        // Entity upside down. Canonical +x becomes world -x; canonical +y becomes world -y.
                                        checkGlobalX = x - local_xx;
                                        checkGlobalY = y - local_yy;
                                        spawnRot = Quaternion.Euler(0, 0, 180);
                                        break;
                                    case AttachmentType.WallLeft:
                                        // Canonical +x (entity's right) becomes world +y (up).
                                        // Canonical +y (entity's up) becomes world -x (left).
                                        checkGlobalX = x - local_yy;
                                        checkGlobalY = y + local_xx;
                                        spawnRot = Quaternion.Euler(0, 0, 90);
                                        break;
                                    case AttachmentType.WallRight:
                                        // Canonical +x (entity's right) becomes world -y (down).
                                        // Canonical +y (entity's up) becomes world +x (right).
                                        checkGlobalX = x + local_yy;
                                        checkGlobalY = y - local_xx;
                                        spawnRot = Quaternion.Euler(0, 0, -90);
                                        break;
                                    default:
                                        // Debug.LogError($"Unknown attachment type: {attachment}");
                                        canSpawn = false;
                                        goto end_loops; // Exit both loops
                                }
                                // 4. Fetch the actual world tile at the calculated (checkGlobalX, checkGlobalY)
                                var tileToCheck = GetTileFromChunkOrWorld(chunkData, new Vector2Int(worldX, worldY), chunkSize,
                                    checkGlobalX,          // Target tile X, local to chunk
                                    checkGlobalY,          // Target tile Y, local to chunk
                                                           //checkGlobalY - worldY,          // Target tile Y, local to chunk
                                    cm
                                ); // TODO worldCoord input could be wrong here

                                // 5. Check the tile based on whether it's an anchor cell or a volume cell requirement
                                bool isCanonicalAnchorCell = local_yy == canonicalAnchorLocalY;
                                if (isCanonicalAnchorCell) {
                                    // This cell corresponds to the entity's canonical anchor row.
                                    // The world tile it points to MUST be solid.
                                    if (!IsSolid(tileToCheck)) {
                                        // Debug.Log($"Anchor requirement FAILED for {attachment} at world ({checkGlobalX},{checkGlobalY}). Expected Solid for local_yy={local_yy}. Tile was: {tileToCheck}");
                                        canSpawn = false;
                                        goto end_loops;
                                    }
                                } else {
                                    // This cell is part of the entity's volume (not an anchor row cell).
                                    // The world tile it points to MUST be empty or non-blocking.
                                    if (!IsEmptyOrNonBlocking(tileToCheck)) {
                                        // Debug.Log($"Volume requirement FAILED for {attachment} at world ({checkGlobalX},{checkGlobalY}). Expected Empty/NonBlocking. Tile was: {tileToCheck}");
                                        canSpawn = false;
                                        goto end_loops;
                                    }
                                }
                                // Reaching here means we passed this tiles checks, add it to occupied list
                                occopied.Add(new Vector2Int(checkGlobalX, checkGlobalY)); // As local chunk bounds
                            }
                        }
                        canSpawn = true; // we never hit a false so this must mean we can spawn
                    end_loops:
                        ;
                        if (canSpawn)
                            break; // Break out of the attachment loop if we have found a valid spot to spawn at
                        // Reaching here means we CANT spawn, so clear the occopied list and try a different orrientation
                        occopied.Clear();
                    }
                    if (!canSpawn)
                        continue;
                    // --- ALL CHECKS PASSED --- Spawn this entity ---
                    Vector3Int spawnPos = new(worldX, worldY);
                    entitySpawns.Add(new EntitySpawnInfo(entityDef.entityPrefab, entityDef.entityID, spawnPos, spawnRot, Vector3.one));
                    //occupiedAnchors.Add(new Vector2Int(x, y));
                    occupiedAnchors.AddRange(occopied);
                    break; // Spawned one entity for this anchor, move to next anchor
                }
            }
        }
       */
    }
}
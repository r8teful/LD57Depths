using Sirenix.Utilities;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;
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

[System.Serializable]
public struct OreDefinition {
    public ushort tileID;

    public float worldDepthProcent;
    // The maximum spawn chance at the exact edge of the circle (0.0 to 1.0)
    public float maxChance;
    // How quickly the chance drops as you move away from the radius.
    // Small number (0.005) = slow falloff)
    // Large number (0.1) = fast falloff
    public float widthPercent;
    // Noise
    public float noiseScale;
    public float noiseThreshold;
    public float2 noiseOffset;
}
public class WorldGen : MonoBehaviour {
    private Unity.Mathematics.Random noiseRandomGen;
    private float seedOffsetX;
    private float seedOffsetY;
    private WorldManager worldmanager;
    private ChunkManager chunkManager;
    private List<WorldSpawnEntitySO> worldSpawnEntities;
    private Camera _renderCamera; // Orthographic camera for rendering chunks
    private WorldGenSettings _cachedSettings;
    private RenderTexture _renderTexture; // Target 96x96 RenderTexture

    public const int CHUNK_TILE_DIMENSION = 16; // Size of a chunk in tiles (e.g., 16x16)

    // These are derived from the problem statement but made const for clarity
    public const int RENDER_TEXTURE_DIMENSION = 96*2; // Fixed size of the RenderTexture (e.g., 96x96)
    public const int CHUNKS_IN_VIEW_DIMENSION = RENDER_TEXTURE_DIMENSION / CHUNK_TILE_DIMENSION; // 96/16 = 6

    private bool _isProcessingChunks = false;

    // Helper struct to pass data to the async callback
    private struct ReadbackContext {
        public List<Vector2Int> ChunksToRequestBatch;
        public Vector2Int RenderAreaOriginChunkCoord; // Bottom-left chunk coord of the 6x6 render area
        public System.Action<List<ChunkPayload>,Dictionary<Vector2Int,ChunkData>, Dictionary<Vector2Int, List<EntitySpawnInfo>>> OnCompleteCallback;
    }
    private struct ChunkProcessingJobData {
        public Vector2Int ChunkCoord;
        public ChunkData OriginalChunkData; // From GPU
        public NativeArray<ushort> BaseTileIDs_NA; // NA for NativeArray
        public NativeArray<ushort> ProcessedTileIDs_NA;
        public NativeArray<ushort> OreTileIDs_NA; 
        public NativeList<Vector3Int> EntitySpawnPoints_NA; // For the entity job
        public JobHandle OreJobHandle;
        public JobHandle EntityJobHandle;
        public JobHandle CombinedHandle; // To depend on previous jobs
    }

    public void Init(RenderTexture renderTexture, WorldGenSettings settings, WorldManager worldmanager, ChunkManager chunkManager,Camera renderCamera) {
        _renderTexture = renderTexture;
        this.worldmanager = worldmanager;
        this.chunkManager = chunkManager;
        _renderCamera = renderCamera;
        _cachedSettings = settings;
        InitializeNoise(settings.seed); 
        worldSpawnEntities = App.ResourceSystem.GetAllWorldSpawnEntities();
        //worldSpawnEntities = App.ResourceSystem.GetDebugEntity(); // For debug only!! 
    }

    // Call this if you change the seed at runtime
    public void InitializeNoise(int seed) {
        // Use the seed to initialize the random generator for noise offsets
        noiseRandomGen = new Unity.Mathematics.Random((uint)seed);
        // Generate large offsets based on the seed to shift noise patterns
        seedOffsetX = noiseRandomGen.NextFloat(-10000f, 10000f);
        seedOffsetY = noiseRandomGen.NextFloat(-10000f, 10000f);
        App.ResourceSystem.InitializeWorldEntities(seed, new(seedOffsetX, seedOffsetY));
        // Note: Unity.Mathematics.noise doesn't *directly* use this Random object for per-call randomness,
        // but we use it here to get deterministic offsets for the noise input coordinates.
    }
    
    /// <summary>
    /// Generates chunk data for the requested chunks using GPU rendering and async readback.
    /// </summary>
    /// <param name="chunksToRequestBatch">A list of chunk coordinates that need to be generated. These are expected to fall within the 6x6 view.</param>
    /// <param name="newClientChunkCoord">The chunk coordinate the player/client is currently in or moving to. This will be the center of the 6x6 render area.</param>
    /// <param name="onGenerationComplete">Callback action that receives the list of generated ChunkData.</param>
    public IEnumerator GenerateChunkAsync(List<Vector2Int> chunksToRequestBatch, Vector2Int newClientChunkCoord, 
            System.Action<List<ChunkPayload>,Dictionary<Vector2Int,ChunkData>,
            Dictionary<Vector2Int, List<EntitySpawnInfo>>> onGenerationComplete) {

        if (_isProcessingChunks) {
            Debug.LogWarning("Chunk generation already in progress. Request ignored.");
            onGenerationComplete?.Invoke(new List<ChunkPayload>(), new Dictionary<Vector2Int, ChunkData>(),new Dictionary<Vector2Int, List<EntitySpawnInfo>>()); // Return empty list
            yield break;
        }
        _isProcessingChunks = true;

        // --- 1. Camera Setup ---
        // Orthographic size: 1 pixel = 1 tile. RT is 96x96 tiles. Ortho size is half-height.
        _renderCamera.orthographicSize = RENDER_TEXTURE_DIMENSION / 2.0f;

        // Determine the bottom-left chunk coordinate of the 6x6 area to render.
        // newClientChunkCoord is the "center" of this 6x6 area.
        // E.g., if CHUNKS_IN_VIEW_DIMENSION is 6, newClientChunkCoord (10,10) means rendering
        // chunks from (10 - 6/2, 10 - 6/2) = (7,7) up to (7+5, 7+5) = (12,12).
        Vector2Int renderAreaOriginChunk = new Vector2Int(
            newClientChunkCoord.x - CHUNKS_IN_VIEW_DIMENSION / 2,
            newClientChunkCoord.y - CHUNKS_IN_VIEW_DIMENSION / 2
        );

        // World position of the bottom-left corner of the entire 96x96 tile render area
        float renderAreaWorldX = renderAreaOriginChunk.x * CHUNK_TILE_DIMENSION;
        float renderAreaWorldY = renderAreaOriginChunk.y * CHUNK_TILE_DIMENSION;

        // Position camera at the center of this 96x96 tile area
        // The camera's position is the center of its view.
        // If bottom-left of render area is (worldX, worldY) and it's 96 units wide/high,
        // center is (worldX + 96/2, worldY + 96/2).
        _renderCamera.transform.position = new Vector3(
            renderAreaWorldX + RENDER_TEXTURE_DIMENSION / 2.0f,
            renderAreaWorldY + RENDER_TEXTURE_DIMENSION / 2.0f,
            _renderCamera.transform.position.z // Keep original Z
        );

        // Ensure camera renders its view to the RenderTexture.
        // If camera is enabled and targetTexture is set, it usually renders automatically.
        // Forcing a render or waiting for end of frame ensures data is fresh.
        // _renderCamera.Render(); // Manually trigger render if camera is disabled or needed.
        yield return new WaitForEndOfFrame(); // Good practice to ensure rendering is complete before readback

        // --- 2. Async GPU Readback ---
        var context = new ReadbackContext {
            ChunksToRequestBatch = new List<Vector2Int>(chunksToRequestBatch), // Copy list
            RenderAreaOriginChunkCoord = renderAreaOriginChunk,
            OnCompleteCallback = onGenerationComplete
        };

        // Request readback. The callback 'OnReadbackCompleted' will be invoked when data is ready.
        AsyncGPUReadback.Request(_renderTexture, 0, TextureFormat.RGBAFloat, request => OnGPUReadbackCompleted(request, context));
    }
    
    private void OnGPUReadbackCompleted(AsyncGPUReadbackRequest request, ReadbackContext context) {
        Dictionary<Vector2Int, ChunkData> generatedChunks = new Dictionary<Vector2Int, ChunkData>();
        if (request.hasError) {
            Debug.LogError("GPU Readback Error!");
        } else if (request.done) // Check if 'done' just in case, though Request usually ensures it.
          {
            NativeArray<float4> pixelData = request.GetData<float4>();
            // pixelData is a 1D array representing the 2D texture.
            // For a 96x96 texture, it has 96*96 = 9216 elements.
            // Pixels are typically ordered row by row, starting from bottom-left (0,0).
            foreach (Vector2Int requestedChunkCoord in context.ChunksToRequestBatch) {
                // Calculate where this chunk's data starts within the 96x96 RenderTexture.
                // Offset of the requested chunk from the origin chunk of the render texture (in chunk units)
                int chunkOffsetX_from_RT_origin_chunks = requestedChunkCoord.x - context.RenderAreaOriginChunkCoord.x;
                int chunkOffsetY_from_RT_origin_chunks = requestedChunkCoord.y - context.RenderAreaOriginChunkCoord.y;

                // Convert to pixel offset (bottom-left corner of the chunk within the RenderTexture)
                int chunkPixelStartX_in_RT = chunkOffsetX_from_RT_origin_chunks * CHUNK_TILE_DIMENSION;
                int chunkPixelStartY_in_RT = chunkOffsetY_from_RT_origin_chunks * CHUNK_TILE_DIMENSION;

                // Basic validation: Is this chunk actually within the 6x6 rendered area?
                if (chunkOffsetX_from_RT_origin_chunks < 0 || chunkOffsetX_from_RT_origin_chunks >= CHUNKS_IN_VIEW_DIMENSION ||
                    chunkOffsetY_from_RT_origin_chunks < 0 || chunkOffsetY_from_RT_origin_chunks >= CHUNKS_IN_VIEW_DIMENSION) {
                    Debug.LogWarning($"Requested chunk {requestedChunkCoord} is outside the rendered area defined by origin {context.RenderAreaOriginChunkCoord} and view dimension {CHUNKS_IN_VIEW_DIMENSION}. Skipping.");
                    continue;
                }

                //ChunkData currentChunkData = new ChunkData(requestedChunkCoord, CHUNK_TILE_DIMENSION);
                ChunkData currentChunkData = new ChunkData(CHUNK_TILE_DIMENSION, CHUNK_TILE_DIMENSION);

                for (int yTileInChunk = 0; yTileInChunk < CHUNK_TILE_DIMENSION; yTileInChunk++) {
                    for (int xTileInChunk = 0; xTileInChunk < CHUNK_TILE_DIMENSION; xTileInChunk++) {
                        // Absolute pixel coordinates in the RenderTexture
                        int pixelX_in_RT = chunkPixelStartX_in_RT + xTileInChunk;
                        int pixelY_in_RT = chunkPixelStartY_in_RT + yTileInChunk;

                        // Convert 2D pixel coordinate to 1D index in pixelData array
                        // (Unity's Texture2D.GetPixel(0,0) is bottom-left, matching GPU readback usually)
                        int pixelIndex = pixelY_in_RT * RENDER_TEXTURE_DIMENSION + pixelX_in_RT;

                        if (pixelIndex < 0 || pixelIndex >= pixelData.Length) {
                            Debug.LogError($"Pixel index out of bounds: ({pixelX_in_RT}, {pixelY_in_RT}) -> index {pixelIndex}. RT dim: {RENDER_TEXTURE_DIMENSION}. Data length: {pixelData.Length}");
                            continue;
                        }
                        float4 color = pixelData[pixelIndex];
                        Vector3Int IDData = new (Mathf.RoundToInt(color.x * 255.0f), Mathf.RoundToInt(color.y * 255.0f), 
                                           Mathf.RoundToInt(color.z * 255.0f));
                        
                        // Tile ID is stored in the R channel (0-255).
                        ushort tileID = 0;
                        byte biomeID = 0;
                        // tileID determines durability and drops
                        if (IDData.x == 1) { 
                            tileID = ResourceSystem.StoneID; 
                        } else if (IDData.x == 0 || IDData.x==255) {
                            tileID = ResourceSystem.AirID;
                        } else if (IDData.x == 2) {
                            tileID = ResourceSystem.StoneToughID;
                        } else if (IDData.x == 3) {
                            tileID = ResourceSystem.StoneVeryToughID;
                        }
                        // Biome determines visual and biome info for entities and bio buffs etc
                        if (IDData.y == 1) {
                            biomeID = 1; // Trench
                        } else if (IDData.y == 2) {
                            biomeID = (byte)BiomeType.Trench1; 
                        } else if (IDData.y == 3) {
                            biomeID = (byte)BiomeType.Trench2;
                        } else if (IDData.y == 4) {
                            biomeID = (byte)BiomeType.Trench3;
                        } else if(IDData.y == 253) {
                            biomeID = (byte)BiomeType.Bioluminescent;
                        } else if (IDData.y == 133) {
                            biomeID = (byte)BiomeType.Fungal;
                        } else if (IDData.y == 175) {
                            biomeID = (byte)BiomeType.Forest;
                        } else if (IDData.y == 220) {
                            biomeID = (byte)BiomeType.Deadzone;
                        }
                        currentChunkData.tiles[xTileInChunk, yTileInChunk] = tileID;
                        currentChunkData.biomeID[xTileInChunk, yTileInChunk] = biomeID;
                    }
                }
                generatedChunks.Add(requestedChunkCoord,currentChunkData);
            }
        }

        // Invoke the callback with the generated chunk data
        
        StartCoroutine(ProcessChunksWithJobs(generatedChunks, (processedPayloads,processedChunks,entities) => {
            // World gen complete, send them over the network.
            _isProcessingChunks = false; // Allow next request
            if (processedPayloads.Count > 0) {
                // TODO
                context.OnCompleteCallback?.Invoke(processedPayloads, processedChunks, entities);
                //TargetReceiveChunkDataMultiple(requester, processedPayloads);
                //Debug.Log($"Sent {processedPayloads.Count} processed chunks to player");
            } 
        }));
    }


    // We can always extend this later if there is a bottleneck somewhere
    private IEnumerator ProcessChunksWithJobs(
        Dictionary<Vector2Int,ChunkData> initialChunks, // Chunk inputs that might get modified
        System.Action<
            List<ChunkPayload>, // Callback action with the data that the worldgen generated
            Dictionary<Vector2Int,ChunkData>,  // Chunkcoord to chunkdata, containing the generated chunks 
            Dictionary<Vector2Int, List<EntitySpawnInfo>>
            > onProcessingComplete) { // Chunkcoord with entity data incase we need to spawn any entitys in that chunk
        
        if (initialChunks == null || initialChunks.Count == 0) {
            onProcessingComplete?.Invoke(new List<ChunkPayload>(), new Dictionary<Vector2Int, ChunkData>(), new Dictionary<Vector2Int, List<EntitySpawnInfo>>());
            yield break;
        }

        List<ChunkProcessingJobData> jobDataList = new List<ChunkProcessingJobData>(initialChunks.Count);
        List<JobHandle> jobHandles = new List<JobHandle>(initialChunks.Count * 2); // Max jobs per chunk (ore + entity)

        uint worldSeed = 12345; // Get your world seed

        var sharedOreDefinitions = GetOreDefinitions();
        // --- 1. Setup Jobs for each chunk ---
        foreach (var kvp in initialChunks) {
            ChunkData chunk = kvp.Value;
            var processingData = new ChunkProcessingJobData { OriginalChunkData = chunk,ChunkCoord = kvp.Key };

            // Convert ushort[,] to NativeArray<ushort>
            int tileCount = CHUNK_TILE_DIMENSION * CHUNK_TILE_DIMENSION;
            processingData.BaseTileIDs_NA = new NativeArray<ushort>(tileCount, Allocator.TempJob);
            processingData.ProcessedTileIDs_NA = new NativeArray<ushort>(tileCount, Allocator.TempJob); // For ore job output
            processingData.OreTileIDs_NA = new NativeArray<ushort>(tileCount, Allocator.TempJob); // For ore job output

            int k = 0;
            for (int y = 0; y < CHUNK_TILE_DIMENSION; y++) {
                for (int x = 0; x < CHUNK_TILE_DIMENSION; x++) {
                    processingData.BaseTileIDs_NA[k] = chunk.tiles[x, y]; // Assuming tileIDs[x,y] convention
                    processingData.OreTileIDs_NA[k] = ResourceSystem.InvalidID;
                    k++;
                }
            }
            // --- Ore Generation Job ---
            var oreJob = new GenerateOresJob {
                baseTileIDs = processingData.BaseTileIDs_NA, // GPU output
                processedOreIDs = processingData.OreTileIDs_NA, // This will be modified
                chunkCoord = kvp.Key,
                chunkSize = CHUNK_TILE_DIMENSION,
                worldCenter = new(0, _cachedSettings.MaxDepth), // We're spawning at maxDepth so its like the center
                seed = worldSeed + (uint)kvp.Key.x, // Vary seed per chunk slightly or use chunkCoord for determinism
                oreDefinitions = sharedOreDefinitions
            };
            processingData.OreJobHandle = oreJob.Schedule();
            jobHandles.Add(processingData.OreJobHandle);

            // --- Entity Spawn Points Job (depends on ore job's output, or could use BaseTileIDs_NA) ---
            // This example assumes it can run in parallel or on base tiles. For chaining, see below.
            
            /*processingData.EntitySpawnPoints_NA = new NativeList<Vector3Int>(Allocator.TempJob);
            var entityJob = new FindEntitySpawnPointsJob {
                // If depends on ore output:
                // tileIDs = processingData.ProcessedTileIDs_NA,
                // And schedule with dependency: entityJob.Schedule(processingData.OreJobHandle);

                // If runs on base tiles (parallel to ore):
                tileIDs = processingData.BaseTileIDs_NA,
                potentialSpawnPoints = processingData.EntitySpawnPoints_NA,
                chunkCoord = kvp.Key,
                chunkSize = CHUNK_TILE_DIMENSION
            };*/
            // Schedule with dependency if needed:
            // processingData.EntityJobHandle = entityJob.Schedule(processingData.OreJobHandle);

            // Or schedule independently and combine later:
            
            //processingData.EntityJobHandle = entityJob.Schedule();
            
            //jobHandles.Add(processingData.EntityJobHandle);

            // Store a combined handle for this chunk's processing steps if they are sequential
            // processingData.CombinedHandle = processingData.EntityJobHandle; // If EntityJob depends on OreJob
            // If they are parallel initially, we'll combine all handles later.

            jobDataList.Add(processingData);
        }

        // --- 2. Wait for All Jobs to Complete ---
        // We could yield until JobHandle.IsCompleted, but for batching, completing all is cleaner.
        // JobHandle.CompleteAll(new NativeArray<JobHandle>(jobHandles.ToArray(), Allocator.TempJob));
        // A more Unity-conventional way in a coroutine:
        while (jobHandles.Any(jh => !jh.IsCompleted)) {
            yield return null; // Wait a frame
        }
        // Ensure completion if any stragglers
        foreach (var jh in jobHandles) {
            jh.Complete(); // This will block if not already done
        }

    
        // --- 3. Process Results and Dispose NativeArrays ---
        Dictionary<Vector2Int, ChunkData> chunksToSend = new Dictionary<Vector2Int, ChunkData>(); // For server
        Dictionary<Vector2Int, List<EntitySpawnInfo>> entitySpawnInfos = new Dictionary<Vector2Int, List<EntitySpawnInfo>>();
        Dictionary<Vector2Int, ChunkPayload> payloadsToSend = new Dictionary<Vector2Int, ChunkPayload>();
        foreach (var data in jobDataList) {
            // Make sure to complete specific handles if you didn't use CompleteAll
            // data.OreJobHandle.Complete(); 
            // data.EntityJobHandle.Complete(); // Or data.CombinedHandle.Complete();

            // Recostruct new ChunkData
            //var finalChunk = new ChunkData(CHUNK_TILE_DIMENSION, CHUNK_TILE_DIMENSION); 
            
            
            // --!!! --- Assuming none of the original chunkdata is actually modified,
            // If we're generating other tiles before and (LIKE ARTIFACT) here that data would be overwritten
            // TODO if it is, we have to recostruct base layer aswell
            var finalChunk = data.OriginalChunkData; 
            var k = 0;
            for (int y = 0; y < CHUNK_TILE_DIMENSION; y++) {
                for (int x = 0; x < CHUNK_TILE_DIMENSION; x++) {
                    finalChunk.oreID[x,y] = data.OreTileIDs_NA[k++]; 
                    //finalChunk.tiles[x,y] = data.ProcessedTileIDs_NA[k++]; 
                }
            }

            // TODO 
            /*  if (data.EntitySpawnPoints_NA.IsCreated) {
                  Debug.Log($"Chunk {data.OriginalChunkData.chunkCoord} found {data.EntitySpawnPoints_NA.Length} potential entity spawn points.");
                  foreach (var point in data.EntitySpawnPoints_NA) {
                      // Add to a global list, or process immediately if simple
                      // e.g., serverEntityManager.RegisterPotentialSpawn(point);
                  }
              }*/
            chunksToSend.Add(data.ChunkCoord,finalChunk);
            payloadsToSend.Add(data.ChunkCoord, ProcessingDataToPayload(data));
            // Dispose of Native Collections
            if (data.BaseTileIDs_NA.IsCreated)
                data.BaseTileIDs_NA.Dispose();
            if (data.ProcessedTileIDs_NA.IsCreated)
                data.ProcessedTileIDs_NA.Dispose();
            if (data.EntitySpawnPoints_NA.IsCreated)
                data.EntitySpawnPoints_NA.Dispose();
            if (data.OreTileIDs_NA.IsCreated)
                data.OreTileIDs_NA.Dispose();
        }
        if (sharedOreDefinitions.IsCreated)
            sharedOreDefinitions.Dispose();
        // Jobs done, now main thread...


        // Here we go

        // Structure here
        foreach (var chunk in chunksToSend) {
            var chunkPayload = payloadsToSend[chunk.Key];
            // Structure logic will have to modify both chunk and payload data that we got from the jobs
            SpawnStructuresInChunk(chunk.Value, chunk.Key, chunkPayload); 
        }

        // Entities
        Dictionary<Vector2Int,List<ulong>> entityIdsDict = new Dictionary<Vector2Int,List<ulong>>();
        List<ChunkPayload> clientPayload = new List<ChunkPayload>();
        foreach (var chunks in chunksToSend) {
            var entityInChunk = SpawnEntitiesInChunk(chunks.Value, chunks.Key, chunkManager);
            entitySpawnInfos.Add(chunks.Key, entityInChunk);
            yield return null; // Wait a frame
            // Now we have the enemy info, get the persistant IDs from EntityManager
            entityIdsDict.Add(chunks.Key, EntityManager.Instance.AddGeneratedEntityData(chunks.Key, entityInChunk));
        }
        foreach(var data in payloadsToSend) {
            var entityList = entityIdsDict.TryGetValue(data.Key, out var entities);
            clientPayload.Add(new ChunkPayload(data.Value, entities));
        }
        //Debug.Log($"All chunk processing jobs complete. {clientPayload.Count} payloads ready.");
        onProcessingComplete?.Invoke(clientPayload, chunksToSend,entitySpawnInfos);
    }

    private NativeArray<OreDefinition> GetOreDefinitions() {
        int oreCount = _cachedSettings.worldOres.Count;
        var nativeOreDefinitions = new NativeArray<OreDefinition>(oreCount, Allocator.TempJob);
        for (int i = 0; i < oreCount; i++) {
            WorldGenOreSO data = _cachedSettings.worldOres[i];
            //float yStart = worldmanager.GetWorldLayerYPos(data.LayerStartSpawn);
            // TODO for layerStartSpawn 0 it should be wherever the bedrock starts 
            //yStart = GameSetupManager.Instance.WorldGenSettings.GetWorldLayerYPos(data.CircleLayer);
            nativeOreDefinitions[i] = new OreDefinition {
                tileID = data.oreTile.ID,
               maxChance = data.maxChance,
                worldDepthProcent = data.WorldDepthBandProcent,
                widthPercent = data.widthPercent,
                noiseScale = data.noiseScale,
                noiseThreshold = data.noiseThreshold,
                noiseOffset = new float2(data.noiseOffset.x, data.noiseOffset.y)
            };
        }
        return nativeOreDefinitions;
    }

    private ChunkPayload ProcessingDataToPayload(ChunkProcessingJobData data) {
        // Create the payload for networking
        List<ushort> finalTileIdsList = new List<ushort>(data.ProcessedTileIDs_NA.Length);
        for (int tileIdx = 0; tileIdx < data.ProcessedTileIDs_NA.Length; tileIdx++) {
            finalTileIdsList.Add(data.BaseTileIDs_NA[tileIdx]); // !!! Using base tile ids because we don't actually modify this data
        }
        List<ushort> finalOreIdsList = new List<ushort>(data.OreTileIDs_NA.Length);
        for (int tileIdx = 0; tileIdx < data.OreTileIDs_NA.Length; tileIdx++) {
            finalOreIdsList.Add(data.OreTileIDs_NA[tileIdx]);
        }
        List<float> durabilities = new List<float>(CHUNK_TILE_DIMENSION*CHUNK_TILE_DIMENSION);
        for (int i = 0; i < CHUNK_TILE_DIMENSION * CHUNK_TILE_DIMENSION; i++)
        {
            durabilities.Add(-1);
        }
        return new ChunkPayload(data.ChunkCoord, finalTileIdsList, finalOreIdsList, durabilities, null);
    }

    /*
    // --- Pass 4: Structure Placement ---
    private  void PlaceStructuresInChunk(ChunkData chunkData, Vector3Int chunkOriginCell, int chunkSize) {
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
    private void SpawnStructuresInChunk(ChunkData chunkData, Vector2Int chunkCoord, ChunkPayload chunkPayload) {
        var structures = worldmanager.StructureManager.StructurePlacements;

        var chunkRect = ChunkCoordToRect(chunkCoord);

        foreach (var placement in structures) {
            if (placement.fullyStamped) continue;

            StructureSO structure = App.ResourceSystem.GetStructureByID(placement.ID);
            if (structure == null) 
                continue;

            var structureRect = new RectInt(placement.bottomLeftAnchor, structure.Size);
            var intersect = RectIntersection(chunkRect, structureRect);
            if (intersect.width == 0 || intersect.height == 0) {
                continue; // No intersection with this chunk
            }
            // Debug.Log($"Spawning {placement.structureID} at {placement.bottomLeftAnchor}. Intersection: {intersect}");

            List<TileBase> tiles = structure.tiles;
            int structureWidth = structure.Size.x;

            for (int wy = intersect.yMin; wy < intersect.yMax; ++wy) {
                for (int wx = intersect.xMin; wx < intersect.xMax; ++wx) {
                    // Structure-local coords (0 to width-1, 0 to height-1)
                    int localDx = wx - structureRect.x;
                    int localDy = wy - structureRect.y;

                    // Chunk-local coords
                    int chunkLocalX = wx - chunkRect.x;
                    int chunkLocalY = wy - chunkRect.y;

                    // Flat Array Indexing using dynamic width
                    int index = localDx + (localDy * structureWidth);

                    // Check bounds just in case the template data is shorter than the size definitions
                    if (index >= tiles.Count) continue;
                    var tile = tiles[index] as TileSO;
                    if (tile == null) continue;
                    // WRITE DATA
                    int payloadIndex = chunkLocalX + chunkLocalY * CHUNK_TILE_DIMENSION;

                    // We can either treat the tiles on the structure as ore (like an overlay). Or as the base. We can't currently have both 
                    ushort baseTileID;
                    if (structure.tileIsOreLayer) {
                        baseTileID = ResourceSystem.StoneToughID; // Should be depending on what layer you're on but cba figuring that out here
                        chunkData.oreID[chunkLocalX, chunkLocalY] = tile.ID;
                        chunkPayload.OreIds[payloadIndex] = tile.ID;
                    } else {
                        if(tile.ID == ResourceSystem.AirID) {
                            // Clear the ore because those got spawned before structures
                            chunkData.oreID[chunkLocalX, chunkLocalY] = ResourceSystem.InvalidID;
                            chunkPayload.OreIds[payloadIndex] = ResourceSystem.InvalidID;
                        }
                        baseTileID = tile.ID; // structure tile
                    }
                    chunkData.tiles[chunkLocalX, chunkLocalY] = baseTileID;
                    chunkPayload.TileIds[payloadIndex] = baseTileID;
                }
            }
        }
    }


    private RectInt ChunkCoordToRect(Vector2Int chunkCoord) {
        // I'm assuming chunkcoord is bottom left
        int worldX = chunkCoord.x * CHUNK_TILE_DIMENSION;
        int worldY = chunkCoord.y * CHUNK_TILE_DIMENSION;
        return new RectInt(worldX, worldY, CHUNK_TILE_DIMENSION, CHUNK_TILE_DIMENSION);
    }

    public static RectInt RectIntersection(RectInt chunkRect, RectInt structureRect) {
        int aMinX = chunkRect.x;
        int aMinY = chunkRect.y;
        int aMaxX = chunkRect.x + chunkRect.width - 1; // inclusive max
        int aMaxY = chunkRect.y + chunkRect.height - 1;

        int bMinX = structureRect.x;
        int bMinY = structureRect.y;
        int bMaxX = structureRect.x + structureRect.width - 1;
        int bMaxY = structureRect.y + structureRect.height - 1;

        int ixMin = Math.Max(aMinX, bMinX);
        int iyMin = Math.Max(aMinY, bMinY);
        int ixMax = Math.Min(aMaxX, bMaxX);
        int iyMax = Math.Min(aMaxY, bMaxY);

        if (ixMin > ixMax || iyMin > iyMax) {
            return new RectInt(0, 0, 0, 0); // no intersection
        }

        int width = ixMax - ixMin + 1;
        int height = iyMax - iyMin + 1;
        return new RectInt(ixMin, iyMin, width, height);
    }
    public RectInt RectIntersection2(RectInt chunkRect, RectInt structureRect) {
        var cx = chunkRect.x; var cy = chunkRect.y;
        var ch = chunkRect.height; var cw = chunkRect.width;
        var sx = structureRect.x; var sy = structureRect.y;
        var sh = structureRect.height; var sw = structureRect.width;  // Corrected variable names (assuming typo in original)
        var left = Math.Max(cx, sx);
        var bottom = Math.Max(cy, sy);
        var right = Math.Min(cx + cw, sx + sw);
        var top = Math.Min(cy + ch, sy + sh);
        var width = right - left;
        var height = top - bottom;
        if (width <= 0 || height <= 0) {
            return RectInt.zero;
        }
        return new RectInt(left, bottom, width, height);
    }
    private List<EntitySpawnInfo> SpawnEntitiesInChunk(ChunkData chunkData, Vector2Int chunkOriginCell, ChunkManager cm) {
        List<EntitySpawnInfo> entities = new List<EntitySpawnInfo>();
        HashSet<Vector2Int> occupiedAnchors = new HashSet<Vector2Int>();
        List<WorldSpawnEntitySO> entityList = new List<WorldSpawnEntitySO>(worldSpawnEntities);
        //Shuffle(entityList);  // So it doesn't just always try to spawn the first entity first
        // So larger entities have a chanse to spawn first. 
        System.Random rng = new System.Random();
        entityList = entityList
            .OrderByDescending(e => e.GetBoundSize())        // primary: largest first
            .ThenBy(e => rng.Next())                         // secondary: random order among equals
            .ToList();
        for (int y = 0; y < CHUNK_TILE_DIMENSION; y++) {
            for (int x = 0; x < CHUNK_TILE_DIMENSION; x++) {
                ushort anchorTileID = chunkData.tiles[x, y];
                if (!IsSolid(anchorTileID)) {
                    // We're doing a more extensive anchor check later
                    continue;
                }
                if (occupiedAnchors.Contains(new Vector2Int(x, y))) {
                    continue; // Could also check the whole anchor is overlapping here but EH, a bit dence will look nice
                }
                int worldX = chunkOriginCell.x * CHUNK_TILE_DIMENSION + x;
                int worldY = chunkOriginCell.y * CHUNK_TILE_DIMENSION + y;

                // Global placement value
                if (SampleNoise(worldX, worldY, 0.3f, new(seedOffsetX, seedOffsetY)) < 0.5f)
                    continue;
                // --- Iterate through Entity Definitions ---
                foreach (var entityDef in entityList) {
                    if (entityDef.entityPrefab == null)
                        continue;
                    if (entityDef.spawnConditions == null)
                        continue;
                    // 1. Stochastic Check
                    float placementValue = SampleNoise(worldX, worldY, entityDef.placementFrequency, entityDef.PlacementOffset);
                    if (placementValue < entityDef.placementThreshold)
                        continue;

                    // 2. Basic Filters
                    if (worldY < entityDef.spawnConditions.minY || worldY > entityDef.spawnConditions.maxY)
                        continue;

                    // Early biome check
                    var b = GetBiomeFromChunk(chunkData, CHUNK_TILE_DIMENSION, x, y);
                    if (b == byte.MaxValue || !entityDef.spawnConditions.requiredBiomes.Contains((BiomeType)b))
                        continue;
                    /*
                    // DEBUG JUST LOOKING HOW NOISE LOOKS
                    // --- ALL CHECKS PASSED --- Spawn this entity ---
                    Vector3Int spawnPoz = new(worldX, worldY);
                    entities.Add(new EntitySpawnInfo(entityDef.entityID, spawnPoz, Quaternion.identity));
                    break;
                    */
                    // --- 3. Attachment and Clearance Checks ---
                    bool canSpawn = true;
                    //bool canSpawnBiome = false;
                    Quaternion spawnRot = Quaternion.identity; // Default rotation
                    var occopied = new List<Vector2Int>();
                    var bounds = entityDef.BoundingOffset;
                    int canonicalAnchorLocalY = bounds.Item1.y;
                    //Debug.Log("Canon:" + canonicalAnchorLocalY);
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
                                int checkLocalX = 0;
                                int checkLocalY = 0;

                                switch (attachment) {
                                    case AttachmentType.Ground:
                                        // Entity is upright. Canonical (local_xx, local_yy) maps directly to world offset.
                                        checkLocalX = x + local_xx;
                                        checkLocalY = y + local_yy;
                                        spawnRot = Quaternion.Euler(0, 0, 0);
                                        break;
                                    case AttachmentType.Ceiling:
                                        // Entity upside down. Canonical +x becomes world -x; canonical +y becomes world -y.
                                        checkLocalX = x - local_xx;
                                        checkLocalY = y - local_yy;
                                        spawnRot = Quaternion.Euler(0, 0, 180);
                                        break;
                                    case AttachmentType.WallRight:
                                        // Canonical +x (entity's right) becomes world +y (up).
                                        // Canonical +y (entity's up) becomes world -x (left).
                                        checkLocalX = x - local_yy;
                                        checkLocalY = y + local_xx;
                                        spawnRot = Quaternion.Euler(0, 0, 90);
                                        break;
                                    case AttachmentType.WallLeft:
                                        // Canonical +x (entity's right) becomes world -y (down).
                                        // Canonical +y (entity's up) becomes world +x (right).
                                        checkLocalX = x + local_yy;
                                        checkLocalY = y - local_xx;
                                        spawnRot = Quaternion.Euler(0, 0, -90);
                                        break;
                                    default:
                                        // Debug.LogError($"Unknown attachment type: {attachment}");
                                        canSpawn = false;
                                        goto end_loops; // Exit both loops
                                }
                                // 4. Fetch the actual world tile at the calculated (checkGlobalX, checkGlobalY)
                                var tileToCheck = GetTileFromChunkOrWorld(chunkData, new Vector2Int(worldX, worldY), CHUNK_TILE_DIMENSION,
                                    checkLocalX,          // Target tile X, local to chunk
                                    checkLocalY,          // Target tile Y, local to chunk
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
                                occopied.Add(new Vector2Int(checkLocalX, checkLocalY)); // As local chunk bounds
                            }
                        }
                        canSpawn = true; // we never hit a false so this must mean we can spawn
                    end_loops:;
                        if (canSpawn)
                            break; // Break out of the attachment loop if we have found a valid spot to spawn at
                        // Reaching here means we CANT spawn, so clear the occopied list and try a different orrientation
                        occopied.Clear();
                    }
                    if (!canSpawn)
                        continue;
                    // --- ALL CHECKS PASSED --- Spawn this entity ---
                    Vector3Int spawnPos = new(worldX, worldY);
                    entities.Add(new EntitySpawnInfo(entityDef.entityID, spawnPos, spawnRot));
                    occupiedAnchors.AddRange(occopied);
                    //occupiedAnchors.Add(new Vector2Int(x, y));
                    break; // Spawned one entity for this anchor, move to next anchor
                }
            }
        }
        return entities;
    }
    private bool IsSolid(ushort tileID) {
        return tileID != ResourceSystem.InvalidID && tileID != ResourceSystem.AirID;
        // Add checks for air tiles if you have them
    }

    private ushort GetTileFromChunkOrWorld(ChunkData currentChunkData, Vector2Int worldCoord, int chunkSize,
                                                    int localX, int localY, ChunkManager cm) {
        if (localX >= 0 && localX < chunkSize && localY >= 0 && localY < chunkSize) {
            return currentChunkData.tiles[localX, localY];
        } else {
            // Calculate world coordinates
            int worldX = worldCoord.x;
            int worldY = worldCoord.y;
            //Debug.Log($"Getting Tile at world pos: x:{worldX} y:{worldY}");
            // You'll need a method in WorldManager to get a tile at any world position
            // This method should handle loading adjacent chunks if necessary, or return null/empty if out of bounds.
            return cm.GetTileAtWorldPos(worldX, worldY); // Implement this in WorldManager
        }
    }
    private byte GetBiomeFromChunk(ChunkData currentChunkData, int chunkSize,
                                                    int localX, int localY) {
        if (localX >= 0 && localX < chunkSize && localY >= 0 && localY < chunkSize) {
            return currentChunkData.biomeID[localX, localY];
        } else {
            return byte.MaxValue;
        }
    }

    // Some non-blocking tiles like vines or something might be non blocking later
    private bool IsEmptyOrNonBlocking(ushort tileID) {
        return !IsSolid(tileID) && tileID != ResourceSystem.InvalidID; 
    }

    private bool IsAdjacentWater(ChunkData chunkData, int x, int y) {
        int width = chunkData.tiles.GetLength(0);
        int height = chunkData.tiles.GetLength(0);


        int[,] neighbors = { { 0, 1 }, { 0, -1 }, { 1, 0 }, { -1, 0 } }; // N, S, E, W

        for (int i = 0; i < neighbors.GetLength(0); ++i) {
            int nx = x + neighbors[i, 0];
            int ny = y + neighbors[i, 1];

            // Check bounds (simple version, doesn't check neighbour chunks)
            if (nx >= 0 && nx < width && ny >= 0 && ny < height) {
                ushort neighborTile = chunkData.tiles[nx, ny];
                if (neighborTile == ResourceSystem.AirID) {
                    return true;
                }
            }
            // Else: Tile is outside this chunk's data. For perfect checks,
            // you'd need access to neighbor chunk data here. Often ignored for performance.
        }
        return false;
    }

    private float SampleNoise(float x, float y, float frequency, Vector2 offset) {
        float sampleX = (x + offset.x) * frequency;
        float sampleY = (y + offset.y) * frequency;
        // noise.snoise returns [-1,1]
        return (noise.snoise(new float2(sampleX, sampleY)) + 1f) * 0.5f;
    }
   
    // Simple Fisher-Yates shuffle method
    void Shuffle<T>(List<T> list) {
        int n = list.Count;
        for (int i = n - 1; i > 0; i--) {
            int j = UnityEngine.Random.Range(0, i + 1);
            T temp = list[i];
            list[i] = list[j];
            list[j] = temp;
        }
    }
}
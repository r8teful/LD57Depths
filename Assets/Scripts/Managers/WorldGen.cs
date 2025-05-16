using FishNet.Connection;
using Sirenix.Utilities;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEditor.PackageManager.Requests;
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

public class WorldGen : MonoBehaviour {
    private Unity.Mathematics.Random noiseRandomGen;
    private float seedOffsetX;
    private float seedOffsetY;
    private Vector3Int chunkOriginCell;
    private Dictionary<BiomeType, BiomeLayerSO> biomeLookup = new Dictionary<BiomeType, BiomeLayerSO>();
    private WorldGenSettingSO _settings;
    private float maxDepth;
    private WorldManager worldmanager;
    private ChunkManager chunkManager;
    private EntityManager _entityManager;
    private List<WorldSpawnEntitySO> worldSpawnEntities;

    private Camera _renderCamera; // Orthographic camera for rendering chunks
    private RenderTexture _renderTexture; // Target 96x96 RenderTexture

    public const int CHUNK_TILE_DIMENSION = 16; // Size of a chunk in tiles (e.g., 16x16)

    // These are derived from the problem statement but made const for clarity
    public const int RENDER_TEXTURE_DIMENSION = 96; // Fixed size of the RenderTexture (e.g., 96x96)
    public const int CHUNKS_IN_VIEW_DIMENSION = RENDER_TEXTURE_DIMENSION / CHUNK_TILE_DIMENSION; // 96/16 = 6

    private bool _isProcessingChunks = false;

    // Helper struct to pass data to the async callback
    private struct ReadbackContext {
        public List<Vector2Int> ChunksToRequestBatch;
        public Vector2Int RenderAreaOriginChunkCoord; // Bottom-left chunk coord of the 6x6 render area
        public NetworkConnection req;
        public System.Action<List<ChunkPayload>,Dictionary<Vector2Int,ChunkData>, Dictionary<Vector2Int, List<EntitySpawnInfo>>, NetworkConnection> OnCompleteCallback;
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
    public float GetDepth() => maxDepth;

    public void Init(RenderTexture renderTexture, WorldGenSettingSO settings, WorldManager worldmanager, ChunkManager chunkManager,Camera renderCamera) {
        _renderTexture = renderTexture;
        _settings = settings;
        this.worldmanager = worldmanager;
        this.chunkManager = chunkManager;
        _renderCamera = renderCamera;
        _entityManager = FindFirstObjectByType<EntityManager>();
        // This should be in the constructor but I think this works like so?
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
    }

    // Call this if you change the seed at runtime
    public void InitializeNoise() {
        // Use the seed to initialize the random generator for noise offsets
        noiseRandomGen = new Unity.Mathematics.Random((uint)_settings.seed);
        // Generate large offsets based on the seed to shift noise patterns
        seedOffsetX = noiseRandomGen.NextFloat(-10000f, 10000f);
        seedOffsetY = noiseRandomGen.NextFloat(-10000f, 10000f);
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
        NetworkConnection requester, System.Action<List<ChunkPayload>,Dictionary<Vector2Int,ChunkData>,
            Dictionary<Vector2Int, List<EntitySpawnInfo>>, NetworkConnection> onGenerationComplete) {

        if (_isProcessingChunks) {
            Debug.LogWarning("Chunk generation already in progress. Request ignored.");
            onGenerationComplete?.Invoke(new List<ChunkPayload>(), new Dictionary<Vector2Int, ChunkData>(),new Dictionary<Vector2Int, List<EntitySpawnInfo>>(), requester); // Return empty list
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
            OnCompleteCallback = onGenerationComplete,
            req = requester
        };

        // Request readback. The callback 'OnReadbackCompleted' will be invoked when data is ready.
        // Using RGBA32 as it's a common, flexible format. Shader should output accordingly.
        AsyncGPUReadback.Request(_renderTexture, 0, TextureFormat.RGBA32, request => OnGPUReadbackCompleted(request, context));
    }

    private void OnGPUReadbackCompleted(AsyncGPUReadbackRequest request, ReadbackContext context) {
        Dictionary<Vector2Int, ChunkData> generatedChunks = new Dictionary<Vector2Int, ChunkData>();
        if (request.hasError) {
            Debug.LogError("GPU Readback Error!");
        } else if (request.done) // Check if 'done' just in case, though Request usually ensures it.
          {
            NativeArray<Color32> pixelData = request.GetData<Color32>();
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
                        Color32 color = pixelData[pixelIndex];

                        // --- Convert color to tile ID ---
                        // Simplest assumption: tile ID is stored in the R channel (0-255).
                        
                        ushort tileID = color.r;
                        // If tile IDs are true ushort (0-65535) and packed into R and G channels by the shader:
                        // Shader might do: outColor.r = (id % 256) / 255.0; outColor.g = floor(id / 256.0) / 255.0;
                        // Then here: tileID = (ushort)(color.r + (color.g * 256)); // color.r and .g are 0-255 bytes
                        // Or bitwise: tileID = (ushort)(color.r | (color.g << 8));

                        currentChunkData.tiles[xTileInChunk, yTileInChunk] = tileID;
                    }
                }
                generatedChunks.Add(requestedChunkCoord,currentChunkData);
            }
        }

        // Invoke the callback with the generated chunk data
        
        // Continue with generation...
            
        NetworkConnection requester = context.req; 
        StartCoroutine(ProcessChunksWithJobs(generatedChunks, requester, (processedPayloads,processedChunks,entities) => {
            // World gen complete, send them over the network.
            _isProcessingChunks = false; // Allow next request
            if (processedPayloads.Count > 0 && requester != null && requester.IsValid) {
                // TODO
                context.OnCompleteCallback?.Invoke(processedPayloads, processedChunks, entities, context.req);
                //TargetReceiveChunkDataMultiple(requester, processedPayloads);
                Debug.Log($"Sent {processedPayloads.Count} processed chunks to client {requester.ClientId}");
            } else if (requester == null || !requester.IsValid) {
                Debug.LogWarning("Requester is null or invalid, cannot send processed chunks.");
            }
        }));
    }

    internal ChunkData GenerateRestOfChunks(Vector3Int chunkOrigin, out List<EntitySpawnInfo> entitySpawns) {
        ChunkData chunkData = new ChunkData(CHUNK_TILE_DIMENSION, CHUNK_TILE_DIMENSION);
        entitySpawns = new List<EntitySpawnInfo>();
        chunkOriginCell = chunkOrigin;
        // --- Pass 3: Ore Generation ---
        // Iterate again, placing ores only on non-cave, non-water tiles
        
        
        SpawnOresInChunk(chunkData, chunkOriginCell, CHUNK_TILE_DIMENSION); // Encapsulate ore logic similar to structures/entities

        // --- Pass 4: Structure Placement ---
        // This needs careful design. It checks potential anchor points within the chunk.
        //PlaceStructuresInChunk(chunkData, chunkOriginCell,chunkSize);

        // --- Pass 5: Decorative Entity Spawning ---
        // Determines WHERE entities should be placed, adds them to chunkData.entitiesToSpawn
        
        
        //SpawnEntitiesInChunk(chunkData, chunkOriginCell, chunkSize, entitySpawns, cm);



        return chunkData;
    }

    // We can always extend this later if there is a bottleneck somewhere
    private IEnumerator ProcessChunksWithJobs(Dictionary<Vector2Int,ChunkData> initialChunks, NetworkConnection requester, 
        System.Action<List<ChunkPayload>,Dictionary<Vector2Int,ChunkData>, Dictionary<Vector2Int, List<EntitySpawnInfo>>> onProcessingComplete) {
        
        if (initialChunks == null || initialChunks.Count == 0) {
            onProcessingComplete?.Invoke(new List<ChunkPayload>(), new Dictionary<Vector2Int, ChunkData>(), new Dictionary<Vector2Int, List<EntitySpawnInfo>>());
            yield break;
        }

        List<ChunkProcessingJobData> jobDataList = new List<ChunkProcessingJobData>(initialChunks.Count);
        List<JobHandle> jobHandles = new List<JobHandle>(initialChunks.Count * 2); // Max jobs per chunk (ore + entity)

        uint worldSeed = 12345; // Get your world seed

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
                seed = worldSeed + (uint)kvp.Key.x // Vary seed per chunk slightly or use chunkCoord for determinism
            };
            processingData.OreJobHandle = oreJob.Schedule();
            jobHandles.Add(processingData.OreJobHandle);

            // --- Entity Spawn Points Job (depends on ore job's output, or could use BaseTileIDs_NA) ---
            // If entity placement depends on the ores, it needs ProcessedTileIDs_NA
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
        // Jobs done, now main thread...
        Dictionary<Vector2Int,List<ulong>> entityIdsDict = new Dictionary<Vector2Int,List<ulong>>();
        List<ChunkPayload> clientPayload = new List<ChunkPayload>();
        foreach (var chunks in chunksToSend) {
            var entityInChunk = SpawnEntitiesInChunk(chunks.Value, chunks.Key, chunkManager);
            entitySpawnInfos.Add(chunks.Key, entityInChunk);
            yield return null; // Wait a frame
            // Now we have the enemy info, get the persistant IDs from EntityManager
            entityIdsDict.Add(chunks.Key, _entityManager.AddGeneratedEntityData(chunks.Key, entityInChunk));
        }
        foreach(var data in payloadsToSend) {
            var entityList = entityIdsDict.TryGetValue(data.Key, out var entities);
            clientPayload.Add(new ChunkPayload(data.Value, entities));
        }
     
        Debug.Log($"All chunk processing jobs complete. {clientPayload.Count} payloads ready.");
        onProcessingComplete?.Invoke(clientPayload, chunksToSend,entitySpawnInfos);
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
        return new ChunkPayload(data.ChunkCoord, finalTileIdsList, finalOreIdsList, null, null);
    }
    // 0 Air, 1 Stone, 
    // --- Pass 1 Helper: Determine Base Terrain & Primary Biome ---
    private ushort DetermineBaseTerrainAndBiome(int worldX, int worldY, out BiomeType primaryBiome) {
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
                return 0;//_settings.surfaceWaterTile ?? _settings.mainWaterTile; // Use specified surface water or fallback
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
            return 0; // Inside main trench
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
            // Use different noise samples for start/end for independent boundaries -0.5 * 2 to get -1 to 1 range
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
               // return biome.defaultGroundTile; // TODODODODODODO
            }
        }
        // Fallback if outside all biome influences
        return 1;
    }

    // --- New Cave Generation Function (Pass 2) ---
    private void GenerateNoiseCavesForChunk(ChunkData chunkData, Vector3Int chunkOriginCell, int chunkSize) {
        for (int y = 0; y < chunkSize; y++) {
            for (int x = 0; x < chunkSize; x++) {

                // Only carve caves into existing rock/ground tiles
                if (!IsSolid(chunkData.tiles[x, y])) {
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
                    chunkData.tiles[x, y] = 0;
                }
            }
        }
    }

    // --- Pass 3 Helper 
    private void SpawnOresInChunk(ChunkData chunkData, Vector3Int chunkOriginCell, int chunkSize) {
        for (int y = 0; y < chunkSize; y++) {
            for (int x = 0; x < chunkSize; x++) {
                ushort currentTileID = chunkData.tiles[x, y];
                if (IsSolid(currentTileID)) // Check if it's a valid tile for ore placement
                {
                    int worldX = chunkOriginCell.x + x;
                    int worldY = chunkOriginCell.y + y;
                    string biomeName = null;//GetBiomeNameAt(worldX, worldY);

                    TileBase oreTile = DetermineOre(worldX, worldY, biomeName);
                    if (oreTile != null) {
                        //Debug.Log($"Generating ore at: X: {worldX} Y: {worldY}");
                        chunkData.oreID[x, y] = App.ResourceSystem.GetIDByTile(oreTile as TileSO); // Todo will probably not work
                    }
                }
            }
        }
    }
    private TileBase DetermineOre(int worldX, int worldY, string biomeName) {
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

    private List<EntitySpawnInfo> SpawnEntitiesInChunk(ChunkData chunkData, Vector2Int chunkOriginCell, ChunkManager cm) {
        List<EntitySpawnInfo> entities = new List<EntitySpawnInfo>();
        HashSet<Vector2Int> occupiedAnchors = new HashSet<Vector2Int>();
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
                                var tileToCheck = GetTileFromChunkOrWorld(chunkData, new Vector2Int(worldX, worldY), CHUNK_TILE_DIMENSION,
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
                    //occupiedAnchors.Add(new Vector2Int(x, y));
                    occupiedAnchors.AddRange(occopied);
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
    private float GetNoise(float x, float y, float frequency) {
        // Apply seed offsets and frequency
        float sampleX = (x + seedOffsetX) * frequency;
        float sampleY = (y + seedOffsetY) * frequency;
        // noise.snoise returns value in range [-1, 1], remap to [0, 1]
        return (noise.snoise(new float2(sampleX, sampleY)) + 1f) * 0.5f;
    }
    // Helper for deterministic hashing (useful for structure placement)
    private float GetHash(int x, int y) {
        // Simple hash combining seed, x, y. Replace with a better one if needed.
        uint hash = (uint)_settings.seed;
        hash ^= (uint)x * 73856093;
        hash ^= (uint)y * 19349663;
        hash ^= (uint)(x * y) * 83492791;
        return (hash & 0x0FFFFFFF) / (float)0x0FFFFFFF; // Convert to [0, 1] float
    }

}
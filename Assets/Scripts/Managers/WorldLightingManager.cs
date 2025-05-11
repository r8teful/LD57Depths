using UnityEngine;
using UnityEngine.Rendering.Universal; 
using System.Collections.Generic;
using System.Linq;
using System;
using Random = UnityEngine.Random;

[System.Serializable]
public struct LightProperties {
    public Color color;
    public float intensity;
    // Potentially falloff, etc.
}

// In your LightingManager file or a separate utility file
public struct EdgeKey : IEquatable<EdgeKey> {
    public Vector2 P1;
    public Vector2 P2;

    // Constructor ensures P1 is always "smaller" for consistent hashing/equality
    public EdgeKey(Vector2 p1, Vector2 p2) {
        if (p1.x < p2.x || (p1.x == p2.x && p1.y < p2.y)) {
            P1 = p1;
            P2 = p2;
        } else {
            P1 = p2;
            P2 = p1;
        }
    }

    public bool Equals(EdgeKey other) {
        return P1.Equals(other.P1) && P2.Equals(other.P2);
    }

    public override bool Equals(object obj) {
        return obj is EdgeKey other && Equals(other);
    }

    public override int GetHashCode() {
        unchecked // Overflow is fine, just wrap
        {
            int hash = 17;
            hash = hash * 23 + P1.GetHashCode();
            hash = hash * 23 + P2.GetHashCode();
            return hash;
        }
    }

    public static bool operator ==(EdgeKey left, EdgeKey right) => left.Equals(right);
    public static bool operator !=(EdgeKey left, EdgeKey right) => !(left == right);
}
public class WorldLightingManager : MonoBehaviour {
    [SerializeField] private ChunkManager _chunkManager;
    [SerializeField] private WorldManager _worldManager;
    [Header("Light Settings")]
    public Light2D lightPrefab; // Assign a prefab with a Freeform Light2D
    public int minRegionSizeForLight = 10;
    public float updateCooldown = 0.2f; // Seconds

    [System.Serializable]
    public class BiomeLightSetting {
        public BiomeType biome;
        public LightProperties properties;
    }
    public List<BiomeLightSetting> biomeLightConfigs;
    private Dictionary<BiomeType, LightProperties> _biomeLightSettingsMap;

    private List<Light2D> _activeLights = new List<Light2D>();
    private Queue<Light2D> _pooledLights = new Queue<Light2D>();

    private bool _needsUpdate = false;
    private float _lastUpdateTime = 0f;

    // Inside LightingManager class

    // Cache these to avoid re-allocation if this function is called very often
    // Adjust size based on typical number of edges/vertices you expect.
    private HashSet<EdgeKey> _reusableUniqueEdgeKeys = new HashSet<EdgeKey>();
    private List<Tuple<Vector2, Vector2>> _reusableBoundaryEdges = new List<Tuple<Vector2, Vector2>>();
    private Dictionary<Vector2, List<Vector2>> _reusableAdj = new Dictionary<Vector2, List<Vector2>>();
    private List<Vector2> _reusableOrderedVertices = new List<Vector2>();
    private HashSet<Vector2> _reusableVisitedPathNodes = new HashSet<Vector2>();
    private HashSet<EdgeKey> _reusableUsedEdgesInPath = new HashSet<EdgeKey>();
    private List<Vector2> _reusableSimplifiedPath = new List<Vector2>();

    // Shadows
    private GameObject _mainTilemap;
    private CompositeCollider2D _compositeCollider;

    private List<ShadowCaster2D> _activeShadowCasters = new List<ShadowCaster2D>();
    private Queue<ShadowCaster2D> _pooledShadowCasters = new Queue<ShadowCaster2D>();
    // Reusable lists to avoid allocations in the update loop
    private List<Vector2> _reusablePointsInPath = new List<Vector2>();
    private List<Vector3> _reusablePointsInPath3D = new List<Vector3>();
    private Vector3[] _pathArrayForSetter; // Reusable array for SetPathOptimized
    // --- Methods ---
    void Start() {
        Initialize();
    }

    void Initialize() {
        // Biome stuff
        _biomeLightSettingsMap = new Dictionary<BiomeType, LightProperties>();
        foreach (var config in biomeLightConfigs) {
            if (!_biomeLightSettingsMap.ContainsKey(config.biome)) {
                _biomeLightSettingsMap.Add(config.biome, config.properties);
            }
        }
        _mainTilemap = _worldManager.GetMainTileMap();
        // References
        _compositeCollider = _mainTilemap.GetComponent<CompositeCollider2D>();
    }

    public void RequestLightUpdate() {
        Debug.Log("Updating lights!");
        _needsUpdate = true;
    }

    void Update() {
        if (_needsUpdate && Time.time > _lastUpdateTime + updateCooldown) {
            //PerformFullLightUpdateOld();
            PerformFullShadowUpdate();
            _needsUpdate = false;
            _lastUpdateTime = Time.time;
        }
    }
    void PerformFullShadowUpdate() {
        // Then it creates the new shadow casters, based on the paths of the composite collider
        int pathCount = _compositeCollider.pathCount;
        // Ensure active casters list can accommodate all paths
        // and get/create casters as needed
        for (int i = 0; i < pathCount; ++i) {
            ShadowCaster2D currentCaster;
            if (i < _activeShadowCasters.Count) {
                currentCaster = _activeShadowCasters[i];
                if (!currentCaster.gameObject.activeSelf) // Ensure it's active if reused
                {
                    currentCaster.gameObject.SetActive(true);
                }
            } else {
                currentCaster = GetShadowCasterFromPool();
                _activeShadowCasters.Add(currentCaster);
            }

            // Get path data
            _compositeCollider.GetPath(i, _reusablePointsInPath);

            // Convert List<Vector2> to Vector3[] for SetPathOptimized
            // Reuse _reusablePointsInPath3D list and _pathArrayForSetter array
            _reusablePointsInPath3D.Clear();
            for (int j = 0; j < _reusablePointsInPath.Count; ++j) {
                _reusablePointsInPath3D.Add(_reusablePointsInPath[j]); // Implicit conversion from Vector2 to Vector3 (z=0)
            }

            // Ensure our reusable array is large enough
            if (_pathArrayForSetter == null || _pathArrayForSetter.Length < _reusablePointsInPath3D.Count) {
                _pathArrayForSetter = new Vector3[_reusablePointsInPath3D.Count];
            }

            // Copy to the array segment that will be used
            for (int j = 0; j < _reusablePointsInPath3D.Count; ++j) {
                _pathArrayForSetter[j] = _reusablePointsInPath3D[j];
            }

            // If the actual path is shorter than the array, we need to pass a correctly sized array or a sub-segment.
            // SetPath likely makes a copy, but to be safe and clear, let's give it exactly what it needs.
            // A simple way if SetPathOptimized doesn't handle sub-arrays is to create the exact size.
            // However, if SetPath *does* use the array reference directly (unlikely for safety), then giving it a larger array where only a prefix is valid could be an issue.
            // Given the reflection, it's highly probable Unity's internal code for m_ShapePath copies the array.
            // So, creating a new array of the exact size here is safer and often what ToArray() does.
            // Let's stick to ToArray() for simplicity unless proven it's a major bottleneck AFTER other optimizations.
            // The previous loop created _reusablePointsInPath3D.ToArray() for each. We can optimize the array creation.

            Vector3[] finalPath;
            if (_reusablePointsInPath3D.Count == _pathArrayForSetter.Length) {
                // If counts match, we can use the array directly (assuming it was fully populated)
                finalPath = _pathArrayForSetter;
            } else {
                // If counts don't match (e.g. _pathArrayForSetter was larger and we only filled part of it),
                // or to be absolutely safe if _pathArrayForSetter isn't perfectly managed for exact size.
                finalPath = _reusablePointsInPath3D.ToArray(); // This still allocates, but only if needed or for safety.
            }
            // A more performant way if SetPath *always* copies the data:
            // No need to use _pathArrayForSetter if we always call ToArray().
            // Just use _reusablePointsInPath3D.ToArray(). The primary win is reusing the list.

            currentCaster.SetPathOptimized(finalPath); // Or _reusablePointsInPath3D.ToArray()
            currentCaster.SetPathHashOptimized(Random.Range(int.MinValue, int.MaxValue));
            // component.Update() was in your original code. Test if it's still needed.
            currentCaster.Update();
            // SetPathHash *should* trigger the internal rebuild.
            // currentCaster.Update(); // Potentially redundant. Profile this.

            // Clear the reusable list for the next path (already done at the start of the loop)
            // _reusablePointsInPath.Clear(); // Done by GetPath
        }

        // Deactivate and pool any casters that are no longer needed
        if (pathCount < _activeShadowCasters.Count) {
            for (int i = _activeShadowCasters.Count - 1; i >= pathCount; --i) {
                ReturnShadowCasterToPool(_activeShadowCasters[i]);
                _activeShadowCasters.RemoveAt(i); // Remove from the end is efficient
            }
        }
    }
    ShadowCaster2D CreateAndPoolNewShadowCaster() {
        GameObject newShadowCasterGO = new GameObject("PooledShadowCaster2D");
        newShadowCasterGO.isStatic = true; // Important: if they are static, they should not move with the parent unless the parent is also static and part of the same static batch.
                                           // If the tilemap itself moves, these children might not update correctly if marked static.
                                           // If the tilemap is static, then this is fine.
                                           // If the tilemap MOVES, then newShadowCasterGO.isStatic should be false.
        newShadowCasterGO.transform.SetParent(_compositeCollider.transform, false); // Set parent
        ShadowCaster2D component = newShadowCasterGO.AddComponent<ShadowCaster2D>();
        component.selfShadows = true; // Set this once
        newShadowCasterGO.SetActive(false); // Start inactive
        _pooledShadowCasters.Enqueue(component);
        return component;
    }
    ShadowCaster2D GetShadowCasterFromPool() {
        if (_pooledShadowCasters.Count > 0) {
            ShadowCaster2D caster = _pooledShadowCasters.Dequeue();
            caster.gameObject.SetActive(true);
            return caster;
        }
        // Pool is empty, create a new one (and it won't be added to the pool queue until returned)
        GameObject newShadowCasterGO = new GameObject("ShadowCaster2D_New");
        // Consider if isStatic is appropriate if the tilemap itself moves.
        // If the tilemap is truly static in the world, then newShadowCasterGO.isStatic = true is good.
        // If the tilemap (and thus the composite collider) can move, set isStatic = false.
        newShadowCasterGO.isStatic = true; // Or false, see comment above
        newShadowCasterGO.transform.SetParent(_compositeCollider.transform, false);
        ShadowCaster2D component = newShadowCasterGO.AddComponent<ShadowCaster2D>();
        component.selfShadows = true;
        // No need to set active true here, it will be used immediately
        return component;
    }
    void ReturnShadowCasterToPool(ShadowCaster2D caster) {
        if (caster != null) {
            caster.gameObject.SetActive(false);
            // Optionally, reset its path to an empty array or a default state if necessary
            // caster.SetPathOptimized(new Vector3[0]); 
            // caster.SetPathHashOptimized(0); // Or some default hash
            _pooledShadowCasters.Enqueue(caster);
        }
    }
    void PerformFullLightUpdateOld() {
        Debug.Log("Updating lights ACTUALLY!");
        // 1. Deactivate and pool existing lights
        foreach (var light in _activeLights) {
            light.gameObject.SetActive(false);
            _pooledLights.Enqueue(light);
        }
        _activeLights.Clear();

        // 2. Get all non-solid tiles from loaded chunks
        // This part heavily depends on your chunk/world management system
        // For simplicity, let's assume a function GetRelevantNonSolidTiles()
        // which returns a list of GLOBAL coordinates of non-solid tiles.
        var d = _chunkManager.GetAllNonSolidTilesInLoadedChunks();
        HashSet<Vector2Int> allNonSolidGlobalTiles = d.Item1;
        Dictionary<Vector2Int, BiomeType> tilesByBiome = d.Item2;
        HashSet<Vector2Int> visitedTilesThisUpdate = new HashSet<Vector2Int>();

        foreach (Vector2Int globalTilePos in allNonSolidGlobalTiles) {
            if (!visitedTilesThisUpdate.Contains(globalTilePos)) {
                List<Vector2Int> currentRegionTiles = FloodFill(globalTilePos, allNonSolidGlobalTiles, visitedTilesThisUpdate);

                if (currentRegionTiles.Count < minRegionSizeForLight) {
                    continue; // Skip small regions
                }

                // Determine dominant biome
                BiomeType dominantBiome = DetermineDominantBiome(currentRegionTiles,tilesByBiome);
                Debug.Log("Dominant biome is:" + dominantBiome);
                // Generate outline (This is the complex part)
                List<Vector2> outlineWorldVertices = GenerateOutlineForRegion2(currentRegionTiles, allNonSolidGlobalTiles);
                
                if (outlineWorldVertices != null && outlineWorldVertices.Count >= 3) {
                    Light2D light = GetPooledLight();
                    light.transform.position = Vector3.zero; // Paths are in world space
                    light.lightType = Light2D.LightType.Freeform;

                    // Convert Vector2 to Vector3 for SetShapePath
                    Vector3[] shapePath = new Vector3[outlineWorldVertices.Count];
                    for (int i = 0; i < outlineWorldVertices.Count; i++) {
                        shapePath[i] = new Vector3(outlineWorldVertices[i].x, outlineWorldVertices[i].y, 0);
                    }

                    // SetShapePath expects an array of paths (for holes). We provide one.
                    light.SetShapePath(shapePath);


                    if (_biomeLightSettingsMap.TryGetValue(dominantBiome, out LightProperties props)) {
                        light.color = props.color;
                        light.intensity = props.intensity;
                    } else {
                        // Default light if biome not found
                        light.color = Color.white;
                        light.intensity = 0.5f;
                    }

                    light.gameObject.SetActive(true);
                    _activeLights.Add(light);
                }
            }
        }
    }
    void PerformFullLightUpdate() {
        Debug.Log("Updating lights ACTUALLY!");
        // 1. Deactivate and pool existing lights
        foreach (var light in _activeLights) {
            light.gameObject.SetActive(false);
            _pooledLights.Enqueue(light);
        }
        _activeLights.Clear();

        // 2. Get all non-solid tiles from loaded chunks
        // This part heavily depends on your chunk/world management system
        // For simplicity, let's assume a function GetRelevantNonSolidTiles()
        // which returns a list of GLOBAL coordinates of non-solid tiles.
        var d = _chunkManager.GetAllNonSolidTilesInLoadedChunks();
        HashSet<Vector2Int> allNonSolidGlobalTiles = d.Item1;
        Dictionary<Vector2Int, BiomeType> tilesByBiome = d.Item2;
        HashSet<Vector2Int> visitedTilesThisUpdate = new HashSet<Vector2Int>();

        foreach (Vector2Int globalTilePos in allNonSolidGlobalTiles) {
            if (!visitedTilesThisUpdate.Contains(globalTilePos)) {
                List<Vector2Int> currentRegionTiles = FloodFill(globalTilePos, allNonSolidGlobalTiles, visitedTilesThisUpdate);

                if (currentRegionTiles.Count < minRegionSizeForLight) {
                    continue; // Skip small regions
                }

                // Determine dominant biome
                BiomeType dominantBiome = DetermineDominantBiome(currentRegionTiles, tilesByBiome);
                Debug.Log("Dominant biome is:" + dominantBiome);
                // Generate outline (This is the complex part)
                List<List<Vector2>> polygons = GenerateRegionPolygons(currentRegionTiles, allNonSolidGlobalTiles, 1f);

                if (polygons != null && polygons.Count > 0) {
                    Light2D light = GetPooledLight();
                    light.transform.position = Vector3.zero; // Paths are world space
                    light.lightType = Light2D.LightType.Freeform;

                    Vector3[] shapePaths = polygons
                        // keep only non-null polygons with at least 3 points
                        .Where(poly => poly != null && poly.Count >= 3)
                        // project each polygon to a sequence of Vector3, flattening them
                        .SelectMany(poly => poly.Select(point => new Vector3(point.x, point.y, 0)))
                        .ToArray();  // materialize as Vector3[] :contentReference[oaicite:0]{index=0}
             
                    if (shapePaths.Length > 0) {
                        light.SetShapePath(shapePaths);

                        if (_biomeLightSettingsMap.TryGetValue(dominantBiome, out LightProperties props)) {
                            light.color = props.color;
                            light.intensity = props.intensity;
                        } else {
                            light.color = Color.white;
                            light.intensity = 0.5f;
                        }

                        light.gameObject.SetActive(true);
                        _activeLights.Add(light);
                    } else {
                        // No valid paths generated, return light to pool
                        light.gameObject.SetActive(false);
                        _pooledLights.Enqueue(light);
                    }
                }
            }
        }
    }

    private Light2D GetPooledLight() {
        if (_pooledLights.Count > 0) {
            return _pooledLights.Dequeue();
        }
        Light2D newLight = Instantiate(lightPrefab, transform); // Parent to LightingManager
        return newLight;
    }

    private List<Vector2Int> FloodFill(Vector2Int startPos, HashSet<Vector2Int> availableNonSolidTiles, HashSet<Vector2Int> visitedGlobally) {
        List<Vector2Int> regionTiles = new List<Vector2Int>();
        Queue<Vector2Int> queue = new Queue<Vector2Int>();
        HashSet<Vector2Int> visitedInThisFill = new HashSet<Vector2Int>(); // Local visited set for this specific flood fill

        if (!availableNonSolidTiles.Contains(startPos) || visitedGlobally.Contains(startPos))
            return regionTiles;

        queue.Enqueue(startPos);
        visitedInThisFill.Add(startPos);
        visitedGlobally.Add(startPos); // Mark as globally visited

        Vector2Int[] directions = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };

        while (queue.Count > 0) {
            Vector2Int current = queue.Dequeue();
            regionTiles.Add(current);

            foreach (var dir in directions) {
                Vector2Int neighbor = current + dir;
                if (availableNonSolidTiles.Contains(neighbor) && !visitedInThisFill.Contains(neighbor)) {
                    visitedInThisFill.Add(neighbor);
                    visitedGlobally.Add(neighbor); // Also mark as globally visited
                    queue.Enqueue(neighbor);
                }
            }
        }
        return regionTiles;
    }

    private BiomeType DetermineDominantBiome(List<Vector2Int> regionTiles, Dictionary<Vector2Int, BiomeType> tilesByBiome) {
        if (regionTiles == null || regionTiles.Count == 0)
            return default(BiomeType); // Or a default biome

        return regionTiles
            .Select(tilePos => tilesByBiome[tilePos]) // You need a GetBlockBiome(Vector2Int globalPos)
            .GroupBy(biome => biome)
            .OrderByDescending(group => group.Count())
            .First().Key;
    }

    // --- Outline Generation (COMPLEX - this is a simplified stub) ---
    // A robust solution here would use a boundary tracing algorithm like Moore-Neighbor
    // or Marching Squares variant to get an ordered list of vertices.
    // This simplified version just collects all external edges.
    // You'll need to replace this with a proper outline stitcher.
    private List<Vector2> GenerateOutlineForRegion(List<Vector2Int> regionTiles, HashSet<Vector2Int> allNonSolidGlobalTiles) {
        if (regionTiles == null || regionTiles.Count == 0)
            return null;

        HashSet<Vector2Int> regionSet = new HashSet<Vector2Int>(regionTiles);
        List<Tuple<Vector2, Vector2>> boundaryEdges = new List<Tuple<Vector2, Vector2>>();

        // Define tile corners relative to tile's bottom-left origin (0,0) for that tile
        // In world space, tile (tx,ty) covers from (tx*tileSize, ty*tileSize) to ((tx+1)*tileSize, (ty+1)*tileSize)
        // Assuming tileSize = 1 for simplicity here. Adjust if your tileSize is different.
        float tileSize = 1.0f;

        foreach (var tilePos in regionTiles) {
            // Tile world coordinates (bottom-left)
            float worldX = tilePos.x * tileSize;
            float worldY = tilePos.y * tileSize;

            // Vertices of the current tile (counter-clockwise from bottom-left)
            Vector2 v0 = new Vector2(worldX, worldY);               // Bottom-left
            Vector2 v1 = new Vector2(worldX + tileSize, worldY);    // Bottom-right
            Vector2 v2 = new Vector2(worldX + tileSize, worldY + tileSize); // Top-right
            Vector2 v3 = new Vector2(worldX, worldY + tileSize);    // Top-left

            // Check neighbors
            // Bottom edge (v0-v1)
            if (!regionSet.Contains(tilePos + Vector2Int.down))
                AddEdgeIfUnique(boundaryEdges, v0, v1);
            // Right edge (v1-v2)
            if (!regionSet.Contains(tilePos + Vector2Int.right))
                AddEdgeIfUnique(boundaryEdges, v1, v2);
            // Top edge (v2-v3)
            if (!regionSet.Contains(tilePos + Vector2Int.up))
                AddEdgeIfUnique(boundaryEdges, v2, v3);
            // Left edge (v3-v0)
            if (!regionSet.Contains(tilePos + Vector2Int.left))
                AddEdgeIfUnique(boundaryEdges, v3, v0);
        }

        // Now, boundaryEdges contains unique, unordered edges. We need to stitch them.
        // This is the hard part omitted for brevity. A proper algorithm is needed here.
        // For a placeholder, you could try to find the convex hull or a simple chain.
        // For now, we'll return a very simple, likely incorrect, path for testing SetShapePath.
        if (boundaryEdges.Count == 0)
            return null;

        // VERY DUMMY STITCHING - REPLACE THIS!
        List<Vector2> orderedVertices = new List<Vector2>();
        if (boundaryEdges.Count > 0) {
            // This simple example just takes all unique vertices from the edges.
            // This will NOT form a correct path for SetShapePath generally.
            // A proper implementation needs to order these vertices correctly to form a polygon.
            HashSet<Vector2> uniqueVertices = new HashSet<Vector2>();
            foreach (var edge in boundaryEdges) {
                uniqueVertices.Add(edge.Item1);
                uniqueVertices.Add(edge.Item2);
            }
            // A minimal convex hull algorithm or gift wrapping could be a starting point
            // for a slightly better placeholder if you have few points.
            // For actual game use, a robust boundary tracing algorithm is required.
            // Example: https://en.wikipedia.org/wiki/Moore_neighbor_tracing
            // Or even simpler: Pick an edge, find next connected edge, repeat.

            // Simple chain attempt (will fail for complex shapes/holes)
            if (boundaryEdges.Count > 0) {
                var adj = new Dictionary<Vector2, List<Vector2>>();
                foreach (var edge in boundaryEdges) {
                    if (!adj.ContainsKey(edge.Item1))
                        adj[edge.Item1] = new List<Vector2>();
                    if (!adj.ContainsKey(edge.Item2))
                        adj[edge.Item2] = new List<Vector2>();
                    adj[edge.Item1].Add(edge.Item2);
                    adj[edge.Item2].Add(edge.Item1);
                }

                if (adj.Count > 0) {
                    Vector2 startNode = adj.Keys.First();
                    Vector2 currentNode = startNode;
                    HashSet<Vector2> visitedPathNodes = new HashSet<Vector2>();

                    for (int i = 0; i < adj.Count * 2 && orderedVertices.Count < uniqueVertices.Count; ++i) // Safety break
                    {
                        orderedVertices.Add(currentNode);
                        visitedPathNodes.Add(currentNode);
                        bool foundNext = false;
                        if (adj.TryGetValue(currentNode, out List<Vector2> neighbors)) {
                            foreach (var neighbor in neighbors) {
                                // Prefer unvisited, or if it's the start node and path is long enough
                                if (!visitedPathNodes.Contains(neighbor)) {
                                    currentNode = neighbor;
                                    foundNext = true;
                                    break;
                                } else if (neighbor == startNode && orderedVertices.Count > 2) {
                                    currentNode = neighbor; // Close the loop
                                    foundNext = true;
                                    break;
                                }
                            }
                            // If all neighbors visited but not back to start, pick one not last one (bad for complex)
                            if (!foundNext && neighbors.Count > 0) {
                                if (neighbors.Count == 1 && neighbors[0] == startNode && orderedVertices.Count > 2) {
                                    currentNode = neighbors[0]; // Close loop
                                } else {
                                    // This part is problematic and means the simple chaining fails
                                    var nextOpt = neighbors.FirstOrDefault(n => orderedVertices.Count < 2 || n != orderedVertices[orderedVertices.Count - 2]);
                                    if (nextOpt != default(Vector2))
                                        currentNode = nextOpt;
                                    else
                                        break;
                                }
                            } else if (!foundNext)
                                break; // No more ways to go
                        } else
                            break; // Should not happen if adj was built correctly

                        if (currentNode == startNode && orderedVertices.Count > 2) {
                            // orderedVertices.Add(currentNode); // Close the loop
                            break;
                        }
                    }
                }
            }
            // If orderedVertices doesn't form a closed loop back to its start, SetShapePath may behave unexpectedly.
            // Make sure the last point is same as first for closed shape.
            if (orderedVertices.Count > 2 && orderedVertices[0] != orderedVertices[orderedVertices.Count - 1]) {
                //This is bad logic. SetShapePath does NOT want the last point to be the first. It closes it automatically.
                // So if your tracer adds the start point at the end, remove it.
            }
            Debug.LogWarning("Outline generation is using a placeholder stitcher. Replace with a robust algorithm.");
            if (orderedVertices.Count < 3)
                return null; // Not enough for a shape
            return orderedVertices;
        }
        return null; // Fallback
    }
    private List<Vector2> GenerateOutlineForRegion2(List<Vector2Int> regionTiles, HashSet<Vector2Int> allNonSolidGlobalTiles) {
        if (regionTiles == null || regionTiles.Count == 0)
            return null;

        // Clear reusable collections
        _reusableUniqueEdgeKeys.Clear();
        _reusableBoundaryEdges.Clear();
        _reusableAdj.Clear();
        _reusableOrderedVertices.Clear();
        _reusableVisitedPathNodes.Clear();
        _reusableUsedEdgesInPath.Clear();
        _reusableSimplifiedPath.Clear();


        HashSet<Vector2Int> regionSet = new HashSet<Vector2Int>(regionTiles); // Could pool this too if regions are massive
        float tileSize = 1.0f;

        // --- 1. Identify Boundary Edges (Optimized Uniqueness Check) ---
        foreach (var tilePos in regionTiles) {
            float worldX = tilePos.x * tileSize;
            float worldY = tilePos.y * tileSize;

            Vector2 v0_bl = new Vector2(worldX, worldY); // Bottom-left
            Vector2 v1_br = new Vector2(worldX + tileSize, worldY); // Bottom-right
            Vector2 v2_tr = new Vector2(worldX + tileSize, worldY + tileSize); // Top-right
            Vector2 v3_tl = new Vector2(worldX, worldY + tileSize); // Top-left

            // Helper to add edge if its key is unique
            Action<Vector2, Vector2> TryAddBoundaryEdge = (p1, p2) => {
                EdgeKey key = new EdgeKey(p1, p2); // Canonical key
                if (_reusableUniqueEdgeKeys.Add(key)) // HashSet.Add is O(1)
                {
                    // Store original edge for stitching (p1, p2 order might matter for naive stitcher)
                    _reusableBoundaryEdges.Add(Tuple.Create(p1, p2));
                }
            };

            // Check neighbors and add edges if they are boundaries
            // Tile relative point naming (v0=BL, v1=BR, v2=TR, v3=TL)
            // Edge: Bottom (v0_bl -> v1_br)
            if (!regionSet.Contains(tilePos + Vector2Int.down))
                TryAddBoundaryEdge(v0_bl, v1_br);
            // Edge: Right (v1_br -> v2_tr)
            if (!regionSet.Contains(tilePos + Vector2Int.right))
                TryAddBoundaryEdge(v1_br, v2_tr);
            // Edge: Top (v3_tl -> v2_tr) - For CCW path this should be v2_tr -> v3_tl if tile is below
            // To keep SetShapePath happy, path should be wound consistently (e.g. CCW for outer).
            // Current dummy stitcher may not guarantee winding.
            // Let's stick to (p1, p2) and let the stitcher determine final order for now.
            if (!regionSet.Contains(tilePos + Vector2Int.up))
                TryAddBoundaryEdge(v3_tl, v2_tr); // or (v2_tr, v3_tl)
                                                  // Edge: Left (v0_bl -> v3_tl)
            if (!regionSet.Contains(tilePos + Vector2Int.left))
                TryAddBoundaryEdge(v0_bl, v3_tl); // or (v3_tl, v0_bl)
        }

        if (_reusableBoundaryEdges.Count == 0)
            return null;

        // --- 2. Stitch Edges (Your "Dummy Stitching" adapted) ---
        foreach (var edge in _reusableBoundaryEdges) {
            if (!_reusableAdj.ContainsKey(edge.Item1))
                _reusableAdj[edge.Item1] = new List<Vector2>();
            if (!_reusableAdj.ContainsKey(edge.Item2))
                _reusableAdj[edge.Item2] = new List<Vector2>();
            _reusableAdj[edge.Item1].Add(edge.Item2);
            _reusableAdj[edge.Item2].Add(edge.Item1); // For undirected graph behavior
        }

        if (_reusableAdj.Count > 0) {
            // Find a deterministic start node (e.g., min X, then min Y)
            Vector2 startNode = Vector2.positiveInfinity;
            bool firstKey = true;
            foreach (var key in _reusableAdj.Keys) { // More robust than LINQ OrderBy for finding min
                if (firstKey) {
                    startNode = key;
                    firstKey = false;
                } else {
                    if (key.x < startNode.x)
                        startNode = key;
                    else if (key.x == startNode.x && key.y < startNode.y)
                        startNode = key;
                }
            }


            Vector2 currentNode = startNode;

            // Max iterations: number of edges + 1 (to close loop)
            for (int i = 0; i < _reusableBoundaryEdges.Count + 1; ++i) {
                _reusableOrderedVertices.Add(currentNode);
                _reusableVisitedPathNodes.Add(currentNode);

                bool foundNext = false;
                if (_reusableAdj.TryGetValue(currentNode, out List<Vector2> neighbors)) {
                    // Optional: Sort neighbors for deterministic traversal if needed for specific complex cases
                    // neighbors.Sort((a, b) => GetAngle(currentNode, a).CompareTo(GetAngle(currentNode, b)));

                    foreach (var neighbor in neighbors) {
                        EdgeKey currentEdgeKey = new EdgeKey(currentNode, neighbor);
                        if (!_reusableUsedEdgesInPath.Contains(currentEdgeKey)) // Try to use an unused edge
                        {
                            if (neighbor == startNode && _reusableOrderedVertices.Count > 2) // Path long enough to close
                            {
                                _reusableUsedEdgesInPath.Add(currentEdgeKey);
                                currentNode = neighbor; // Will add startNode again to close loop
                                foundNext = true;
                                break;
                            } else if (!_reusableVisitedPathNodes.Contains(neighbor)) // Prefer unvisited nodes
                              {
                                _reusableUsedEdgesInPath.Add(currentEdgeKey);
                                currentNode = neighbor;
                                foundNext = true;
                                break;
                            }
                        }
                    }

                    // If no preferred path found (all neighbors visited or their edges used),
                    // try to pick any unused edge to a neighbor, even if node visited (might happen in complex shapes before loop closes)
                    if (!foundNext) {
                        foreach (var neighbor in neighbors) {
                            EdgeKey currentEdgeKey = new EdgeKey(currentNode, neighbor);
                            if (!_reusableUsedEdgesInPath.Contains(currentEdgeKey)) {
                                _reusableUsedEdgesInPath.Add(currentEdgeKey);
                                currentNode = neighbor; // This might lead back to startNode or another visited node via a new edge
                                foundNext = true;
                                break;
                            }
                        }
                    }
                }

                if (!foundNext || (currentNode == startNode && _reusableOrderedVertices.Count > 1)) // Loop closed or stuck
                {
                    // If it closed by reaching startNode, ensure startNode is added if not last one.
                    if (currentNode == startNode && _reusableOrderedVertices.Count > 0 && _reusableOrderedVertices[_reusableOrderedVertices.Count - 1] != startNode) {
                        _reusableOrderedVertices.Add(startNode);
                    }
                    break;
                }
            }
        }

        // SetShapePath automatically closes the path from last to first.
        // If our stitcher explicitly added the start node at the end, remove it.
        if (_reusableOrderedVertices.Count > 1 && _reusableOrderedVertices[0] == _reusableOrderedVertices[_reusableOrderedVertices.Count - 1]) {
            _reusableOrderedVertices.RemoveAt(_reusableOrderedVertices.Count - 1);
        }

        if (_reusableOrderedVertices.Count < 3)
            return null; // Not enough for a shape


        // --- 3. Path Simplification (Remove Collinear Points) ---
        if (_reusableOrderedVertices.Count > 0) {
            _reusableSimplifiedPath.Add(_reusableOrderedVertices[0]);
            for (int i = 1; i < _reusableOrderedVertices.Count; i++) {
                if (_reusableSimplifiedPath.Count < 2) {
                    _reusableSimplifiedPath.Add(_reusableOrderedVertices[i]);
                    continue;
                }

                Vector2 p0 = _reusableSimplifiedPath[_reusableSimplifiedPath.Count - 2];
                Vector2 p1 = _reusableSimplifiedPath[_reusableSimplifiedPath.Count - 1];
                // Current point from original path to consider adding
                Vector2 p2 = (i == _reusableOrderedVertices.Count - 1 && _reusableSimplifiedPath.Count > 1) ?
                                _reusableOrderedVertices[0] : // For last segment, check against the start of simplified path for closure
                                _reusableOrderedVertices[i];

                // If checking closure: p2 is the start of the simplified path, p0/p1 are last two added.
                bool checkAgainstStartForClosure = (i == _reusableOrderedVertices.Count - 1 && _reusableSimplifiedPath.Count > 1 && _reusableSimplifiedPath[0] == _reusableOrderedVertices[i]);
                if (checkAgainstStartForClosure)
                    p2 = _reusableSimplifiedPath[0];
                else
                    p2 = _reusableOrderedVertices[i];


                // For grid-aligned geometry, it's easier to check if slopes are same (vertical or horizontal)
                bool isCollinear = false;
                // Check for horizontal collinearity
                if (Mathf.Approximately(p0.y, p1.y) && Mathf.Approximately(p1.y, p2.y)) {
                    isCollinear = true;
                }
                // Check for vertical collinearity
                else if (Mathf.Approximately(p0.x, p1.x) && Mathf.Approximately(p1.x, p2.x)) {
                    isCollinear = true;
                }
                // Optional: General collinearity check using cross product (if not perfectly grid aligned)
                // float crossProduct = (p1.y - p0.y) * (p2.x - p1.x) - (p1.x - p0.x) * (p2.y - p1.y);
                // if (Mathf.Approximately(crossProduct, 0f)) { isCollinear = true; }


                if (isCollinear) {
                    _reusableSimplifiedPath[_reusableSimplifiedPath.Count - 1] = p2; // Replace p1 with p2
                } else {
                    // Before adding p2, if it's the last original vertex and it's the same as the first simplified vertex, skip.
                    if (!(i == _reusableOrderedVertices.Count - 1 && p2 == _reusableSimplifiedPath[0])) {
                        _reusableSimplifiedPath.Add(p2);
                    }
                }
            }
            // Final check for closed loop in simplified path if original was trying to close
            if (_reusableSimplifiedPath.Count > 1 && _reusableSimplifiedPath[0] == _reusableSimplifiedPath[_reusableSimplifiedPath.Count - 1]) {
                _reusableSimplifiedPath.RemoveAt(_reusableSimplifiedPath.Count - 1);
            }
        }

        if (_reusableSimplifiedPath.Count < 3) {
            // Debug.LogWarning($"Simplified path from {_reusableOrderedVertices.Count} to {_reusableSimplifiedPath.Count} vertices, <3 result. Tiles: {regionTiles.Count}");
            return null;
        }

        // Return a new list copy as the reusable one will be cleared
        return new List<Vector2>(_reusableSimplifiedPath);
    }

    private List<Vector2> GenerateOutlineWithMooreNeighbor(
      List<Vector2Int> regionTiles,
      HashSet<Vector2Int> allNonSolidGlobalTiles) // allNonSolidGlobalTiles is less relevant here,
                                                  // we operate on the given regionTiles
  {
        _reusableOrderedVertices.Clear();
        _reusableSimplifiedPath.Clear();

        if (regionTiles == null || regionTiles.Count == 0)
            return null;

        HashSet<Vector2Int> currentRegionSet = new HashSet<Vector2Int>(regionTiles);

        Vector2Int pStart = Vector2Int.zero;
        Vector2Int sFromPStart = Vector2Int.zero; // The first non-region neighbor of pStart (used for stopping)
        bool foundStart = false;

        Vector2Int[] mooreNeighborsOffset = { // Clockwise starting East
        new Vector2Int(1, 0), new Vector2Int(1, 1), new Vector2Int(0, 1), new Vector2Int(-1, 1),
        new Vector2Int(-1, 0), new Vector2Int(-1, -1), new Vector2Int(0, -1), new Vector2Int(1, -1)
    };
        // To conform to the "left of S" rule for CCW trace:
        // If S is found at mooreNeighborsOffset[k], P_next is at mooreNeighborsOffset[(k-1 + 8)%8] relative to current P
        // But my current logic is: Find S at offset[k], P_next is offset[(k-1+8)%8] which is boundaryPixelPath.add(P_current + offset[(k-1+8)%8])
        // This assumes S is relative to P_current. The algorithm scans from P_current to find the next S.
        // The next P on the boundary is the pixel *before* S in the clockwise scan from P_current.

        // Find Start Pixel (P_start) and its initial S (sFromPStart)
        foreach (var tile in regionTiles) {
            for (int i = 0; i < mooreNeighborsOffset.Length; i++) {
                Vector2Int neighbor = tile + mooreNeighborsOffset[i];
                if (!currentRegionSet.Contains(neighbor)) // Found a non-region neighbor (S)
                {
                    pStart = tile;          // This is our boundary start pixel
                    sFromPStart = neighbor; // This is the S for pStart
                    foundStart = true;
                    break;
                }
            }
            if (foundStart)
                break;
        }

        if (!foundStart) {
            // Debug.LogWarning("Moore-Neighbor: Could not find a starting boundary pixel for a region.");
            return null;
        }

        List<Vector2Int> boundaryPixelPath = new List<Vector2Int>();
        Vector2Int pCurrent = pStart;
        Vector2Int sCurrent = sFromPStart; // The 'S' pixel that led us to pCurrent (or initial S for pStart)

        int safetyBreak = regionTiles.Count * 8 + 10; // A bit more generous
        int count = 0;

        do {
            boundaryPixelPath.Add(pCurrent);
            count++;
            if (count > safetyBreak) {
                Debug.LogError($"Moore-Neighbor Safety Break! Region Size: {regionTiles.Count}, Path Length: {boundaryPixelPath.Count}, pStart: {pStart}, pCurrent: {pCurrent}, sCurrent: {sCurrent}, sFromPStart: {sFromPStart}");
                // You can draw the partial path here for debugging
                // DrawPathGizmos(boundaryPixelPath, Color.red);
                return null; // Safety break triggered
            }

            // We are at pCurrent. We arrived from sCurrent (sCurrent is the non-region pixel).
            // We need to scan clockwise around pCurrent, starting from sCurrent's position + 1,
            // to find the *next* boundary pixel pNext.
            // The first region pixel encountered in this scan is pNext.
            // The non-region pixel *just before* pNext in the scan becomes the new sCurrent for pNext.

            int sIndexInScannedOrder = -1; // Index of sCurrent in mooreNeighborsOffset relative to pCurrent
            for (int i = 0; i < mooreNeighborsOffset.Length; ++i) {
                if (pCurrent + mooreNeighborsOffset[i] == sCurrent) {
                    sIndexInScannedOrder = i;
                    break;
                }
            }

            if (sIndexInScannedOrder == -1) {
                // This should not happen if sCurrent is indeed a neighbor of pCurrent
                Debug.LogError($"Moore-Neighbor: sCurrent {sCurrent} is not a Moore neighbor of pCurrent {pCurrent}.");
                return null;
            }

            Vector2Int pNext = Vector2Int.zero;
            Vector2Int sForPNext = Vector2Int.zero; // This will be the new sCurrent
            bool foundPNext = false;

            for (int i = 1; i <= mooreNeighborsOffset.Length; i++) // Scan all 8 spots
            {
                // Index of neighbor to check (clockwise from sCurrent)
                int checkNeighborIndex = (sIndexInScannedOrder + i) % mooreNeighborsOffset.Length;
                Vector2Int potentialPNext = pCurrent + mooreNeighborsOffset[checkNeighborIndex];

                if (currentRegionSet.Contains(potentialPNext)) // Found next boundary pixel
                {
                    pNext = potentialPNext;
                    // The sForPNext is the pixel *before* potentialPNext in the scan (relative to pCurrent)
                    int sForPNextIndex = (checkNeighborIndex - 1 + mooreNeighborsOffset.Length) % mooreNeighborsOffset.Length;
                    sForPNext = pCurrent + mooreNeighborsOffset[sForPNextIndex];
                    foundPNext = true;
                    break;
                }
            }

            if (!foundPNext) {
                // This implies pCurrent is isolated or an error.
                // If pCurrent is the *only* pixel in the region (e.g. a 1x1 region), and foundStart was true,
                // it means all its neighbors are non-region. It should still be able to make one step.
                // If boundaryPixelPath has only pStart, and no pNext is found, it's a problem.
                Debug.LogError($"Moore-Neighbor: Stuck at {pCurrent}! Could not find next P. Path: {boundaryPixelPath.Count}. RegionSize: {currentRegionSet.Count}");
                // DrawPathGizmos(boundaryPixelPath, Color.magenta);
                return null;
            }

            pCurrent = pNext;
            sCurrent = sForPNext;

            // Stopping condition: back to start pixel AND the s-pixel is the original s-pixel.
            // This is a common robust stopping condition.
        } while (pCurrent != pStart || sCurrent != sFromPStart);


        // --- Convert Boundary Pixel Path to Edge Vertices ---
        // (This logic might need adjustment based on how Moore path is generated)
        // The `AddVertexBasedOnTurn` and simplification remains complex and highly dependent
        // on the exact nature of `boundaryPixelPath`. If boundaryPixelPath truly represents
        // the sequence of "outermost" region pixels, the vertex extraction is key.

        _reusableOrderedVertices.Clear();
        if (boundaryPixelPath.Count < 1)
            return null;

        float tileSize = 1.0f;
        Vector2Int previousDirection = Vector2Int.zero;

        for (int i = 0; i < boundaryPixelPath.Count; i++) {
            Vector2Int currentBoundaryTile = boundaryPixelPath[i];
            Vector2Int nextBoundaryTileOnPath = boundaryPixelPath[(i + 1) % boundaryPixelPath.Count];

            Vector2Int currentGlobalDir = nextBoundaryTileOnPath - currentBoundaryTile;
            currentGlobalDir = NormalizeDir(currentGlobalDir); // Normalize to unit vector

            if (i == 0) { // First point needs special handling for its "previous direction"
                Vector2Int dirFromLastToFirst = currentBoundaryTile - boundaryPixelPath[boundaryPixelPath.Count - 1];
                previousDirection = NormalizeDir(dirFromLastToFirst);
                AddVertexBasedOnTurn(currentBoundaryTile, previousDirection, currentGlobalDir, tileSize, _reusableOrderedVertices);
            } else if (currentGlobalDir != previousDirection) {
                AddVertexBasedOnTurn(currentBoundaryTile, previousDirection, currentGlobalDir, tileSize, _reusableOrderedVertices);
            }
            previousDirection = currentGlobalDir;
        }

        SimplifyPath(_reusableOrderedVertices, _reusableSimplifiedPath, true);

        if (_reusableSimplifiedPath.Count < 3) {
            // Debug.LogWarning($"Moore: Simplified path < 3 vertices. Original pixel path count: {boundaryPixelPath.Count}");
            return null;
        }

        return new List<Vector2>(_reusableSimplifiedPath);
    }
    // Helper for Moore-Neighbor to add vertex based on turn
    // prevDirLocal: direction from previous boundary tile to current boundary tile
    // nextDirLocal: direction from current boundary tile to next boundary tile
    private void AddVertexBasedOnTurn(Vector2Int currentTilePos, Vector2Int prevDirLocalNormalized, Vector2Int nextDirLocalNormalized, float tileSize, List<Vector2> outputPath) {
        // Convert tilePos to bottom-left world coordinate
        float tileWorldX = currentTilePos.x * tileSize;
        float tileWorldY = currentTilePos.y * tileSize;

        // Define the 4 corners of the current tile in world space
        Vector2 bl = new Vector2(tileWorldX, tileWorldY);
        Vector2 br = new Vector2(tileWorldX + tileSize, tileWorldY);
        Vector2 tr = new Vector2(tileWorldX + tileSize, tileWorldY + tileSize);
        Vector2 tl = new Vector2(tileWorldX, tileWorldY + tileSize);

        // Normalize directions (already done but good practice)
        Vector2Int prevDir = NormalizeDir(prevDirLocalNormalized);
        Vector2Int nextDir = NormalizeDir(nextDirLocalNormalized);

        // This logic determines which corner of `currentTilePos` is the vertex.
        // It depends on the "turn" from prevDir to nextDir.
        // If moving East (1,0) then North (0,1), the turn is at the Top-Right corner of the tile.
        // If moving East (1,0) then South (0,-1), the turn is at the Bottom-Right corner.

        // Simplified logic for 4-directional movements (N,E,S,W)
        // This assumes input directions are cardinal. For diagonal Moore steps, this gets more complex.
        // For Moore-Neighbor, the boundaryPixelPath can have diagonal steps.
        // The vertex logic below works best if directions are cardinal relative to the grid.
        // The `currentGlobalDir` above can be diagonal. This vertex logic needs to account for that.

        // Let's consider the OUTSIDE corners of the tile for vertex generation.
        // Imagine standing on currentTilePos, facing nextDir.
        // The vertex is to your "left" (for CCW winding) or "right" (for CW winding)
        // depending on the type of fill. Freeform Light expects CCW for outer path.

        // Simplified Vertex Logic - THIS IS A CRITICAL PART AND CAN BE TRICKY
        // A common method is to look at the pair of (prevDir, nextDir)
        // (0,0) can mean first point.
        if (prevDir == Vector2Int.right && nextDir == Vector2Int.up)
            outputPath.Add(tr); // Turn left
        else if (prevDir == Vector2Int.right && nextDir == Vector2Int.down)
            outputPath.Add(br); // Turn right
        else if (prevDir == Vector2Int.up && nextDir == Vector2Int.left)
            outputPath.Add(tl); // Turn left
        else if (prevDir == Vector2Int.up && nextDir == Vector2Int.right)
            outputPath.Add(tr); // Turn right
        else if (prevDir == Vector2Int.left && nextDir == Vector2Int.down)
            outputPath.Add(bl); // Turn left
        else if (prevDir == Vector2Int.left && nextDir == Vector2Int.up)
            outputPath.Add(tl); // Turn right
        else if (prevDir == Vector2Int.down && nextDir == Vector2Int.right)
            outputPath.Add(br); // Turn left
        else if (prevDir == Vector2Int.down && nextDir == Vector2Int.left)
            outputPath.Add(bl); // Turn right
                                // Handle cases where there's no turn (straight line) - shouldn't add vertex if path already started
                                // but initial vertex for a straight segment needs one.
                                // For first point (prevDir might be 0,0 or derived from wrap-around):
        else if (prevDir == Vector2Int.zero) { // First point handling based on entry/exit
            if (nextDir == Vector2Int.right)
                outputPath.Add(bl); // Starting East, came from South (or West if it's a line)
            else if (nextDir == Vector2Int.up)
                outputPath.Add(br);   // Starting North, came from East
            else if (nextDir == Vector2Int.left)
                outputPath.Add(tr); // Starting West, came from North
            else if (nextDir == Vector2Int.down)
                outputPath.Add(tl); // Starting South, came from West
            else { /* Diagonal or unknown start */ AddDefaultCorner(currentTilePos, tileSize, outputPath); }
        }
        // Diagonal movements - if boundaryPixelPath has them, this vertex logic needs refinement
        // to pick the correct "outer" corner or even mid-points of edges.
        // For Light2D, strictly grid-aligned corners usually look best.
        else {
            // Fallback or more complex diagonal logic.
            // For simplicity, if it's a diagonal step, we might just continue along an axis
            // or pick a corner that "encloses" the diagonal.
            // This is where visual results will guide refinement.
            // A robust system often involves an explicit "winding" rule.
            // One common way: if nextDir is (1,1), prevDir was (1,0) -> TR corner. prevDir was (0,1) -> TR corner.
            AddDefaultCorner(currentTilePos, tileSize, outputPath); // Default for unhandled turns
                                                                    // Debug.LogWarning($"Unhandled turn: prev{prevDir} next{nextDir} at {currentTilePos}");
        }

        // Ensure no duplicate consecutive points
        if (outputPath.Count > 1 && outputPath[outputPath.Count - 1] == outputPath[outputPath.Count - 2]) {
            outputPath.RemoveAt(outputPath.Count - 1);
        }
    }

    // Normalize a direction vector to be cardinal or diagonal unit vector
    private Vector2Int NormalizeDir(Vector2Int dir) {
        Vector2Int nDir = Vector2Int.zero;
        if (dir.x != 0)
            nDir.x = (int)Mathf.Sign(dir.x);
        if (dir.y != 0)
            nDir.y = (int)Mathf.Sign(dir.y);
        return nDir;
    }

    private void AddDefaultCorner(Vector2Int tilePos, float tileSize, List<Vector2> outputPath) {
        // Default to bottom-left for simplicity, this is not robust
        outputPath.Add(new Vector2(tilePos.x * tileSize, tilePos.y * tileSize));
    }
    // Path Simplification (reusable)
    // Pass reusableSimplifiedPath list by reference
    private void SimplifyPath(List<Vector2> originalPath, List<Vector2> simplifiedPathOutput, bool isClosedPath) {
        simplifiedPathOutput.Clear();
        if (originalPath == null || originalPath.Count < 2) {
            if (originalPath != null)
                simplifiedPathOutput.AddRange(originalPath);
            return;
        }

        simplifiedPathOutput.Add(originalPath[0]);

        for (int i = 1; i < originalPath.Count; i++) {
            if (simplifiedPathOutput.Count < 2) { // Need at least two points in simplified to check collinearity
                if (originalPath[i] != simplifiedPathOutput[simplifiedPathOutput.Count - 1]) // Avoid duplicate if first two were same
                    simplifiedPathOutput.Add(originalPath[i]);
                continue;
            }

            Vector2 p0 = simplifiedPathOutput[simplifiedPathOutput.Count - 2];
            Vector2 p1 = simplifiedPathOutput[simplifiedPathOutput.Count - 1];
            Vector2 p2 = originalPath[i];

            bool isCollinear = false;
            // Check for horizontal collinearity
            if (Mathf.Approximately(p0.y, p1.y) && Mathf.Approximately(p1.y, p2.y)) {
                isCollinear = true;
            }
            // Check for vertical collinearity
            else if (Mathf.Approximately(p0.x, p1.x) && Mathf.Approximately(p1.x, p2.x)) {
                isCollinear = true;
            }
            // General collinearity (optional, for non-grid aligned paths if they occur)
            // float crossProduct = (p1.y - p0.y) * (p2.x - p1.x) - (p1.x - p0.x) * (p2.y - p1.y);
            // if (Mathf.Approximately(crossProduct, 0f)) { isCollinear = true; }

            if (isCollinear) {
                simplifiedPathOutput[simplifiedPathOutput.Count - 1] = p2; // Replace p1 with p2
            } else {
                simplifiedPathOutput.Add(p2);
            }
        }

        // For closed paths, check collinearity of last segment with first segment
        if (isClosedPath && simplifiedPathOutput.Count >= 3) {
            Vector2 pLast = simplifiedPathOutput[simplifiedPathOutput.Count - 1];
            Vector2 pFirst = simplifiedPathOutput[0];
            Vector2 pSecond = simplifiedPathOutput[1];

            // Check if pLast, pFirst, pSecond are collinear
            bool lastCollinear = false;
            if (Mathf.Approximately(pLast.y, pFirst.y) && Mathf.Approximately(pFirst.y, pSecond.y))
                lastCollinear = true;
            else if (Mathf.Approximately(pLast.x, pFirst.x) && Mathf.Approximately(pFirst.x, pSecond.x))
                lastCollinear = true;

            if (lastCollinear) {
                simplifiedPathOutput.RemoveAt(0); // Remove pFirst, pLast effectively connects to pSecond
            }

            // Ensure the path isn't closed by having the last point same as first (SetShapePath handles closure)
            if (simplifiedPathOutput.Count > 1 && simplifiedPathOutput[0] == simplifiedPathOutput[simplifiedPathOutput.Count - 1]) {
                simplifiedPathOutput.RemoveAt(simplifiedPathOutput.Count - 1);
            }
        }
    }


    private void AddEdgeIfUnique(List<Tuple<Vector2, Vector2>> edges, Vector2 p1, Vector2 p2) {
        // Ensure consistent order for uniqueness check (e.g., smaller X first, then smaller Y)
        Tuple<Vector2, Vector2> edge;
        if (p1.x < p2.x || (p1.x == p2.x && p1.y < p2.y)) {
            edge = Tuple.Create(p1, p2);
        } else {
            edge = Tuple.Create(p2, p1);
        }

        if (!edges.Contains(edge)) // This Contains check on List<Tuple> can be slow for many edges.
                                   // A HashSet<Tuple<Vector2,Vector2>> would be faster for AddEdgeIfUnique part.
        {
            edges.Add(Tuple.Create(p1, p2)); // Add original order for potential tracing
        }
    }

    private List<List<Vector2>> GenerateRegionPolygons(List<Vector2Int> regionTiles, HashSet<Vector2Int> allNonSolidGlobalTiles, float tileSize) {
        if (regionTiles == null || regionTiles.Count == 0)
            return new List<List<Vector2>>();

        HashSet<Vector2Int> regionSet = new HashSet<Vector2Int>(regionTiles);
        HashSet<EdgeKey> boundaryEdges = new HashSet<EdgeKey>();

        // --- 1. Collect Boundary Edges ---
        foreach (var tilePos in regionTiles) {
            float worldX = tilePos.x * tileSize;
            float worldY = tilePos.y * tileSize;

            // Tile corners (bottom-left, bottom-right, top-right, top-left)
            Vector2 v0 = new Vector2(worldX, worldY);
            Vector2 v1 = new Vector2(worldX + tileSize, worldY);
            Vector2 v2 = new Vector2(worldX + tileSize, worldY + tileSize);
            Vector2 v3 = new Vector2(worldX, worldY + tileSize);

            // Check neighbors. If neighbor is NOT in regionSet, edge is a boundary.
            if (!regionSet.Contains(tilePos + Vector2Int.down))
                boundaryEdges.Add(new EdgeKey(v0, v1));   // Bottom
            if (!regionSet.Contains(tilePos + Vector2Int.right))
                boundaryEdges.Add(new EdgeKey(v1, v2));  // Right
            if (!regionSet.Contains(tilePos + Vector2Int.up))
                boundaryEdges.Add(new EdgeKey(v2, v3));     // Top
            if (!regionSet.Contains(tilePos + Vector2Int.left))
                boundaryEdges.Add(new EdgeKey(v3, v0));   // Left
        }

        if (boundaryEdges.Count == 0)
            return new List<List<Vector2>>();

        // --- 2. Stitch Edges into Polygons ---
        // This is the complex part. The implementation below is a conceptual guide
        // and would need careful implementation of angular sorting and path traversal.
        // For a robust solution, consider looking up "polygonization of a planar graph".

        var adjList = new Dictionary<Vector2, List<Vector2>>();
        foreach (var edge in boundaryEdges) {
            if (!adjList.ContainsKey(edge.P1))
                adjList[edge.P1] = new List<Vector2>();
            if (!adjList.ContainsKey(edge.P2))
                adjList[edge.P2] = new List<Vector2>();
            adjList[edge.P1].Add(edge.P2);
            adjList[edge.P2].Add(edge.P1);
        }

        // Sort adjacent vertices by angle
        foreach (var vertexKey in adjList.Keys.ToList()) // ToList() to copy keys if modifying dict
        {
            Vector2 center = vertexKey;
            adjList[center].Sort((p1, p2) => {
                float angle1 = Mathf.Atan2(p1.y - center.y, p1.x - center.x);
                float angle2 = Mathf.Atan2(p2.y - center.y, p2.x - center.x);
                return angle1.CompareTo(angle2);
            });
        }

        List<List<Vector2>> rawPolygons = new List<List<Vector2>>();
        HashSet<EdgeKey> usedEdges = new HashSet<EdgeKey>();

        foreach (var startEdge in boundaryEdges) {
            if (usedEdges.Contains(startEdge))
                continue;

            List<Vector2> currentPath = new List<Vector2>();
            Vector2 currentVertex = startEdge.P1;
            Vector2 nextVertex = startEdge.P2;

            // Try to trace a path starting with startEdge.p1 -> startEdge.p2
            // This assumes startEdge.p1 is where we begin the actual path list
            currentPath.Add(currentVertex);

            int safetyBreak = 0; // Prevent infinite loops in complex cases

            while (safetyBreak++ < boundaryEdges.Count * 2) // Max possible path length
            {
                if (usedEdges.Contains(new EdgeKey(currentVertex, nextVertex))) {
                    // This edge has been used, but we might be closing a loop with it.
                    // If nextVertex is the start of the path, we are closing.
                    // Otherwise, this might be a multi-use edge in a complex non-simple polygon or an error.
                    if (nextVertex == currentPath[0] && currentPath.Count >= 2) { // Allow path of 2 edges + closing to start
                                                                                  // currentPath.Add(nextVertex); // Path is closed. Do not add start again for SetShapePath
                        usedEdges.Add(new EdgeKey(currentVertex, nextVertex)); // Mark this closing segment as used too
                        break; // Path closed
                    } else {
                        // This is a problematic scenario for simple polygon tracing.
                        // It could mean the edge is already part of another polygon, or it's an articulation point.
                        // For simplicity, we might break here, potentially leaving some polygons unfound or incomplete.
                        // A more robust algorithm would handle this (e.g. by trying different traversal orders at junctions).
                        Debug.LogWarning($"Edge {currentVertex}-{nextVertex} already used mid-path or invalid junction. Path tracing stopped.");
                        currentPath = null; // Invalidate this path
                        break;
                    }
                }

                currentPath.Add(nextVertex);
                usedEdges.Add(new EdgeKey(currentVertex, nextVertex));

                Vector2 prevVertex = currentVertex;
                currentVertex = nextVertex;

                if (currentVertex == currentPath[0]) { // Path closed (already handled by above check potentially, but good failsafe)
                    break;
                }

                var neighbors = adjList[currentVertex];
                if (neighbors.Count < 1) { // Should be at least one (the one we came from)
                    Debug.LogError("Vertex with no neighbors found mid-path.");
                    currentPath = null;
                    break;
                }

                // Find prevVertex in the sorted list of neighbors to determine turning direction
                int incomingIndex = -1;
                for (int i = 0; i < neighbors.Count; ++i) {
                    if (neighbors[i] == prevVertex) {
                        incomingIndex = i;
                        break;
                    }
                }

                if (incomingIndex == -1 && neighbors.Count > 0 && currentPath.Count > 2) { // Just started path of length 2, no real "incoming" yet from prevVertex in neighbors
                                                                                           // This case can happen if a vertex only has two edges (straight line segment as part of a larger path)
                                                                                           // and prevVertex was the first point. Pick the one that is not prevVertex.
                    if (neighbors.Count == 1)
                        nextVertex = neighbors[0]; // Should be the one we came from if error
                    else if (neighbors.Count == 2)
                        nextVertex = (neighbors[0] == prevVertex) ? neighbors[1] : neighbors[0];
                    else { /* More complex junction, the rule below should handle it */ }
                    // If incomingIndex is -1 it means something is odd or it's the very start
                    // For tracing polygons, we usually assume degree 2 vertices or a clear turning rule
                }

                if (neighbors.Count == 1 && neighbors[0] == prevVertex && currentPath[0] != currentVertex) {
                    // This is a dead end if not closing the polygon
                    Debug.LogWarning("Dead end encountered in path tracing.");
                    currentPath = null;
                    break;
                }


                // Select next vertex: "turn left" (for CCW outer paths) relative to edge (prevVertex -> currentVertex)
                // In an angularly sorted list, this means taking the *next* vertex after prevVertex in the list (circularly).
                // To keep "solid on right" / "air on left" when traversing CCW
                // This "turn left" logic might need to be "turn right" depending on how angles were sorted
                // and initial edge direction relative to desired winding.

                bool foundNextEdge = false;
                for (int i = 1; i <= neighbors.Count; i++) // Iterate through potential turns
                {
                    int nextNeighborIndex = (incomingIndex + i) % neighbors.Count; // "Turn left" if sorted CW, or "turn right" if sorted CCW
                                                                                   // This needs to be robust!
                                                                                   // Assuming CW sort of Atan2: incomingIndex+1 is "left most" from incoming.

                    Vector2 potentialNext = neighbors[nextNeighborIndex];
                    if (!usedEdges.Contains(new EdgeKey(currentVertex, potentialNext)) || (potentialNext == currentPath[0] && currentPath.Count >= 2)) {
                        nextVertex = potentialNext;
                        foundNextEdge = true;
                        break;
                    }
                }

                if (!foundNextEdge) {
                    // Fallback or error: If all connecting edges are used and we haven't closed the loop
                    // This suggests an issue with the graph or the tracing logic for complex cases.
                    // Could try the "most CCW unused edge" that isn't the one we arrived on.
                    Debug.LogWarning($"Path tracing stuck at {currentVertex}. Could not find valid next untraversed edge.");
                    currentPath = null; // Invalidate path
                    break;
                }
            } // End while(true) for path tracing

            if (currentPath != null && currentPath.Count >= 3) {
                // Check if the path actually closed to the start. If loop broke early, it might not have.
                if (currentPath[currentPath.Count - 1] == currentPath[0]) {
                    currentPath.RemoveAt(currentPath.Count - 1); // SetShapePath doesn't want duplicate point
                } else if (currentPath[0] != nextVertex && nextVertex == currentPath[0]) {
                    // If the loop broke due to used edge *and* nextVertex was the start
                    // The currentPath doesn't include the last nextVertex yet
                } else {
                    // Path didn't close properly
                    // For now, we'll be lenient and try to use it if it has enough points,
                    // but ideally, non-closed paths from this stage are errors.
                    // However, if the LAST nextVertex identified was the start point, it IS closed.
                    if (nextVertex != currentPath[0]) {
                        Debug.LogWarning($"Path starting {currentPath[0]} did not close properly. Last point: {currentPath[currentPath.Count - 1]}, Next vertex to close was: {nextVertex}");
                        // continue; // Skip adding this unclosed path
                    }
                }

                if (currentPath.Count >= 3)
                    rawPolygons.Add(currentPath);
            }
        } // End foreach starting edge


        // --- 3. Classify, Order, and Ensure Winding for Polygons ---
        List<List<Vector2>> finalPolygons = new List<List<Vector2>>();
        if (rawPolygons.Count == 0)
            return finalPolygons;

        // Calculate signed areas to determine winding and size
        List<Tuple<List<Vector2>, float>> polyAreas = new List<Tuple<List<Vector2>, float>>();
        foreach (var poly in rawPolygons) {
            polyAreas.Add(Tuple.Create(poly, CalculateSignedArea(poly)));
        }

        // Sort by area (descending absolute value to get largest ones first, then check winding)
        // For SetShapePath: outer CCW (positive area), inner CW (negative area)
        polyAreas.Sort((a, b) => Mathf.Abs(b.Item2).CompareTo(Mathf.Abs(a.Item2)));

        bool outerPathFound = false;
        foreach (var pa in polyAreas) {
            List<Vector2> currentPoly = pa.Item1;
            float area = pa.Item2;

            if (!outerPathFound) // First (largest area) polygon is assumed outer
            {
                if (area < 0)
                    currentPoly.Reverse(); // Ensure outer is CCW
                finalPolygons.Add(currentPoly);
                outerPathFound = true;
            } else // Subsequent polygons are holes
              {
                if (area > 0)
                    currentPoly.Reverse(); // Ensure holes are CW
                finalPolygons.Add(currentPoly);
            }
        }
        return finalPolygons;
    }


    private float CalculateSignedArea(List<Vector2> polygon) {
        if (polygon == null || polygon.Count < 3)
            return 0;
        float area = 0;
        for (int i = 0; i < polygon.Count; i++) {
            Vector2 p1 = polygon[i];
            Vector2 p2 = polygon[(i + 1) % polygon.Count]; // Loop back to first point
            area += (p1.x * p2.y - p2.x * p1.y);
        }
        return area / 2.0f;
    }
}
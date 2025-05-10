using UnityEngine;
using UnityEngine.Rendering.Universal; 
using System.Collections.Generic;
using System.Linq;
using System; 

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
    public static WorldLightingManager Instance { get; private set; }
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

    // --- Methods ---
    void Awake() {
        if (Instance == null) {
            Instance = this;
            InitializeBiomeSettings();
        } else {
            Destroy(gameObject);
        }
    }

    void InitializeBiomeSettings() {
        _biomeLightSettingsMap = new Dictionary<BiomeType, LightProperties>();
        foreach (var config in biomeLightConfigs) {
            if (!_biomeLightSettingsMap.ContainsKey(config.biome)) {
                _biomeLightSettingsMap.Add(config.biome, config.properties);
            }
        }
    }

    public void RequestLightUpdate() {
        Debug.Log("Updating lights!");
        _needsUpdate = true;
    }

    void Update() {
        if (_needsUpdate && Time.time > _lastUpdateTime + updateCooldown) {
            PerformFullLightUpdate();
            _needsUpdate = false;
            _lastUpdateTime = Time.time;
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

}
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class GridManager : MonoBehaviour {
    [Header("Random manager stuff")]
    public Transform player; 
    [SerializeField] private Slider progressBar;
    [Header("Grid Settings")]

    public int gridWidth = 20;
    public int gridHeight = 50; // Increased height for vertical trench
    public float tileSize = 1f;
    public GameObject tilePrefab;

    [Header("Trench Settings")]
    public int trenchWidthTop = 7;      // Width of trench at the surface (top)
    public int trenchWidthBottom = 3;   // Width of trench at the bottom
    public int trenchDepthTrapezoid = 30; // Depth over which trench narrows to bottom width
    public float trenchEdgeNoiseScale = 3f; // Scale for noise on trench edges
    public float trenchEdgeNoiseIntensity = 2f; // Intensity of noise offset for edges

    [Header("Noise Settings - General")]
    public float noiseScale = 5f; // Adjust for noise detail
    public float noiseOffset_X = 0f; // For different noise patterns each run
    public float noiseOffset_Y = 0f;

    [Header("Ore Settings - Copper")]
    [Range(0f, 1f)] public float copperOreFrequencySurface = 0.1f; // Frequency near surface
    [Range(0f, 1f)] public float copperOreFrequencyDeep = 0.05f;   // Frequency deeper down
    public float copperOreDepthStart = 10f; // Depth where copper frequency starts decreasing
    public float copperOreDepthEnd = 30f;   // Depth where copper frequency reaches deep value

    [Header("Ore Settings - Silver")]
    [Range(0f, 1f)] public float silverOreFrequencySurface = 0.02f;
    [Range(0f, 1f)] public float silverOreFrequencyDeep = 0.08f;
    public float silverOreDepthStart = 20f;
    public float silverOreDepthEnd = 40f;


    private Tile[,] grid;
    private Vector2 gridOrigin = Vector2.zero;

    void Start() {
        // Generate random noise offsets for each game run
        noiseOffset_X = Random.value * 1000f;
        noiseOffset_Y = Random.value * 1000f;
        StartCoroutine(CreateGridRoutine());
    }

    void CreateGrid() {
        grid = new Tile[gridWidth, gridHeight];

        for (int x = 0; x < gridWidth; x++) {
            for (int y = 0; y < gridHeight; y++) {
                Vector3 worldPosition = new Vector3(x * tileSize + gridOrigin.x, y * -tileSize + gridOrigin.y, 0);
                GameObject tileObject = Instantiate(tilePrefab, worldPosition, Quaternion.identity, this.transform);
                Tile tile = tileObject.GetComponent<Tile>();
                if (tile != null) {
                    Tile.TileType tileType = DetermineTileType(x, y);
                    tile.InitializeTile(tileType, new Vector2Int(x, y));
                    grid[x, y] = tile;
                } else {
                    Debug.LogError("Tile Prefab is missing the Tile script!");
                }
            }
        }
    }

    IEnumerator CreateGridRoutine() {
        grid = new Tile[gridWidth, gridHeight];
        int totalTiles = gridWidth * gridHeight;
        int tilesProcessed = 0;

        for (int x = 0; x < gridWidth; x++) {
            for (int y = 0; y < gridHeight; y++) {
                Vector3 worldPosition = new Vector3(x * tileSize + gridOrigin.x, y * -tileSize + gridOrigin.y, 0);
                GameObject tileObject = Instantiate(tilePrefab, worldPosition, Quaternion.identity, this.transform);
                Tile tile = tileObject.GetComponent<Tile>();
                if (tile != null) {
                    Tile.TileType tileType = DetermineTileType(x, y);
                    tile.InitializeTile(tileType, new Vector2Int(x, y));
                    grid[x, y] = tile;
                } else {
                    Debug.LogError("Tile Prefab is missing the Tile script!");
                }

                tilesProcessed++;
                if (progressBar != null) {
                    progressBar.value = (float)tilesProcessed / totalTiles; // Update progress bar
                }

                if (tilesProcessed % 100 == 0) // Reduce frame stutter
                    yield return null;
            }
        } // Place the player at the bottom center
        if (player != null) {
            int centerX = gridWidth / 2;
            Vector3 playerStartPos = new Vector3(centerX * tileSize + gridOrigin.x, (gridHeight - 1) * -tileSize + gridOrigin.y, 0);
            player.position = playerStartPos;
        }
        // Hide progress bar after completion
        if (progressBar != null)
            progressBar.gameObject.SetActive(false);
    }

Tile.TileType DetermineTileType(int x, int y)
    {
        // 1. Calculate Trapezoidal Trench Width based on depth (y)
        float normalizedDepth = Mathf.Clamp01((float)y / trenchDepthTrapezoid); // 0 at top, 1 at trenchDepthTrapezoid
        float currentTrenchWidth = Mathf.Lerp(trenchWidthTop, trenchWidthBottom, normalizedDepth);
        float trenchCenterX = gridWidth * 0.5f; // Center of the grid horizontally

        // 2. Apply Noise to Trench Edges
        float leftEdgeNoise = Mathf.PerlinNoise((y * trenchEdgeNoiseScale) + noiseOffset_Y, noiseOffset_X) - 0.5f; // -0.5 to center noise around 0
        float rightEdgeNoise = Mathf.PerlinNoise((y * trenchEdgeNoiseScale) + noiseOffset_Y + 100f, noiseOffset_X) - 0.5f; // Offset for different noise

        float noisyTrenchStartX = trenchCenterX - (currentTrenchWidth * 0.5f) + (leftEdgeNoise * trenchEdgeNoiseIntensity);
        float noisyTrenchEndX = trenchCenterX + (currentTrenchWidth * 0.5f) + (rightEdgeNoise * trenchEdgeNoiseIntensity);

        // Convert noisy edges to integer grid coordinates (clamp to grid boundaries)
        int trenchStartX = Mathf.Clamp(Mathf.RoundToInt(noisyTrenchStartX), 0, gridWidth);
        int trenchEndX = Mathf.Clamp(Mathf.RoundToInt(noisyTrenchEndX), 0, gridWidth);


        // 3. Determine Tile Type based on Trench and Ore Noise
        bool isInTrench = (x >= trenchStartX && x < trenchEndX);

        if (isInTrench)
        {
            return Tile.TileType.Empty; // Trench area is always empty
        }
        else
        {
            // Outside the trench - Stone with potential Ore replacement
            float noiseValue = Mathf.PerlinNoise((x + noiseOffset_X) / noiseScale, (y + noiseOffset_Y) / noiseScale);

            float copperFrequency = CalculateOreFrequency(y, copperOreFrequencySurface, copperOreFrequencyDeep, copperOreDepthStart, copperOreDepthEnd);
            float silverFrequency = CalculateOreFrequency(y, silverOreFrequencySurface, silverOreFrequencyDeep, silverOreDepthStart, silverOreDepthEnd);

            if (noiseValue < copperFrequency)
            {
                return Tile.TileType.Ore_Copper;
            }
            if (noiseValue < silverFrequency)
            {
                return Tile.TileType.Ore_Silver;
            }

            return Tile.TileType.Stone;
        }
    }

    // Helper function to calculate ore frequency based on depth (y-coordinate)
    float CalculateOreFrequency(int y, float surfaceFrequency, float deepFrequency, float depthStart, float depthEnd) {
        if (y < depthStart) {
            return surfaceFrequency; // Surface frequency above depthStart
        } else if (y >= depthEnd) {
            return deepFrequency;    // Deep frequency below depthEnd
        } else // Interpolate frequency between depthStart and depthEnd
          {
            float t = Mathf.InverseLerp(depthStart, depthEnd, y); // 0 at depthStart, 1 at depthEnd
            return Mathf.Lerp(surfaceFrequency, deepFrequency, t);
        }
    }


    public void DestroyTileAt(int x, int y) {
        if (x >= 0 && x < gridWidth && y >= 0 && y < gridHeight) {
            if (grid[x, y] != null) {
                grid[x, y].DestroyTile();
            }
        } else {
            Debug.LogWarning("Invalid grid coordinates for tile destruction: " + x + ", " + y);
        }
    }

    public Tile GetTileAt(int x, int y) {
        if (x >= 0 && x < gridWidth && y >= 0 && y < gridHeight) {
            return grid[x, y];
        }
        return null;
    }

    public Vector2Int GetGridPositionFromWorldPosition(Vector3 worldPosition) {
        int x = Mathf.FloorToInt((worldPosition.x - gridOrigin.x) / tileSize);
        int y = Mathf.FloorToInt((gridOrigin.y - worldPosition.y) / tileSize);
        return new Vector2Int(x, y);
    }
}
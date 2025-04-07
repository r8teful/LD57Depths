using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using static Tile;

public class GridManager : StaticInstance<GridManager> {
    [Header("Random manager stuff")]
    public Transform player; 
    [SerializeField] private Slider progressBar;
    [SerializeField] private GameObject sub;
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
    [Header("Trench Padding")]
    public int trenchPaddingTop = 0;     // Stone layers above the trench entrance
    public int trenchPaddingBottom = 5;  // Stone layers at the bottom of the trench and grid
    public int trenchPaddingSides = 2;   // Stone layers on each side of the trench

    [Header("Noise Settings - General")]
    public float silverNoiseScale = 10f; // Adjust for noise detail
    public float goldNoiseScale = 7f; // Adjust for noise detail
    public float rubyNoiseScale = 2f; // Adjust for noise detail
    public float diamondNoiseScale = 1f; // Adjust for noise detail
    public float noiseOffset_X = 0f; // For different noise patterns each run
    public float noiseOffset_Y = 0f;

    [Header("Ore Settings - Silver")]
    [Range(0f, 0.4f)] public float silverOreFrequencySurface = 0.02f;
    [Range(0f, 0.4f)] public float silverOreFrequencyDeep = 0.08f;
    [Range(0f, 1f)] public float silverDepthStart;
    [Range(0f, 1f)] public float silverDepthEnd;
    
    [Header("Ore Settings - Gold")]
    [Range(0f, 0.4f)] public float GoldFrequencySurface = 0.1f; // Frequency near surface
    [Range(0f, 0.4f)] public float GoldFrequencyDeep = 0.05f;   // Frequency deeper down
    [Range(0f, 1f)] public float GoldDepthStart; 
    [Range(0f, 1f)] public float GoldDepthEnd;   

    [Header("Ore Settings - Ruby")]
    [Range(0f, 0.4f)] public float RubyFrequencySurface = 0.1f; // Frequency near surface
    [Range(0f, 0.4f)] public float RubyFrequencyDeep = 0.05f;   // Frequency deeper down
    [Range(0f, 1f)] public float RubyDepthStart;
    [Range(0f, 1f)] public float RubyDepthEnd;   

    [Header("Ore Settings - Diamond")]
    [Range(0f, 0.4f)] public float DiamondFrequencySurface = 0.1f; // Frequency near surface
    [Range(0f, 0.4f)] public float DiamondFrequencyDeep = 0.05f;   // Frequency deeper down
    [Range(0f, 1f)] public float DiamondDepthStart ; 
    [Range(0f, 1f)] public float DiamondDepthEnd;




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
            Vector3 playerStartPos = new Vector3(centerX * tileSize + gridOrigin.x, (gridHeight  - trenchPaddingBottom - 1) * -tileSize + gridOrigin.y, 0);
            player.position = playerStartPos;
            playerStartPos.y += 1; // submarine above player
            Instantiate(sub, playerStartPos, Quaternion.identity);
        }
        // Hide progress bar after completion
        if (progressBar != null)
            progressBar.gameObject.SetActive(false);
    }

    Tile.TileType DetermineTileType(int x, int y) {
        // Check for Boundary Tiles
        if (x < 2 || x >= gridWidth - 2 // Sides 
            || y >= gridHeight - 2) { // Bottom
            return Tile.TileType.Boundary; // Create boundary tile
        }
        // Apply Padding Layers (Stone)
        // Top Padding
        if (y < trenchPaddingTop) {
            return Tile.TileType.Ore_Stone;
        }

        // Bottom Padding
        if (y >= gridHeight - trenchPaddingBottom) {
            return Tile.TileType.Ore_Stone;
        }

        // Side Padding
        int paddedGridWidth = gridWidth - (trenchPaddingSides * 2); // Effective grid width after side padding
        int sidePaddingStartX = trenchPaddingSides;                // Starting X for non-padded area
        if (x < sidePaddingStartX || x >= sidePaddingStartX + paddedGridWidth) {
            return Tile.TileType.Ore_Stone;
        }


        // Calculate Trapezoidal Trench Shape (within the non-padded area)

        float normalizedDepth = Mathf.Clamp01((float)(y - trenchPaddingTop) / trenchDepthTrapezoid); // Depth adjusted for top padding
        float currentTrenchWidth = Mathf.Lerp(trenchWidthTop, trenchWidthBottom, normalizedDepth);
        float trenchCenterX = gridWidth * 0.5f;

        float leftEdgeNoise = Mathf.PerlinNoise((y * trenchEdgeNoiseScale) + noiseOffset_Y, noiseOffset_X) - 0.5f;
        float rightEdgeNoise = Mathf.PerlinNoise((y * trenchEdgeNoiseScale) + noiseOffset_Y + 100f, noiseOffset_X) - 0.5f;

        float noisyTrenchStartX = trenchCenterX - (currentTrenchWidth * 0.5f) + (leftEdgeNoise * trenchEdgeNoiseIntensity);
        float noisyTrenchEndX = trenchCenterX + (currentTrenchWidth * 0.5f) + (rightEdgeNoise * trenchEdgeNoiseIntensity);

        int trenchStartX = Mathf.Clamp(Mathf.RoundToInt(noisyTrenchStartX), sidePaddingStartX, sidePaddingStartX + paddedGridWidth); // Clamped to padded width
        int trenchEndX = Mathf.Clamp(Mathf.RoundToInt(noisyTrenchEndX), sidePaddingStartX, sidePaddingStartX + paddedGridWidth); // Clamped to padded width


        // Determine Tile Type based on Trench and Ore Noise (within the trench area)

        bool isInTrench = (x >= trenchStartX && x < trenchEndX);

        if (isInTrench) {
            return Tile.TileType.Empty; // Trench area is still empty
        } else {
            // Generate separate noise value for each ore
            // Generate separate noise value for each ore, using its specific noiseScale
            float diamondNoiseValue = Mathf.PerlinNoise((x + noiseOffset_X) / diamondNoiseScale, (y + noiseOffset_Y) / diamondNoiseScale);
            float rubyNoiseValue = Mathf.PerlinNoise((x + noiseOffset_X + 100) / rubyNoiseScale, (y + noiseOffset_Y + 100) / rubyNoiseScale);
            float goldNoiseValue = Mathf.PerlinNoise((x + noiseOffset_X + 200) / goldNoiseScale, (y + noiseOffset_Y + 200) / goldNoiseScale);
            float silverNoiseValue = Mathf.PerlinNoise((x + noiseOffset_X + 300) / silverNoiseScale, (y + noiseOffset_Y) / silverNoiseScale);


            float diamondFrequency = CalculateOreFrequency(x, y, DiamondFrequencySurface, DiamondFrequencyDeep, DiamondDepthStart, DiamondDepthEnd);
            float rubyFrequency = CalculateOreFrequency(x, y, RubyFrequencySurface, RubyFrequencyDeep, RubyDepthStart, RubyDepthEnd);
            float goldFrequency = CalculateOreFrequency(x, y, GoldFrequencySurface, GoldFrequencyDeep, GoldDepthStart, GoldDepthEnd);
            float silverFrequency = CalculateOreFrequency(x, y, silverOreFrequencySurface, silverOreFrequencyDeep, silverDepthStart, silverDepthEnd);


            // Check for ores in order of rarity (most rare to least rare)
            if (diamondNoiseValue < diamondFrequency) {
                return TileType.Ore_Diamond;
            }
            if (rubyNoiseValue < rubyFrequency) {
                return TileType.Ore_Ruby;
            }
            if (goldNoiseValue < goldFrequency) {
                return TileType.Ore_Gold;
            }
            if (silverNoiseValue < silverFrequency) {
                return TileType.Ore_Silver;
            }

            return TileType.Ore_Stone; // Default to Stone
        }
    }

    float CalculateOreFrequency(int x, int y, float surfaceFrequency, float deepFrequency,
                               float depthStartPercent, float depthEndPercent) {
        // ... (CalculateOreFrequency function remains the same) ...
        // ... (The logic for depth and x-multiplier is unchanged and still good) ...
        // ... (You don't need to modify this function for this change) ...
        float depthStart = gridHeight * depthStartPercent;
        float depthEnd = gridHeight * depthEndPercent;

        float baseFrequency;
        if (y < depthStart) {
            baseFrequency = surfaceFrequency;
        } else if (y >= depthEnd) {
            baseFrequency = deepFrequency;
        } else {
            float t = Mathf.InverseLerp(depthStart, depthEnd, y);
            baseFrequency = Mathf.Lerp(surfaceFrequency, deepFrequency, t);
        }

        float centerX = gridWidth / 2.0f;
        float maxDistance = gridWidth / 2.0f;
        float xDistance = Mathf.Abs(x - centerX);
        float xNormalizedDistance = xDistance / maxDistance;
        float xMultiplier = Mathf.Lerp(1.0f, 1.0f + (xNormalizedDistance * xNormalizedDistance), 1f);


        return baseFrequency * xMultiplier;
    }
public void DamageTileAtGridPosition(Vector2Int gridPosition, float damage) {
        if (gridPosition.x >= 0 && gridPosition.x < gridWidth && gridPosition.y >= 0 && gridPosition.y < gridHeight) {
            if (grid[gridPosition.x, gridPosition.y] != null) {
                grid[gridPosition.x, gridPosition.y].TakeDamage(damage);
            }
        } else {
            Debug.LogWarning("Invalid grid coordinates for tile damage: " + gridPosition);
        }
    }
    public float GetWorldHeightFromRatio(float ratio) {
        return gridHeight * ratio * tileSize;
    }
    /*
    float CalculateOreFrequency(int x, int y, float surfaceFrequency, float deepFrequency,
                             float depthStartPercent, float depthEndPercent) {
        // Convert percentage depths to actual y-values
        float depthStart = gridHeight* depthStartPercent;
        float depthEnd = gridHeight * depthEndPercent;

        // Calculate the base frequency based on depth
        float baseFrequency;
        if (y < depthStart) {
            baseFrequency = surfaceFrequency; // If above depthStart, use surface frequency
        } else if (y >= depthEnd) {
            baseFrequency = deepFrequency;    // If below depthEnd, use deep frequency
        } else {
            float t = Mathf.InverseLerp(depthStart, depthEnd, y);
            baseFrequency = Mathf.Lerp(surfaceFrequency, deepFrequency, t);
        }

        // Dynamically determine centerX and maxDistance
        float centerX = gridWidth / 2.0f;  // Middle of the world
        float maxDistance = gridWidth / 2.0f;  // Maximum possible distance from center

        // Calculate x-based multiplier: higher frequency further from centerX
        float xDistance = Mathf.Abs(x - centerX);  // Distance from the center
        float xMultiplier = Mathf.Lerp(1.0f, 2.0f, xDistance / maxDistance);
        // Multiplier smoothly increases the further we get from center

        return baseFrequency * xMultiplier;
    }
    */
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
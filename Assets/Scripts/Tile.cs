using UnityEngine;

public class Tile : MonoBehaviour {
    public enum TileType {
        Empty, // New Empty Tile Type
        Dirt,
        Stone,
        Ore_Copper,
        Ore_Silver,
        // Add more tile types as needed
    }

    private TileType type;
    public Vector2Int gridPosition;
    private BoxCollider2D _boxCollider;
    public float maxHealth; // Maximum health of the tile
    private float currentHealth; // Current health of the tile
    public Resource resourcePrefab;
    public TileType Type { get => type; 
        set { 
            type = value;
            if (type == TileType.Empty) { 
                _boxCollider.enabled = false;
                gameObject.layer = 2; // Ignore raycast
            } else {
                if (!_boxCollider.enabled) { 
                    _boxCollider.enabled = true; // Will probably not happen because we're not placing blocks but EH
                    gameObject.layer = 0; // default
                } 
            }
        } 
    }

    public void InitializeTile(TileType type, Vector2Int position) {
        _boxCollider = GetComponent<BoxCollider2D>(); 
        this.Type = type;
        this.gridPosition = position;
        // Set Max Health based on Tile Type (customize these values!)
        switch (type) {
            case TileType.Dirt:
                maxHealth = 50f;
                break;
            case TileType.Stone:
                maxHealth = 100f;
                break;
            case TileType.Ore_Copper:
                maxHealth = 120f;
                break;
            case TileType.Ore_Silver:
                maxHealth = 150f;
                break;
            case TileType.Empty: // Empty tiles should be indestructible for mining gun
                maxHealth = Mathf.Infinity; // Infinite health
                break;
            default:
                maxHealth = 80f; // Default health value
                break;
        }
        currentHealth = maxHealth; // Initialize current health to max
        UpdateTileVisual();
    }
    public void TakeDamage(float damage) {
        if (type != TileType.Empty) // Only take damage if not destroyed and not empty
        {
            currentHealth -= damage;
            Debug.Log($"TileHit! {currentHealth}");
            if (currentHealth <= 0f) {
                DestroyTile();
            } else {
                // Optionally, you could add visual feedback for damage here, like a quick color flash
                // For example, you could call a coroutine to briefly change the sprite color.
            }
        }
    }
    public void DestroyTile() {
        if (Type != TileType.Empty) // Prevent destroying empty tiles
        {
            // Destruction logic
            Instantiate(resourcePrefab, transform.position, Quaternion.identity).SetResource(Type);
            Type = TileType.Empty;
            UpdateTileVisual();
        }
    }

    private void UpdateTileVisual() {
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null) {
            if (Type == TileType.Empty) // Handle Empty tiles visually
            {
                sr.color = new Color(sr.color.r, sr.color.g, sr.color.b, 0f); // Transparent for destroyed or empty
                // Or sr.enabled = false;
            } else {
                switch (Type) {
                    case TileType.Dirt:
                        sr.color = new Color(0.5372f, 0.3176f, 0.161f); // Brown
                        break;
                    case TileType.Stone:
                        sr.color = Color.gray;
                        break;
                    case TileType.Ore_Copper:
                        sr.color = new Color(1f, 0.5f, 0f);
                        break;
                    case TileType.Ore_Silver:
                        sr.color = new Color(0.753f, 0.753f, 0.753f);
                        break;
                    default:
                        sr.color = Color.white;
                        break;
                }
            }
        }
    }
}
using UnityEngine;
using UnityEngine.UIElements;

public class Tile : MonoBehaviour {
    public enum TileType {
        Empty, // New Empty Tile Type
        Dirt,
        Stone,
        Ore_Copper,
        Ore_Silver,
        Boundary
    }

    private TileType type;
    public Vector2Int gridPosition;
    private BoxCollider2D _boxCollider;
    public float maxHealth; // Maximum health of the tile
    private float currentHealth; // Current health of the tile
    public Resource resourcePrefab;
    private Renderer renderer;
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
        renderer = gameObject.GetComponent<Renderer>();
        renderer.material= new Material(renderer.material);
        renderer.material.SetFloat("_Rand", Random.Range(0,900));

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
            case TileType.Boundary: // Boundary tiles are indestructible
                maxHealth = Mathf.Infinity;
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
            //Debug.Log($"TileHit! {currentHealth}");
            if (currentHealth <= 10f) {
                DestroyTile();
            } else {
                var healthAmount = currentHealth / maxHealth;
                renderer.material.SetFloat("_DissolveAmount", 1-healthAmount);
                // Optionally, you could add visual feedback for damage here, like a quick color flash
                // For example, you could call a coroutine to briefly change the sprite color.
            }
        }
    }
    public void DestroyTile() {
        if (Type != TileType.Empty) // Prevent destroying empty tiles
        {
            // Block is 0.04
            var xOffset = Random.Range(-0.015f, 0.015f);
            var yOffset = Random.Range(-0.015f, 0.015f);
            var pos = new Vector3(transform.position.x + xOffset, transform.position.y + yOffset, transform.position.z);
            Instantiate(resourcePrefab, pos, Quaternion.identity).SetResource(Type);
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
                        sr.color = Color.white;
                        break;
                    case TileType.Ore_Copper:
                        sr.color = new Color(1f, 0.5f, 0f);
                        break;
                    case TileType.Ore_Silver:
                        sr.color = new Color(0.753f, 0.753f, 0.753f);
                        break;
                    case TileType.Boundary: 
                        sr.color = Color.black;
                        break;
                    default:
                        sr.color = Color.white;
                        break;
                }
            }
        }
    }
}
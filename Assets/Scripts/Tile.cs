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
    public bool isDestroyed = false;
    public Vector2Int gridPosition;
    private BoxCollider2D _boxCollider;
    public TileType Type { get => type; 
        set { 
            type = value;
            if (type == TileType.Empty) { 
                _boxCollider.enabled = false;
            } else {
                if (!_boxCollider.enabled) _boxCollider.enabled = true; // Will probably not happen because we're not placing blocks but EH
            }
        } 
    }

    public void InitializeTile(TileType type, Vector2Int position) {
        _boxCollider = GetComponent<BoxCollider2D>(); 
        this.Type = type;
        this.gridPosition = position;
        this.isDestroyed = false;
        UpdateTileVisual();
    }

    public void DestroyTile() {
        if (!isDestroyed && Type != TileType.Empty) // Prevent destroying empty tiles
        {
            isDestroyed = true;
            UpdateTileVisual();
            // Destruction logic
        }
    }

    private void UpdateTileVisual() {
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null) {
            if (isDestroyed || Type == TileType.Empty) // Handle Empty tiles visually
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
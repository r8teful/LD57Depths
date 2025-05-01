// Runtime data for a tile
using UnityEngine;
using UnityEngine.Tilemaps;

public struct TileData {
    public TileSO TileSO; // The "Main" tile
    public RuleTile TileBase; // The underlying tile, good for stones   
    public TileBase TileOverlayOre;  // Overlay for ores
    public TileBase TileOverlayBreak; // Overlay for breaking effects  
    public int tileDurability;

    public TileData(TileSO tileSO, RuleTile tileBase, TileBase tileOverlayOre, TileBase tileOverlayBreak) {
        TileSO = tileSO;
        TileBase = tileBase;
        TileOverlayOre = tileOverlayOre;
        TileOverlayBreak = tileOverlayBreak;
        tileDurability = TileSO.maxDurability;
    }
    public TileData(TileSO tileSO) {
        TileSO = tileSO;
        TileBase = null;
        TileOverlayOre = null;
        TileOverlayBreak = null;
        tileDurability = TileSO.maxDurability;
    }
    public TileData(TileSO tileSO, RuleTile underlying) {
        TileSO = tileSO;
        TileBase = underlying;
        TileOverlayOre = null;
        TileOverlayBreak = null;
        tileDurability = TileSO.maxDurability;
    }
}
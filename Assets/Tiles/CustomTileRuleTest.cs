using UnityEngine;
using UnityEngine.Tilemaps;
[CreateAssetMenu(fileName = "CustomTileRuleTest", menuName = "ScriptableObjects/CustomTileRuleTest")]
public class CustomTileRuleTest : RuleTile {
    public override bool RuleMatch(int neighbor, TileBase tile) {
        // Always return true if there is any tile at the neighbor
        if (neighbor == TilingRule.Neighbor.NotThis) {
            // If there is a tile, we treat it as a valid neighbor
            return tile != null;
        }

        // Default behavior for "This"
        return base.RuleMatch(neighbor, tile);
    }
}

using System.Collections.Generic;

public class UIInventoryManager : StaticInstance<UIInventoryManager> {
    private List<UIResourceElement> _instantitated = new List<UIResourceElement>();

    internal void UpdateInventory(Dictionary<TileScript.TileType, int> playerResources) {
        foreach (var item in playerResources) {
            
           // _instantitated.Add(i.gameObject);
        }
    }
}
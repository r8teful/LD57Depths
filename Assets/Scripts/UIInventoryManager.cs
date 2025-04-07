using System;
using System.Collections.Generic;
using UnityEngine;

public class UIInventoryManager : StaticInstance<UIInventoryManager> {
    private List<UIResourceElement> _instantitated = new List<UIResourceElement>();

    internal void UpdateInventory(Dictionary<Tile.TileType, int> playerResources) {
        foreach (var item in playerResources) {
            
           // _instantitated.Add(i.gameObject);
        }
    }
}
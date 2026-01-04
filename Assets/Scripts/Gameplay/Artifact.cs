using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class Artifact : MonoBehaviour {
    public List<TileBase> tiles;
    public void Init(WorldGenBiomeData biome, WorldManager worldManager) {
        var pos = biome.RandomInside(new(20, 20)); // 20 blocks padding 
        //worldManager.Place3x3Artifact(transform, worldManager.WorldToCell(pos),tiles);
    }

    internal void Init(StructurePlacementResult data) {
        transform.position = new(data.centerAnchor.x, data.centerAnchor.y,0);
    }
}

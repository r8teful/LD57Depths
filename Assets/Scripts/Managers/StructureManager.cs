using System.Collections;
using System.Collections.Generic;
using UnityEngine;
// Holds runtime data for structures
public class StructureManager {
    public Dictionary<byte,StructurePlacementResult> ArtifactPlacements = new Dictionary<byte,StructurePlacementResult>();


    public void AddStructureData(byte b, StructurePlacementResult placement) {
        ArtifactPlacements.Add(b, placement);
    }
    public void SetStructureFullyStamped(byte b) {
        if(ArtifactPlacements.TryGetValue(b, out var s)){
            s.fullyStamped = true;
        }else {
            Debug.LogError($"structure data doesn't exist for {b}!");
        }
    }
    public StructurePlacementResult GenerateArtifact(WorldGenBiomeData biome) {
        var pos = biome.RandomInside(new(20, 20)); // 20 blocks padding
        Debug.Log($"generated new artifact for biome {biome.BiomeType} at {pos}");
        AddStructureData((byte)biome.BiomeType, new(pos));
        return ArtifactPlacements[(byte)biome.BiomeType];
    }
   
}
public class StructurePlacementResult {
    public bool generated;       // true if a structure exists
    public Vector2Int centerAnchor; // only valid if generated==true
    public bool fullyStamped;    // whether all chunk pieces are stamped

    public StructurePlacementResult(Vector2Int pos) {
        generated = true;
        centerAnchor = pos;
        fullyStamped = false;
    }
}
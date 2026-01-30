using System.Collections.Generic;
using UnityEngine;
// Holds runtime data for structures
public class StructureManager {
    public Dictionary<byte,StructurePlacementResult> StructurePlacements = new Dictionary<byte,StructurePlacementResult>();


    public void AddStructureData(byte b, StructurePlacementResult placement) {
        StructurePlacements.Add(b, placement);
    }
    public void SetStructureFullyStamped(byte b) {
        if(StructurePlacements.TryGetValue(b, out var s)){
            s.fullyStamped = true;
        }else {
            Debug.LogError($"structure data doesn't exist for {b}!");
        }
    }
    public StructurePlacementResult GenerateArtifact(WorldGenBiomeData biome) {
        var pos = biome.RandomInside(new(20, 20)); // 20 blocks padding
        Debug.Log($"generated new artifact for biome {biome.BiomeType} at {pos}");
        AddStructureData((byte)biome.BiomeType, new(pos, biome.BiomeType,ResourceSystem.ArtifactStructureID));
        return StructurePlacements[(byte)biome.BiomeType];
    }
   
}
public class StructurePlacementResult {
    public ushort ID; 
    public BiomeType biome;
    public bool generated;       // true if a structure exists
    public Vector2Int bottomLeftAnchor; // only valid if generated==true
    public bool fullyStamped;    // whether all chunk pieces are stamped

    public StructurePlacementResult(Vector2Int pos,BiomeType biome, ushort iD) {
        generated = true;
        bottomLeftAnchor = pos;
        fullyStamped = false;
        this.biome = biome;
        ID = iD;
    }
}
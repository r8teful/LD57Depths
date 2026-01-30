using r8teful;
using System;
using System.Collections.Generic;
using UnityEngine;
using static UnityEditor.PlayerSettings;
// Holds runtime data for structures
public class StructureManager : MonoBehaviour {
    public List<StructurePlacementResult> StructurePlacements = new List<StructurePlacementResult>();


    public void AddStructureData(StructurePlacementResult placement) {
        StructurePlacements.Add(placement);
    }
    public StructurePlacementResult GenerateArtifact(WorldGenBiomeData biome) {
        var pos = biome.RandomInside(new(20, 20)); // 20 blocks padding
        Debug.Log($"generated new artifact for biome {biome.BiomeType} at {pos}");
        StructurePlacementResult structure = new(pos, ResourceSystem.StructureArtifactID);
        StructurePlacements.Add(structure);
        Instantiate(App.ResourceSystem.GetPrefab<Artifact>("Artifact")).Init(structure,biome.BiomeType);
        return structure;
    }
    
    public void GenerateExplorationEntities(WorldGenSettings settings) {
        float width = 500f;
        // this will only create within a certain area, much like the artifacts, but its okay for now
        // If we really want proper infinate worlds we'd have to generate this on the go instead of at stat

        // 40 seems a bit too dense, 70 good for only chests, but would need spots for other exploration entities 
        var positions = RandomnessHelpers.PoissonDisc(width, Mathf.Abs(settings.MaxDepth), 65, settings.seed); // just guessing values
        foreach (var pos in positions) {
            // offset point to match world. (half of world width to get it into center, max deth to get it down to player 
            var offsetPos = pos -  new Vector2(width *0.5f, Mathf.Abs(settings.MaxDepth));
            
            Vector2Int intPos = new(Mathf.RoundToInt(offsetPos.x), Mathf.RoundToInt(offsetPos.y));
            // Also don't know if its performant to spawn all these things but also I don't think it is that performant heavy
            // If its a problem simply integrate this into the entity manager 
            StructurePlacementResult structure = new(intPos, ResourceSystem.StructureChestID);
            StructurePlacements.Add(structure);
            Instantiate(App.ResourceSystem.GetPrefab<Chest>("Chest")).Init(structure);
        }
    }
     
}
public class StructurePlacementResult {
    public ushort ID; 
    public bool generated;       // true if a structure exists
    public Vector2Int bottomLeftAnchor; // only valid if generated==true
    public bool fullyStamped;    // whether all chunk pieces are stamped

    public StructurePlacementResult(Vector2Int pos, ushort iD) {
        generated = true;
        bottomLeftAnchor = pos;
        fullyStamped = false;
        ID = iD;
    }
}
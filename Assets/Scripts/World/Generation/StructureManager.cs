using r8teful;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
// Holds runtime data for structures
public class StructureManager : MonoBehaviour {
    public List<StructurePlacementResult> StructurePlacements = new List<StructurePlacementResult>();
    private WorldGenSettings _cachedSettings;

    public void AddStructureData(StructurePlacementResult placement) {
        StructurePlacements.Add(placement);
    }
    public StructurePlacementResult GenerateArtifact(WorldGenBiomeData biome) {
        var pos = biome.RandomInside(new(20, 20)); // 20 blocks padding
        Debug.Log($"generated new artifact for biome {biome.BiomeType} at {pos}");
        StructurePlacementResult structure = new(pos, ResourceSystem.StructureArtifactID);
        StructurePlacements.Add(structure);
        Instantiate(App.ResourceSystem.GetPrefab<Artifact>("Artifact"),transform).Init(structure,biome.BiomeType);
        return structure;
    }
    
    public void GenerateExplorationEntities(WorldGenSettings settings) {
        float width = 500f;
        _cachedSettings = settings;
        // this will only create within a certain area, much like the artifacts, but its okay for now
        // If we really want proper infinate worlds we'd have to generate this on the go instead of at stat

        // 40 seems a bit too dense, 70 good for only chests, but would need spots for other exploration entities 
        var chestPositions = RandomnessHelpers.PoissonDisc(width, Mathf.Abs(settings.MaxDepth), 65, settings.seed); // just guessing values
        var shrinePositions = RandomnessHelpers.PoissonDisc(width, Mathf.Abs(settings.MaxDepth), 65, settings.seed * 235); 
        var eventCavePositions = RandomnessHelpers.PoissonDisc(width, Mathf.Abs(settings.MaxDepth), 65, settings.seed * 723);

        // Convert lists into int and offset, then we check and remove invalid points
        float halfWidth = width * 0.5f;
        float depthAbs = Mathf.Abs(settings.MaxDepth);

        List<Vector2Int> chestIntPos = chestPositions
            .Select(pos => new Vector2Int(
                Mathf.RoundToInt(pos.x - halfWidth),
                Mathf.RoundToInt(pos.y - depthAbs)
            )).ToList();

        List<Vector2Int> shrineIntPos = shrinePositions
            .Select(pos => new Vector2Int(
                Mathf.RoundToInt(pos.x - halfWidth),
                Mathf.RoundToInt(pos.y - depthAbs)
            )).ToList();

        List<Vector2Int> eventCaveIntPos = eventCavePositions
            .Select(pos => new Vector2Int(
                Mathf.RoundToInt(pos.x - halfWidth),
                Mathf.RoundToInt(pos.y - depthAbs)
            )).ToList();



        RemoveClosePoints(chestIntPos, shrineIntPos, eventCaveIntPos, 10, out var chestP, out var shrineP, out var eventP);
        foreach (var pos in chestP) {

            // Also don't know if its performant to spawn all these things but also I don't think it is that performant heavy
            // If its a problem simply integrate this into the entity manager 
            StructurePlacementResult structure = new(pos, ResourceSystem.StructureChestID);
            StructurePlacements.Add(structure);
            Instantiate(App.ResourceSystem.GetPrefab<Chest>("Chest"),transform).Init(structure);
        }
        foreach (var pos in shrineP) {
            StructurePlacementResult structure = new(pos, ResourceSystem.StructureShrineID);
            StructurePlacements.Add(structure);
            Instantiate(App.ResourceSystem.GetPrefab<Shrine>("Shrine"), transform).Init(structure);
        }
        foreach (var pos in eventP) {
            StructurePlacementResult structure = new(pos, ResourceSystem.StructureEventCaveID);
            StructurePlacements.Add(structure);
            Instantiate(App.ResourceSystem.GetPrefab<EventCave>("EventCave"), transform).Init(structure);
        }
    }


    public void RemoveClosePoints(
       List<Vector2Int> list1,
       List<Vector2Int> list2,
       List<Vector2Int> list3,
       float minDistanceBetween,
       out List<Vector2Int> out1,
       out List<Vector2Int> out2,
       out List<Vector2Int> out3) {
        out1 = new List<Vector2Int>();
        out2 = new List<Vector2Int>();
        out3 = new List<Vector2Int>();

        List<Vector2Int> allAcceptedPoints = new List<Vector2Int>();
        float thresholdSqr = minDistanceBetween * minDistanceBetween;

        foreach (Vector2Int p in list1) {
            if (IsPositionValid(p, allAcceptedPoints, thresholdSqr)) {
                allAcceptedPoints.Add(p);
                out1.Add(p);
            }
        }

        foreach (Vector2Int p in list2) {
            if (IsPositionValid(p, allAcceptedPoints, thresholdSqr)) {
                allAcceptedPoints.Add(p);
                out2.Add(p);
            }
        }

        foreach (Vector2Int p in list3) {
            if (IsPositionValid(p, allAcceptedPoints, thresholdSqr)) {
                allAcceptedPoints.Add(p);
                out3.Add(p);
            }
        }
    }

    private bool IsPositionValid(Vector2Int candidate, List<Vector2Int> acceptedPoints, float thresholdSqr) {
        int count = acceptedPoints.Count;
        for (int i = 0; i < count; i++) {
            // Squared distance is much faster than Vector2.Distance
            float distSqr = (candidate - acceptedPoints[i]).sqrMagnitude;

            if (distSqr < thresholdSqr) {
                return false; // Too close to an existing point
            }
            float fromSpawn = (candidate - new Vector2(0, -Mathf.Abs(_cachedSettings.MaxDepth))).sqrMagnitude;
            if (fromSpawn < thresholdSqr*2) {
                return false; // Too close to spawn
            }
        }
        return true;
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
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

// Runtime data of a world gen setting
[Serializable]
public class WorldGenSettings {
    // Base authoring values
    public int seed;
    public ushort id;

    public float trenchBaseWidth;
    public float trenchWidenFactor;
    public float trenchEdgeNoiseFrequency;
    public float trenchEdgeNoiseAmplitude;
    public float caveNoiseScale;
    public float caveAmp;
    public float caveCutoff;
    public float caveOctavesBase;
    public float caveOctavesRidge;
    public float cavewWarpamp;
    public float caveWorleyWeight;


    // Deep-copy of biomes (create a serializable struct/class)
    public List<WorldGenBiomeData> biomes = new List<WorldGenBiomeData>();

    // Runtime-only:
    [NonSerialized] public Material worldGenSquareSprite;
    [NonSerialized] public Vector3 runtimeCameraPosition; // for parallax

    public WorldGenSettings() { }

    public static WorldGenSettings FromSO(WorldGenSettingSO so) {
        Debug.Log("generating runinstance of worldSettings!"); 
        var s = new WorldGenSettings();
        s.seed = so.seed;
        s.id = so.ID;

        s.trenchBaseWidth = so.trenchBaseWidth;
        s.trenchWidenFactor = so.trenchWidenFactor;
        s.trenchEdgeNoiseFrequency = so.trenchEdgeNoiseFrequency;
        s.trenchEdgeNoiseAmplitude = so.trenchEdgeNoiseAmplitude;
        s.caveNoiseScale = so.caveNoiseScale;
        s.caveAmp = so.caveAmp;
        s.caveCutoff = so.caveCutoff;
        s.caveOctavesBase = so.caveOctavesBase;
        s.caveOctavesRidge = so.caveOctavesRidge;
        s.cavewWarpamp = so.cavewWarpamp;
        s.caveWorleyWeight = so.caveWorleyWeight;

        s.biomes = new List<WorldGenBiomeData>();
        foreach (var bSO in so.biomes)
            s.biomes.Add(WorldGenBiomeData.FromSO(bSO));
        // Each biome has set their size, so now we need to place them properly
        PlaceBiomes(s.biomes);
        return s;
    }

    private static void PlaceBiomes(List<WorldGenBiomeData> biomes) {
        // Start from the bottom, using the pool (or weighted chance based) of that layer, (if we are having that some biomes appear at the top)
        //var placedBiomes = new List<WorldGenBiomeData>();
        // Just use random placement for now
        System.Random rng = new System.Random();
        biomes = biomes.OrderBy(e => rng.Next()).ToList(); // Randomize list
        var currentLayer = 0;
        var amountPlaced = 0;
        foreach (var biome in biomes) {
            // Place biomes one by one, selecting either left or right side of trench

            /* Old place code 
            bool placeLeft = false;
            if (UnityEngine.Random.value > 0.5f) placeLeft = true;
            // After selecting a side, check if it is not already full on that side
            
            // Simplification right now: each side can only have two biomes, this would have to be in WorldGenBiomeData, or we calculate it dynamically based on parametres or something
            int amountPlacedOnSpecifiedSide = biomes.Count(b => b.IsPlacedLeft() == placeLeft);
            if (amountPlacedOnSpecifiedSide >= 2) {
                // Too many on specified side
                placeLeft = !placeLeft; // This assumes there is enough space on the other side now
            }
             */
            bool firstLayerPlacement = amountPlaced % 2 == 0;
            // X placement
            var edgePos = firstLayerPlacement ? -biome.HorSize : biome.HorSize; // Place it on the very edge
            var OFFSET_TO_TRENCH = UnityEngine.Random.Range(50,90); // a min 100 seems fine for now but it would ofcourse depend on world size 
            biome.XOffset = firstLayerPlacement ? edgePos - OFFSET_TO_TRENCH : edgePos + OFFSET_TO_TRENCH; // Shift it by offsetToTrench
            
            // y placement
            var yPos = WorldManager.Instance.GetWorldLayerYPos(currentLayer);
            biome.YStart = UnityEngine.Random.Range(yPos*0.95f, yPos* 1.05f);
            amountPlaced++;
            if (!firstLayerPlacement) currentLayer++; // increment only when not first placed, that would be every other because that would put two on each layer
            biome.placed = true;
        }

        // Ystart is the bottom of the biome, meaning that yStart+yHeight is the top of the biome, meaning that, incase there is a biome under us, we need atleast yStart+yHeight+ofssetToBiome of y height between it
        // But instead of that we could also just ensure the position + height never passes a certain range so that biomes would not overlap, or we just let themoverlap lol

    }
}

[Serializable]
public class WorldGenBiomeData {
    public BiomeType BiomeType;
    public float EdgeNoiseScale = 1.0f;
    public float EdgeNoiseAmp = 0.2f;
    public float BlockNoiseScale = 2.0f;
    public float BlockNoiseAmp = 0.8f;
    public float BlockCutoff = 0.5f;
    public int   BaseOctaves = 1;
    public int   RidgeOctaves = 1;
    public float WarpAmp = 0.5f;
    public float WorleyWeight = 0.5f;
    public int   CaveType = 0; // 0 Default, 1 Tunnels

    [Header("Size")]
    public float HorSize = 40.0f;
    public float YHeight = 16.0f;

    // important to make these exact because we read the color value
    [Header("Color")]
    public Color TileColor = Color.white;
    public Color AirColor = Color.white;

    // Set at runtime by cpu
    [Header("Runtime")]
    public float YStart = 0.0f;
    public float XOffset = 0.0f;

    public bool placed = false;
    [Header("Visual Shader")]
    public Color DarkenedColor;

    public static WorldGenBiomeData FromSO(WorldGenBiomeSO so) {
        var b = new WorldGenBiomeData();
        b.BiomeType = so.biomeType;
        b.EdgeNoiseScale = so.EdgeNoiseScale;
        b.EdgeNoiseAmp = so.EdgeNoiseAmp;
        b.BlockNoiseScale = so.BlockNoiseScale;
        b.BlockNoiseAmp = so.BlockNoiseAmp;
        b.BlockCutoff = so.BlockCutoff;
        b.BaseOctaves = so.BaseOctaves;
        b.RidgeOctaves = so.RidgeOctaves;
        b.WarpAmp = so.WarpAmp;
        b.WorleyWeight = so.WorleyWeight;
        b.CaveType = so.CaveType;
        b.TileColor = so.TileColor;
        b.AirColor = so.AirColor;
        // Biome placement rules
        // Generate size of biome first
        b.HorSize = UnityEngine.Random.Range(so.HorSize * 0.8f, so.HorSize * 1.2f); // Using 80 to 120% of biome size right now but could also just have a set size
        b.YHeight = UnityEngine.Random.Range(so.YHeight* 0.8f, so.YHeight* 1.2f); // Using 80 to 120% of biome size right now but could also just have a set size
    
        // Set for now, will be overwritten by the random placement
        b.placed = false;
        b.YStart = so.YStart; 
        b.XOffset = so.XOffset;
        return b;
    }
    public bool IsPlacedLeft() {
        return XOffset <= 0;
    }
    // Basic bounds
    public float Left() =>
        XOffset - HorSize;

    public float Right() =>
        XOffset + HorSize;

    public float Bottom() => YStart;

    public float Top() => YStart + YHeight;

    // Size helpers
    public float Width(float horizontalSize) => HorSize * 2f;

    public float Height(float yHeight) => yHeight;

    // Center point (x is already the center)
    public Vector2 Center(float xOffset, float yStart, float yHeight) =>
        new Vector2(xOffset, yStart + yHeight * 0.5f);

    // Corners
    public Vector2 BottomLeft() =>
        new Vector2(Left(), Bottom());

    public Vector2 BottomRight() =>
        new Vector2(Right(), Bottom());

    public Vector2 TopLeft() =>
        new Vector2(Left(), Top());

    public Vector2 TopRight() =>
        new Vector2(Right(), Top());

    public Vector2Int RandomInside(Vector2 padding) {

        // padded bounds
        float minX = Left() + padding.x;
        float maxX = Right() - padding.x;

        // If padding removes horizontal space, fall back to X center (xOffset)
        if (minX > maxX) {
            minX = maxX = XOffset;
        }

        float minY = Bottom() + padding.y;
        float maxY = Top() - padding.y;

        // If padding removes vertical space, fall back to Y center
        float centerY = YStart + YHeight * 0.5f;
        if (minY > maxY) {
            minY = maxY = centerY;
        }

        float x = Random.Range(minX, maxX);
        float y = Random.Range(minY, maxY);

        return new Vector2Int(Mathf.RoundToInt(x), Mathf.RoundToInt(y));
    }
}
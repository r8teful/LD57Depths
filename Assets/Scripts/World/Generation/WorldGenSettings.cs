using System;
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
    public List<WorldGenOre> worldOres;
   
    public float MaxDepth { 
        get { 
            // 90% of the max theoretical depth, shader also uses 90%
            var maxD = trenchBaseWidth / trenchWidenFactor;
            return -Mathf.Abs(maxD) * 0.70f; 
        } 
    }

    public float GetWorldLayerYPos(int number) {
        int totalLayers = 3; // We'll have to check how many this will be later 
        return MaxDepth * ((float)Mathf.Abs(number - totalLayers) / totalLayers);
    }


    public List<WorldGenBiomeData> biomes = new List<WorldGenBiomeData>();

    // Runtime-only:
    [NonSerialized] public Material worldGenSquareSprite;
    [NonSerialized] public Vector3 runtimeCameraPosition; // for parallax

    public WorldGenSettings() { }

    public static WorldGenSettings FromSO(WorldGenSettingSO so, bool randomizeBiomes = true) {
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
        s.worldOres = new List<WorldGenOre>();
        foreach (var bSO in so.biomes)
            s.biomes.Add(WorldGenBiomeData.FromSO(bSO,randomizeBiomes));
        // Each biome has set their size, so now we need to place them properly
        PlaceBiomes(s, randomizeBiomes);

        if (so.associatedMaterial != null) {
            s.worldGenSquareSprite = new Material(so.associatedMaterial); // Don't want to change original 
            s.worldGenSquareSprite.name = "WorldRunInstanceMat";
        }

        // Todo here we'd want to set the biomes depending on what biome placements we've done
        var oreSOs = App.ResourceSystem.GetAllOreData();
        foreach (var ore in oreSOs) {
            s.worldOres.Add(WorldGenOre.FromSO(ore,s));
        }
        PlaceOres(s);
        return s;
    }

    private static void PlaceOres(WorldGenSettings s) {
        bool isLeft = Random.value < 0.5; 
        List<WorldGenBiomeData> biomes = s.biomes;
        foreach (var ore in s.worldOres) {
            // Place first layer ores either left or right ( its random ). Also this should probably be a generic function
            if (ore.oreStage == 1) {
                int biomeIndex = isLeft ? 0 : 1;
                ore.oreStart = biomes[biomeIndex].Center();
                ore.allowedBiomes[0] = biomes[biomeIndex].BiomeType;
            }
            if (ore.oreStage == 2) {
                int biomeIndex = !isLeft ? 0 : 1; // ! INVERTED HERE ! 
                ore.oreStart = biomes[biomeIndex].Center();
                ore.allowedBiomes[0] = biomes[biomeIndex].BiomeType;
            }
        }
    }

    private static void PlaceBiomes(WorldGenSettings settings, bool randomizeBiomes = true) {
        // Start from the bottom, using the pool (or weighted chance based) of that layer, (if we are having that some biomes appear at the top)
        //var placedBiomes = new List<WorldGenBiomeData>();
        // Just use random placement for now
        if (randomizeBiomes) {
            System.Random rng = new System.Random();
            settings.biomes = settings.biomes.OrderBy(e => rng.Next()).ToList(); // Randomize list
        }
        var currentLayer = 0;
        var amountPlaced = 0;
        foreach (var biome in settings.biomes) {
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

            float OFFSET_TO_TRENCH = 0;
            if (randomizeBiomes) {
                OFFSET_TO_TRENCH = Random.Range(20,30); // a min 100 seems fine for now but it would ofcourse depend on world size 
            }
            biome.XOffset = firstLayerPlacement ? edgePos - OFFSET_TO_TRENCH : edgePos + OFFSET_TO_TRENCH; // Shift it by offsetToTrench
            
            // y placement
            var yPos = settings.GetWorldLayerYPos(currentLayer);
            if (randomizeBiomes) {
                biome.YStart = Random.Range(yPos*0.95f, yPos* 1.05f);
            } else {
                biome.YStart = Random.Range(yPos, yPos);
            }
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
    public int TextureIndex;

    public static WorldGenBiomeData FromSO(WorldGenBiomeSO so, bool shouldRandomize) {
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
        b.DarkenedColor= so.DarkenedColor;
        b.TextureIndex = so.TextureIndex;
        // Biome placement rules
        // Generate size of biome first
        if (shouldRandomize) {
           b.HorSize = UnityEngine.Random.Range(so.HorSize * 0.8f, so.HorSize * 1.2f); // Using 80 to 120% of biome size right now but could also just have a set size
           b.YHeight = UnityEngine.Random.Range(so.YHeight* 0.8f, so.YHeight* 1.2f); // Using 80 to 120% of biome size right now but could also just have a set size

        } else {
            b.HorSize = so.HorSize; 
            b.YHeight = so.YHeight; 
        }

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
    public float Width() => HorSize * 2f;


    // Center point (x is already the center)
    public Vector2 Center() =>
        new Vector2(XOffset, Bottom() + YHeight * 0.5f);

    // Corners
    public Vector2 BottomLeft() =>
        new Vector2(Left(), Bottom());

    public Vector2 BottomRight() =>
        new Vector2(Right(), Bottom());

    public Vector2 CenterRight() =>
        new Vector2(Right(), Center().y);
    public Vector2 CenterLeft() =>
        new Vector2(Left(), Center().y);
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

[Serializable]
public class WorldGenOre {
    public TileSO oreTile; // We pull ID and name from this
    public int oreStage;
    public BiomeType[] allowedBiomes;
    public float WorldDepthBandProcent = 0;
    public float maxChance = 0.2f;

    public float widthPercent = 0.2f;

    public float noiseScale = 0.1f;
    public float noiseThreshold = 0.6f;

    public Vector2 noiseOffset;
    public Vector2 oreStart;
    public Color DebugColor = Color.white;

    public static WorldGenOre FromSO(WorldGenOreSO ore, WorldGenSettings settings) {
        WorldGenOre o = new();
        o.oreTile = ore.oreTile;
        o.oreStage = ore.oreStage;
        o.WorldDepthBandProcent = ore.WorldDepthBandProcent;
        o.maxChance = ore.maxChance;
        o.widthPercent = ore.widthPercent;
        o.noiseScale = ore.noiseScale;
        o.noiseThreshold = ore.noiseThreshold;
        o.noiseOffset = ore.noiseOffset;
        o.DebugColor = ore.DebugColor; 
        List<WorldGenBiomeData> biomes = settings.biomes;
        o.allowedBiomes = new BiomeType[6];
        // Horribly hard coded right now, for stage 0 it makes sence to just have it in the trench
        // But for stage 0 and 1 (if we are doing that one is within a biome. We should have it alteast randomize
        // if it eather takes the first (left) or second (right) biome
        if (ore.oreStage == 0) {
            o.oreStart = new(0,settings.MaxDepth);
            o.allowedBiomes[0] = BiomeType.Trench;
        }
        
        return o;
    }

    public uint BiomeMask() {
        uint mask = 0;

        for (int i = 0; i < allowedBiomes.Length; i++) {
            mask |= 1u << (int)allowedBiomes[i];
        }

        return mask;
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

// Runtime data of a world gen setting
[Serializable]
public class WorldGenData {
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
    public Dictionary<BiomeType, float> BiomeTileHardness;

    public const int TotalLayers = 3;
    public float[] WorldLayerYPositions;

    public List<WorldGenBiomeData> biomes = new List<WorldGenBiomeData>();
    // Runtime-only:
    [NonSerialized] public Material worldGenSquareSprite;
    [NonSerialized] public Vector3 runtimeCameraPosition; // for parallax
    public int GetBiomeProgressionIndex(BiomeType b) {
        // biomes don't have biome data (yet) so just hard code it here lol
        if (b == BiomeType.Trench1) {
            return 3;
        } else if ( b == BiomeType.Trench2) {
            return 6;
        } else if (b == BiomeType.Trench3) {
            return 9;
        }
        var biome = biomes.FirstOrDefault(s => s.BiomeType == b);
        if (biome == null) return 0;
        return biome.ProgressionIndex;
    }
    public WorldGenBiomeData GetBiome(BiomeType b) {
        return biomes.FirstOrDefault(biome => biome.BiomeType == b);
    }
    public float MaxDepth { 
        get { 
            // 90% of the max theoretical depth, shader also uses 90%
            var maxD = trenchBaseWidth / trenchWidenFactor;
            return -Mathf.Abs(maxD) * 0.70f; 
        } 
    }

    public float[] GenerateLayerYPositions() {
        float[] positions = new float[TotalLayers];

        for (int i = 0; i < TotalLayers; i++) {
            positions[i] = MaxDepth * ((float)Mathf.Abs(i - TotalLayers) / TotalLayers);
        }

        return positions;
    }

    public float GetWorldLayerYPos(int index) {
        return WorldLayerYPositions[index];
    }

    public int GetLayerIndexFromY(float y) {
        float bandHeight = MaxDepth / TotalLayers;

        // convert Y into band index
        int index = TotalLayers - 1 - Mathf.FloorToInt(y / bandHeight);

        return Mathf.Clamp(index, 0, TotalLayers - 1);
    }

   
    public WorldGenData() { }

    public static WorldGenData FromSO(WorldGenSettingSO so, bool randomizeBiomes = true, int seed = 1) {
        Debug.Log("generating runinstance of worldSettings!"); 
        var s = new WorldGenData();
        s.seed = seed;
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
        s.WorldLayerYPositions = s.GenerateLayerYPositions();

        s.biomes = new List<WorldGenBiomeData>();
        s.worldOres = new List<WorldGenOre>();
        s.BiomeTileHardness = new Dictionary<BiomeType, float>();
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

    private static void PlaceOres(WorldGenData s) {
        List<WorldGenBiomeData> biomes = s.biomes;
        // Progression index indicates what ORE should be there
        foreach (var biome in biomes) {
            // Find all ores with the matching progression index
            var ores = s.worldOres.Where(ore => ore.oreStage == biome.ProgressionIndex);
            foreach (var ore in ores) {
                ore.oreStart = biome.IsPlacedLeft() ? biome.CenterSlightRight() : biome.CenterSlightLeft();
                ore.allowedBiomes[0] = biome.BiomeType;
            }
        }
    }

    private static void PlaceBiomes(WorldGenData settings, bool randomizeBiomes = true) {
        // Start from the bottom, using the pool (or weighted chance based) of that layer, (if we are having that some biomes appear at the top)
        //var placedBiomes = new List<WorldGenBiomeData>();
        // Just use random placement for now
        if (randomizeBiomes) {
            System.Random rng = new System.Random(settings.seed);
            settings.biomes = settings.biomes.OrderBy(e => rng.Next()).ToList(); // Randomize list
        }
        var currentLayer = 0;
        var amountPlaced = 0;
        bool indexIncrease = false; // We radnomize this bool each layer to determine what progression side shoulg go one
        float startingHardness = 3f;
        float hardnessIncrease = 3f; // how much the hardness increases each biome. Should be modifiable by the player 
        foreach (var biome in settings.biomes) {
            // Place biomes one by one, selecting either left or right side of trench
            bool firstLayerPlacement = amountPlaced % 2 == 0;
            // X placement
            var edgePos = firstLayerPlacement ? -biome.HorSize : biome.HorSize; // Place it on the very edge

            float OFFSET_TO_TRENCH = 0;
            if (randomizeBiomes) {
                OFFSET_TO_TRENCH = Random.Range(25,35); 
            }
            biome.XOffset = firstLayerPlacement ? edgePos - OFFSET_TO_TRENCH : edgePos + OFFSET_TO_TRENCH; // Shift it by offsetToTrench
            
            // Y placement
            var yPos = settings.GetWorldLayerYPos(currentLayer);
            if (randomizeBiomes) {
                biome.YStart = Random.Range(yPos*0.95f, yPos* 1.05f);
            } else {
                biome.YStart = Random.Range(yPos, yPos);
            }
            
            // Progression
            int layerProgression = (currentLayer * 3) + 1;
            if (firstLayerPlacement) {
                // generate new random value
                indexIncrease = Random.value < 0.5;
                // Times 3 because we have three progressoin each layer
                biome.ProgressionIndex = indexIncrease ? layerProgression + 1 : layerProgression; // set this biome directly 
                biome.isSecondProgression = indexIncrease;
            } else {
                // second biome in this layer
                biome.ProgressionIndex = !indexIncrease ? layerProgression + 1 : layerProgression; // opposite of first 
                biome.isSecondProgression = !indexIncrease;
            }
            float thisHardness = 1;
            layerProgression = currentLayer * 2; // we now change it to current layer x 2 because we don't count the middle as a progression for hardness
            if (!biome.isSecondProgression) {
                // first layer
                thisHardness = startingHardness + (layerProgression * hardnessIncrease);
            } else {
                // second layer
                thisHardness = startingHardness + ((layerProgression + 1)* hardnessIncrease);
            }
            settings.BiomeTileHardness.Add(biome.BiomeType, thisHardness);

            amountPlaced++;
            if (!firstLayerPlacement) currentLayer++; // increment only when not first placed, that would be every other because that would put two on each layer
            biome.placed = true;
        }

        // Ystart is the bottom of the biome, meaning that yStart+yHeight is the top of the biome, meaning that, incase there is a biome under us, we need atleast yStart+yHeight+ofssetToBiome of y height between it
        // But instead of that we could also just ensure the position + height never passes a certain range so that biomes would not overlap, or we just let themoverlap lol

    }

    internal float TrenchWidthAtY(int y) {
        return trenchBaseWidth + Mathf.Abs(y) * trenchWidenFactor;
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
    public int   ProgressionIndex = 0; // Testing out that biomes are visited sequentually as the game progresses
    public bool  isSecondProgression; // Testing out that biomes are visited sequentually as the game progresses

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

    // DEBUG
    //public Action OnDataChanged;
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
        b.TileColor = so.TileColor; // hardness of tile (so also color) is determined by the biomes placement
        b.placed = false;
        b.YStart = so.YStart; 
        b.XOffset = so.XOffset;

        // DEBUG
        //b.OnDataChanged = so.onDataChanged;
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
    public Vector2 CenterSlightRight() =>
       new Vector2(XOffset + Width()*0.25f, Bottom() + YHeight * 0.5f);
    public Vector2 CenterSlightLeft() =>
       new Vector2(XOffset - Width() * 0.25f, Bottom() + YHeight * 0.5f);

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

    public static WorldGenOre FromSO(WorldGenOreSO ore, WorldGenData settings) {
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
        // SO UGLY
        if (ore.oreStage == 0) {
            o.oreStart = new(0,settings.MaxDepth);
            o.allowedBiomes[0] = BiomeType.Trench;
        }
        if (ore.oreStage == 3) {
            o.oreStart = new(0, settings.GetWorldLayerYPos(1));
            o.allowedBiomes[0] = BiomeType.Trench1;
        }
        if (ore.oreStage == 6) {
            o.oreStart = new(0, settings.GetWorldLayerYPos(2));
            o.allowedBiomes[0] = BiomeType.Trench2;
        }
        if (ore.oreStage == 9) {
            o.oreStart = new(0, settings.GetWorldLayerYPos(3));
            o.allowedBiomes[0] = BiomeType.Trench3;
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
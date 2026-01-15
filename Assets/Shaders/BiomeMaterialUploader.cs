using Sirenix.OdinInspector;
using UnityEngine;

[ExecuteAlways] // This is so cool it lets us call PushBiomesToMaterial when changing the asset
public class BiomeMaterialUploader : StaticInstance<BiomeMaterialUploader> {
    public static int NUM_BIOMES = 6; // MUST match shader's NUM_BIOMES
    public float uvScale = 100.0f; // tune to match the transform in shader (if using the example uv transform)
    [OnValueChanged("PushBiomesToMaterial")]
    public float DebugupdateMaterial = 10f;
    [SerializeField] SpriteRenderer worldSpriteRenderer;
    public static WorldGenSettings WorldGenSetting { get => WorldGenSettingsManager.Instance.WorldGenSettings; }
    //void Awake() {
    //    PushBiomesToMaterial();
    //}
    private void OnEnable() {
    }
    // Call this whenever you change biome descriptors

    public void PushBiomesToMaterial() {
        Debug.Log("Pushing..,");
        var targetMaterial = WorldGenSetting.worldGenSquareSprite;
        if (targetMaterial == null) {
            Debug.LogWarning("No target material assigned.");
            return;
        }

        // init arrays of exact size
        var edgeNoiseScale = new float[NUM_BIOMES];
        var edgeNoiseAmp = new float[NUM_BIOMES];
        var blockNoiseScale = new float[NUM_BIOMES];
        var blockNoiseAmp = new float[NUM_BIOMES];
        var blockCutoff = new float[NUM_BIOMES];
        var baseOctaves = new float[NUM_BIOMES];
        var ridgeOctaves = new float[NUM_BIOMES];
        var warpAmp = new float[NUM_BIOMES];
        var wordleyWeight = new float[NUM_BIOMES];
        var caveType = new float[NUM_BIOMES];

        var yStart = new float[NUM_BIOMES];
        var yHeight = new float[NUM_BIOMES];
        var horSize = new float[NUM_BIOMES];
        var xOffset = new float[NUM_BIOMES];
        var tileColors = new Vector4[NUM_BIOMES];
        var airColors = new Vector4[NUM_BIOMES];

        for (int i = 0; i < NUM_BIOMES; ++i) {
            if (i < WorldGenSetting.biomes.Count) {
                var b = WorldGenSetting.biomes[i];
                edgeNoiseScale[i] = b.EdgeNoiseScale;
                edgeNoiseAmp[i] = b.EdgeNoiseAmp;
                blockNoiseScale[i] = b.BlockNoiseScale;
                blockNoiseAmp[i] = b.BlockNoiseAmp;
                blockCutoff[i] = b.BlockCutoff;
                baseOctaves[i] = b.BaseOctaves;
                ridgeOctaves[i] = b.RidgeOctaves;
                warpAmp[i] = b.WarpAmp;
                wordleyWeight[i] = b.WorleyWeight;
                caveType[i] = b.CaveType;
                yStart[i] = b.YStart;
                yHeight[i] = b.YHeight;
                horSize[i] = b.HorSize;
                xOffset[i] = b.XOffset;
                tileColors[i] = new Vector4(b.TileColor.r, b.TileColor.g, b.TileColor.b, b.TileColor.a);
                airColors[i] = new Vector4(b.AirColor.r, b.AirColor.g, b.AirColor.b, b.AirColor.a);
            } else {
                // sensible default
                edgeNoiseScale[i] = 1.0f;
                edgeNoiseAmp[i] = 0.2f;
                blockNoiseScale[i] = 2.0f;
                blockNoiseAmp[i] = 0.8f;
                blockCutoff[i] = 0.5f;
                yStart[i] = 0.0f;
                yHeight[i] = 16.0f;
                horSize[i] = 40.0f;
                xOffset[i] = (i - NUM_BIOMES / 2) * 30.0f;
                tileColors[i] = new Vector4(0.35f, 0.6f, 0.3f, 1.0f);
                airColors[i] = new Vector4(1, 1, 1, 1);
            }
        }

        targetMaterial.SetFloatArray("_edgeNoiseScale", edgeNoiseScale);
        targetMaterial.SetFloatArray("_edgeNoiseAmp", edgeNoiseAmp);
        targetMaterial.SetFloatArray("_blockNoiseScale", blockNoiseScale);
        targetMaterial.SetFloatArray("_blockNoiseAmp", blockNoiseAmp);
        targetMaterial.SetFloatArray("_blockCutoff", blockCutoff);
        targetMaterial.SetFloatArray("_baseOctaves", baseOctaves);
        targetMaterial.SetFloatArray("_ridgeOctaves", ridgeOctaves);
        targetMaterial.SetFloatArray("_warpAmp", warpAmp);
        targetMaterial.SetFloatArray("_worldeyWeight", wordleyWeight);
        targetMaterial.SetFloatArray("_caveType", caveType);
        
        targetMaterial.SetFloatArray("_YStart", yStart);
        targetMaterial.SetFloatArray("_YHeight", yHeight);
        targetMaterial.SetFloatArray("_horSize", horSize);
        targetMaterial.SetFloatArray("_XOffset", xOffset);
        targetMaterial.SetVectorArray("_tileColor", tileColors);
        targetMaterial.SetVectorArray("_airColor", airColors);
        // global floats
        targetMaterial.SetFloat("_GlobalSeed", WorldGenSetting.seed);

        // Finally we set the material to the target, we have to do this because when we create the runtime instance of the worldGenSettings we copy the original 
        worldSpriteRenderer.material = targetMaterial;
    }

   
}

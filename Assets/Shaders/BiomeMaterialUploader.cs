using UnityEngine;
using System.Collections.Generic;
using Sirenix.OdinInspector;
[System.Serializable]
public class BiomeDescriptor {
    public float edgeNoiseScale = 1.0f;
    public float edgeNoiseAmp = 0.2f;
    public float blockNoiseScale = 2.0f;
    public float blockNoiseAmp = 0.8f;
    public float blockCutoff = 0.5f;
    public float YStart = 0.0f;
    public float YHeight = 16.0f;
    public float horSize = 40.0f;
    public float XOffset = 0.0f;
    public Color tileColor = Color.white;
    public Color airColor = Color.white;
}

[ExecuteAlways]
public class BiomeMaterialUploader : MonoBehaviour {
    public Material targetMaterial;
    [OnCollectionChanged("PushBiomesToMaterial", "PushBiomesToMaterial")]
    public List<BiomeDescriptor> biomes = new List<BiomeDescriptor>();
    public int NUM_BIOMES = 6; // MUST match shader's NUM_BIOMES
    public float globalSeed = 1234.0f;
    public float uvScale = 100.0f; // tune to match the transform in shader (if using the example uv transform)
    [OnValueChanged("PushBiomesToMaterial")]
    public float DebugupdateMaterial = 10f;
    void Start() {
        PushBiomesToMaterial();
    }

    // Call this whenever you change biome descriptors
    public void PushBiomesToMaterial() {
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
        var yStart = new float[NUM_BIOMES];
        var yHeight = new float[NUM_BIOMES];
        var horSize = new float[NUM_BIOMES];
        var xOffset = new float[NUM_BIOMES];
        var tileColors = new Vector4[NUM_BIOMES];
        var airColors = new Vector4[NUM_BIOMES];

        for (int i = 0; i < NUM_BIOMES; ++i) {
            if (i < biomes.Count) {
                var b = biomes[i];
                edgeNoiseScale[i] = b.edgeNoiseScale;
                edgeNoiseAmp[i] = b.edgeNoiseAmp;
                blockNoiseScale[i] = b.blockNoiseScale;
                blockNoiseAmp[i] = b.blockNoiseAmp;
                blockCutoff[i] = b.blockCutoff;
                yStart[i] = b.YStart;
                yHeight[i] = b.YHeight;
                horSize[i] = b.horSize;
                xOffset[i] = b.XOffset;
                tileColors[i] = new Vector4(b.tileColor.r, b.tileColor.g, b.tileColor.b, b.tileColor.a);
                airColors[i] = new Vector4(b.airColor.r, b.airColor.g, b.airColor.b, b.airColor.a);
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
        targetMaterial.SetFloatArray("_YStart", yStart);
        targetMaterial.SetFloatArray("_YHeight", yHeight);
        targetMaterial.SetFloatArray("_horSize", horSize);
        targetMaterial.SetFloatArray("_XOffset", xOffset);
        targetMaterial.SetVectorArray("_tileColor", tileColors);
        targetMaterial.SetVectorArray("_airColor", airColors);

        // global floats
        targetMaterial.SetFloat("_GlobalSeed", globalSeed);

    }
}

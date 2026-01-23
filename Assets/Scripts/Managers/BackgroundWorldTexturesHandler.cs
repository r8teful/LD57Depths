using Sirenix.OdinInspector;
using System.Collections.Generic;
using UnityEngine;
public class BackgroundWorldTexturesHandler : StaticInstance<BackgroundWorldTexturesHandler> {

    public static WorldGenSettings WorldGenSetting { get => WorldGenSettingsManager.Instance.WorldGenSettings; }
    public List<Material> layerMaterials; // 4 materials for 4 layers
    public List<float> layerParallax; // each layer's parallax
    public List<float> layerPixelSize; // pixel sizes for each layer
    [OnValueChanged("PushBiomesToMaterials")]
    public float DebugupdateMaterial = 10f;
    public int numBiomes = 6;
    
    private void OnEnable() {
        //worldGenSetting.biomes.ForEach(biome => { biome.onDataChanged += PushBiomesToMaterials; });
    }
       // GameSetupManager.LocalInstance.OnHostSettingsChanged += HostSettingsChanged;
    
    private void HostSettingsChanged(GameSettings obj) {
        //_worldGenSetting = App.ResourceSystem.GetWorldGenByID(obj.WorldGenID);
        //PushBiomesToMaterials();
    }

    void Update() {
        if (Screen.width == 0 || Screen.height == 0) return;
        Vector3 camPos = Camera.main.transform.position;
        foreach (var mat in layerMaterials) {
            mat.SetVector("_CameraPos", new Vector4(camPos.x, camPos.y, 0, 0));
        }
        // If layers use different parallax/pixel sizes, set those per-material (or hold 4 separate materials)
        for (int i = 0; i < layerMaterials.Count; ++i) {
            var m = layerMaterials[i];
            m.SetFloat("_ParallaxFactor", layerParallax[i]);
            m.SetFloat("_PixelSize", layerPixelSize[i]);
        }
       
    }
  
    public void PushBiomesToMaterials() {
        Debug.Log("pushing");
        if(WorldGenSetting == null) {
            Debug.LogWarning("WorldGenSettings are null");
            return;
        }
        var index = 0;
        foreach (var mat in layerMaterials) {
            PushBiomeToLayerMaterial(mat, index); // todo set current index where we start!
            index++;
        }
    }
    public void PushBiomeToLayerMaterial(Material mat,int matIndex) {
        if (mat == null) return;

        // push per-biome arrays (if not already pushed globally)
        float[] edgeNoiseScale = new float[numBiomes];
        float[] edgeNoiseAmp = new float[numBiomes];
        float[] blockNoiseScale = new float[numBiomes];
        float[] blockNoiseAmp = new float[numBiomes];
        float[] blockCutoff = new float[numBiomes];
        float[] baseOctaves = new float[numBiomes];
        float[] ridgeOctaves = new float[numBiomes];
        float[] warpAmp = new float[numBiomes];
        float[] wordleyWeight = new float[numBiomes];
        float[] caveType = new float[numBiomes];

        float[] yStart = new float[numBiomes];
        float[] yHeight = new float[numBiomes];
        float[] horSize = new float[numBiomes];
        float[] xOffset = new float[numBiomes];
        Color[] backgroundColors = new Color[numBiomes];

        for (int i = 0; i < numBiomes; ++i) {
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
                backgroundColors[i] = b.DarkenedColor.linear;
            } else {
                // sane defaults
                edgeNoiseScale[i] = 1f;
                edgeNoiseAmp[i] = 0.2f;
                blockNoiseScale[i] = 2f;
                blockNoiseAmp[i] = 0.8f;
                blockCutoff[i] = 0.5f;
                yStart[i] = 0f;
                yHeight[i] = 16f;
                horSize[i] = 40f;
                xOffset[i] = (i - numBiomes / 2) * 30f;
                backgroundColors[i] = new(0,0,0,1);
            }
        }

        // assign arrays
        mat.SetFloatArray("_edgeNoiseScale", edgeNoiseScale);
        mat.SetFloatArray("_edgeNoiseAmp", edgeNoiseAmp);
        mat.SetFloatArray("_blockNoiseScale", blockNoiseScale);
        mat.SetFloatArray("_blockNoiseAmp", blockNoiseAmp);
        mat.SetFloatArray("_blockCutoff", blockCutoff);
        mat.SetFloatArray("_YStart", yStart);
        mat.SetFloatArray("_YHeight", yHeight);
        mat.SetFloatArray("_horSize", horSize);
        mat.SetFloatArray("_XOffset", xOffset);
        mat.SetColorArray("_ColorArray", backgroundColors);
        mat.SetFloatArray("_baseOctaves", baseOctaves);
        mat.SetFloatArray("_ridgeOctaves", ridgeOctaves);
        mat.SetFloatArray("_warpAmp", warpAmp);
        mat.SetFloatArray("_worldeyWeight", wordleyWeight);
        mat.SetFloatArray("_caveType", caveType);

        // global seed
        mat.SetFloat("_GlobalSeed", WorldGenSetting.seed * 1+ matIndex * 2352.124f);

        // Cave and trench
        mat.SetFloat("_CaveNoiseScale", WorldGenSetting.caveNoiseScale);
        mat.SetFloat("_CaveAmp", WorldGenSetting.caveAmp);
        mat.SetFloat("_CaveCutoff", WorldGenSetting.caveCutoff);
        mat.SetFloat("_BaseOctaves", WorldGenSetting.caveOctavesBase);
        mat.SetFloat("_RidgeOctaves", WorldGenSetting.caveOctavesRidge);
        mat.SetFloat("_WarpAmp", WorldGenSetting.cavewWarpamp);
        mat.SetFloat("_WorleyWeight", WorldGenSetting.caveWorleyWeight);

        mat.SetFloat("_TrenchBaseWiden", WorldGenSetting.trenchWidenFactor);
        mat.SetFloat("_TrenchBaseWidth", WorldGenSetting.trenchBaseWidth);
        mat.SetFloat("_TrenchNoiseScale", WorldGenSetting.trenchEdgeNoiseFrequency);
        mat.SetFloat("_TrenchEdgeAmp", WorldGenSetting.trenchEdgeNoiseAmplitude);
    }
}

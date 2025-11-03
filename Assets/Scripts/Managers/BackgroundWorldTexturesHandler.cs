using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using UnityEngine;

public class BackgroundWorldTexturesHandler : MonoBehaviour {
    // Example fields
    public List<Texture2D> edgeTextures; // length == numBiomes
    public List<Texture2D> fillTextures; // length == numBiomes
    private WorldGenSettingSO _worldGenSetting;
    [SerializeField] private WorldGenSettingSO DEBUGWolrdSetting;
    public List<Material> layerMaterials; // 4 materials for 4 layers
    public Material blurMat;
    public List<float> layerParallax; // each layer's parallax
    public List<float> layerPixelSize; // pixel sizes for each layer
    [OnValueChanged("PushBiomesToMaterials")]
    public float DebugupdateMaterial = 10f;
    public int numBiomes = 6;

    public SpriteRenderer targetSprite; // sprite which will display final blurred RT
    // 
    RenderTexture rtA;
    RenderTexture rtB;
    int rtW, rtH;
    public int downsample = 2;
    private void OnEnable() {
        SetupRTs();
        //worldGenSetting.biomes.ForEach(biome => { biome.onDataChanged += PushBiomesToMaterials; });
    }
    private void Awake() {
        GameSetupManager.LocalInstance.OnHostSettingsChanged += HostSettingsChanged;
    }

    private void HostSettingsChanged(GameSettings obj) {
        _worldGenSetting = App.ResourceSystem.GetWorldGenByID(obj.WorldGenID);
        PushBiomesToMaterials();
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
        /*
        var blurIterations = 2;
        var blurSpread = 1.0f;
        int desiredW = Mathf.Max(1, Screen.width / Mathf.Max(1, downsample));
        int desiredH = Mathf.Max(1, Screen.height / Mathf.Max(1, downsample));
        if (rtA == null || rtW != desiredW || rtH != desiredH) {
            SetupRTs();
        }

        // Render procedural material into rtA
        Graphics.Blit(null, rtA, layerMaterials[0]); // TODO need render textures for all the backgrounds that will get blurred

        // Start with rtA -> rtB (copy) so we always blur the procedural output
        Graphics.Blit(rtA, rtB);

        // Separable blur iterations: horizontal then vertical
        for (int i = 0; i < blurIterations; i++) {
            float iterationSpread = blurSpread + i;
            blurMat.SetFloat("_BlurSize", iterationSpread);

            // horizontal
            Graphics.Blit(rtB, rtA, blurMat, 0);
            // vertical
            Graphics.Blit(rtA, rtB, blurMat, 1);
        }

        // Assign final blurred RT (rtB) to the sprite's material
        // Make sure the sprite uses a shader that samples _MainTex (Sprites/Default or Unlit/Transparent)
        Material matInstance = targetSprite.sharedMaterial;
        if (matInstance == null || matInstance.name.Contains(" (Instance)") == false) {
            // Create or clone to avoid modifying other sprites
            matInstance = new Material(Shader.Find("Sprites/Default"));
            targetSprite.sharedMaterial = matInstance;
        }

        matInstance.SetTexture("_MainTex", rtB);
        */
    }

    void OnDisable() {
        ReleaseRTs();
    }

  
    void SetupRTs() {
        ReleaseRTs();
        rtW = Mathf.Max(1, Screen.width / Mathf.Max(1, downsample));
        rtH = Mathf.Max(1, Screen.height / Mathf.Max(1, downsample));

        rtA = new RenderTexture(rtW, rtH, 0, RenderTextureFormat.DefaultHDR) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
        rtA.Create();

        rtB = new RenderTexture(rtW, rtH, 0, RenderTextureFormat.DefaultHDR) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
        rtB.Create();
    }

    void ReleaseRTs() {
        if (rtA) { rtA.Release(); DestroyImmediate(rtA); rtA = null; }
        if (rtB) { rtB.Release(); DestroyImmediate(rtB); rtB = null; }
    }
    public void PushBiomesToMaterials() {
        Debug.Log("pushing");
        if(_worldGenSetting == null) {
            _worldGenSetting = DEBUGWolrdSetting;
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
            if (i < _worldGenSetting.biomes.Count) {
                var b = _worldGenSetting.biomes[i];
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
        mat.SetFloat("_GlobalSeed", _worldGenSetting.seed * 1+ matIndex * 2352.124f);

        // Cave and trench
        mat.SetFloat("_CaveNoiseScale", _worldGenSetting.caveNoiseScale);
        mat.SetFloat("_CaveAmp", _worldGenSetting.caveAmp);
        mat.SetFloat("_CaveCutoff", _worldGenSetting.caveCutoff);
        mat.SetFloat("_BaseOctaves", _worldGenSetting.caveOctavesBase);
        mat.SetFloat("_RidgeOctaves", _worldGenSetting.caveOctavesRidge);
        mat.SetFloat("_WarpAmp", _worldGenSetting.cavewWarpamp);
        mat.SetFloat("_WorleyWeight", _worldGenSetting.caveWorleyWeight);

        mat.SetFloat("_TrenchBaseWiden", _worldGenSetting.trenchWidenFactor);
        mat.SetFloat("_TrenchBaseWidth", _worldGenSetting.trenchBaseWidth);
        mat.SetFloat("_TrenchNoiseScale", _worldGenSetting.trenchEdgeNoiseFrequency);
        mat.SetFloat("_TrenchEdgeAmp", _worldGenSetting.trenchEdgeNoiseAmplitude);
    }
}

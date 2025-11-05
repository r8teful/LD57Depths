using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using static Unity.VisualScripting.Member;

public class BackgroundWorldTexturesHandler : MonoBehaviour {
    // Example fields
    private WorldGenSettingSO _worldGenSetting;
    [SerializeField] private WorldGenSettingSO DEBUGWolrdSetting;
    public List<Material> layerMaterials; // 4 materials for 4 layers
    public Material[] blurMaterials;
    public Material[] outputMaterials;
    public List<float> layerParallax; // each layer's parallax
    public List<float> layerPixelSize; // pixel sizes for each layer
    [OnValueChanged("PushBiomesToMaterials")]
    public float DebugupdateMaterial = 10f;
    public int numBiomes = 6;
    private static readonly int BlurSizeID = Shader.PropertyToID("_BlurSize");
    public SpriteRenderer targetSprite; // sprite which will display final blurred RT
    // 
    [SerializeField] private RenderTextureCamera[] _backgroundCameras; // The cameras that render the background layers
    [SerializeField] private RenderTexture[] _outputRenderTextures; // The cameras that render the background layers
    
    private RenderTexture[] _intermediateRenderTextures = new RenderTexture[3]; // The cameras that render the background layers
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
        
        var blurIterations = 1;
        var blurSpread = 1.0f;
        if (_intermediateRenderTextures == null || _intermediateRenderTextures.Length == 0) {
            SetupRTs();
        }

        // Render procedural material into rtA
        //Graphics.Blit(null, rtA, layerMaterials[0]); // TODO need render textures for all the backgrounds that will get blurred

        // Start with rtA -> rtB (copy) so we always blur the procedural output
        //Graphics.Blit(rtA, rtB);
        // Separable blur iterations: horizontal then vertical
        for (int i = 0; i < blurIterations; i++) {
            float iterationSpread = blurSpread + i;
            blurMaterials[i].SetFloat(BlurSizeID, iterationSpread);

            // Now loop through the backgrounds
            for (int j = 0; j < _backgroundCameras.Length; j++) {
                // horizontal
                Graphics.Blit(_backgroundCameras[j].GetInput, _intermediateRenderTextures[j], blurMaterials[j], 0);
                // vertical
                Graphics.Blit(_intermediateRenderTextures[j], _outputRenderTextures[j], blurMaterials[j], 1);
            }
        }

        // Assign final blurred RT (rtB) to the sprite's material
        // Make sure the sprite uses a shader that samples _MainTex (Sprites/Default or Unlit/Transparent)
        //if (targetSprite == null)
        //    return;
        //Material matInstance = targetSprite.material;
        //if (matInstance == null || matInstance.name.Contains(" (Instance)") == false) {
        //    // Create or clone to avoid modifying other sprites
        //    matInstance = new Material(Shader.Find("Sprites/Default"));
        //    targetSprite.sharedMaterial = matInstance;
        //}
        for (int j = 0; j < _backgroundCameras.Length; j++) {
            // horizontal
            if (outputMaterials[j] == null) continue;
            outputMaterials[j].SetTexture("_InputTex", _outputRenderTextures[j]);
        }
    }

    void OnDisable() {
        ReleaseRTs();
    }

  
    void SetupRTs() {
        _intermediateRenderTextures = new RenderTexture[3]; 
        //rtA = new RenderTexture(rtW, rtH, 0, RenderTextureFormat.DefaultHDR) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
        for (int i = 0; i < _backgroundCameras.Length; i++) {
            _intermediateRenderTextures[i] = new RenderTexture(_outputRenderTextures[i]); // Copy the settings of the destination output rT
            _intermediateRenderTextures[i].Create();
        }
    }

    void ReleaseRTs() {
        for (int i = 0; i < _backgroundCameras.Length; i++) {
            _intermediateRenderTextures[i].Release();
            DestroyImmediate(_intermediateRenderTextures[i]);
            _intermediateRenderTextures[i] = null;
        }
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

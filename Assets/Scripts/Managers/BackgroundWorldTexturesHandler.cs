using System.Collections.Generic;
using UnityEngine;

public class BackgroundWorldTexturesHandler : MonoBehaviour {
    // Example fields
    public Material layerMaterial; // material instance for one layer
    public List<Texture2D> edgeTextures; // length == numBiomes
    public List<Texture2D> fillTextures; // length == numBiomes
    public List<BiomeDescriptor> biomeDescriptors; // same struct as in BiomMaterialUploader
    public List<Material> layerMaterials; // 4 materials for 4 layers
    public List<float> layerParallax; // each layer's parallax
    public List<float> layerPixelSize; // pixel sizes for each layer

    public int numBiomes = 6;
    public int currentBiomeIndex = 0;
    public float globalSeed = 1234f;
    private void Start() {
        foreach (var mat in layerMaterials) {
            PushBiomeToLayerMaterial(mat, currentBiomeIndex); // todo set current index where we start!
        }
    }
    void Update() {
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
    public void PushBiomeToLayerMaterial(Material mat, int biomeIdx) {
        if (mat == null) return;
        int bi = Mathf.Clamp(biomeIdx, 0, numBiomes - 1);

        // set textures for this layer's material (fast)
        mat.SetTexture("_EdgeTex", edgeTextures[bi]);
        mat.SetTexture("_FillTex", fillTextures[bi]);


        // push per-biome arrays (if not already pushed globally)
        float[] edgeNoiseScale = new float[numBiomes];
        float[] edgeNoiseAmp = new float[numBiomes];
        float[] blockNoiseScale = new float[numBiomes];
        float[] blockNoiseAmp = new float[numBiomes];
        float[] blockCutoff = new float[numBiomes];
        float[] yStart = new float[numBiomes];
        float[] yHeight = new float[numBiomes];
        float[] horSize = new float[numBiomes];
        float[] xOffset = new float[numBiomes];

        for (int i = 0; i < numBiomes; ++i) {
            if (i < biomeDescriptors.Count) {
                var b = biomeDescriptors[i];
                edgeNoiseScale[i] = b.edgeNoiseScale;
                edgeNoiseAmp[i] = b.edgeNoiseAmp;
                blockNoiseScale[i] = b.blockNoiseScale;
                blockNoiseAmp[i] = b.blockNoiseAmp;
                blockCutoff[i] = b.blockCutoff;
                yStart[i] = b.YStart;
                yHeight[i] = b.YHeight;
                horSize[i] = b.horSize;
                xOffset[i] = b.XOffset;
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

        // global seed
        mat.SetFloat("_GlobalSeed", globalSeed);
    }
}

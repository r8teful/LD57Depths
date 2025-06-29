using System.Collections.Generic;
using UnityEngine;
[CreateAssetMenu(fileName = "WorldGenSettingSO", menuName = "ScriptableObjects/WorldGenSettingSO", order =4 )]
public class WorldGenSettingSO : ScriptableObject {
    public int seed = 12345;
    public Material associatedMaterial;
  
    public List<OreType> oreTypes;

    // Decorations
    public List<WorldSpawnEntitySO> worldSpawnEntities;


    private float trenchBaseWidth;
    private float trenchWidenFactor; 
    private float trenchEdgeNoiseFrequency;
    private float trenchEdgeNoiseAmplitude;
    public float caveNoiseScale;
    public float caveAmp;
    public float caveCutoff;
    public float biomeEdgeNoiseScale;
    public float biomeEdgeNoiseAmp;
    public float biomeblockCutoff;
    public float biomeblockNoiseScale;
    public float biomeblockNoiseAmp;
    public float biomeYStart;
    public float biomeYHeight;
    public float biomeHorSize;
    private float worldSeed;
    public float GetTrenchWidth() => trenchBaseWidth;
    public float GetTrenchWiden() => trenchWidenFactor;
    public float GetTrenchEdgeFreq() => trenchEdgeNoiseFrequency;
    public float GetTrenchEdgeNoiseAmp() => trenchEdgeNoiseAmplitude;
    public void initWorldSettings(float width, float widen, float edgeFreq, float edgeAmp, float caveNoiseScale,float caveAmp, float caveCutoff, float edgeNoiseScale, float edgeNoiseAmp, float blockNoiseScale, float blockNoiseAmp, float blockCutoff, float YStart, float YHeight, float horSize,float worldSeed) {
        trenchBaseWidth = width;
        trenchWidenFactor = widen;
        trenchEdgeNoiseFrequency = edgeFreq;
        trenchEdgeNoiseAmplitude = edgeAmp;
        this.caveNoiseScale = caveNoiseScale;
        this.caveAmp = caveAmp;
        this.caveCutoff = caveCutoff;
        this.biomeEdgeNoiseScale = edgeNoiseScale;
        this.biomeEdgeNoiseAmp = edgeNoiseAmp;
        this.biomeblockNoiseScale = blockNoiseScale;
        this.biomeblockNoiseAmp = blockNoiseAmp;
        this.biomeblockCutoff = blockCutoff;
        this.biomeYStart = YStart;
        this.biomeYHeight = YHeight;
        this.biomeHorSize = horSize;
        this.worldSeed = worldSeed;
    }
}
using Sirenix.OdinInspector;
using System.Collections.Generic;
using UnityEngine;
[CreateAssetMenu(fileName = "WorldGenSettingSO", menuName = "ScriptableObjects/WorldGen/WorldGenSettingSO", order =1 )]
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
    [InlineEditor]
    public List<WorldGenBiomeSO> biomes = new List<WorldGenBiomeSO>();
    // For now:
    // 0 bioluminesence
    // 1 fungal 
    // 2 Forest
    // 3 Deadzone
    // 4 Gems
    // 5 Ice
    // 6 Caves
    // 7 Reef
    // 8 Shipgraves
    // 9 Marble
    // 10 Volcanic

    private float worldSeed;
    public float GetTrenchWidth() => trenchBaseWidth;
    public float GetTrenchWiden() => trenchWidenFactor;
    public float GetTrenchEdgeFreq() => trenchEdgeNoiseFrequency;
    public float GetTrenchEdgeNoiseAmp() => trenchEdgeNoiseAmplitude;
    public void InitWorldSettings(float width, float widen, float edgeFreq, float edgeAmp, float caveNoiseScale,float caveAmp, float caveCutoff,float worldSeed) {
        trenchBaseWidth = width;
        trenchWidenFactor = widen;
        trenchEdgeNoiseFrequency = edgeFreq;
        trenchEdgeNoiseAmplitude = edgeAmp;
        this.caveNoiseScale = caveNoiseScale;
        this.caveAmp = caveAmp;
        this.caveCutoff = caveCutoff;
        this.worldSeed = worldSeed;
    }
}
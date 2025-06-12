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
    public float GetTrenchWidth() => trenchBaseWidth;
    public float GetTrenchWiden() => trenchWidenFactor;
    public float GetTrenchEdgeFreq() => trenchEdgeNoiseFrequency;
    public float GetTrenchEdgeNoiseAmp() => trenchEdgeNoiseAmplitude;
    public void initTrenchSettings(float width, float widen, float edgeFreq, float edgeAmp) {
        trenchBaseWidth = width;
        trenchWidenFactor = widen;
        trenchEdgeNoiseFrequency = edgeFreq;
        trenchEdgeNoiseAmplitude = edgeAmp;

    }
}
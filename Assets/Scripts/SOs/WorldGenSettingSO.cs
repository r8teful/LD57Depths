using System.Collections.Generic;
using UnityEngine;
[CreateAssetMenu(fileName = "WorldGenSettingSO", menuName = "ScriptableObjects/WorldGenSettingSO", order =4 )]
public class WorldGenSettingSO : ScriptableObject {
    public int seed = 12345;
    public float trenchBaseWidth;
    public float trenchWidenFactor; // How much wider per unit Y increase
    public float trenchEdgeNoiseFrequency;
    public float trenchEdgeNoiseAmplitude;
    public Material associatedMaterial;
    // Surface
    public float surfaceCenterY = 0f; // The conceptual "sea level"
    public float surfaceMaxDepth = 15f; // How far down the solid "surface" can reach from surfaceCenterY
    public float surfaceNoiseFrequency = 0.03f;
    public float surfaceNoiseAmplitude = 5f; // Additional wiggle

    public List<OreType> oreTypes;

    // Decorations
    public List<WorldSpawnEntitySO> worldSpawnEntities;
}
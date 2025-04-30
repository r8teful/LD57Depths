using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
[CreateAssetMenu(fileName = "WorldGenSettingSO", menuName = "ScriptableObjects/WorldGenSettingSO", order =4 )]
public class WorldGenSettingSO : ScriptableObject {
    public int seed = 12345;
    public float trenchBaseWidth;
    public float trenchWidenFactor; // How much wider per unit Y increase
    public float trenchEdgeNoiseFrequency;
    public float trenchEdgeNoiseAmplitude;
    // Caves
    [Range(0f, 1f)] public float initialCaveNoiseThreshold; // Density of initial "potential cave" tiles
    public bool generateCaves;
    public float initialCaveNoiseFrequency;
    public int caveCASteps = 4;          // Number of CA iterations 4
    public int caveBirthThreshold = 5; // Become wall if >= N neighbours are walls (Rule: B5678) 5 
    public int caveSurvivalThreshold = 4;// Stay wall if >= N neighbours are walls (Rule: S45678) 4 
    public List<BiomeLayer> biomeLayers;
    public List<OreType> oreTypes;

    // Decorations
    public List<WorldSpawnEntitySO> worldSpawnEntities;
}
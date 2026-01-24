using Sirenix.OdinInspector;
using System.Collections.Generic;
using UnityEngine;
[CreateAssetMenu(fileName = "WorldGenSettingSO", menuName = "ScriptableObjects/WorldGen/WorldGenSettingSO", order =1 )]
public class WorldGenSettingSO : ScriptableObject, IIdentifiable {
    public int seed = 12345;
    public Material associatedMaterial;
    public ushort id;
    public ushort ID => id;

    public float trenchBaseWidth;
    public float trenchWidenFactor; 
    public float trenchEdgeNoiseFrequency;
    public float trenchEdgeNoiseAmplitude;
    public float caveNoiseScale;
    public float caveAmp;
    public float caveCutoff;
    public float caveOctavesBase;
    public float caveOctavesRidge;
    public float cavewWarpamp;
    public float caveWorleyWeight;

    // For now: (order matters for shader which uses the background textures, etc)
    // -1 Trench/default
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
    [InlineEditor]
    public List<WorldGenBiomeSO> biomes = new List<WorldGenBiomeSO>();

}
public enum BiomeType : byte {
    // Note that numbering here doesn't matter, just make sure not to change becuase any existing numbers because entities are tied to it
    Trench = 1,
    Bioluminescent = 7,
    Fungal = 8,
    Forest = 9,
    Surface = 2,
    AncientCaves = 3,
    Algea = 4,
    Reef = 5,
    Ocean = 6,
    Deadzone = 10,
    LostCity = 11,
    Snow = 12,
    Gems = 13,
    ShipGraveyard = 14,
    Volcanic = 15,
    None = 0
}
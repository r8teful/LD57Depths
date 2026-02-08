using System;
using UnityEngine;
[CreateAssetMenu(fileName = "WorldGenBiomeSO", menuName = "ScriptableObjects/WorldGen/WorldGenBiomeSO", order = 2)]

public class WorldGenBiomeSO : ScriptableObject {
    public BiomeType biomeType;
    public float EdgeNoiseScale = 1.0f;
    public float EdgeNoiseAmp = 0.2f;
    public float BlockNoiseScale = 2.0f;
    public float BlockNoiseAmp = 0.8f;
    public float BlockCutoff = 0.5f;
    public int BaseOctaves = 1;
    public int RidgeOctaves = 1;
    public float WarpAmp = 0.5f;
    public float WorleyWeight = 0.5f;
    public int CaveType = 0; // 0 Default, 1 Tunnels

    [Header("Size")]
    public float HorSize = 40.0f;
    public float YHeight = 16.0f;
    
    // important to make these exact because we read the color value
    [Header("Color")]
    public Color TileColor = Color.white; 
    public Color AirColor = Color.white;

    // Set at runtime by cpu
    [Header("Runtime")]
    public float YStart = 0.0f;
    public float XOffset = 0.0f;


    [Header("Visual Shader")]
    public Color DarkenedColor;
    public int TextureIndex;// what texture index the biome has in the textureArray

    // Event to invoke on changes
    public event Action onDataChanged;
    private void OnValidate() {
        onDataChanged?.Invoke();
    }
}

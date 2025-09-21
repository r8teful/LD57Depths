using UnityEngine;
[CreateAssetMenu(fileName = "WorldGenBiomeSO", menuName = "ScriptableObjects/WorldGen/WorldGenBiomeSO", order = 2)]

public class WorldGenBiomeSO : ScriptableObject {
    public float EdgeNoiseScale = 1.0f;
    public float EdgeNoiseAmp = 0.2f;
    public float BlockNoiseScale = 2.0f;
    public float BlockNoiseAmp = 0.8f;
    public float BlockCutoff = 0.5f;

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


    [Header("Background")]
    public Texture2D FillTexture;
    public Texture2D EdgeTexture;
}

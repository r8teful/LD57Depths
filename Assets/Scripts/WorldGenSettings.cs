using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Runtime data of a world gen setting
[Serializable]
public class WorldGenSettings {
    // Base authoring values
    public int seed;
    public ushort id;

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


    // Deep-copy of biomes (create a serializable struct/class)
    public List<WorldGenBiomeData> biomes = new List<WorldGenBiomeData>();

    // Runtime-only:
    [NonSerialized] public Material worldGenSquareSprite;
    [NonSerialized] public Vector3 runtimeCameraPosition; // for parallax

    public WorldGenSettings() { }

    public static WorldGenSettings FromSO(WorldGenSettingSO so) {
        var s = new WorldGenSettings();
        s.seed = so.seed;
        s.id = so.ID;
        s.trenchBaseWidth = so.trenchBaseWidth;
        s.trenchWidenFactor = so.trenchWidenFactor;
        s.trenchEdgeNoiseFrequency = so.trenchEdgeNoiseFrequency;
        s.trenchEdgeNoiseAmplitude = so.trenchEdgeNoiseAmplitude;
        s.caveNoiseScale = so.caveNoiseScale;
        s.caveAmp = so.caveAmp;
        s.caveCutoff = so.caveCutoff;
        s.caveOctavesBase = so.caveOctavesBase;
        s.caveOctavesRidge = so.caveOctavesRidge;
        s.cavewWarpamp = so.cavewWarpamp;
        s.caveWorleyWeight = so.caveWorleyWeight;

        s.biomes = new List<WorldGenBiomeData>();
        foreach (var bSO in so.biomes)
            s.biomes.Add(WorldGenBiomeData.FromSO(bSO));
        return s;
    }
}

[Serializable]
public class WorldGenBiomeData {
    public float EdgeNoiseScale = 1.0f;
    public float EdgeNoiseAmp = 0.2f;
    public float BlockNoiseScale = 2.0f;
    public float BlockNoiseAmp = 0.8f;
    public float BlockCutoff = 0.5f;
    public int   BaseOctaves = 1;
    public int   RidgeOctaves = 1;
    public float WarpAmp = 0.5f;
    public float WorleyWeight = 0.5f;
    public int   CaveType = 0; // 0 Default, 1 Tunnels

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
    public static WorldGenBiomeData FromSO(WorldGenBiomeSO so) {
        var b = new WorldGenBiomeData();
        b.EdgeNoiseScale = so.EdgeNoiseScale;
        b.EdgeNoiseAmp = so.EdgeNoiseAmp;
        b.BlockNoiseScale = so.BlockNoiseScale;
        b.BlockNoiseAmp = so.BlockNoiseAmp;
        b.BlockCutoff = so.BlockCutoff;
        b.BaseOctaves = so.BaseOctaves;
        b.RidgeOctaves = so.RidgeOctaves;
        b.WarpAmp = so.WarpAmp;
        b.WorleyWeight = so.WorleyWeight;
        b.CaveType = so.CaveType;
        b.HorSize = so.HorSize;
        b.YHeight = so.YHeight;
        b.TileColor = so.TileColor;
        b.AirColor = so.AirColor;

        // Todo here you'd put random pos depending on stuff I guess??
        b.YStart = so.YStart; 
        b.XOffset = so.XOffset;
        return b;
    }
}
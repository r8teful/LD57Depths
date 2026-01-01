using System.Collections;
using System.Collections.Generic;
using UnityEngine;
// One single spot where the world settings are handled, we create runtime instances here
public class WorldGenSettingsManager : StaticInstance<WorldGenSettingsManager> {
    public WorldGenSettings WorldGenSettings;
    protected override void Awake() {
        base.Awake();
        InitializeFromSO();
    }
    private void Start() {
        BackgroundWorldTexturesHandler.Instance.PushBiomesToMaterials();
        BiomeMaterialUploader.Instance.PushBiomesToMaterial();
    }

    void InitializeFromSO() {
        var so = ResourceSystem.GetMainMap();
        var runtime = WorldGenSettings.FromSO(so); // This does most the heavy lifting for us
        if (so.associatedMaterial != null)
            runtime.worldGenSquareSprite = new Material(so.associatedMaterial);
        WorldGenSettings = runtime;
    }
}
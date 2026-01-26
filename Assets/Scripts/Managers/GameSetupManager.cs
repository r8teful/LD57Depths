using Sirenix.OdinInspector;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;


[DefaultExecutionOrder(-100)]
public class GameSetupManager : PersistentSingleton<GameSetupManager> {
    [ShowInInspector]
    private WorldGenSettings _worldGenSettings;
    public WorldGenSettings WorldGenSettings => _worldGenSettings;
    public GameSettings CurrentGameSettings;
    private WorldGenSettingSO _settings;
    // TODO remove this
    private string _upgradeTreeName = "DefaultTree"; // Would depend on what the player chooses for tools etc

    // Todo get this in a proper way
    private string gameplaySceneName = "PlayScene";
    public string GetUpgradeTreeName() => _upgradeTreeName;
    
    private void OnEnable() {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }
    private void OnDisable() {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode) {
        // Check if we just loaded the Gameplay Scene
        if (scene.name == gameplaySceneName) {
            StartCoroutine(BootSequence());
        }
        // Actually this is fine if we set App.Backdrop.Require before we get here
    }
    private IEnumerator BootSequence() {
        Debug.Log("boot seq start");

        SetupSettings();

        yield return null;

        // Apparently this is fine? They're singletons anyway and we're on a loading screen so I feel like no one will notice
        WorldManager w = FindFirstObjectByType<WorldManager>();
        if (w == null) 
            LogError(w);
        w.Init(this);
        yield return null;
        
        var bm = FindFirstObjectByType<BiomeMaterialUploader>();
        if (bm == null)
            LogError(bm);
        bm.PushBiomesToMaterial(WorldGenSettings);
        yield return null;
        
        var bw = FindFirstObjectByType<BackgroundWorldTexturesHandler>();
        if (bw == null)
            LogError(bw);
        bw.PushBiomesToMaterials(WorldGenSettings);
    
        yield return null;// App.Backdrop.Release();
    }

    private void LogError(object script) {
        Debug.LogError($"Coudn't find script {script}!!");
    }

    private void SetupSettings() {
        _settings = ResourceSystem.GetMainMap();
        _worldGenSettings = WorldGenSettings.FromSO(_settings); // This does most the heavy lifting for us
    }


    public void OnDrawGizmos() {
        if (_worldGenSettings == null)
            return;
        Vector2 center = new Vector2(0, _worldGenSettings.MaxDepth);
        foreach (var ore in _worldGenSettings.worldOres) {
            var color = ore.DebugColor;
            var targetR = ore.WorldDepthBandProcent * Mathf.Abs(_worldGenSettings.MaxDepth);
            float bandWidth = targetR * ore.widthPercent; 
            DrawWireCircle(center, targetR, color);

            // How to include bandwidth so that we show these circles when the falloff is practicly 0??
            float tLimit = Mathf.Sqrt(-Mathf.Log(0.1f));
            float delta = bandWidth * tLimit;
            float outer = targetR + delta;
            float inner = Mathf.Max(0f, targetR - delta); // clamp so radius doesn't go negative

            Color faded = color; faded.a = 0.15f;
            DrawWireCircle(center, outer, faded);
            DrawWireCircle(center, inner, faded);
        }
    }

    private void DrawWireCircle(Vector2 center, float radius, Color color) {
        var segments = 64;
        float angleStep = 2f * Mathf.PI / segments;
        Vector3 prevPoint = center + Vector2.right * radius;

        for (int i = 1; i <= segments; i++) {
            float angle = angleStep * i;
            Vector3 nextPoint = center + new Vector2(
                Mathf.Cos(angle),
                Mathf.Sin(angle)
            ) * radius;
            Gizmos.color = color;
            Gizmos.DrawLine(prevPoint, nextPoint);
            prevPoint = nextPoint;
        }
    }
}

[Serializable]
public class GameSettings {
    public int WorldSeed;
    public ushort WorldGenID;
    public ushort[] EnabledModifierIds = Array.Empty<ushort>();
    public List<AbilitySO> AvailableAbilities;

    public HashSet<ushort> AvailableAbilityIDs 
        => AvailableAbilities.Select(a => a.ID).ToHashSet();

    public GameSettings(int worldSeed, ushort[] enabledModifierIds) {
        WorldSeed = worldSeed;
        EnabledModifierIds = enabledModifierIds;
    }
    public GameSettings(WorldGenSettingSO worldGenData) {
        WorldSeed = worldGenData.seed;
        EnabledModifierIds = Array.Empty<ushort>();
    }
}

[Serializable]
public struct CharacterData {
    public ushort CharacterId;
    public ushort CosmeticId;
    public ushort[] StartingEquipmentIds;
}

public enum Difficulty {
    Easy = 0,
    Normal = 1,
    Hard = 2,
    Custom = 3
}
  
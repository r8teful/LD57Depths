using Sirenix.OdinInspector;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using UnityEngine;
using UnityEngine.SceneManagement;


[DefaultExecutionOrder(-100)]
public class GameSetupManager : PersistentSingleton<GameSetupManager> {
    [ShowInInspector]
    private WorldGenSettings _worldGenSettings;
    public WorldGenSettings WorldGenSettings => _worldGenSettings;
    public GameSettings CurrentGameSettings;
    private WorldGenSettingSO _settings;
    [SerializeField] private PlayerManager _playerPrefab;
    private Coroutine _bootRoutine;
    // TODO remove this
    private string _upgradeTreeName = "DefaultTree"; // Would depend on what the player chooses for tools etc

    public static event Action OnSetupComplete;

    public string GetUpgradeTreeName() => _upgradeTreeName;
    
    private void OnEnable() {
        Debug.Log("GameSetupManager Enable!!");
        SceneManager.sceneLoaded += OnSceneLoaded;
    }
    private void OnDisable() {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode) {
        // Check if we just loaded the Gameplay Scene
        if (scene.name == ResourceSystem.ScenePlayName) {
            if (_bootRoutine != null) 
                return; // already running just return 
            _bootRoutine = StartCoroutine(BootSequence());
        }
        // Actually this is fine if we set App.Backdrop.Require before we get here
    }
    private IEnumerator BootSequence() {
        Debug.Log($"boot seq start: {GetInstanceID()}");
        var s = NewSeed();
        UnityEngine.Random.InitState(s);
        
        SetupSettings(true,s);

        yield return null;

        // Apparently this is fine? They're singletons anyway and we're on a loading screen so I feel like no one will notice
        WorldManager w = FindFirstObjectByType<WorldManager>();
        if (w == null) 
            LogError(w);
        w.Init(this);
        yield return null;
        
        // This just does biomes. Not caves or anything 
        var bm = FindFirstObjectByType<BiomeMaterialUploader>();
        if (bm == null)
            LogError(bm);
        bm.PushBiomesToMaterial(WorldGenSettings);
        yield return null;
        
        var bw = FindFirstObjectByType<BackgroundWorldTexturesHandler>();
        if (bw == null)
            LogError(bw);
        bw.PushBiomesToMaterials(WorldGenSettings);

        // Spawn player
        var p = Instantiate(_playerPrefab, w.PlayerSpawn,Quaternion.identity);
        yield return null;

        p.PlayerLayerController.PutPlayerInSub();// Is it that easy? lol
        
        yield return null;// App.Backdrop.Release();
        
        _bootRoutine = null;
        OnSetupComplete?.Invoke();
    }
    private int NewSeed() {
        byte[] bytes = new byte[4];
        using (var rng = RandomNumberGenerator.Create()) {
            rng.GetBytes(bytes);
        }
        int seed = BitConverter.ToInt32(bytes, 0);
        // make non-negative
        return seed & 0x7FFFFFFF;
    }

    private void LogError(object script) {
        Debug.LogError($"Coudn't find script {script}!!");
    }

    private void SetupSettings(bool randomizeBiomes,int seed = 0) {

        _settings = ResourceSystem.GetMainMap(); // We'll have to properly set this up later with nice menu icons etc..

        _worldGenSettings = WorldGenSettings.FromSO(_settings, randomizeBiomes, seed); // This does most the heavy lifting for us
    }


    public void OnDrawGizmos() {
        if (_worldGenSettings == null)
            return;
        foreach (var ore in _worldGenSettings.worldOres) {
            //Vector2 center = new Vector2(0, _worldGenSettings.MaxDepth);
            Vector2 center = ore.oreStart;
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

    internal void RebuildSettings() {
        SetupSettings(false);
    }
}

// User defines this if they want, any empty should not be used
[Serializable]
public class GameSettings {
    public int WorldSeed;
    public ushort WorldGenID;
    public ushort[] EnabledModifierIds = Array.Empty<ushort>();
    public List<AbilitySO> AvailableAbilities;
    public List<EventCaveSO> AvailableEventcaves; // Either take all from resource system or just setting defined idk

    public HashSet<ushort> AvailableAbilityIDs 
        => AvailableAbilities.Select(a => a.ID).ToHashSet();
    public HashSet<ushort> AvailableEventCaveIDs
        => AvailableEventcaves.Select(a => a.ID).ToHashSet();

    public GameSettings(int worldSeed, ushort[] enabledModifierIds) {
        WorldSeed = worldSeed;
        EnabledModifierIds = enabledModifierIds;
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
  